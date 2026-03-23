using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Models.Content;

namespace API_Althea_systems.Data.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AltheaDbContext context)
    {
        if (context.Users.Any()) return; // Already seeded

        // ── Users ────────────────────────────────────────
        var adminId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var user3Id = Guid.NewGuid();

        var users = new List<User>
        {
            new()
            {
                Id = adminId,
                Name = "Admin Althea",
                Email = "admin@altheasystems.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                EmailConfirmed = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        context.Users.AddRange(users);

        // ── Addresses ────────────────────────────────────
        var addresses = new List<Address>
        {
            new() { Id = Guid.NewGuid(), UserId = user1Id, Label = "Cabinet", FirstName = "Sophie", LastName = "Martin", Company = "Cabinet Médical Martin", Street = "15 rue de la République", City = "Lyon", PostalCode = "69002", Country = "France", Phone = "+33 4 72 00 00 01" },
            new() { Id = Guid.NewGuid(), UserId = user2Id, Label = "Clinique", FirstName = "Pierre", LastName = "Dubois", Company = "Clinique Saint-Louis", Street = "42 boulevard Haussmann", City = "Paris", PostalCode = "75009", Country = "France", Phone = "+33 1 42 00 00 02" },
            new() { Id = Guid.NewGuid(), UserId = user3Id, Label = "CHU", FirstName = "Marie", LastName = "Lefevre", Company = "CHU de Bordeaux", Street = "1 place Amélie Raba-Léon", City = "Bordeaux", PostalCode = "33076", Country = "France", Phone = "+33 5 56 79 56 79" }
        };

        context.Addresses.AddRange(addresses);

        // ── Categories ───────────────────────────────────
        var cat1Id = Guid.NewGuid();
        var cat2Id = Guid.NewGuid();
        var cat3Id = Guid.NewGuid();
        var cat4Id = Guid.NewGuid();
        var cat5Id = Guid.NewGuid();
        var cat6Id = Guid.NewGuid();
        var cat7Id = Guid.NewGuid();
        var cat8Id = Guid.NewGuid();

        var categories = new List<Category>
        {
            new() { Id = cat1Id, Slug = "imagerie-medicale", NameFr = "Imagerie Médicale", NameEn = "Medical Imaging", DescriptionFr = "Équipements d'imagerie diagnostique", DescriptionEn = "Diagnostic imaging equipment", Image = "imaging", DisplayOrder = 1 },
            new() { Id = cat2Id, Slug = "moniteurs-diagnostics", NameFr = "Moniteurs & Diagnostics", NameEn = "Monitors & Diagnostics", DescriptionFr = "Moniteurs de surveillance patient", DescriptionEn = "Patient monitoring systems", Image = "monitors", DisplayOrder = 2 },
            new() { Id = cat3Id, Slug = "sterilisation-hygiene", NameFr = "Stérilisation & Hygiène", NameEn = "Sterilization & Hygiene", DescriptionFr = "Matériel de stérilisation et d'hygiène", DescriptionEn = "Sterilization and hygiene equipment", Image = "sterilization", DisplayOrder = 3 },
            new() { Id = cat4Id, Slug = "instruments-chirurgicaux", NameFr = "Instruments Chirurgicaux", NameEn = "Surgical Instruments", DescriptionFr = "Instruments de chirurgie professionnels", DescriptionEn = "Professional surgical instruments", Image = "surgical", DisplayOrder = 4 },
            new() { Id = cat5Id, Slug = "mobilier-medical", NameFr = "Mobilier Médical", NameEn = "Medical Furniture", DescriptionFr = "Mobilier professionnel pour établissements de santé", DescriptionEn = "Professional furniture for healthcare facilities", Image = "furniture", DisplayOrder = 5 },
            new() { Id = cat6Id, Slug = "equipements-respiratoires", NameFr = "Équipements Respiratoires", NameEn = "Respiratory Equipment", DescriptionFr = "Matériel respiratoire et ventilation", DescriptionEn = "Respiratory and ventilation equipment", Image = "respiratory", DisplayOrder = 6 },
            new() { Id = cat7Id, Slug = "consommables-medicaux", NameFr = "Consommables Médicaux", NameEn = "Medical Consumables", DescriptionFr = "Consommables et fournitures médicales", DescriptionEn = "Medical consumables and supplies", Image = "consumables", DisplayOrder = 7 },
            new() { Id = cat8Id, Slug = "equipements-laboratoire", NameFr = "Équipements de Laboratoire", NameEn = "Laboratory Equipment", DescriptionFr = "Matériel de laboratoire d'analyse", DescriptionEn = "Laboratory analysis equipment", Image = "laboratory", DisplayOrder = 8 }
        };

        context.Categories.AddRange(categories);

        // ── Sample Products (8, one per category) ────────
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Slug = "proscan-radiographie", NameFr = "ProScan Radiographie", NameEn = "ProScan X-Ray", DescriptionFr = "Système de radiographie numérique haute résolution", DescriptionEn = "High-resolution digital X-ray system", LongDescriptionFr = "Le ProScan offre une imagerie diagnostique de pointe.", LongDescriptionEn = "The ProScan offers cutting-edge diagnostic imaging.", PriceHT = 45000, VatRate = VatRate.Standard, StockQty = 8, StockStatus = StockStatus.InStock, IsNew = true, PriorityRank = 1, Images = ["imaging-1"], Status = ProductStatus.Active },
            new() { Id = Guid.NewGuid(), Slug = "vitalguard-moniteur", NameFr = "VitalGuard Moniteur", NameEn = "VitalGuard Monitor", DescriptionFr = "Moniteur patient multiparamétrique", DescriptionEn = "Multi-parameter patient monitor", LongDescriptionFr = "Surveillance continue des signes vitaux.", LongDescriptionEn = "Continuous vital signs monitoring.", PriceHT = 8500, VatRate = VatRate.Standard, StockQty = 25, StockStatus = StockStatus.InStock, IsNew = false, PriorityRank = 2, Images = ["monitors-1"], Status = ProductStatus.Active },
            new() { Id = Guid.NewGuid(), Slug = "sterilipro-autoclave", NameFr = "SteriliPro Autoclave", NameEn = "SteriliPro Autoclave", DescriptionFr = "Autoclave de classe B pour stérilisation", DescriptionEn = "Class B autoclave for sterilization", LongDescriptionFr = "Stérilisation professionnelle conforme aux normes.", LongDescriptionEn = "Professional sterilization meeting standards.", PriceHT = 18500, VatRate = VatRate.Standard, StockQty = 12, StockStatus = StockStatus.InStock, IsNew = false, PriorityRank = 0, Images = ["sterilization-1"], Status = ProductStatus.Active },
            new() { Id = Guid.NewGuid(), Slug = "precisioncut-scalpels", NameFr = "PrecisionCut Scalpels", NameEn = "PrecisionCut Scalpels", DescriptionFr = "Set de scalpels chirurgicaux en acier inoxydable", DescriptionEn = "Stainless steel surgical scalpel set", LongDescriptionFr = "Précision maximale pour les interventions.", LongDescriptionEn = "Maximum precision for procedures.", PriceHT = 450, VatRate = VatRate.Standard, StockQty = 50, StockStatus = StockStatus.InStock, IsNew = false, PriorityRank = 0, Images = ["surgical-1"], Status = ProductStatus.Active },
            new() { Id = Guid.NewGuid(), Slug = "flexibed-lit", NameFr = "FlexiBed Lit Médicalisé", NameEn = "FlexiBed Hospital Bed", DescriptionFr = "Lit médicalisé électrique 3 fonctions", DescriptionEn = "3-function electric hospital bed", LongDescriptionFr = "Confort optimal pour les patients hospitalisés.", LongDescriptionEn = "Optimal comfort for hospitalized patients.", PriceHT = 5500, VatRate = VatRate.Standard, StockQty = 15, StockStatus = StockStatus.InStock, IsNew = true, PriorityRank = 3, Images = ["furniture-1"], Status = ProductStatus.Active },
            new() { Id = Guid.NewGuid(), Slug = "respiracare-ventilateur", NameFr = "RespiraCare Ventilateur", NameEn = "RespiraCare Ventilator", DescriptionFr = "Ventilateur de soins intensifs", DescriptionEn = "Intensive care ventilator", LongDescriptionFr = "Ventilation mécanique avancée.", LongDescriptionEn = "Advanced mechanical ventilation.", PriceHT = 28000, VatRate = VatRate.Standard, StockQty = 6, StockStatus = StockStatus.LowStock, IsNew = false, PriorityRank = 0, Images = ["respiratory-1"], Status = ProductStatus.Active },
            new() { Id = Guid.NewGuid(), Slug = "gants-nitrile", NameFr = "Gants Nitrile", NameEn = "Nitrile Gloves", DescriptionFr = "Gants d'examen en nitrile non poudrés", DescriptionEn = "Powder-free nitrile examination gloves", LongDescriptionFr = "Protection fiable pour les professionnels de santé.", LongDescriptionEn = "Reliable protection for healthcare professionals.", PriceHT = 12, VatRate = VatRate.Reduced, StockQty = 500, StockStatus = StockStatus.InStock, IsNew = false, PriorityRank = 0, Images = ["consumables-1"], Status = ProductStatus.Active },
            new() { Id = Guid.NewGuid(), Slug = "bioanalyzer", NameFr = "BioAnalyzer", NameEn = "BioAnalyzer", DescriptionFr = "Analyseur biochimique automatique", DescriptionEn = "Automatic biochemistry analyzer", LongDescriptionFr = "Analyse biochimique haute cadence.", LongDescriptionEn = "High-throughput biochemistry analysis.", PriceHT = 65000, VatRate = VatRate.Standard, StockQty = 3, StockStatus = StockStatus.LowStock, IsNew = true, PriorityRank = 4, Images = ["laboratory-1"], Status = ProductStatus.Active }
        };

        context.Products.AddRange(products);

        // ── Product-Category links ───────────────────────
        var catIds = new[] { cat1Id, cat2Id, cat3Id, cat4Id, cat5Id, cat6Id, cat7Id, cat8Id };
        var productCategories = products.Select((p, i) => new ProductCategory
        {
            ProductId = p.Id,
            CategoryId = catIds[i]
        }).ToList();

        context.ProductCategories.AddRange(productCategories);

        // ── Hero Slides ──────────────────────────────────
        var heroSlides = new List<HeroSlide>
        {
            new() { Id = Guid.NewGuid(), Image = "hero-1", TitleFr = "Équipement Médical de Pointe", TitleEn = "Cutting-Edge Medical Equipment", SubtitleFr = "Althea Systems", SubtitleEn = "Althea Systems", DescriptionFr = "Solutions professionnelles pour les établissements de santé", DescriptionEn = "Professional solutions for healthcare facilities", CtaFr = "Découvrir", CtaEn = "Discover", Link = "/categories", DisplayOrder = 1 },
            new() { Id = Guid.NewGuid(), Image = "hero-2", TitleFr = "Nouveautés 2026", TitleEn = "New in 2026", SubtitleFr = "Innovation", SubtitleEn = "Innovation", DescriptionFr = "Découvrez nos dernières innovations en imagerie médicale", DescriptionEn = "Discover our latest innovations in medical imaging", CtaFr = "Voir les nouveautés", CtaEn = "See what's new", Link = "/categories/imagerie-medicale", DisplayOrder = 2 },
            new() { Id = Guid.NewGuid(), Image = "hero-3", TitleFr = "Service & Support", TitleEn = "Service & Support", SubtitleFr = "Accompagnement", SubtitleEn = "Support", DescriptionFr = "Un accompagnement technique dédié pour votre établissement", DescriptionEn = "Dedicated technical support for your facility", CtaFr = "Nous contacter", CtaEn = "Contact us", Link = "/contact", DisplayOrder = 3 }
        };

        context.HeroSlides.AddRange(heroSlides);

        // ── Static Pages ─────────────────────────────────
        var staticPages = new List<Models.Content.StaticPage>
        {
            new() { Id = Guid.NewGuid(), Slug = "cgu", TitleFr = "Conditions Générales d'Utilisation", TitleEn = "Terms of Service", ContentFr = "Contenu des CGU...", ContentEn = "Terms of Service content..." },
            new() { Id = Guid.NewGuid(), Slug = "mentions-legales", TitleFr = "Mentions Légales", TitleEn = "Legal Notice", ContentFr = "Contenu des mentions légales...", ContentEn = "Legal notice content..." },
            new() { Id = Guid.NewGuid(), Slug = "a-propos", TitleFr = "À Propos", TitleEn = "About Us", ContentFr = "À propos d'Althea Systems...", ContentEn = "About Althea Systems..." }
        };

        context.StaticPages.AddRange(staticPages);

        await context.SaveChangesAsync();
    }
}
