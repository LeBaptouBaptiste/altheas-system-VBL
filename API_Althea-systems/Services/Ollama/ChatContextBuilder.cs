using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services.Ollama;

/// <summary>
/// Builds the per-turn knowledge block injected into the chatbot's prompt.
/// All queries are read-only and small (capped lists) — running this on
/// every message costs a few hundred ms and keeps the LLM grounded in
/// real data instead of inventing prices / order ids / etc.
/// </summary>
public class ChatContextBuilder : IChatContextBuilder
{
    private static readonly CultureInfo FrFr = new("fr-FR");
    // Max recent orders surfaced to the model. Beyond this we trust the
    // human admin to take over. 5 fits a reasonable medical-supply
    // customer's recent activity without bloating the prompt.
    private const int MaxRecentOrders = 5;
    // Top-K product search results when the question looks product-shaped.
    private const int MaxProductSuggestions = 5;

    private readonly IUserRepository _users;
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly ILogger<ChatContextBuilder> _logger;

    public ChatContextBuilder(
        IUserRepository users,
        IOrderRepository orders,
        IProductRepository products,
        ICategoryRepository categories,
        ILogger<ChatContextBuilder> logger)
    {
        _users = users;
        _orders = orders;
        _products = products;
        _categories = categories;
        _logger = logger;
    }

    public async Task<string> BuildAsync(
        Guid? userId,
        string userMessage,
        string? locale = null,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        // Language banner at the TOP — small models (qwen2.5:3b) tend to
        // mirror the language of the conversation history even when a final
        // directive says otherwise. Stating the target language up front
        // AND at the bottom anchors it on both ends.
        sb.AppendLine(BuildLanguageBanner(locale));
        sb.AppendLine("=== Contexte du client (utilise ces données réelles, ne les invente pas) ===");

        // ── 1. Customer profile ──────────────────────────
        if (userId.HasValue)
        {
            try
            {
                var user = await _users.GetByIdAsync(userId.Value);
                if (user is not null)
                {
                    sb.Append("Client : ").Append(user.Name).Append(" <").Append(user.Email).AppendLine(">");
                    if (user.CreditBalanceCents > 0)
                    {
                        sb.Append("Solde d'avoir disponible : ")
                          .AppendFormat(FrFr, "{0:N2} €", user.CreditBalanceCents / 100m)
                          .AppendLine(" (utilisable au prochain checkout)");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatContextBuilder: failed to load user {UserId}.", userId);
            }
        }
        else
        {
            sb.AppendLine("Client : non identifié (visiteur).");
        }

        // ── 2. Recent orders ─────────────────────────────
        if (userId.HasValue)
        {
            try
            {
                var orders = await _orders.GetAllAsync(1, MaxRecentOrders, userId.Value);
                var orderList = orders.ToList();
                if (orderList.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Commandes récentes du client ({orderList.Count}) :");
                    foreach (var o in orderList)
                    {
                        var totalTTC = o.Items.Sum(i => i.PriceHT * i.Quantity * (1m + VatMultiplier(i.VatRate)))
                                     + o.ShippingCost;
                        sb.Append("- #").Append(o.Id.ToString("N")[..8].ToUpperInvariant())
                          .Append(" · ").Append(o.Date.ToString("dd/MM/yyyy", FrFr))
                          .Append(" · statut : ").Append(StatusLabel(o.Status))
                          .Append(" · paiement : ").Append(PaymentStatusLabel(o.PaymentStatus))
                          .Append(" · total ").AppendFormat(FrFr, "{0:N2} €", totalTTC)
                          .AppendLine();
                        // List first 3 items inline so the model can answer
                        // "qu'est-ce que j'ai commandé la dernière fois ?"
                        foreach (var item in o.Items.Take(3))
                        {
                            sb.Append("    · ").Append(item.ProductNameFr)
                              .Append(" x").Append(item.Quantity).AppendLine();
                        }
                        if (o.Items.Count > 3)
                        {
                            sb.Append("    · (").Append(o.Items.Count - 3).AppendLine(" autres articles)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatContextBuilder: failed to load orders for user {UserId}.", userId);
            }
        }

        // ── 3. Catalog overview ──────────────────────────
        try
        {
            var categories = await _categories.GetAllAsync();
            var names = categories
                .Where(c => c.Active)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => c.NameFr)
                .ToList();
            if (names.Count > 0)
            {
                sb.AppendLine();
                sb.Append("Catégories du catalogue Althea Systems : ")
                  .AppendLine(string.Join(", ", names))
                  .AppendLine(".");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChatContextBuilder: failed to load categories.");
        }

        // ── 4. Product search on the user's message ──────
        // Tokenized: split the message into content words and search each
        // separately. The repo's SearchAsync does a substring LIKE on the
        // full string, so "Parle moi du BioAnalyzer" wouldn't match
        // a product named just "BioAnalyzer" — it'd only match if a product
        // had the entire phrase in its name/description, which is never.
        if (LooksLikeProductQuery(userMessage))
        {
            try
            {
                var matchList = await TokenizedProductSearchAsync(userMessage);
                if (matchList.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Produits du catalogue pertinents pour la question (utilise CES prix exacts) :");
                    foreach (var p in matchList)
                    {
                        sb.Append("- ").Append(p.NameFr)
                          .Append(" — ").AppendFormat(FrFr, "{0:N2} € HT", p.PriceHT)
                          .Append(" · ").Append(StockLabel(p.StockStatus));
                        if (!string.IsNullOrWhiteSpace(p.DescriptionFr))
                        {
                            sb.Append(" · ").Append(p.DescriptionFr);
                        }
                        sb.AppendLine();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChatContextBuilder: product search failed for query '{Query}'.", userMessage);
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== Fin du contexte ===");
        // Locale-aware final directive. The context block above is in French
        // (most compact, the model handles mixed-language fine) but this
        // last line tells the model the target reply language explicitly.
        // /no_think disables Qwen3 reasoning mode inline (belt-and-suspenders
        // with the top-level `think:false` flag for older Ollama builds).
        sb.AppendLine(BuildReplyDirective(locale));
        return sb.ToString();
    }

    /// <summary>
    /// Top-of-context language banner. Stating the target language BEFORE
    /// dumping data makes small models commit to it more reliably than a
    /// trailing directive alone, especially when the conversation history
    /// is in a different language.
    /// </summary>
    private static string BuildLanguageBanner(string? locale) => (locale?.ToLowerInvariant()) switch
    {
        "en" => "!! LANGUAGE OVERRIDE !! Reply ONLY in English for this turn, regardless of the language of previous messages. The customer just switched the UI to English.",
        "ms" => "!! ARAHAN BAHASA !! Jawab HANYA dalam Bahasa Melayu untuk giliran ini, tanpa mengira bahasa mesej sebelumnya. Pelanggan baru sahaja menukar UI kepada Bahasa Melayu.",
        "ar" => "!! تجاوز اللغة !! أجب فقط بالعربية في هذه الجولة، بغض النظر عن لغة الرسائل السابقة. لقد قام العميل للتو بتبديل واجهة المستخدم إلى العربية.",
        _ => "!! LANGUE IMPOSÉE !! Réponds UNIQUEMENT en français pour ce tour, quelle que soit la langue des messages précédents. Le client vient de basculer l'interface en français.",
    };

    /// <summary>
    /// Final instruction line, localised. The model sees the rest of the
    /// context in French; this last line overrides the output language.
    /// Falls back to French for unknown locales.
    /// </summary>
    private static string BuildReplyDirective(string? locale) => (locale?.ToLowerInvariant()) switch
    {
        "en" => "/no_think Reply in English, concise and factual (3-5 sentences max). Use EXACTLY the prices, order ids, statuses, and balance from the context above — never invent. The catalog above is stored in French: TRANSLATE generic product names into English in your reply (e.g. \"Gants Nitrile\" → \"Nitrile Gloves\", \"Blouse jetable\" → \"Disposable Gown\"), but keep brand names, model numbers, and proper nouns verbatim. If the info isn't there, say so plainly and offer to open a support ticket. NEVER expose your reasoning, give the final answer directly.",

        "ms" => "/no_think Jawab dalam Bahasa Melayu, ringkas dan tepat (maksimum 3-5 ayat). Gunakan TEPAT harga, nombor pesanan, status, dan baki dari konteks di atas — jangan reka. Katalog di atas disimpan dalam bahasa Perancis: TERJEMAHKAN nama produk generik ke dalam Bahasa Melayu (cth. \"Gants Nitrile\" → \"Sarung Tangan Nitril\"), tetapi kekalkan nama jenama, nombor model, dan kata nama khas seperti asal. Jika maklumat tiada, beritahu pelanggan dengan jelas dan tawarkan untuk buka tiket sokongan. JANGAN PERNAH dedahkan penaakulan anda, beri jawapan akhir secara terus.",

        "ar" => "/no_think أجب باللغة العربية، بإيجاز ودقة (3-5 جمل كحد أقصى). استخدم بالضبط الأسعار وأرقام الطلبات والحالات والرصيد من السياق أعلاه — لا تخترع. الكتالوج أعلاه مخزن بالفرنسية: تَرجِم أسماء المنتجات العامة إلى العربية (مثلاً \"Gants Nitrile\" → \"قفازات نتريل\")، لكن احتفظ بأسماء العلامات التجارية وأرقام الموديلات والأسماء الخاصة كما هي. إذا لم تكن المعلومات موجودة، فقل ذلك بوضوح واعرض فتح تذكرة دعم. لا تُظهر استدلالك أبداً، أعطِ الإجابة النهائية مباشرة.",

        _ => "/no_think Réponds en français, de manière concise et factuelle (3-5 phrases maximum). Si la question demande une donnée précise (prix, statut de commande, solde), utilise EXACTEMENT les valeurs du contexte ci-dessus. Si l'information n'y est pas, dis-le clairement et propose de créer un ticket support. N'expose JAMAIS ton raisonnement, donne directement la réponse finale.",
    };

    // ─────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────

    private static string StatusLabel(OrderStatus s) => s switch
    {
        OrderStatus.Pending => "en attente",
        OrderStatus.Confirmed => "confirmée",
        OrderStatus.Processing => "en préparation",
        OrderStatus.Shipped => "expédiée",
        OrderStatus.Delivered => "livrée",
        OrderStatus.Cancelled => "annulée",
        OrderStatus.Returned => "retournée",
        _ => s.ToString(),
    };

    private static string PaymentStatusLabel(PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => "en attente",
        PaymentStatus.Validated => "réglé",
        PaymentStatus.Failed => "échec",
        PaymentStatus.Refunded => "remboursé",
        _ => s.ToString(),
    };

    private static string StockLabel(StockStatus s) => s switch
    {
        StockStatus.InStock => "en stock",
        StockStatus.LowStock => "stock faible",
        StockStatus.OutOfStock => "épuisé",
        _ => s.ToString(),
    };

    private static decimal VatMultiplier(VatRate r) => r switch
    {
        VatRate.Standard => 0.20m,
        VatRate.Intermediate => 0.10m,
        VatRate.Reduced => 0.055m,
        _ => 0m,
    };

    /// <summary>
    /// Cheap "does this look like a product question" gate. We only skip the
    /// catalog search on obvious greetings / very short non-content messages
    /// so empty searches don't waste an EF roundtrip. Otherwise we search —
    /// false positives just inject 0 results, which costs ~50 ms.
    /// </summary>
    private static bool LooksLikeProductQuery(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var trimmed = message.Trim().ToLowerInvariant();
        if (trimmed.Length < 4) return false;
        // Filter pure greetings.
        string[] greetings = ["bonjour", "salut", "hello", "hi", "bonsoir", "coucou", "merci"];
        if (greetings.Contains(trimmed)) return false;
        return true;
    }

    // French + English stopwords that show up in chatbot phrasing. NOT a
    // linguistically complete list — just enough to keep the per-token
    // search loop focused on the words that actually describe a product.
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Articles / determiners
        "le", "la", "les", "un", "une", "des", "du", "de", "d", "l",
        "the", "a", "an",
        // Pronouns / question words
        "je", "tu", "il", "elle", "on", "nous", "vous", "ils", "elles",
        "me", "moi", "te", "toi", "se", "soi",
        "i", "you", "he", "she", "we", "they",
        // Common verbs that show up in chat phrasing
        "est", "es", "suis", "sont", "été", "etre", "être",
        "ai", "as", "avez", "avons", "ont", "avoir",
        "fait", "faire", "veux", "voudrais", "veut", "veulent",
        "peux", "peut", "pouvez", "pouvoir",
        "cherche", "cherches", "recherche", "trouve", "trouver",
        "is", "are", "have", "has", "want", "looking", "find",
        // Common chat verbs / requests
        "parle", "parler", "parles", "dit", "dis", "raconte", "explique",
        "donne", "donner", "donnez", "montre", "montrer",
        "tell", "show", "explain",
        // Prepositions / conjunctions
        "à", "au", "aux", "sur", "sous", "dans", "par", "pour", "avec", "sans",
        "et", "ou", "mais", "si", "que", "qui", "quoi", "quel", "quelle",
        "in", "on", "to", "for", "with", "without", "and", "or", "but", "if",
        "that", "what", "which", "how",
        // Misc
        "ce", "cette", "ces", "cet", "ça", "sa", "son", "ses",
        "tout", "tous", "toute", "toutes", "rien",
        "this", "these", "those", "all",
        // Generic catalog vocabulary that matches every product
        "produit", "produits", "article", "articles", "matériel", "materiel",
        "équipement", "equipement", "équipements", "equipements",
        "product", "products", "item", "items",
        // Politeness
        "bonjour", "salut", "hello", "merci", "svp", "stp", "please",
    };

    /// <summary>
    /// Splits the user message into content tokens and runs the repo's
    /// SearchAsync for each, accumulating de-duplicated hits up to
    /// <see cref="MaxProductSuggestions"/>. Fixes the "Parle moi du
    /// BioAnalyzer" case where the full-phrase substring search returns
    /// nothing — at least one token ("bioanalyzer") matches by itself.
    /// </summary>
    private async Task<List<Product>> TokenizedProductSearchAsync(string userMessage)
    {
        // Strip punctuation, keep letters / digits / hyphens. Letters
        // outside ASCII are allowed so accented French words ("éclairage")
        // survive intact.
        var tokens = Regex.Split(userMessage.ToLowerInvariant(), @"[^\p{L}\p{N}\-]+")
            .Where(t => t.Length >= 3 && !StopWords.Contains(t))
            .Distinct()
            .Take(8) // cap to bound EF roundtrips on very long messages
            .ToList();

        if (tokens.Count == 0) return new();

        var seen = new HashSet<Guid>();
        var hits = new List<Product>();
        foreach (var token in tokens)
        {
            var rows = await _products.SearchAsync(token, 1, MaxProductSuggestions);
            foreach (var p in rows)
            {
                if (seen.Add(p.Id))
                {
                    hits.Add(p);
                    if (hits.Count >= MaxProductSuggestions) return hits;
                }
            }
        }
        return hits;
    }
}
