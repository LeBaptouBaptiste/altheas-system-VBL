using API_Althea_systems.Common.Enums;
using API_Althea_systems.Models.Users;
using API_Althea_systems.Models.Products;
using API_Althea_systems.Models.Content;
using API_Althea_systems.Models.Order;
using API_Althea_systems.Models.Messaging;

namespace API_Althea_systems.Data.Seed;

/// <summary>
/// Demo seeder. Fires once on a fresh database (gated by
/// <c>context.Users.Any()</c>) and lays down enough data to make the admin
/// dashboard, customer space, and chatbot context all look like a site that's
/// been live for about a week:
///   • 1 admin + 10 medical-professional customers (varied statuses, signup
///     dates spread across the past 8 days)
///   • 8 categories, 32 products (~4 per category, mix of in-stock / low /
///     out-of-stock and new flags)
///   • ~15 addresses, ~20 orders across all OrderStatus values, varied
///     payment methods, shipping options, totals
///   • Contact messages, chat conversations (one escalated), support tickets
///
/// To re-apply on an existing DB: <c>docker compose down -v</c> then up again.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(AltheaDbContext context)
    {
        if (context.Users.Any()) return; // Already seeded

        // "Now" anchor — every relative date below is computed from this, so
        // the seeded "last 7 days" of activity always lines up with the
        // actual seed run time.
        var now = DateTime.UtcNow;

        // ── Users ────────────────────────────────────────
        var adminId = Guid.NewGuid();
        var admin = new User
        {
            Id = adminId,
            Name = "Admin Althea",
            Email = "admin@altheasystems.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            CreatedAt = now.AddDays(-30),
            UpdatedAt = now.AddDays(-30),
            LastLogin = now.AddHours(-2),
        };

        // Shared password for all demo customers: "Demo1234!"
        var demoHash = BCrypt.Net.BCrypt.HashPassword("Demo1234!");

        // 10 customers. Spread signup dates across the past 9 days; mix
        // statuses (1 unconfirmed, 1 inactive, rest active). Two carry a
        // small store-credit balance so the "Solde d'avoir" branch of the
        // chatbot context is exercised.
        var c1 = NewCustomer("Sophie Martin", "sophie.martin@cabinet-martin.fr", demoHash, now.AddDays(-9), now.AddHours(-5));
        var c2 = NewCustomer("Jean Dupuis", "jean.dupuis@pharmacieducentre.fr", demoHash, now.AddDays(-8), now.AddHours(-26));
        var c3 = NewCustomer("Pierre Lefèvre", "p.lefevre@cabinet-fontaine.fr", demoHash, now.AddDays(-7), now.AddHours(-4));
        var c4 = NewCustomer("Marie Rousseau", "achats@clinique-sainte-anne.fr", demoHash, now.AddDays(-6), now.AddHours(-12));
        var c5 = NewCustomer("Antoine Bernard", "a.bernard@bioanalys-lab.fr", demoHash, now.AddDays(-6), now.AddDays(-1));
        var c6 = NewCustomer("Camille Leroy", "c.leroy@veto-leroy.fr", demoHash, now.AddDays(-5), now.AddHours(-3));
        var c7 = NewCustomer("Nathalie Garnier", "n.garnier@ehpad-tilleuls.fr", demoHash, now.AddDays(-5), now.AddDays(-2));
        var c8 = NewCustomer("Thomas Moreau", "thomas.moreau@orl-moreau.fr", demoHash, now.AddDays(-4), now.AddHours(-9));
        var c9 = NewCustomer("Marc Petit", "m.petit@kineplus-paris.fr", demoHash, now.AddDays(-3), now.AddHours(-1));
        var c10 = NewCustomer("Élise Fontaine", "elise.fontaine@maison-naissance.fr", demoHash, now.AddDays(-1), null);

        // Variations on the otherwise-uniform "active confirmed" baseline.
        c5.CreditBalanceCents = 4500;       // 45 € avoir
        c7.CreditBalanceCents = 12000;      // 120 € avoir
        c10.EmailConfirmed = false;         // just signed up, hasn't clicked
        c2.Status = UserStatus.Inactive;    // payment dispute → désactivé

        var customers = new[] { c1, c2, c3, c4, c5, c6, c7, c8, c9, c10 };
        context.Users.Add(admin);
        context.Users.AddRange(customers);

        // ── Addresses ────────────────────────────────────
        // Most customers get one address; a few have two (work + billing).
        // Default flag follows the "exactly one default per user" invariant.
        var addresses = new List<Address>
        {
            // c1 — dentist, single address
            NewAddress(c1.Id, "Cabinet", "Sophie", "Martin", "Cabinet Dentaire Martin",
                       "14 rue Victor Hugo", null, "Lyon", "69002", "0472418200", true),

            // c2 — pharmacy
            NewAddress(c2.Id, "Officine", "Jean", "Dupuis", "Pharmacie du Centre",
                       "3 place de la République", null, "Bordeaux", "33000", "0556522010", true),

            // c3 — general practitioner, two addresses (cabinet + home billing)
            NewAddress(c3.Id, "Cabinet", "Pierre", "Lefèvre", "Cabinet Dr. Lefèvre",
                       "27 avenue de la Fontaine", "Bât. B, 1er étage", "Nantes", "44000", "0240482101", true),
            NewAddress(c3.Id, "Domicile (facturation)", "Pierre", "Lefèvre", null,
                       "12 rue des Lilas", null, "Nantes", "44100", "0240482101", false),

            // c4 — clinic purchasing dept, two addresses
            NewAddress(c4.Id, "Clinique Ste-Anne", "Marie", "Rousseau", "Clinique Sainte-Anne",
                       "45 boulevard Pasteur", "Service Achats", "Toulouse", "31000", "0561220045", true),
            NewAddress(c4.Id, "Antenne Blagnac", "Marie", "Rousseau", "Clinique Sainte-Anne",
                       "8 chemin du Pigeonnier", null, "Blagnac", "31700", "0561220046", false),

            // c5 — biology lab
            NewAddress(c5.Id, "Laboratoire", "Antoine", "Bernard", "BioAnalys SARL",
                       "21 zone d'activité du Plateau", null, "Grenoble", "38100", "0476443510", true),

            // c6 — vet
            NewAddress(c6.Id, "Clinique vétérinaire", "Camille", "Leroy", "Vétérinaires Leroy",
                       "9 rue des Acacias", null, "Rennes", "35000", "0299782200", true),

            // c7 — care home, two addresses
            NewAddress(c7.Id, "EHPAD Les Tilleuls", "Nathalie", "Garnier", "EHPAD Les Tilleuls",
                       "16 allée des Tilleuls", null, "Strasbourg", "67000", "0388614422", true),
            NewAddress(c7.Id, "Siège associatif", "Nathalie", "Garnier", "Association Les Tilleuls",
                       "2 rue de l'Espérance", null, "Strasbourg", "67000", "0388614400", false),

            // c8 — ENT specialist
            NewAddress(c8.Id, "Cabinet ORL", "Thomas", "Moreau", "Cabinet ORL Moreau",
                       "18 avenue Foch", null, "Lille", "59000", "0320308815", true),

            // c9 — physio centre
            NewAddress(c9.Id, "Cabinet Kiné+", "Marc", "Petit", "Kiné+ Paris 11",
                       "67 rue de la Roquette", null, "Paris", "75011", "0143546677", true),

            // c10 — midwife (no orders yet, but address ready)
            NewAddress(c10.Id, "Maison de naissance", "Élise", "Fontaine", "Maison de naissance Sérénité",
                       "5 impasse des Roses", null, "Montpellier", "34000", "0467663388", true),
        };
        context.Addresses.AddRange(addresses);

        // ── Categories ───────────────────────────────────
        var cat1Id = Guid.NewGuid(); // Imagerie médicale
        var cat2Id = Guid.NewGuid(); // Moniteurs & Diagnostics
        var cat3Id = Guid.NewGuid(); // Stérilisation & Hygiène
        var cat4Id = Guid.NewGuid(); // Instruments Chirurgicaux
        var cat5Id = Guid.NewGuid(); // Mobilier Médical
        var cat6Id = Guid.NewGuid(); // Équipements Respiratoires
        var cat7Id = Guid.NewGuid(); // Consommables Médicaux
        var cat8Id = Guid.NewGuid(); // Équipements de Laboratoire

        var categories = new List<Category>
        {
            new() { Id = cat1Id, Slug = "imagerie-medicale",
                NameFr = "Imagerie Médicale", NameEn = "Medical Imaging",
                NameMs = "Pengimejan Perubatan", NameAr = "التصوير الطبي",
                DescriptionFr = "Équipements d'imagerie diagnostique", DescriptionEn = "Diagnostic imaging equipment",
                DescriptionMs = "Peralatan pengimejan diagnostik", DescriptionAr = "أجهزة التصوير التشخيصي",
                Image = "imaging", DisplayOrder = 1 },
            new() { Id = cat2Id, Slug = "moniteurs-diagnostics",
                NameFr = "Moniteurs & Diagnostics", NameEn = "Monitors & Diagnostics",
                NameMs = "Monitor & Diagnostik", NameAr = "أجهزة المراقبة والتشخيص",
                DescriptionFr = "Moniteurs de surveillance patient", DescriptionEn = "Patient monitoring systems",
                DescriptionMs = "Sistem pemantauan pesakit", DescriptionAr = "أنظمة مراقبة المرضى",
                Image = "monitors", DisplayOrder = 2 },
            new() { Id = cat3Id, Slug = "sterilisation-hygiene",
                NameFr = "Stérilisation & Hygiène", NameEn = "Sterilization & Hygiene",
                NameMs = "Pensterilan & Kebersihan", NameAr = "التعقيم والنظافة",
                DescriptionFr = "Matériel de stérilisation et d'hygiène", DescriptionEn = "Sterilization and hygiene equipment",
                DescriptionMs = "Peralatan pensterilan dan kebersihan", DescriptionAr = "معدات التعقيم والنظافة",
                Image = "sterilization", DisplayOrder = 3 },
            new() { Id = cat4Id, Slug = "instruments-chirurgicaux",
                NameFr = "Instruments Chirurgicaux", NameEn = "Surgical Instruments",
                NameMs = "Alat Pembedahan", NameAr = "أدوات جراحية",
                DescriptionFr = "Instruments de chirurgie professionnels", DescriptionEn = "Professional surgical instruments",
                DescriptionMs = "Alat pembedahan profesional", DescriptionAr = "أدوات جراحية احترافية",
                Image = "surgical", DisplayOrder = 4 },
            new() { Id = cat5Id, Slug = "mobilier-medical",
                NameFr = "Mobilier Médical", NameEn = "Medical Furniture",
                NameMs = "Perabot Perubatan", NameAr = "أثاث طبي",
                DescriptionFr = "Mobilier professionnel pour établissements de santé", DescriptionEn = "Professional furniture for healthcare facilities",
                DescriptionMs = "Perabot profesional untuk kemudahan penjagaan kesihatan", DescriptionAr = "أثاث احترافي لمرافق الرعاية الصحية",
                Image = "furniture", DisplayOrder = 5 },
            new() { Id = cat6Id, Slug = "equipements-respiratoires",
                NameFr = "Équipements Respiratoires", NameEn = "Respiratory Equipment",
                NameMs = "Peralatan Pernafasan", NameAr = "معدات الجهاز التنفسي",
                DescriptionFr = "Matériel respiratoire et ventilation", DescriptionEn = "Respiratory and ventilation equipment",
                DescriptionMs = "Peralatan pernafasan dan pengudaraan", DescriptionAr = "أجهزة التنفس والتهوية",
                Image = "respiratory", DisplayOrder = 6 },
            new() { Id = cat7Id, Slug = "consommables-medicaux",
                NameFr = "Consommables Médicaux", NameEn = "Medical Consumables",
                NameMs = "Bahan Pakai Buang Perubatan", NameAr = "المستهلكات الطبية",
                DescriptionFr = "Consommables et fournitures médicales", DescriptionEn = "Medical consumables and supplies",
                DescriptionMs = "Bahan pakai buang dan bekalan perubatan", DescriptionAr = "المستهلكات والمستلزمات الطبية",
                Image = "consumables", DisplayOrder = 7 },
            new() { Id = cat8Id, Slug = "equipements-laboratoire",
                NameFr = "Équipements de Laboratoire", NameEn = "Laboratory Equipment",
                NameMs = "Peralatan Makmal", NameAr = "معدات المختبر",
                DescriptionFr = "Matériel de laboratoire d'analyse", DescriptionEn = "Laboratory analysis equipment",
                DescriptionMs = "Peralatan analisis makmal", DescriptionAr = "معدات تحليل المختبر",
                Image = "laboratory", DisplayOrder = 8 }
        };
        context.Categories.AddRange(categories);

        // ── Products (32 total: 4 per category) ──────────
        // Names kept descriptive in FR/EN so the chatbot's catalog block
        // produces useful per-token search hits. Prices in € HT, varied
        // VAT rates (Reduced 5.5% mostly for routine consumables).
        // Each product carries 5-6 specs (FR labels — ProductSpec is single
        // language; the front renders them as-is in the "Caractéristiques
        // techniques" tab).

        var productSpecs = new List<ProductSpec>();
        // Each spec is a (Label, Value) pair where each element is a 4-language
        // TextL10n. Pass nulls for Ms/Ar on language-agnostic values like
        // pure measurements ("12 L", "3 008 × 3 008 px") — they fall back to
        // the French canonical value on the front.
        void AddSpec(Guid productId, TextL10n label, TextL10n value)
        {
            productSpecs.Add(new ProductSpec
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Label = label.Fr,
                LabelEn = label.En,
                LabelMs = label.Ms,
                LabelAr = label.Ar,
                Value = value.Fr,
                ValueEn = value.En,
                ValueMs = value.Ms,
                ValueAr = value.Ar,
            });
        }

        // Helper for language-agnostic spec values (pure numeric/units that
        // read the same in every language). Keeps spec rows readable.
        TextL10n Num(string s) => new(s, s);

        // Imagerie médicale (cat1)
        var pProScan = NewProduct("proscan-radiographie",
            new("ProScan Radiographie", "ProScan X-Ray", "ProScan Sinar-X", "بروسكان للأشعة السينية"),
            new("Système de radiographie numérique haute résolution",
                "High-resolution digital X-ray system",
                "Sistem sinar-X digital resolusi tinggi",
                "نظام تصوير بالأشعة السينية الرقمي عالي الدقة"),
            new("Le ProScan associe un détecteur CsI flat-panel haute résolution (3 008 × 3 008 px) à un générateur 150 kV pour une imagerie diagnostique précise en routine comme en urgence. Compatible DICOM 3.0, il s'intègre directement à votre PACS via Wi-Fi 6 ou Ethernet. Livraison, installation sur site et formation des manipulateurs incluses.",
                "The ProScan pairs a high-resolution CsI flat-panel detector (3,008 × 3,008 px) with a 150 kV generator for accurate diagnostic imaging in both routine and emergency settings. DICOM 3.0 compatible, it plugs straight into your PACS via Wi-Fi 6 or Ethernet. Delivery, on-site installation and operator training included.",
                "ProScan menggabungkan pengesan panel rata CsI resolusi tinggi (3,008 × 3,008 px) dengan penjana 150 kV untuk pengimejan diagnostik yang tepat dalam keadaan rutin dan kecemasan. Serasi DICOM 3.0, ia bersambung terus dengan PACS anda melalui Wi-Fi 6 atau Ethernet. Penghantaran, pemasangan di tapak dan latihan pengendali disertakan.",
                "يجمع ProScan بين كاشف لوحي مسطح CsI عالي الدقة (3008 × 3008 بكسل) ومولّد 150 كيلوفولت للتصوير التشخيصي الدقيق في الحالات الروتينية والطوارئ. متوافق مع DICOM 3.0، ويتكامل مباشرة مع نظام PACS الخاص بك عبر Wi-Fi 6 أو إيثرنت. يشمل التسليم والتركيب في الموقع وتدريب المشغلين."),
            45000, VatRate.Standard, 8, StockStatus.InStock, true, 1, "imaging-1");
        AddSpec(pProScan.Id,
            new("Résolution détecteur", "Detector resolution", "Resolusi pengesan", "دقة الكاشف"),
            Num("3 008 × 3 008 px"));
        AddSpec(pProScan.Id,
            new("Détecteur", "Detector", "Pengesan", "الكاشف"),
            Num("CsI flat-panel"));
        AddSpec(pProScan.Id,
            new("Génération haute tension", "High-voltage generator", "Penjana voltan tinggi", "مولّد الجهد العالي"),
            Num("50 - 150 kV"));
        AddSpec(pProScan.Id,
            new("Connectique", "Connectivity", "Sambungan", "التوصيلات"),
            Num("DICOM 3.0 / Wi-Fi 6 / Ethernet"));
        AddSpec(pProScan.Id,
            new("Poids", "Weight", "Berat", "الوزن"),
            Num("220 kg"));
        AddSpec(pProScan.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("3 ans pièces et main d'œuvre", "3 years parts and labor", "3 tahun alat ganti dan upah", "3 سنوات قطع غيار ويد عاملة"));

        var pEchoView = NewProduct("echoview-pro-portable",
            new("EchoView Pro portable", "EchoView Pro portable", "EchoView Pro mudah alih", "EchoView Pro المحمول"),
            new("Échographe portable couleur Doppler",
                "Portable color Doppler ultrasound",
                "Ultrasound Doppler warna mudah alih",
                "جهاز فحص بالأمواج فوق الصوتية دوبلر ملوّن محمول"),
            new("L'EchoView Pro est un échographe portable couleur Doppler conçu pour la médecine de proximité et les déplacements à domicile. Son écran tactile 12\" HD et ses sondes interchangeables (convex et linéaire) couvrent l'abdominal, le vasculaire et le superficiel. Batterie Li-ion de 4 h pour une journée d'examens sans secteur.",
                "The EchoView Pro is a portable color Doppler ultrasound built for community-based medicine and home visits. Its 12\" HD touchscreen and swappable convex / linear probes cover abdominal, vascular and superficial exams. A 4-hour Li-ion battery delivers a full day of off-grid scans.",
                "EchoView Pro ialah ultrasound Doppler warna mudah alih yang direka untuk perubatan komuniti dan lawatan ke rumah. Skrin sentuh HD 12\" dan probe boleh tukar (konveks dan linear) merangkumi pemeriksaan abdomen, vaskular dan superficial. Bateri Li-ion 4 jam memberikan sehari penuh imbasan tanpa pengecasan.",
                "EchoView Pro هو جهاز فحص بالأمواج فوق الصوتية دوبلر ملوّن محمول مصمم للطب المجتمعي والزيارات المنزلية. شاشته التي تعمل باللمس مقاس 12 بوصة عالية الدقة والمسبارات القابلة للتبديل (محدّبة وخطية) تغطي فحوصات البطن والأوعية الدموية والسطحية. بطارية ليثيوم أيون 4 ساعات لفحوصات يوم كامل بدون شحن."),
            5500, VatRate.Standard, 14, StockStatus.InStock, true, 0, "imaging-2");
        AddSpec(pEchoView.Id,
            new("Écran", "Display", "Paparan", "الشاشة"),
            new("12\" tactile HD", "12\" HD touchscreen", "Skrin sentuh HD 12\"", "شاشة لمس 12 بوصة عالية الدقة"));
        AddSpec(pEchoView.Id,
            new("Modes", "Modes", "Mod", "الأوضاع"),
            Num("B, M, Color Doppler, PW Doppler"));
        AddSpec(pEchoView.Id,
            new("Sondes incluses", "Included probes", "Probe disertakan", "المسبارات المضمنة"),
            new("Convex 3,5 MHz + linéaire 7,5 MHz", "3.5 MHz convex + 7.5 MHz linear", "Konveks 3.5 MHz + linear 7.5 MHz", "محدّب 3.5 ميغاهرتز + خطي 7.5 ميغاهرتز"));
        AddSpec(pEchoView.Id,
            new("Autonomie", "Battery life", "Hayat bateri", "عمر البطارية"),
            new("4 h sur batterie Li-ion", "4 h on Li-ion battery", "4 jam pada bateri Li-ion", "4 ساعات على بطارية ليثيوم أيون"));
        AddSpec(pEchoView.Id,
            new("Poids", "Weight", "Berat", "الوزن"),
            Num("6,2 kg"));
        AddSpec(pEchoView.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("2 ans", "2 years", "2 tahun", "سنتان"));

        var pMagniScan = NewProduct("magniscan-irm-compact",
            new("MagniScan IRM compact", "MagniScan compact MRI", "MagniScan MRI padat", "ماغني سكان للرنين المغناطيسي المدمج"),
            new("IRM compact 0.5T pour cabinets spécialisés",
                "Compact 0.5T MRI for specialty clinics",
                "MRI padat 0.5T untuk klinik pakar",
                "جهاز رنين مغناطيسي مدمج 0.5 تسلا للعيادات المتخصصة"),
            new("Le MagniScan est une IRM compacte 0,5 T pensée pour les cabinets d'imagerie spécialisée disposant d'une surface limitée. Son refroidissement à hélium scellé supprime les recharges régulières et réduit drastiquement les coûts d'exploitation. Console DICOM intégrée, post-traitement et reconstruction 3D inclus.",
                "The MagniScan is a compact 0.5 T MRI designed for specialty imaging clinics with limited footprint. Its sealed helium cooling eliminates recurring refills and slashes running costs. Integrated DICOM console, post-processing and 3D reconstruction included.",
                "MagniScan ialah MRI padat 0.5 T yang direka untuk klinik pengimejan pakar dengan ruang terhad. Sistem penyejukan helium tertutupnya menghapuskan pengisian semula berulang dan mengurangkan kos operasi secara mendadak. Konsol DICOM bersepadu, pemprosesan pasca dan pembinaan semula 3D disertakan.",
                "MagniScan هو جهاز رنين مغناطيسي مدمج بقوة 0.5 تسلا مصمم لعيادات التصوير المتخصصة ذات المساحة المحدودة. تبريده بالهيليوم المُحكم يُلغي الحاجة إلى إعادة الشحن الدورية ويخفض تكاليف التشغيل بشكل كبير. يشمل وحدة تحكم DICOM المدمجة والمعالجة اللاحقة وإعادة البناء ثلاثي الأبعاد."),
            95000, VatRate.Standard, 2, StockStatus.LowStock, false, 0, "imaging-3");
        AddSpec(pMagniScan.Id,
            new("Champ magnétique", "Magnetic field", "Medan magnet", "المجال المغناطيسي"),
            Num("0,5 T"));
        AddSpec(pMagniScan.Id,
            new("Diamètre tunnel", "Bore diameter", "Diameter terowong", "قطر النفق"),
            Num("60 cm"));
        AddSpec(pMagniScan.Id,
            new("Empreinte au sol", "Floor footprint", "Ruang lantai", "المساحة الأرضية"),
            Num("12 m²"));
        AddSpec(pMagniScan.Id,
            new("Refroidissement", "Cooling", "Penyejukan", "التبريد"),
            new("Hélium scellé, sans recharge", "Sealed helium, no refills", "Helium tertutup, tanpa pengisian semula", "هيليوم محكم، بدون إعادة شحن"));
        AddSpec(pMagniScan.Id,
            new("Logiciel", "Software", "Perisian", "البرنامج"),
            new("Console DICOM intégrée + reconstruction 3D", "Integrated DICOM console + 3D reconstruction", "Konsol DICOM bersepadu + pembinaan semula 3D", "وحدة تحكم DICOM مدمجة + إعادة بناء ثلاثية الأبعاد"));
        AddSpec(pMagniScan.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("5 ans", "5 years", "5 tahun", "5 سنوات"));

        var pDentalView = NewProduct("dentalview-scanner-3d",
            new("DentalView Scanner 3D", "DentalView 3D scanner", "Pengimbas 3D DentalView", "ماسح ضوئي ثلاثي الأبعاد DentalView"),
            new("Scanner intra-oral 3D pour empreintes numériques",
                "3D intra-oral scanner for digital impressions",
                "Pengimbas intra-oral 3D untuk cetakan digital",
                "ماسح ضوئي ثلاثي الأبعاد داخل الفم للطبعات الرقمية"),
            new("Le DentalView capture des empreintes 3D intra-orales en quelques secondes, supprimant pâtes et porte-empreintes. Les fichiers STL / OBJ s'exportent directement vers votre laboratoire de prothèse ou votre logiciel de CFAO. Idéal pour les cabinets dentaires souhaitant moderniser leur flux numérique.",
                "The DentalView captures 3D intra-oral impressions in seconds, doing away with putty and trays. STL / OBJ files export straight to your prosthetics lab or CAD/CAM software. Ideal for dental practices going fully digital.",
                "DentalView menangkap cetakan intra-oral 3D dalam beberapa saat, menghapuskan keperluan dempul dan dulang. Fail STL / OBJ dieksport terus ke makmal prostetik atau perisian CAD/CAM anda. Sesuai untuk klinik pergigian yang ingin memodenkan aliran kerja digital mereka.",
                "يلتقط DentalView طبعات ثلاثية الأبعاد داخل الفم في ثوانٍ، مما يُلغي الحاجة إلى المعجون وحاملاته. تُصدَّر ملفات STL / OBJ مباشرة إلى مختبر التركيبات أو برنامج CAD/CAM الخاص بك. مثالي لعيادات الأسنان التي ترغب في تحديث سير العمل الرقمي."),
            12500, VatRate.Standard, 6, StockStatus.InStock, false, 0, "imaging-4");
        AddSpec(pDentalView.Id,
            new("Résolution", "Resolution", "Resolusi", "الدقة"),
            Num("25 µm"));
        AddSpec(pDentalView.Id,
            new("Formats export", "Export formats", "Format eksport", "تنسيقات التصدير"),
            Num("STL, OBJ, PLY"));
        AddSpec(pDentalView.Id,
            new("Vitesse de scan", "Scan speed", "Kelajuan imbasan", "سرعة المسح"),
            new("6 secondes par arcade", "6 seconds per arch", "6 saat setiap arka", "6 ثوانٍ لكل قوس"));
        AddSpec(pDentalView.Id,
            new("Connectique", "Connectivity", "Sambungan", "التوصيلات"),
            Num("USB-C, Wi-Fi"));
        AddSpec(pDentalView.Id,
            new("Poids tête", "Wand weight", "Berat kepala", "وزن الرأس"),
            Num("240 g"));
        AddSpec(pDentalView.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("3 ans", "3 years", "3 tahun", "3 سنوات"));

        // Moniteurs & Diagnostics (cat2)
        var pVitalGuard = NewProduct("vitalguard-moniteur",
            new("VitalGuard Moniteur", "VitalGuard Monitor", "Monitor VitalGuard", "مونيتور VitalGuard"),
            new("Moniteur patient multiparamétrique",
                "Multi-parameter patient monitor",
                "Monitor pesakit pelbagai parameter",
                "مونيتور متعدد المعايير للمرضى"),
            new("Le VitalGuard surveille en continu SpO₂, ECG 5 dérivations, PNI, fréquence respiratoire et température sur un écran 12\" couleur lisible à distance. Trois niveaux d'alarmes configurables et 96 h de tendances mémorisées sécurisent l'observation en soins continus. Fonctionne sur secteur ou batterie 6 h.",
                "The VitalGuard provides continuous SpO₂, 5-lead ECG, NIBP, respiratory rate and temperature monitoring on a 12\" color display visible from across the room. Three configurable alarm levels and 96 h of trend memory secure step-down care. Mains or 6 h battery operation.",
                "VitalGuard memantau secara berterusan SpO₂, ECG 5-lead, tekanan darah bukan invasif, kadar pernafasan dan suhu pada paparan warna 12\" yang boleh dibaca dari jauh. Tiga tahap penggera boleh dikonfigurasi dan memori trend 96 jam memastikan pemantauan rapi. Boleh dikendalikan dengan kuasa utama atau bateri 6 jam.",
                "يوفر VitalGuard مراقبة مستمرة لتشبع الأكسجين SpO₂، وتخطيط القلب ECG بخمس أقطاب، وضغط الدم غير الباضع، ومعدل التنفس ودرجة الحرارة على شاشة ملونة مقاس 12 بوصة يمكن قراءتها عن بُعد. ثلاثة مستويات إنذار قابلة للتخصيص وذاكرة اتجاهات لمدة 96 ساعة تؤمّن الرعاية المتواصلة. يعمل بالكهرباء أو ببطارية تدوم 6 ساعات."),
            8500, VatRate.Standard, 25, StockStatus.InStock, false, 2, "monitors-1");
        AddSpec(pVitalGuard.Id,
            new("Paramètres", "Parameters", "Parameter", "المعايير المُقاسة"),
            new("SpO₂, ECG 5 dérivations, PNI, FR, T°", "SpO₂, 5-lead ECG, NIBP, RR, T°", "SpO₂, ECG 5-lead, NIBP, RR, T°", "SpO₂، ECG بخمس أقطاب، ضغط الدم غير الباضع، معدل التنفس، الحرارة"));
        AddSpec(pVitalGuard.Id,
            new("Écran", "Display", "Paparan", "الشاشة"),
            new("12\" couleur LCD", "12\" color LCD", "LCD warna 12\"", "LCD ملوّن 12 بوصة"));
        AddSpec(pVitalGuard.Id,
            new("Mémoire tendances", "Trend memory", "Memori trend", "ذاكرة الاتجاهات"),
            Num("96 h"));
        AddSpec(pVitalGuard.Id,
            new("Alarmes", "Alarms", "Penggera", "الإنذارات"),
            new("3 niveaux configurables", "3 configurable levels", "3 tahap boleh dikonfigurasi", "3 مستويات قابلة للتخصيص"));
        AddSpec(pVitalGuard.Id,
            new("Alimentation", "Power supply", "Bekalan kuasa", "الإمداد بالطاقة"),
            new("Secteur + batterie 6 h", "Mains + 6 h battery", "Kuasa utama + bateri 6 jam", "كهرباء + بطارية 6 ساعات"));
        AddSpec(pVitalGuard.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("2 ans", "2 years", "2 tahun", "سنتان"));

        var pPressureTrack = NewProduct("pressuretrack-tensiometre",
            new("PressureTrack Tensiomètre", "PressureTrack Sphygmomanometer", "Tolok tekanan darah PressureTrack", "مقياس ضغط الدم PressureTrack"),
            new("Tensiomètre digital de bras automatique",
                "Automatic upper-arm digital sphygmomanometer",
                "Tolok tekanan darah lengan atas digital automatik",
                "مقياس ضغط دم رقمي أوتوماتيكي للذراع"),
            new("Le PressureTrack est un tensiomètre de bras automatique validé ESH 2018 pour usage clinique. Sa mémoire de 60 mesures et la détection d'arythmie en font un outil fiable au cabinet comme en visite à domicile. Alimentation mixte secteur ou piles AA.",
                "The PressureTrack is an ESH 2018-validated automatic upper-arm sphygmomanometer for clinical use. A 60-measurement memory and arrhythmia detection make it dependable in clinic and on home visits. Mains or AA-battery powered.",
                "PressureTrack ialah tolok tekanan darah lengan atas automatik yang disahkan ESH 2018 untuk kegunaan klinikal. Memori 60 bacaan dan pengesanan aritmia menjadikannya alat yang boleh dipercayai di klinik dan semasa lawatan ke rumah. Dikuasakan oleh kuasa utama atau bateri AA.",
                "PressureTrack هو مقياس ضغط دم أوتوماتيكي للذراع معتمد من ESH 2018 للاستخدام السريري. ذاكرة 60 قياس وكشف اضطرابات النظم تجعله أداة موثوقة في العيادة والزيارات المنزلية. يعمل بالكهرباء أو ببطاريات AA."),
            250, VatRate.Standard, 80, StockStatus.InStock, false, 0, "monitors-2");
        AddSpec(pPressureTrack.Id,
            new("Type", "Type", "Jenis", "النوع"),
            new("Brassard de bras automatique", "Automatic upper-arm cuff", "Kaf lengan atas automatik", "كفّ أوتوماتيكي للذراع"));
        AddSpec(pPressureTrack.Id,
            new("Plage de mesure", "Measurement range", "Julat pengukuran", "نطاق القياس"),
            Num("30 - 280 mmHg"));
        AddSpec(pPressureTrack.Id,
            new("Mémoire", "Memory", "Memori", "الذاكرة"),
            new("60 mesures", "60 measurements", "60 bacaan", "60 قياس"));
        AddSpec(pPressureTrack.Id,
            new("Détection", "Detection", "Pengesanan", "الكشف"),
            new("Arythmie + mouvement", "Arrhythmia + movement", "Aritmia + pergerakan", "اضطراب النظم + الحركة"));
        AddSpec(pPressureTrack.Id,
            new("Alimentation", "Power supply", "Bekalan kuasa", "الإمداد بالطاقة"),
            new("4 piles AA / secteur", "4 AA batteries / mains", "4 bateri AA / kuasa utama", "4 بطاريات AA / كهرباء"));
        AddSpec(pPressureTrack.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            new("ESH 2018 validé clinique", "ESH 2018 clinically validated", "ESH 2018 disahkan secara klinikal", "ESH 2018 معتمد سريرياً"));

        var pPulseOx = NewProduct("pulseox-mini-oxymetre",
            new("PulseOx Mini Oxymètre", "PulseOx Mini Oximeter", "Oksimeter PulseOx Mini", "مقياس التأكسج PulseOx Mini"),
            new("Oxymètre de pouls de doigt compact",
                "Compact fingertip pulse oximeter",
                "Oksimeter nadi hujung jari padat",
                "مقياس تأكسج نبضي مدمج لأطراف الأصابع"),
            new("L'oxymètre PulseOx Mini mesure la SpO₂ et la fréquence cardiaque sur un écran OLED couleur orientable. Léger (50 g) et alimenté par 2 piles AAA, il offre 30 h d'utilisation continue. Conforme CE médical classe IIa pour usage professionnel.",
                "The PulseOx Mini reads SpO₂ and heart rate on a rotatable OLED color display. Lightweight (50 g) and powered by 2 AAA batteries, it delivers 30 hours of continuous use. CE medical class IIa certified for professional use.",
                "PulseOx Mini membaca SpO₂ dan kadar denyutan jantung pada paparan OLED warna boleh putar. Ringan (50 g) dan dikuasakan oleh 2 bateri AAA, ia memberikan 30 jam penggunaan berterusan. Diperakui CE perubatan kelas IIa untuk kegunaan profesional.",
                "يقرأ PulseOx Mini تشبع الأكسجين SpO₂ ومعدل ضربات القلب على شاشة OLED ملوّنة قابلة للدوران. خفيف (50 جم) ويعمل ببطاريتي AAA، يوفر 30 ساعة من الاستخدام المستمر. معتمد CE الطبي فئة IIa للاستخدام المهني."),
            85, VatRate.Standard, 200, StockStatus.InStock, false, 0, "monitors-3");
        AddSpec(pPulseOx.Id,
            new("Mesures", "Measurements", "Pengukuran", "القياسات"),
            Num("SpO₂ 70 - 99 %, FC 30 - 250 bpm"));
        AddSpec(pPulseOx.Id,
            new("Écran", "Display", "Paparan", "الشاشة"),
            new("OLED couleur orientable", "Rotatable color OLED", "OLED warna boleh putar", "OLED ملوّن قابل للدوران"));
        AddSpec(pPulseOx.Id,
            new("Autonomie", "Battery life", "Hayat bateri", "عمر البطارية"),
            new("30 h en continu", "30 h continuous", "30 jam berterusan", "30 ساعة متواصلة"));
        AddSpec(pPulseOx.Id,
            new("Alimentation", "Power supply", "Bekalan kuasa", "الإمداد بالطاقة"),
            new("2 piles AAA", "2 AAA batteries", "2 bateri AAA", "بطاريتا AAA"));
        AddSpec(pPulseOx.Id,
            new("Poids", "Weight", "Berat", "الوزن"),
            Num("50 g"));
        AddSpec(pPulseOx.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            new("CE médical classe IIa", "CE medical class IIa", "CE perubatan kelas IIa", "CE الطبي فئة IIa"));

        var pCardioTrace = NewProduct("cardiotrace-ecg-portable",
            new("CardioTrace ECG portable", "CardioTrace portable ECG", "ECG mudah alih CardioTrace", "جهاز تخطيط القلب المحمول CardioTrace"),
            new("ECG portable 12 dérivations",
                "12-lead portable ECG",
                "ECG mudah alih 12-lead",
                "تخطيط قلب محمول بـ 12 قطباً"),
            new("Le CardioTrace réalise des ECG 12 dérivations en une minute avec rapport PDF automatique. Sa mémoire interne stocke 200 examens et l'export PC/USB simplifie l'archivage dans le dossier patient. Écran tactile 7\" et impression thermique intégrée.",
                "The CardioTrace performs 12-lead ECGs in under a minute with automatic PDF reporting. 200-exam internal memory plus USB/PC export streamline EMR archiving. 7\" touchscreen and built-in thermal printer.",
                "CardioTrace melakukan ECG 12-lead dalam masa kurang dari seminit dengan laporan PDF automatik. Memori dalaman 200 pemeriksaan ditambah eksport USB/PC memudahkan pengarkiban rekod perubatan. Skrin sentuh 7\" dan pencetak terma terbina dalam.",
                "يُجري CardioTrace تخطيط قلب بـ 12 قطباً في أقل من دقيقة مع تقرير PDF تلقائي. ذاكرة داخلية لـ 200 فحص مع تصدير USB/PC تُسهّل الأرشفة في الملف الطبي. شاشة لمس 7 بوصة وطابعة حرارية مدمجة."),
            2800, VatRate.Standard, 12, StockStatus.InStock, true, 0, "monitors-4");
        AddSpec(pCardioTrace.Id,
            new("Dérivations", "Leads", "Lead", "الأقطاب"),
            new("12 simultanées", "12 simultaneous", "12 serentak", "12 متزامن"));
        AddSpec(pCardioTrace.Id,
            new("Vitesse impression", "Print speed", "Kelajuan cetakan", "سرعة الطباعة"),
            Num("25 mm/s"));
        AddSpec(pCardioTrace.Id,
            new("Mémoire", "Memory", "Memori", "الذاكرة"),
            new("200 examens", "200 exams", "200 pemeriksaan", "200 فحص"));
        AddSpec(pCardioTrace.Id,
            new("Export", "Export", "Eksport", "التصدير"),
            Num("PDF, XML, USB, PC link"));
        AddSpec(pCardioTrace.Id,
            new("Écran", "Display", "Paparan", "الشاشة"),
            new("7\" tactile couleur", "7\" color touchscreen", "Skrin sentuh warna 7\"", "شاشة لمس ملوّنة 7 بوصة"));
        AddSpec(pCardioTrace.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("2 ans", "2 years", "2 tahun", "سنتان"));

        // Stérilisation & Hygiène (cat3)
        var pSteriliPro = NewProduct("sterilipro-autoclave",
            new("SteriliPro Autoclave", "SteriliPro Autoclave", "Autoklaf SteriliPro", "موصدة SteriliPro"),
            new("Autoclave de classe B pour stérilisation",
                "Class B autoclave for sterilization",
                "Autoklaf kelas B untuk pensterilan",
                "موصدة من الفئة B للتعقيم"),
            new("Le SteriliPro est un autoclave de classe B 18 L conforme à la norme EN 13060 pour la stérilisation de tous types de charges (massives, creuses, poreuses). Son réservoir d'eau intégré et son écran de pilotage facilitent l'usage quotidien au cabinet ou en bloc opératoire.",
                "The SteriliPro is an 18 L class B autoclave compliant with EN 13060 for sterilizing all load types (solid, hollow, porous). Built-in water reservoir and operator display make daily use effortless in clinic or OR.",
                "SteriliPro ialah autoklaf kelas B 18 L yang mematuhi EN 13060 untuk pensterilan semua jenis muatan (pejal, berongga, berliang). Takungan air terbina dalam dan paparan pengendali memudahkan penggunaan harian di klinik atau bilik bedah.",
                "SteriliPro هي موصدة من الفئة B بسعة 18 لتراً متوافقة مع EN 13060 لتعقيم جميع أنواع الأحمال (الصلبة والمجوفة والمسامية). خزان الماء المدمج وشاشة التحكم يجعلان الاستخدام اليومي سهلاً في العيادة أو غرفة العمليات."),
            18500, VatRate.Standard, 12, StockStatus.InStock, false, 0, "sterilization-1");
        AddSpec(pSteriliPro.Id,
            new("Classe", "Class", "Kelas", "الفئة"),
            new("B (charges A et creuses)", "B (solid + hollow loads)", "B (muatan pejal + berongga)", "B (الأحمال الصلبة والمجوفة)"));
        AddSpec(pSteriliPro.Id,
            new("Capacité", "Capacity", "Kapasiti", "السعة"),
            Num("18 L"));
        AddSpec(pSteriliPro.Id,
            new("Cycle standard", "Standard cycle", "Kitaran standard", "الدورة القياسية"),
            Num("134 °C / 18 min"));
        AddSpec(pSteriliPro.Id,
            new("Réservoir eau", "Water reservoir", "Takungan air", "خزان الماء"),
            new("Intégré 4 L", "Built-in 4 L", "Terbina dalam 4 L", "مدمج 4 لتر"));
        AddSpec(pSteriliPro.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            Num("EN 13060"));
        AddSpec(pSteriliPro.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("3 ans", "3 years", "3 tahun", "3 سنوات"));

        var pSterilWash = NewProduct("sterilwash-laveur-thermique",
            new("SterilWash Laveur thermique", "SterilWash thermal washer", "Pencuci terma SterilWash", "غسالة حرارية SterilWash"),
            new("Laveur-désinfecteur d'instruments thermique",
                "Thermal instrument washer-disinfector",
                "Pencuci-pembasmi kuman instrumen terma",
                "غسالة-مطهّرة حرارية للأدوات"),
            new("Le SterilWash automatise le pré-traitement des instruments : nettoyage, désinfection thermique (A0 = 3 000) et séchage en un seul cycle. Quatre programmes validés couvrent les besoins du bloc au cabinet. Conforme EN ISO 15883-1.",
                "The SterilWash automates instrument reprocessing — cleaning, thermal disinfection (A0 = 3 000) and drying in a single cycle. Four validated programs cover OR-to-clinic workloads. EN ISO 15883-1 compliant.",
                "SterilWash mengautomasi pra-pemprosesan instrumen — pembersihan, pembasmian kuman terma (A0 = 3,000) dan pengeringan dalam satu kitaran. Empat program disahkan merangkumi beban kerja dari bilik bedah ke klinik. Mematuhi EN ISO 15883-1.",
                "تُؤتمت SterilWash إعادة معالجة الأدوات — التنظيف والتطهير الحراري (A0 = 3,000) والتجفيف في دورة واحدة. أربعة برامج معتمدة تغطي أعباء العمل من غرفة العمليات إلى العيادة. متوافقة مع EN ISO 15883-1."),
            8500, VatRate.Standard, 5, StockStatus.LowStock, false, 0, "sterilization-2");
        AddSpec(pSterilWash.Id,
            new("Capacité", "Capacity", "Kapasiti", "السعة"),
            new("6 plateaux DIN", "6 DIN trays", "6 dulang DIN", "6 صواني DIN"));
        AddSpec(pSterilWash.Id,
            new("Cycles préprogrammés", "Pre-programmed cycles", "Kitaran pra-program", "دورات مبرمجة مسبقاً"),
            Num("4"));
        AddSpec(pSterilWash.Id,
            new("Désinfection", "Disinfection", "Pembasmian kuman", "التطهير"),
            Num("A0 = 3 000"));
        AddSpec(pSterilWash.Id,
            new("Doseur détergent", "Detergent dispenser", "Penyalur detergen", "موزّع المنظّف"),
            new("Automatique", "Automatic", "Automatik", "أوتوماتيكي"));
        AddSpec(pSterilWash.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            Num("EN ISO 15883-1"));
        AddSpec(pSterilWash.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("3 ans", "3 years", "3 tahun", "3 سنوات"));

        var pSoniClean = NewProduct("soniclean-ultrasons",
            new("SoniClean Bac à ultrasons", "SoniClean ultrasonic bath", "Tab ultrasonik SoniClean", "حمّام موجات فوق صوتية SoniClean"),
            new("Bac à ultrasons 1.5L pour pré-désinfection",
                "1.5L ultrasonic bath for pre-disinfection",
                "Tab ultrasonik 1.5L untuk pra-pembasmian kuman",
                "حمّام موجات فوق صوتية 1.5 لتر للتطهير الأولي"),
            new("Le SoniClean est un bac à ultrasons 1,5 L destiné au pré-traitement avant stérilisation. Sa cuve inox 304, son minuteur 30 minutes et son chauffage jusqu'à 80 °C éliminent les résidus dans les anfractuosités. Compact, idéal pour cabinets dentaires et vétérinaires.",
                "The SoniClean is a 1.5 L ultrasonic bath for pre-sterilization treatment. Its stainless 304 tank, 30-min timer and heating up to 80 °C dislodge debris from instrument crevices. Compact — ideal for dental and veterinary clinics.",
                "SoniClean ialah tab ultrasonik 1.5 L untuk rawatan pra-pensterilan. Tangki keluli tahan karat 304, pemasa 30 minit dan pemanasan sehingga 80 °C menanggalkan sisa dari celahan instrumen. Padat — sesuai untuk klinik pergigian dan veterinar.",
                "SoniClean هو حمّام موجات فوق صوتية 1.5 لتر للمعالجة قبل التعقيم. خزانه من الفولاذ المقاوم للصدأ 304، ومؤقّت 30 دقيقة وتسخين حتى 80 °م يزيل البقايا من شقوق الأدوات. مدمج — مثالي لعيادات الأسنان والطب البيطري."),
            650, VatRate.Standard, 18, StockStatus.InStock, false, 0, "sterilization-3");
        AddSpec(pSoniClean.Id,
            new("Capacité cuve", "Tank capacity", "Kapasiti tangki", "سعة الخزان"),
            Num("1,5 L"));
        AddSpec(pSoniClean.Id,
            new("Fréquence", "Frequency", "Frekuensi", "التردد"),
            Num("40 kHz"));
        AddSpec(pSoniClean.Id,
            new("Minuteur", "Timer", "Pemasa", "المؤقّت"),
            Num("1 - 30 min"));
        AddSpec(pSoniClean.Id,
            new("Chauffage", "Heating", "Pemanasan", "التسخين"),
            Num("20 - 80 °C"));
        AddSpec(pSoniClean.Id,
            new("Matériau cuve", "Tank material", "Bahan tangki", "مادة الخزان"),
            new("Inox 304", "Stainless 304", "Keluli tahan karat 304", "فولاذ مقاوم للصدأ 304"));
        AddSpec(pSoniClean.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("2 ans", "2 years", "2 tahun", "سنتان"));

        var pBactoCide = NewProduct("bactocide-desinfectant-5l",
            new("BactoCide désinfectant 5L", "BactoCide disinfectant 5L", "Pembasmi kuman BactoCide 5L", "مطهّر BactoCide 5 لتر"),
            new("Solution désinfectante de surface 5L",
                "5L surface disinfectant solution",
                "Larutan pembasmi kuman permukaan 5L",
                "محلول تطهير الأسطح 5 لتر"),
            new("BactoCide est une solution désinfectante de surface large spectre : bactéricide, virucide (EN 14476) et fongicide. Action en 5 minutes, sans rinçage sur les surfaces non médicales. Bidon 5 L avec bouchon doseur intégré.",
                "BactoCide is a broad-spectrum surface disinfectant — bactericidal, virucidal (EN 14476) and fungicidal. 5-minute contact time, no rinsing required on non-medical surfaces. 5 L jug with integrated dosing cap.",
                "BactoCide ialah pembasmi kuman permukaan spektrum luas — bakterisida, virusida (EN 14476) dan fungisida. Masa tindakan 5 minit, tanpa bilasan pada permukaan bukan perubatan. Bekas 5 L dengan tutup penyukat bersepadu.",
                "BactoCide مطهّر أسطح واسع الطيف — قاتل للبكتيريا والفيروسات (EN 14476) والفطريات. زمن تلامس 5 دقائق، بدون شطف على الأسطح غير الطبية. عبوة 5 لتر مع غطاء جرعات مدمج."),
            45, VatRate.Standard, 120, StockStatus.InStock, false, 0, "sterilization-4");
        AddSpec(pBactoCide.Id,
            new("Volume", "Volume", "Isi padu", "الحجم"),
            Num("5 L"));
        AddSpec(pBactoCide.Id,
            new("Spectre", "Spectrum", "Spektrum", "الطيف"),
            new("Bactéricide, virucide, fongicide", "Bactericidal, virucidal, fungicidal", "Bakterisida, virusida, fungisida", "قاتل للبكتيريا والفيروسات والفطريات"));
        AddSpec(pBactoCide.Id,
            new("Temps d'action", "Contact time", "Masa tindakan", "زمن التلامس"),
            new("5 minutes", "5 minutes", "5 minit", "5 دقائق"));
        AddSpec(pBactoCide.Id,
            new("Substance active", "Active substance", "Bahan aktif", "المادة الفعالة"),
            new("Ammonium quaternaire", "Quaternary ammonium", "Ammonium kuaternari", "أمونيوم رباعي"));
        AddSpec(pBactoCide.Id,
            new("Normes", "Standards", "Piawaian", "المعايير"),
            Num("EN 14476, EN 1276"));
        AddSpec(pBactoCide.Id,
            new("Conservation", "Shelf life", "Jangka hayat", "مدة الصلاحية"),
            new("24 mois à T° ambiante", "24 months at room T°", "24 bulan pada suhu bilik", "24 شهراً في درجة حرارة الغرفة"));

        // Instruments chirurgicaux (cat4)
        var pPrecisionCut = NewProduct("precisioncut-scalpels",
            new("PrecisionCut Scalpels", "PrecisionCut Scalpels", "Skalpel PrecisionCut", "مشارط PrecisionCut"),
            new("Set de scalpels chirurgicaux en acier inoxydable",
                "Stainless steel surgical scalpel set",
                "Set skalpel pembedahan keluli tahan karat",
                "طقم مشارط جراحية من الفولاذ المقاوم للصدأ"),
            new("Le set PrecisionCut comprend 12 lames stériles (tailles 10, 11, 15, 20) et 2 manches en acier inoxydable 420. Pensé pour la chirurgie de bloc comme la petite chirurgie au cabinet, il garantit une précision constante grâce à un affûtage cryogénique. Étui inox stérilisable inclus.",
                "The PrecisionCut set ships with 12 sterile blades (sizes 10, 11, 15, 20) and 2 stainless 420 handles. Built for OR surgery and minor in-clinic procedures, it delivers consistent precision thanks to cryogenic sharpening. Sterilizable stainless case included.",
                "Set PrecisionCut disertakan dengan 12 bilah steril (saiz 10, 11, 15, 20) dan 2 pemegang keluli 420. Direka untuk pembedahan di bilik bedah dan prosedur kecil di klinik, ia memberikan ketepatan yang konsisten berkat pengasahan kriogenik. Bekas keluli boleh disterilkan disertakan.",
                "يأتي طقم PrecisionCut مع 12 شفرة معقّمة (المقاسات 10، 11، 15، 20) ومقبضين من الفولاذ 420. مصمم لجراحة غرفة العمليات والإجراءات الصغيرة في العيادة، يوفر دقة ثابتة بفضل الشحذ بالتبريد. علبة فولاذية قابلة للتعقيم مرفقة."),
            450, VatRate.Standard, 50, StockStatus.InStock, false, 0, "surgical-1");
        AddSpec(pPrecisionCut.Id,
            new("Composition set", "Set composition", "Komposisi set", "محتويات الطقم"),
            new("12 lames + 2 manches", "12 blades + 2 handles", "12 bilah + 2 pemegang", "12 شفرة + مقبضان"));
        AddSpec(pPrecisionCut.Id,
            new("Matériau", "Material", "Bahan", "المواد"),
            new("Acier inoxydable 420", "Stainless steel 420", "Keluli tahan karat 420", "فولاذ مقاوم للصدأ 420"));
        AddSpec(pPrecisionCut.Id,
            new("Tailles lames", "Blade sizes", "Saiz bilah", "مقاسات الشفرات"),
            Num("10, 11, 15, 20"));
        AddSpec(pPrecisionCut.Id,
            new("Stérilisation", "Sterilization", "Pensterilan", "التعقيم"),
            new("Autoclave 134 °C", "Autoclave 134 °C", "Autoklaf 134 °C", "موصدة 134 °م"));
        AddSpec(pPrecisionCut.Id,
            new("Conditionnement", "Packaging", "Pembungkusan", "التعبئة"),
            new("Étui inox", "Stainless case", "Bekas keluli", "علبة فولاذية"));
        AddSpec(pPrecisionCut.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            new("CE médical classe Is", "CE medical class Is", "CE perubatan kelas Is", "CE الطبي فئة Is"));

        var pChirurgPro = NewProduct("chirurgpro-pinces-hemostatiques",
            new("ChirurgPro Pinces hémostatiques", "ChirurgPro hemostatic forceps", "Forceps hemostatik ChirurgPro", "ملاقط إرقاء ChirurgPro"),
            new("Set de 5 pinces hémostatiques inox",
                "Set of 5 stainless hemostatic forceps",
                "Set 5 forceps hemostatik keluli",
                "طقم من 5 ملاقط إرقاء فولاذية"),
            new("Le set ChirurgPro réunit 5 pinces hémostatiques essentielles (Kelly courbes, droites, Kocher) en acier 420 trempé. Conçues pour 2 000 cycles d'autoclave sans perte de fermeture, elles équipent durablement les blocs et cabinets de chirurgie ambulatoire.",
                "The ChirurgPro set bundles 5 essential hemostatic forceps (curved & straight Kelly, Kocher) in hardened 420 steel. Engineered for 2,000 autoclave cycles without loss of jaw alignment, they durably outfit OR and ambulatory surgery clinics.",
                "Set ChirurgPro menggabungkan 5 forceps hemostatik penting (Kelly bengkok & lurus, Kocher) dalam keluli 420 keras. Direka untuk 2,000 kitaran autoklaf tanpa kehilangan jajaran rahang, mereka melengkapi secara tahan lama bilik bedah dan klinik pembedahan ambulatori.",
                "يضم طقم ChirurgPro 5 ملاقط إرقاء أساسية (Kelly منحنية ومستقيمة، Kocher) من الفولاذ المقسّى 420. مصممة لـ 2,000 دورة موصدة دون فقدان محاذاة الفكين، تجهّز بشكل دائم غرف العمليات وعيادات الجراحة المتنقلة."),
            320, VatRate.Standard, 35, StockStatus.InStock, false, 0, "surgical-2");
        AddSpec(pChirurgPro.Id,
            new("Composition set", "Set composition", "Komposisi set", "محتويات الطقم"),
            new("5 pinces (Kelly courbes/droites, Kocher)", "5 forceps (curved/straight Kelly, Kocher)", "5 forceps (Kelly bengkok/lurus, Kocher)", "5 ملاقط (Kelly منحنية/مستقيمة، Kocher)"));
        AddSpec(pChirurgPro.Id,
            new("Matériau", "Material", "Bahan", "المواد"),
            new("Acier inoxydable 420 trempé", "Hardened stainless steel 420", "Keluli tahan karat 420 keras", "فولاذ مقاوم للصدأ 420 مقسّى"));
        AddSpec(pChirurgPro.Id,
            new("Longueur", "Length", "Panjang", "الطول"),
            Num("14 cm"));
        AddSpec(pChirurgPro.Id,
            new("Cycles autoclave validés", "Validated autoclave cycles", "Kitaran autoklaf disahkan", "دورات الموصدة المعتمدة"),
            Num("2 000"));
        AddSpec(pChirurgPro.Id,
            new("Conditionnement", "Packaging", "Pembungkusan", "التعبئة"),
            new("Plateau inox", "Stainless tray", "Dulang keluli", "صينية فولاذية"));
        AddSpec(pChirurgPro.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("5 ans", "5 years", "5 tahun", "5 سنوات"));

        var pMayoSet = NewProduct("ciseaux-mayo-droits-16",
            new("Ciseaux Mayo droits 16cm", "Mayo straight scissors 16cm", "Gunting Mayo lurus 16cm", "مقصات Mayo مستقيمة 16 سم"),
            new("Ciseaux chirurgicaux Mayo droits 16cm",
                "Mayo surgical scissors, straight 16cm",
                "Gunting pembedahan Mayo, lurus 16cm",
                "مقصات Mayo الجراحية، مستقيمة 16 سم"),
            new("Les ciseaux Mayo droits 16 cm sont des ciseaux chirurgicaux polyvalents adaptés à la coupe de tissus moyens et de fils de suture. Lame trempée et prise ergonomique pour un usage prolongé sans fatigue. Stérilisables en autoclave 134 °C.",
                "The 16 cm straight Mayo scissors are versatile surgical scissors for cutting medium tissues and suture threads. Tempered blade and ergonomic grip prevent operator fatigue during long procedures. Autoclave-safe at 134 °C.",
                "Gunting Mayo lurus 16 cm ialah gunting pembedahan serba boleh untuk memotong tisu sederhana dan benang jahitan. Bilah keras dan pemegang ergonomik mencegah keletihan pengendali semasa prosedur panjang. Selamat untuk autoklaf 134 °C.",
                "مقصات Mayo المستقيمة 16 سم هي مقصات جراحية متعددة الاستخدامات لقص الأنسجة المتوسطة وخيوط الخياطة. شفرة مقسّاة ومقبض مريح يمنعان إجهاد المشغل أثناء الإجراءات الطويلة. آمنة للموصدة عند 134 °م."),
            65, VatRate.Standard, 0, StockStatus.OutOfStock, false, 0, "surgical-3");
        AddSpec(pMayoSet.Id,
            new("Longueur", "Length", "Panjang", "الطول"),
            Num("16 cm"));
        AddSpec(pMayoSet.Id,
            new("Lame", "Blade", "Bilah", "الشفرة"),
            new("Pointe mousse", "Blunt tip", "Hujung tumpul", "طرف مدبّب"));
        AddSpec(pMayoSet.Id,
            new("Matériau", "Material", "Bahan", "المواد"),
            new("Acier inoxydable trempé", "Hardened stainless steel", "Keluli tahan karat keras", "فولاذ مقاوم للصدأ مقسّى"));
        AddSpec(pMayoSet.Id,
            new("Stérilisation", "Sterilization", "Pensterilan", "التعقيم"),
            new("Autoclave 134 °C", "Autoclave 134 °C", "Autoklaf 134 °C", "موصدة 134 °م"));
        AddSpec(pMayoSet.Id,
            new("Conditionnement", "Packaging", "Pembungkusan", "التعبئة"),
            new("Pochette papier-plastique", "Paper-plastic pouch", "Beg kertas-plastik", "كيس ورقي-بلاستيكي"));
        AddSpec(pMayoSet.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            new("CE médical", "CE medical", "CE perubatan", "CE الطبي"));

        var pKingVision = NewProduct("kingvision-laryngoscope",
            new("KingVision Laryngoscope", "KingVision Laryngoscope", "Laringoskop KingVision", "منظار الحنجرة KingVision"),
            new("Laryngoscope vidéo à lame jetable",
                "Video laryngoscope with disposable blade",
                "Laringoskop video dengan bilah pakai buang",
                "منظار حنجرة بالفيديو مع شفرة للاستخدام الواحد"),
            new("Le KingVision est un laryngoscope vidéo doté d'un écran 3,5\" couleur anti-buée et de lames jetables (tailles 3 et 4). Sa caméra CMOS autofocus facilite l'intubation difficile en urgence. Étanche IPX7 et batterie Li-ion 100 intubations.",
                "The KingVision is a video laryngoscope featuring a 3.5\" anti-fog color display and disposable blades (sizes 3 and 4). Its autofocus CMOS camera makes difficult emergency intubations far more manageable. IPX7-rated, Li-ion battery good for 100 intubations.",
                "KingVision ialah laringoskop video dengan paparan warna anti-kabus 3.5\" dan bilah pakai buang (saiz 3 dan 4). Kamera CMOS autofokusnya memudahkan intubasi kecemasan yang sukar. Tahan air IPX7, bateri Li-ion untuk 100 intubasi.",
                "KingVision هو منظار حنجرة بالفيديو يحتوي على شاشة ملوّنة 3.5 بوصة مقاومة للضباب وشفرات للاستخدام الواحد (مقاسات 3 و 4). كاميرا CMOS ذاتية التركيز تُسهّل عمليات التنبيب الطارئة الصعبة. مقاومة الماء IPX7، بطارية ليثيوم أيون لـ 100 عملية تنبيب."),
            850, VatRate.Standard, 9, StockStatus.InStock, true, 0, "surgical-4");
        AddSpec(pKingVision.Id,
            new("Écran", "Display", "Paparan", "الشاشة"),
            new("3,5\" couleur anti-buée", "3.5\" anti-fog color", "Warna anti-kabus 3.5\"", "ملوّنة 3.5 بوصة مقاومة للضباب"));
        AddSpec(pKingVision.Id,
            new("Lames", "Blades", "Bilah", "الشفرات"),
            new("Jetables, tailles 3 et 4", "Disposable, sizes 3 and 4", "Pakai buang, saiz 3 dan 4", "للاستخدام الواحد، مقاسات 3 و 4"));
        AddSpec(pKingVision.Id,
            new("Caméra", "Camera", "Kamera", "الكاميرا"),
            new("CMOS autofocus", "CMOS autofocus", "CMOS autofokus", "CMOS ذاتية التركيز"));
        AddSpec(pKingVision.Id,
            new("Batterie", "Battery", "Bateri", "البطارية"),
            new("Li-ion, 100 intubations", "Li-ion, 100 intubations", "Li-ion, 100 intubasi", "ليثيوم أيون، 100 عملية تنبيب"));
        AddSpec(pKingVision.Id,
            new("Étanchéité", "Waterproof rating", "Penarafan kalis air", "تصنيف مقاومة الماء"),
            Num("IPX7"));
        AddSpec(pKingVision.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("2 ans", "2 years", "2 tahun", "سنتان"));

        // Mobilier médical (cat5)
        var pFlexiBed = NewProduct("flexibed-lit",
            new("FlexiBed Lit Médicalisé", "FlexiBed Hospital Bed", "Katil Hospital FlexiBed", "سرير المستشفى FlexiBed"),
            new("Lit médicalisé électrique 3 fonctions",
                "3-function electric hospital bed",
                "Katil hospital elektrik 3-fungsi",
                "سرير مستشفى كهربائي بـ 3 وظائف"),
            new("Le FlexiBed est un lit médicalisé électrique 3 fonctions (relève-buste, relève-jambes, hauteur variable) pour soins continus et hospitalisation à domicile. Sommier à lattes radio-transparentes, charge admissible 200 kg, télécommande patient et infirmière. Conforme NF EN 60601-2-52.",
                "The FlexiBed is a 3-function electric hospital bed (backrest, leg rest, height adjustment) for inpatient and home-care use. Radiolucent slatted base, 200 kg safe working load, patient and nurse remotes. NF EN 60601-2-52 compliant.",
                "FlexiBed ialah katil hospital elektrik 3-fungsi (sandaran belakang, sandaran kaki, pelarasan ketinggian) untuk kegunaan pesakit dalam dan penjagaan di rumah. Tapak bilah lutsinar radio, beban kerja selamat 200 kg, kawalan jauh pesakit dan jururawat. Mematuhi NF EN 60601-2-52.",
                "FlexiBed هو سرير مستشفى كهربائي بـ 3 وظائف (مسند الظهر، مسند الساقين، ضبط الارتفاع) للاستخدام داخل المستشفى وللرعاية المنزلية. قاعدة شرائحية شفافة للأشعة، حمل آمن 200 كجم، جهازا تحكم عن بُعد للمريض والممرضة. متوافق مع NF EN 60601-2-52."),
            5500, VatRate.Standard, 15, StockStatus.InStock, true, 3, "furniture-1");
        AddSpec(pFlexiBed.Id,
            new("Fonctions électriques", "Electric functions", "Fungsi elektrik", "الوظائف الكهربائية"),
            new("3 (dossier, jambes, hauteur)", "3 (back, legs, height)", "3 (belakang, kaki, ketinggian)", "3 (الظهر، الساقان، الارتفاع)"));
        AddSpec(pFlexiBed.Id,
            new("Charge max", "Max load", "Beban maksimum", "الحمل الأقصى"),
            Num("200 kg"));
        AddSpec(pFlexiBed.Id,
            new("Plage de hauteur", "Height range", "Julat ketinggian", "نطاق الارتفاع"),
            Num("38 - 78 cm"));
        AddSpec(pFlexiBed.Id,
            new("Sommier", "Base", "Tapak", "القاعدة"),
            new("Lattes radio-transparentes", "Radiolucent slats", "Bilah lutsinar radio", "شرائح شفافة للأشعة"));
        AddSpec(pFlexiBed.Id,
            new("Roues", "Wheels", "Roda", "العجلات"),
            new("4 freinées Ø 125 mm", "4 braked Ø 125 mm", "4 berbrek Ø 125 mm", "4 مفرملة Ø 125 ملم"));
        AddSpec(pFlexiBed.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            Num("NF EN 60601-2-52"));

        var pMedCart = NewProduct("medcart-chariot-soins",
            new("MedCart chariot de soins", "MedCart treatment trolley", "Troli rawatan MedCart", "عربة العلاج MedCart"),
            new("Chariot de soins multi-niveaux ABS",
                "Multi-tier ABS treatment trolley",
                "Troli rawatan ABS pelbagai tingkat",
                "عربة علاج ABS متعددة الطبقات"),
            new("Le chariot MedCart organise les soins ambulatoires avec 5 tiroirs ABS, dont un sécurisé par serrure pour les stupéfiants. Structure aluminium légère, plateau supérieur désinfectable, 4 roulettes silencieuses dont 2 freinées. Conçu pour le service hospitalier comme l'EHPAD.",
                "The MedCart streamlines treatment workflows with 5 ABS drawers, one lockable for controlled substances. Lightweight aluminum frame, easy-clean top tray, 4 silent casters (2 braked). Built for hospital wards and care homes alike.",
                "MedCart melincirkan aliran kerja rawatan dengan 5 laci ABS, satu boleh dikunci untuk bahan terkawal. Rangka aluminium ringan, dulang atas mudah dibersihkan, 4 roda senyap (2 berbrek). Direka untuk wad hospital dan rumah penjagaan.",
                "تُبسّط MedCart سير عمل العلاج بـ 5 أدراج ABS، أحدها قابل للقفل للمواد الخاضعة للرقابة. هيكل ألومنيوم خفيف، صينية علوية سهلة التنظيف، 4 عجلات صامتة (2 مفرملة). مصممة لأجنحة المستشفيات ودور الرعاية."),
            1450, VatRate.Standard, 22, StockStatus.InStock, false, 0, "furniture-2");
        AddSpec(pMedCart.Id,
            new("Niveaux", "Levels", "Tahap", "المستويات"),
            new("5 tiroirs + 1 plateau", "5 drawers + 1 tray", "5 laci + 1 dulang", "5 أدراج + صينية واحدة"));
        AddSpec(pMedCart.Id,
            new("Matériau", "Material", "Bahan", "المواد"),
            new("ABS + structure aluminium", "ABS + aluminum frame", "ABS + rangka aluminium", "ABS + هيكل ألومنيوم"));
        AddSpec(pMedCart.Id,
            new("Charge max", "Max load", "Beban maksimum", "الحمل الأقصى"),
            Num("60 kg"));
        AddSpec(pMedCart.Id,
            new("Roulettes", "Casters", "Roda", "العجلات"),
            new("4 dont 2 freinées", "4 (2 braked)", "4 (2 berbrek)", "4 (2 مفرملة)"));
        AddSpec(pMedCart.Id,
            new("Sécurité", "Security", "Keselamatan", "الأمان"),
            new("Tiroir supérieur à serrure", "Top drawer with lock", "Laci atas dengan kunci", "الدرج العلوي بقفل"));
        AddSpec(pMedCart.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("5 ans", "5 years", "5 tahun", "5 سنوات"));

        var pErgoSit = NewProduct("ergosit-tabouret-selle",
            new("ErgoSit Tabouret de selle", "ErgoSit saddle stool", "Bangku pelana ErgoSit", "كرسي السرج ErgoSit"),
            new("Tabouret de selle ergonomique réglable",
                "Adjustable ergonomic saddle stool",
                "Bangku pelana ergonomik boleh laras",
                "كرسي سرج مريح قابل للتعديل"),
            new("Le tabouret de selle ErgoSit favorise une posture lombaire saine sur de longues séances de soins (dentaire, esthétique, kiné). Vérin pneumatique 55-75 cm, assise mousse haute densité revêtue PU lavable, base 5 branches aluminium. Roulettes silencieuses pour sols durs.",
                "The ErgoSit saddle stool promotes healthy lumbar posture during long treatment sessions (dental, aesthetics, physio). Pneumatic 55-75 cm adjustment, high-density foam seat with wipeable PU cover, 5-arm aluminum base. Silent casters for hard floors.",
                "Bangku pelana ErgoSit menggalakkan postur lumbar yang sihat semasa sesi rawatan yang panjang (pergigian, estetik, fisio). Pelarasan pneumatik 55-75 cm, kerusi busa berkepadatan tinggi dengan penutup PU boleh dilap, tapak aluminium 5 lengan. Roda senyap untuk lantai keras.",
                "كرسي السرج ErgoSit يعزز وضعية القطنية الصحية خلال جلسات العلاج الطويلة (الأسنان، التجميل، العلاج الطبيعي). ضبط هوائي 55-75 سم، مقعد إسفنجي عالي الكثافة بغطاء PU قابل للمسح، قاعدة ألومنيوم بـ 5 أذرع. عجلات صامتة للأرضيات الصلبة."),
            380, VatRate.Standard, 30, StockStatus.InStock, false, 0, "furniture-3");
        AddSpec(pErgoSit.Id,
            new("Hauteur", "Height", "Ketinggian", "الارتفاع"),
            new("55 - 75 cm (vérin pneumatique)", "55 - 75 cm (pneumatic)", "55 - 75 cm (pneumatik)", "55 - 75 سم (هوائي)"));
        AddSpec(pErgoSit.Id,
            new("Assise", "Seat", "Tempat duduk", "المقعد"),
            new("Mousse haute densité, revêtement PU lavable", "High-density foam, wipeable PU cover", "Busa berkepadatan tinggi, penutup PU boleh dilap", "إسفنج عالي الكثافة، غطاء PU قابل للمسح"));
        AddSpec(pErgoSit.Id,
            new("Base", "Base", "Tapak", "القاعدة"),
            new("5 branches aluminium", "5-arm aluminum", "5 lengan aluminium", "5 أذرع ألومنيوم"));
        AddSpec(pErgoSit.Id,
            new("Roulettes", "Casters", "Roda", "العجلات"),
            new("Silencieuses sol dur", "Silent for hard floors", "Senyap untuk lantai keras", "صامتة للأرضيات الصلبة"));
        AddSpec(pErgoSit.Id,
            new("Charge max", "Max load", "Beban maksimum", "الحمل الأقصى"),
            Num("130 kg"));
        AddSpec(pErgoSit.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("5 ans", "5 years", "5 tahun", "5 سنوات"));

        var pPrivacy = NewProduct("privacyscreen-paravent",
            new("PrivacyScreen Paravent 3 panneaux", "PrivacyScreen 3-panel partition", "Partition 3-panel PrivacyScreen", "حاجز PrivacyScreen بـ 3 ألواح"),
            new("Paravent médical 3 panneaux roulant",
                "Rolling 3-panel medical partition",
                "Partition perubatan 3-panel beroda",
                "حاجز طبي متحرك بـ 3 ألواح"),
            new("Le paravent PrivacyScreen offre une intimité immédiate en chambre ou salle d'examen grâce à ses 3 panneaux articulés de 60 cm chacun (1,80 m linéaire déplié). Toile polyester lavable à 90 °C, structure inox, 4 roulettes dont 2 freinées pour stabilité.",
                "The PrivacyScreen partition delivers instant patient privacy in rooms or exam areas with three articulated 60 cm panels (1.80 m linear when deployed). Polyester fabric machine-washable at 90 °C, stainless frame, 4 wheels (2 braked) for stability.",
                "Partition PrivacyScreen memberikan privasi pesakit segera di bilik atau kawasan pemeriksaan dengan tiga panel berartikulasi 60 cm setiap satu (1.80 m linear apabila digunakan). Fabrik poliester boleh dibasuh mesin pada 90 °C, rangka keluli, 4 roda (2 berbrek) untuk kestabilan.",
                "يوفر حاجز PrivacyScreen خصوصية فورية للمريض في الغرف أو مناطق الفحص بثلاثة ألواح مفصلية بطول 60 سم لكل منها (1.80 م خطي عند الفتح). قماش بوليستر قابل للغسل بالغسالة عند 90 °م، إطار فولاذي، 4 عجلات (2 مفرملة) للثبات."),
            290, VatRate.Standard, 14, StockStatus.InStock, false, 0, "furniture-4");
        AddSpec(pPrivacy.Id,
            new("Panneaux", "Panels", "Panel", "الألواح"),
            new("3 articulés, 60 cm chacun", "3 articulated, 60 cm each", "3 berartikulasi, 60 cm setiap satu", "3 مفصلية، 60 سم لكل منها"));
        AddSpec(pPrivacy.Id,
            new("Hauteur", "Height", "Ketinggian", "الارتفاع"),
            Num("175 cm"));
        AddSpec(pPrivacy.Id,
            new("Toile", "Fabric", "Fabrik", "القماش"),
            new("Polyester lavable 90 °C", "Polyester washable at 90 °C", "Poliester boleh dibasuh 90 °C", "بوليستر قابل للغسل عند 90 °م"));
        AddSpec(pPrivacy.Id,
            new("Structure", "Frame", "Rangka", "الإطار"),
            new("Tubes inox Ø 22 mm", "Stainless tubes Ø 22 mm", "Tiub keluli Ø 22 mm", "أنابيب فولاذية Ø 22 ملم"));
        AddSpec(pPrivacy.Id,
            new("Roulettes", "Casters", "Roda", "العجلات"),
            new("4 dont 2 freinées", "4 (2 braked)", "4 (2 berbrek)", "4 (2 مفرملة)"));
        AddSpec(pPrivacy.Id,
            new("Couleur", "Color", "Warna", "اللون"),
            new("Blanc / bleu médical", "White / medical blue", "Putih / biru perubatan", "أبيض / أزرق طبي"));

        // Équipements respiratoires (cat6)
        var pRespiraCare = NewProduct("respiracare-ventilateur",
            new("RespiraCare Ventilateur", "RespiraCare Ventilator", "Ventilator RespiraCare", "جهاز التنفس RespiraCare"),
            new("Ventilateur de soins intensifs",
                "Intensive care ventilator",
                "Ventilator rawatan rapi",
                "جهاز تنفس للعناية المركزة"),
            new("Le RespiraCare est un ventilateur de soins intensifs offrant tous les modes invasifs et non invasifs (A/C, SIMV, PSV, NIV, APRV). Écran tactile 15\" pour un pilotage clair, monitoring complet et journalisation. Conçu pour USC, USIC et réanimation, conforme ISO 80601-2-12.",
                "The RespiraCare is an intensive care ventilator offering every invasive and non-invasive mode (A/C, SIMV, PSV, NIV, APRV). 15\" touchscreen for clear control, full monitoring and logging. Built for HDU, ICU and resuscitation; ISO 80601-2-12 compliant.",
                "RespiraCare ialah ventilator rawatan rapi yang menawarkan setiap mod invasif dan tidak invasif (A/C, SIMV, PSV, NIV, APRV). Skrin sentuh 15\" untuk kawalan jelas, pemantauan penuh dan pencatatan. Direka untuk HDU, ICU dan resusitasi; mematuhi ISO 80601-2-12.",
                "RespiraCare هو جهاز تنفس للعناية المركزة يوفر جميع الأوضاع الباضعة وغير الباضعة (A/C, SIMV, PSV, NIV, APRV). شاشة لمس 15 بوصة لتحكم واضح، ومراقبة كاملة، وتسجيل. مصمم لوحدات العناية الفائقة والمركزة والإنعاش؛ متوافق مع ISO 80601-2-12."),
            28000, VatRate.Standard, 6, StockStatus.LowStock, false, 0, "respiratory-1");
        AddSpec(pRespiraCare.Id,
            new("Modes", "Modes", "Mod", "الأوضاع"),
            Num("A/C, SIMV, PSV, NIV, APRV"));
        AddSpec(pRespiraCare.Id,
            new("Volume courant", "Tidal volume", "Isi padu tidal", "حجم التيار"),
            Num("20 - 2 000 mL"));
        AddSpec(pRespiraCare.Id,
            new("FiO₂", "FiO₂", "FiO₂", "FiO₂"),
            Num("21 - 100 %"));
        AddSpec(pRespiraCare.Id,
            new("PEEP", "PEEP", "PEEP", "PEEP"),
            Num("0 - 50 cmH₂O"));
        AddSpec(pRespiraCare.Id,
            new("Écran", "Display", "Paparan", "الشاشة"),
            new("15\" tactile", "15\" touchscreen", "Skrin sentuh 15\"", "شاشة لمس 15 بوصة"));
        AddSpec(pRespiraCare.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            Num("ISO 80601-2-12"));

        var pAeroMist = NewProduct("aeromist-nebuliseur",
            new("AeroMist Pro Nébuliseur", "AeroMist Pro nebulizer", "Nebulizer AeroMist Pro", "بخاخ AeroMist Pro"),
            new("Nébuliseur ultrasonique silencieux",
                "Quiet ultrasonic nebulizer",
                "Nebulizer ultrasonik senyap",
                "بخاخ بالموجات فوق الصوتية هادئ"),
            new("L'AeroMist Pro est un nébuliseur ultrasonique 2,4 MHz délivrant des particules à 3,5 µm idéales pour la voie respiratoire basse. Niveau sonore < 35 dB, débit 6 mL/min, réservoir 12 mL — adapté aux soins prolongés en milieu hospitalier comme à domicile.",
                "The AeroMist Pro is a 2.4 MHz ultrasonic nebulizer producing 3.5 µm particles ideal for lower-airway delivery. Below 35 dB noise, 6 mL/min flow, 12 mL reservoir — suited to extended hospital or home-care sessions.",
                "AeroMist Pro ialah nebulizer ultrasonik 2.4 MHz yang menghasilkan zarah 3.5 µm sesuai untuk penghantaran saluran pernafasan bawah. Bunyi di bawah 35 dB, aliran 6 mL/min, takungan 12 mL — sesuai untuk sesi rawatan panjang di hospital atau di rumah.",
                "AeroMist Pro هو بخاخ بالموجات فوق الصوتية بتردد 2.4 ميغاهرتز يُنتج جسيمات بحجم 3.5 ميكرومتر مثالية للممرات الهوائية السفلية. ضوضاء أقل من 35 ديسيبل، تدفق 6 مل/دقيقة، خزان 12 مل — مناسب لجلسات العلاج الطويلة في المستشفى أو المنزل."),
            380, VatRate.Standard, 18, StockStatus.InStock, false, 0, "respiratory-2");
        AddSpec(pAeroMist.Id,
            new("Technologie", "Technology", "Teknologi", "التقنية"),
            new("Ultrasonique 2,4 MHz", "Ultrasonic 2.4 MHz", "Ultrasonik 2.4 MHz", "موجات فوق صوتية 2.4 ميغاهرتز"));
        AddSpec(pAeroMist.Id,
            new("MMAD", "MMAD", "MMAD", "MMAD"),
            Num("3,5 µm"));
        AddSpec(pAeroMist.Id,
            new("Débit", "Flow rate", "Kadar aliran", "معدل التدفق"),
            Num("6 mL/min"));
        AddSpec(pAeroMist.Id,
            new("Capacité réservoir", "Reservoir capacity", "Kapasiti takungan", "سعة الخزان"),
            Num("12 mL"));
        AddSpec(pAeroMist.Id,
            new("Niveau sonore", "Noise level", "Aras bunyi", "مستوى الضوضاء"),
            Num("35 dB"));
        AddSpec(pAeroMist.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("2 ans", "2 years", "2 tahun", "سنتان"));

        var pOxyFlow = NewProduct("oxyflow-concentrateur-portable",
            new("OxyFlow concentrateur 5L portable", "OxyFlow 5L portable concentrator", "Konsentrator mudah alih 5L OxyFlow", "مكثّف الأكسجين المحمول OxyFlow 5 لتر"),
            new("Concentrateur d'oxygène 5L portable",
                "5L portable oxygen concentrator",
                "Konsentrator oksigen mudah alih 5L",
                "مكثّف أكسجين محمول 5 لتر"),
            new("L'OxyFlow est un concentrateur d'oxygène 5 L portable pour oxygénothérapie de longue durée. Modes continu et pulse, autonomie 4 h sur batterie (8 h avec batterie additionnelle), seulement 4,5 kg. Conforme ISO 80601-2-69.",
                "The OxyFlow is a 5 L portable oxygen concentrator for long-term oxygen therapy. Continuous and pulse modes, 4 h battery life (8 h with second battery), just 4.5 kg. ISO 80601-2-69 compliant.",
                "OxyFlow ialah konsentrator oksigen mudah alih 5 L untuk terapi oksigen jangka panjang. Mod berterusan dan denyut, hayat bateri 4 jam (8 jam dengan bateri kedua), hanya 4.5 kg. Mematuhi ISO 80601-2-69.",
                "OxyFlow هو مكثّف أكسجين محمول بسعة 5 لتر للعلاج بالأكسجين على المدى الطويل. أوضاع مستمرة ونبضية، عمر بطارية 4 ساعات (8 ساعات مع بطارية ثانية)، 4.5 كجم فقط. متوافق مع ISO 80601-2-69."),
            1850, VatRate.Standard, 11, StockStatus.InStock, true, 0, "respiratory-3");
        AddSpec(pOxyFlow.Id,
            new("Débit", "Flow rate", "Kadar aliran", "معدل التدفق"),
            Num("0,5 - 5 L/min"));
        AddSpec(pOxyFlow.Id,
            new("Pureté O₂", "O₂ purity", "Ketulenan O₂", "نقاوة الأكسجين"),
            Num("90 - 96 %"));
        AddSpec(pOxyFlow.Id,
            new("Modes", "Modes", "Mod", "الأوضاع"),
            new("Continu + pulse", "Continuous + pulse", "Berterusan + denyut", "مستمر + نبضي"));
        AddSpec(pOxyFlow.Id,
            new("Batterie", "Battery", "Bateri", "البطارية"),
            new("4 h (8 h avec 2nde batt.)", "4 h (8 h with 2nd battery)", "4 jam (8 jam dengan bateri ke-2)", "4 ساعات (8 ساعات مع بطارية ثانية)"));
        AddSpec(pOxyFlow.Id,
            new("Poids", "Weight", "Berat", "الوزن"),
            Num("4,5 kg"));
        AddSpec(pOxyFlow.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            Num("ISO 80601-2-69"));

        var pSilentAir = NewProduct("silentair-masque-cpap",
            new("SilentAir masque CPAP", "SilentAir CPAP mask", "Topeng CPAP SilentAir", "قناع CPAP SilentAir"),
            new("Masque nasal CPAP avec coussin gel",
                "Nasal CPAP mask with gel cushion",
                "Topeng CPAP hidung dengan kusyen gel",
                "قناع CPAP أنفي مع وسادة جل"),
            new("Le masque CPAP SilentAir s'adapte aux patients sous PPC grâce à son coussin gel silicone hypoallergénique et son émission sonore inférieure à 25 dB. Tailles S/M/L incluses pour ajustement immédiat. Lavable à l'eau tiède savonneuse, pièces détachables.",
                "The SilentAir CPAP mask suits patients on CPAP therapy with a hypoallergenic silicone gel cushion and sub-25 dB noise emission. Sizes S/M/L included for immediate fit. Soap-and-water washable, fully serviceable parts.",
                "Topeng CPAP SilentAir sesuai untuk pesakit terapi CPAP dengan kusyen gel silikon hipoalergenik dan pelepasan bunyi di bawah 25 dB. Saiz S/M/L disertakan untuk pemasangan segera. Boleh dibasuh dengan air dan sabun, alat ganti penuh.",
                "قناع SilentAir CPAP يناسب مرضى علاج CPAP بفضل وسادة جل سيليكون مضادة للحساسية وإصدار ضوضاء أقل من 25 ديسيبل. مقاسات S/M/L مرفقة للتركيب الفوري. قابل للغسل بالماء والصابون، قطع غيار كاملة."),
            145, VatRate.Standard, 40, StockStatus.InStock, false, 0, "respiratory-4");
        AddSpec(pSilentAir.Id,
            new("Type", "Type", "Jenis", "النوع"),
            new("Nasal avec coussin gel", "Nasal with gel cushion", "Hidung dengan kusyen gel", "أنفي مع وسادة جل"));
        AddSpec(pSilentAir.Id,
            new("Tailles incluses", "Included sizes", "Saiz disertakan", "المقاسات المضمنة"),
            Num("S, M, L"));
        AddSpec(pSilentAir.Id,
            new("Bruit", "Noise", "Bunyi", "الضوضاء"),
            Num("< 25 dB"));
        AddSpec(pSilentAir.Id,
            new("Matériau", "Material", "Bahan", "المواد"),
            new("Silicone hypoallergénique", "Hypoallergenic silicone", "Silikon hipoalergenik", "سيليكون مضاد للحساسية"));
        AddSpec(pSilentAir.Id,
            new("Entretien", "Maintenance", "Penyelenggaraan", "الصيانة"),
            new("Lavable eau tiède savonneuse", "Washable with warm soapy water", "Boleh dibasuh dengan air sabun suam", "قابل للغسل بالماء الدافئ والصابون"));
        AddSpec(pSilentAir.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("1 an", "1 year", "1 tahun", "سنة واحدة"));

        // Consommables médicaux (cat7) — Reduced VAT (5.5%)
        var pGantsNitrile = NewProduct("gants-nitrile",
            new("Gants Nitrile", "Nitrile Gloves", "Sarung Tangan Nitril", "قفازات نتريل"),
            new("Gants d'examen en nitrile non poudrés",
                "Powder-free nitrile examination gloves",
                "Sarung tangan pemeriksaan nitril bebas serbuk",
                "قفازات فحص من النتريل خالية من البودرة"),
            new("Les gants Nitrile non poudrés bleu cobalt assurent une protection fiable aux professionnels de santé sans risque allergène au latex. AQL 1,5 et conformité EN 455 / EN 374 garantissent les performances en milieu clinique. Boîte de 100 unités, tailles S, M, L, XL.",
                "Powder-free cobalt-blue nitrile gloves deliver dependable protection without latex allergy risk. AQL 1.5 and EN 455 / EN 374 compliance guarantee clinical performance. Box of 100 units, sizes S, M, L, XL.",
                "Sarung tangan nitril biru kobalt bebas serbuk memberikan perlindungan boleh dipercayai tanpa risiko alahan lateks. AQL 1.5 dan pematuhan EN 455 / EN 374 menjamin prestasi klinikal. Kotak 100 unit, saiz S, M, L, XL.",
                "قفازات النتريل الزرقاء الكوبالتية الخالية من البودرة توفر حماية موثوقة بدون مخاطر حساسية اللاتكس. مستوى الجودة المقبول AQL 1.5 والتوافق مع EN 455 / EN 374 يضمنان الأداء السريري. علبة 100 وحدة، مقاسات S، M، L، XL."),
            12, VatRate.Reduced, 500, StockStatus.InStock, false, 0, "consumables-1");
        AddSpec(pGantsNitrile.Id,
            new("Matière", "Material", "Bahan", "المواد"),
            new("Nitrile non poudré", "Powder-free nitrile", "Nitril bebas serbuk", "نتريل خالٍ من البودرة"));
        AddSpec(pGantsNitrile.Id,
            new("Conditionnement", "Packaging", "Pembungkusan", "التعبئة"),
            new("Boîte de 100 unités", "Box of 100 units", "Kotak 100 unit", "علبة 100 وحدة"));
        AddSpec(pGantsNitrile.Id,
            new("Tailles disponibles", "Available sizes", "Saiz tersedia", "المقاسات المتاحة"),
            Num("S, M, L, XL"));
        AddSpec(pGantsNitrile.Id,
            new("AQL", "AQL", "AQL", "AQL"),
            Num("1,5"));
        AddSpec(pGantsNitrile.Id,
            new("Normes", "Standards", "Piawaian", "المعايير"),
            Num("EN 455, EN 374"));
        AddSpec(pGantsNitrile.Id,
            new("Couleur", "Color", "Warna", "اللون"),
            new("Bleu cobalt", "Cobalt blue", "Biru kobalt", "أزرق كوبالت"));

        var pCompresses = NewProduct("compresses-steriles-10x10",
            new("Compresses stériles 10×10 cm", "Sterile gauze 10×10 cm", "Kain kasa steril 10×10 cm", "شاش معقّم 10×10 سم"),
            new("Boîte de 100 compresses stériles 10×10",
                "Box of 100 sterile 10×10 gauze pads",
                "Kotak 100 pad kain kasa steril 10×10",
                "علبة 100 ضمادة شاش معقّمة 10×10"),
            new("Compresses stériles 10 × 10 cm en non-tissé viscose/polyester 30 g/m² à 12 plis, idéales pour le nettoyage de plaies et le pansement. Stérilisées à l'oxyde d'éthylène, conditionnement individuel par sachet. Conformes NF S 90-401.",
                "Sterile 10 × 10 cm gauze pads in 30 g/m² viscose/polyester non-woven, 12 plies — perfect for wound cleaning and dressing. EtO-sterilized, individually pouched. NF S 90-401 compliant.",
                "Pad kain kasa steril 10 × 10 cm dalam bukan tenunan viskos/poliester 30 g/m², 12 lapisan — sempurna untuk pembersihan luka dan pembalutan. Disterilkan dengan EtO, dibungkus individu. Mematuhi NF S 90-401.",
                "ضمادات شاش معقّمة 10 × 10 سم من غير المنسوج فيسكوز/بوليستر 30 جم/م²، 12 طبقة — مثالية لتنظيف الجروح والضمادات. معقّمة بأكسيد الإثيلين، مغلّفة فردياً. متوافقة مع NF S 90-401."),
            28, VatRate.Reduced, 350, StockStatus.InStock, false, 0, "consumables-2");
        AddSpec(pCompresses.Id,
            new("Format", "Format", "Format", "المقاس"),
            Num("10 × 10 cm"));
        AddSpec(pCompresses.Id,
            new("Composition", "Composition", "Komposisi", "التركيب"),
            new("Viscose / polyester 30 g/m²", "Viscose / polyester 30 g/m²", "Viskos / poliester 30 g/m²", "فيسكوز / بوليستر 30 جم/م²"));
        AddSpec(pCompresses.Id,
            new("Plis", "Plies", "Lapisan", "الطبقات"),
            Num("12"));
        AddSpec(pCompresses.Id,
            new("Conditionnement", "Packaging", "Pembungkusan", "التعبئة"),
            new("100 sachets individuels", "100 individual pouches", "100 beg individu", "100 كيس فردي"));
        AddSpec(pCompresses.Id,
            new("Stérilisation", "Sterilization", "Pensterilan", "التعقيم"),
            new("Oxyde d'éthylène", "Ethylene oxide", "Oksida etilena", "أكسيد الإثيلين"));
        AddSpec(pCompresses.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            Num("NF S 90-401"));

        var pVelpeau = NewProduct("bandes-velpeau-10cm",
            new("Bandes Velpeau 10 cm", "Velpeau bandages 10 cm", "Pembalut Velpeau 10 cm", "ضمادات Velpeau 10 سم"),
            new("Bandes élastiques cohésives 10 cm × 4 m",
                "Cohesive elastic bandages 10 cm × 4 m",
                "Pembalut elastik kohesif 10 cm × 4 m",
                "ضمادات مرنة لاصقة 10 سم × 4 م"),
            new("Les bandes Velpeau cohésives 10 cm × 4 m sans latex maintiennent pansements et attelles sans agrafe ni adhésif sur la peau. Composition coton / élasthanne, dix rouleaux par boîte couleur chair. Adaptées aux soins infirmiers et à la traumatologie sportive.",
                "Latex-free 10 cm × 4 m Velpeau cohesive bandages secure dressings and splints without clips or skin adhesives. Cotton / elastane blend, ten rolls per box, skin tone. Suited to nursing care and sports trauma.",
                "Pembalut kohesif Velpeau 10 cm × 4 m bebas lateks memastikan pembalut dan splin tanpa klip atau pelekat pada kulit. Campuran kapas / elastana, sepuluh gulung setiap kotak, warna kulit. Sesuai untuk penjagaan kejururawatan dan trauma sukan.",
                "ضمادات Velpeau اللاصقة 10 سم × 4 م الخالية من اللاتكس تثبّت الضمادات والجبائر بدون مشابك أو لواصق على الجلد. مزيج قطن / إيلاستان، عشر لفات في العلبة، لون البشرة. مناسبة لرعاية التمريض وإصابات الرياضة."),
            12, VatRate.Reduced, 220, StockStatus.InStock, false, 0, "consumables-3");
        AddSpec(pVelpeau.Id,
            new("Largeur", "Width", "Lebar", "العرض"),
            Num("10 cm"));
        AddSpec(pVelpeau.Id,
            new("Longueur (étirée)", "Length (stretched)", "Panjang (diregang)", "الطول (ممدود)"),
            Num("4 m"));
        AddSpec(pVelpeau.Id,
            new("Élasticité", "Elasticity", "Keanjalan", "المرونة"),
            new("Cohésive auto-adhésive", "Self-adhesive cohesive", "Lekat sendiri kohesif", "ذاتية اللصق متماسكة"));
        AddSpec(pVelpeau.Id,
            new("Composition", "Composition", "Komposisi", "التركيب"),
            new("Coton + élasthanne, sans latex", "Cotton + elastane, latex-free", "Kapas + elastana, bebas lateks", "قطن + إيلاستان، خالٍ من اللاتكس"));
        AddSpec(pVelpeau.Id,
            new("Conditionnement", "Packaging", "Pembungkusan", "التعبئة"),
            new("10 rouleaux", "10 rolls", "10 gulung", "10 لفات"));
        AddSpec(pVelpeau.Id,
            new("Couleur", "Color", "Warna", "اللون"),
            new("Chair", "Skin tone", "Warna kulit", "لون البشرة"));

        var pSeringues = NewProduct("seringues-5ml-boite100",
            new("Seringues 5 ml stériles boîte 100", "5 ml syringes box of 100", "Picagari 5 ml kotak 100", "حقن 5 مل علبة 100"),
            new("Boîte de 100 seringues 5 ml stériles",
                "Box of 100 sterile 5 ml syringes",
                "Kotak 100 picagari steril 5 ml",
                "علبة 100 حقنة معقّمة 5 مل"),
            new("Seringues 5 mL à embase Luer Lock pour fixation sécurisée des aiguilles. Graduation 0,2 mL lisible, stérilisation oxyde d'éthylène, conditionnement individuel blistérisé. Boîte de 100 unités conformes EN ISO 7886-1.",
                "5 mL Luer Lock syringes for secure needle attachment. Clear 0.2 mL graduation, EtO-sterilized, individually blister-packed. Box of 100 units, EN ISO 7886-1 compliant.",
                "Picagari Luer Lock 5 mL untuk pemasangan jarum yang selamat. Pengredan 0.2 mL jelas, disterilkan dengan EtO, dibungkus blister individu. Kotak 100 unit, mematuhi EN ISO 7886-1.",
                "حقن 5 مل بقاعدة Luer Lock لتثبيت آمن للإبر. تدرّج واضح 0.2 مل، معقّمة بأكسيد الإثيلين، مغلّفة بشكل فردي. علبة 100 وحدة، متوافقة مع EN ISO 7886-1."),
            18, VatRate.Reduced, 60, StockStatus.LowStock, false, 0, "consumables-4");
        AddSpec(pSeringues.Id,
            new("Volume", "Volume", "Isi padu", "الحجم"),
            Num("5 mL"));
        AddSpec(pSeringues.Id,
            new("Embase", "Fitting", "Pemasangan", "الوصلة"),
            Num("Luer Lock"));
        AddSpec(pSeringues.Id,
            new("Graduation", "Graduation", "Pengredan", "التدرّج"),
            Num("0,2 mL"));
        AddSpec(pSeringues.Id,
            new("Stérilisation", "Sterilization", "Pensterilan", "التعقيم"),
            new("Oxyde d'éthylène", "Ethylene oxide", "Oksida etilena", "أكسيد الإثيلين"));
        AddSpec(pSeringues.Id,
            new("Conditionnement", "Packaging", "Pembungkusan", "التعبئة"),
            new("100 unités blistérisées", "100 blister-packed units", "100 unit blister", "100 وحدة بتغليف بليستر"));
        AddSpec(pSeringues.Id,
            new("Norme", "Standard", "Piawaian", "المعيار"),
            Num("EN ISO 7886-1"));

        // Équipements de laboratoire (cat8)
        var pBioAnalyzer = NewProduct("bioanalyzer",
            new("BioAnalyzer", "BioAnalyzer", "BioAnalyzer", "BioAnalyzer"),
            new("Analyseur biochimique automatique",
                "Automatic biochemistry analyzer",
                "Penganalisis biokimia automatik",
                "محلّل كيمياء حيوية أوتوماتيكي"),
            new("Le BioAnalyzer automatise 60 paramètres biochimiques et immunologiques à raison de 240 tests par heure, avec seulement 5 µL d'échantillon. Réfrigération 2-8 °C intégrée, connexion LIS bidirectionnelle, dilution automatique. Pensé pour les laboratoires d'analyses de moyenne capacité.",
                "The BioAnalyzer automates 60 biochemistry and immunology parameters at 240 tests per hour using just 5 µL of sample. Built-in 2-8 °C refrigeration, bidirectional LIS connection, auto-dilution. Tailored to mid-volume clinical labs.",
                "BioAnalyzer mengautomasi 60 parameter biokimia dan imunologi pada 240 ujian sejam menggunakan hanya 5 µL sampel. Penyejukan terbina dalam 2-8 °C, sambungan LIS dua hala, pencairan automatik. Sesuai untuk makmal klinikal isi padu sederhana.",
                "يُؤتمت BioAnalyzer 60 معيار كيمياء حيوية ومناعة بمعدل 240 اختبار في الساعة باستخدام 5 ميكرولتر فقط من العينة. تبريد مدمج 2-8 °م، اتصال LIS ثنائي الاتجاه، تخفيف تلقائي. مصمم لمختبرات سريرية متوسطة الحجم."),
            65000, VatRate.Standard, 3, StockStatus.LowStock, true, 4, "laboratory-1");
        AddSpec(pBioAnalyzer.Id,
            new("Cadence", "Throughput", "Daya pengeluaran", "الإنتاجية"),
            new("240 tests / heure", "240 tests / hour", "240 ujian / jam", "240 اختبار / ساعة"));
        AddSpec(pBioAnalyzer.Id,
            new("Paramètres", "Parameters", "Parameter", "المعايير"),
            new("60 (chimie + immuno)", "60 (chemistry + immuno)", "60 (kimia + imuno)", "60 (كيمياء + مناعة)"));
        AddSpec(pBioAnalyzer.Id,
            new("Volume échantillon", "Sample volume", "Isi padu sampel", "حجم العينة"),
            new("5 µL minimum", "5 µL minimum", "5 µL minimum", "5 ميكرولتر كحد أدنى"));
        AddSpec(pBioAnalyzer.Id,
            new("Réfrigération", "Refrigeration", "Penyejukan", "التبريد"),
            new("2 - 8 °C intégrée", "2 - 8 °C built-in", "2 - 8 °C terbina dalam", "2 - 8 °م مدمج"));
        AddSpec(pBioAnalyzer.Id,
            new("Connectique", "Connectivity", "Sambungan", "التوصيلات"),
            new("LIS bidirectionnel", "Bidirectional LIS", "LIS dua hala", "LIS ثنائي الاتجاه"));
        AddSpec(pBioAnalyzer.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("5 ans", "5 years", "5 tahun", "5 سنوات"));

        var pSpinPro = NewProduct("spinpro-centrifugeuse",
            new("SpinPro Centrifugeuse 6 tubes", "SpinPro 6-tube centrifuge", "Empar SpinPro 6 tiub", "جهاز طرد مركزي SpinPro بـ 6 أنابيب"),
            new("Centrifugeuse de paillasse 6 tubes",
                "6-tube benchtop centrifuge",
                "Empar meja 6 tiub",
                "جهاز طرد مركزي مكتبي بـ 6 أنابيب"),
            new("La centrifugeuse SpinPro accepte 6 tubes de 15 mL jusqu'à 4 000 tr/min avec minuteur 1-99 min. Rotor amovible autoclavable pour décontamination, détection de déséquilibre et capot ouvert pour la sécurité. Compacte, idéale pour la paillasse et les laboratoires de proximité.",
                "The SpinPro centrifuge handles six 15 mL tubes up to 4,000 rpm with a 1-99 min timer. Removable autoclavable rotor for decontamination, imbalance and open-lid detection for safety. Compact — ideal for benchtop use and community labs.",
                "Empar SpinPro mengendalikan enam tiub 15 mL sehingga 4,000 rpm dengan pemasa 1-99 min. Rotor boleh tanggal yang boleh diautoklaf untuk dekontaminasi, pengesanan ketidakseimbangan dan penutup terbuka untuk keselamatan. Padat — sesuai untuk kegunaan meja dan makmal komuniti.",
                "يتعامل جهاز الطرد المركزي SpinPro مع ستة أنابيب 15 مل حتى 4,000 دورة/دقيقة بمؤقّت 1-99 دقيقة. دوّار قابل للإزالة وقابل للتعقيم للتطهير، كشف عدم التوازن والغطاء المفتوح للأمان. مدمج — مثالي للاستخدام المكتبي والمختبرات المجتمعية."),
            1850, VatRate.Standard, 10, StockStatus.InStock, false, 0, "laboratory-2");
        AddSpec(pSpinPro.Id,
            new("Vitesse", "Speed", "Kelajuan", "السرعة"),
            Num("0 - 4 000 tr/min"));
        AddSpec(pSpinPro.Id,
            new("Capacité", "Capacity", "Kapasiti", "السعة"),
            Num("6 × 15 mL"));
        AddSpec(pSpinPro.Id,
            new("Minuteur", "Timer", "Pemasa", "المؤقّت"),
            Num("1 - 99 min"));
        AddSpec(pSpinPro.Id,
            new("Rotor", "Rotor", "Rotor", "الدوّار"),
            new("Amovible et autoclavable", "Removable and autoclavable", "Boleh tanggal dan diautoklaf", "قابل للإزالة وللتعقيم"));
        AddSpec(pSpinPro.Id,
            new("Sécurité", "Safety", "Keselamatan", "الأمان"),
            new("Détection déséquilibre + capot ouvert", "Imbalance + open-lid detection", "Pengesanan ketidakseimbangan + penutup terbuka", "كشف عدم التوازن + الغطاء المفتوح"));
        AddSpec(pSpinPro.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("3 ans", "3 years", "3 tahun", "3 سنوات"));

        var pOptiView = NewProduct("optiview-microscope-1000x",
            new("OptiView Microscope 1000×", "OptiView 1000× microscope", "Mikroskop OptiView 1000×", "مجهر OptiView بقوة 1000×"),
            new("Microscope binoculaire 40-1000×",
                "40-1000× binocular microscope",
                "Mikroskop binokular 40-1000×",
                "مجهر ثنائي العين 40-1000×"),
            new("Le microscope OptiView propose 4 grossissements (40×, 100×, 400×, 1 000×) avec objectifs plan-achromatiques. Éclairage LED Köhler 3 W pour un champ homogène, tête binoculaire 30° rotative et platine mécanique à mouvements coaxiaux. Pour laboratoire de routine et formation universitaire.",
                "The OptiView microscope offers 4 magnifications (40×, 100×, 400×, 1,000×) with plan-achromatic objectives. 3 W Köhler LED illumination delivers an even field; 30° rotating binocular head and mechanical stage with coaxial controls. For routine lab work and university teaching.",
                "Mikroskop OptiView menawarkan 4 pembesaran (40×, 100×, 400×, 1,000×) dengan kanta objektif plan-akromatik. Pencahayaan LED Köhler 3 W memberikan medan yang sekata; kepala binokular berputar 30° dan pentas mekanikal dengan kawalan koaksial. Untuk kerja makmal rutin dan pengajaran universiti.",
                "يوفر مجهر OptiView 4 تكبيرات (40×، 100×، 400×، 1,000×) مع عدسات شيئية مستوية-لونية. إضاءة LED Köhler بقوة 3 واط توفر مجالاً متجانساً؛ رأس ثنائية العين دوّارة 30° ومرحلة ميكانيكية بمحاور متمحورة. لأعمال المختبرات الروتينية والتدريس الجامعي."),
            1250, VatRate.Standard, 16, StockStatus.InStock, false, 0, "laboratory-3");
        AddSpec(pOptiView.Id,
            new("Grossissements", "Magnifications", "Pembesaran", "التكبيرات"),
            Num("40×, 100×, 400×, 1 000×"));
        AddSpec(pOptiView.Id,
            new("Tête", "Head", "Kepala", "الرأس"),
            new("Binoculaire 30°, rotative", "30° rotating binocular", "Binokular 30°, berputar", "ثنائية العين 30°، دوّارة"));
        AddSpec(pOptiView.Id,
            new("Éclairage", "Illumination", "Pencahayaan", "الإضاءة"),
            new("LED Köhler 3 W", "3 W Köhler LED", "LED Köhler 3 W", "LED Köhler 3 واط"));
        AddSpec(pOptiView.Id,
            new("Objectifs", "Objectives", "Kanta objektif", "العدسات الشيئية"),
            new("Plan-achromatiques", "Plan-achromatic", "Plan-akromatik", "مستوية-لونية"));
        AddSpec(pOptiView.Id,
            new("Platine", "Stage", "Pentas", "المرحلة"),
            new("Mécanique avec coaxiaux", "Mechanical with coaxial controls", "Mekanikal dengan kawalan koaksial", "ميكانيكية بمحاور متمحورة"));
        AddSpec(pOptiView.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("3 ans", "3 years", "3 tahun", "3 سنوات"));

        var pSpectraLab = NewProduct("spectralab-uv-vis",
            new("SpectraLab UV-Vis", "SpectraLab UV-Vis", "SpectraLab UV-Vis", "SpectraLab UV-Vis"),
            new("Spectrophotomètre UV-Visible 190-1100 nm",
                "190-1100 nm UV-Vis spectrophotometer",
                "Spektrofotometer UV-Vis 190-1100 nm",
                "مطياف ضوئي UV-Vis 190-1100 نانومتر"),
            new("Le SpectraLab UV-Vis est un spectrophotomètre à double faisceau couvrant 190-1 100 nm avec une bande passante de 2 nm. Précision photométrique ± 0,3 % T, connectique USB et sortie analogique pour intégration LIMS. Adapté à la chimie analytique et au contrôle qualité.",
                "The SpectraLab UV-Vis is a dual-beam spectrophotometer covering 190-1,100 nm with 2 nm bandwidth. ±0.3 % T photometric accuracy, USB and analog outputs for LIMS integration. Suited to analytical chemistry and QC labs.",
                "SpectraLab UV-Vis ialah spektrofotometer dwi-rasuk meliputi 190-1,100 nm dengan lebar jalur 2 nm. Ketepatan fotometrik ±0.3 % T, output USB dan analog untuk integrasi LIMS. Sesuai untuk kimia analisis dan makmal QC.",
                "SpectraLab UV-Vis هو مطياف ضوئي ثنائي الحزمة يغطي 190-1,100 نانومتر مع عرض نطاق 2 نانومتر. دقة قياس ضوئي ±0.3 % T، مخارج USB وتناظرية لتكامل LIMS. مناسب للكيمياء التحليلية ومختبرات مراقبة الجودة."),
            4500, VatRate.Standard, 4, StockStatus.LowStock, false, 0, "laboratory-4");
        AddSpec(pSpectraLab.Id,
            new("Plage spectrale", "Spectral range", "Julat spektrum", "النطاق الطيفي"),
            Num("190 - 1 100 nm"));
        AddSpec(pSpectraLab.Id,
            new("Bande passante", "Bandwidth", "Lebar jalur", "عرض النطاق"),
            Num("2 nm"));
        AddSpec(pSpectraLab.Id,
            new("Faisceau", "Beam", "Rasuk", "الحزمة"),
            new("Double", "Dual", "Dwi", "ثنائي"));
        AddSpec(pSpectraLab.Id,
            new("Précision photométrique", "Photometric accuracy", "Ketepatan fotometrik", "دقة القياس الضوئي"),
            Num("± 0,3 % T"));
        AddSpec(pSpectraLab.Id,
            new("Connectique", "Connectivity", "Sambungan", "التوصيلات"),
            new("USB, sortie analogique", "USB, analog output", "USB, output analog", "USB، مخرج تناظري"));
        AddSpec(pSpectraLab.Id,
            new("Garantie", "Warranty", "Jaminan", "الضمان"),
            new("2 ans", "2 years", "2 tahun", "سنتان"));

        var allProducts = new[]
        {
            pProScan, pEchoView, pMagniScan, pDentalView,
            pVitalGuard, pPressureTrack, pPulseOx, pCardioTrace,
            pSteriliPro, pSterilWash, pSoniClean, pBactoCide,
            pPrecisionCut, pChirurgPro, pMayoSet, pKingVision,
            pFlexiBed, pMedCart, pErgoSit, pPrivacy,
            pRespiraCare, pAeroMist, pOxyFlow, pSilentAir,
            pGantsNitrile, pCompresses, pVelpeau, pSeringues,
            pBioAnalyzer, pSpinPro, pOptiView, pSpectraLab,
        };
        context.Products.AddRange(allProducts);
        context.ProductSpecs.AddRange(productSpecs);

        // Product-Category links (4 per category, in order).
        var pcLinks = new List<ProductCategory>();
        var catIds = new[] { cat1Id, cat2Id, cat3Id, cat4Id, cat5Id, cat6Id, cat7Id, cat8Id };
        for (int i = 0; i < allProducts.Length; i++)
        {
            pcLinks.Add(new ProductCategory
            {
                ProductId = allProducts[i].Id,
                CategoryId = catIds[i / 4], // 4 products per category, in order
            });
        }
        context.ProductCategories.AddRange(pcLinks);

        // ── Orders ───────────────────────────────────────
        // 20 orders spread across the past 7 days. Distribution:
        //   • 2 Pending (just placed, payment not cleared yet)
        //   • 3 Confirmed (paid, waiting to be processed)
        //   • 3 Processing (being picked/packed)
        //   • 4 Shipped (carrier picked up)
        //   • 5 Delivered (oldest)
        //   • 2 Cancelled (one customer-initiated, one payment failed)
        //   • 1 Returned (delivered then returned)
        // Customer c2 is suspended → no orders for them.
        // Customer c10 is brand-new + unconfirmed → no orders.

        var orders = new List<Order>();
        var orderItems = new List<OrderItem>();
        var statusChanges = new List<OrderStatusChange>();

        // Helper to wire up an order + items + status history in one go.
        void AddOrder(
            User user, Address billing, Address? shipping,
            OrderStatus status, PaymentStatus pay, PaymentMethod method,
            ShippingMethod ship, decimal shipCost,
            DateTime placedAt,
            params (Product Product, int Qty)[] items)
        {
            var orderId = Guid.NewGuid();
            shipping ??= billing;
            var order = new Order
            {
                Id = orderId,
                UserId = user.Id,
                Date = placedAt,
                Status = status,
                PaymentStatus = pay,
                PaymentMethod = method,
                BillingAddressId = billing.Id,
                ShippingAddressId = shipping.Id,
                ShippingMethod = ship,
                ShippingCost = shipCost,
                CreatedAt = placedAt,
                UpdatedAt = placedAt,
                StripePaymentIntentId = method == PaymentMethod.Card ? "pi_demo_" + orderId.ToString("N")[..16] : null,
                StripePaymentStatus = pay switch
                {
                    PaymentStatus.Validated => "succeeded",
                    PaymentStatus.Failed => "requires_payment_method",
                    PaymentStatus.Refunded => "succeeded",
                    _ => "requires_payment_method",
                },
            };
            orders.Add(order);

            foreach (var (p, q) in items)
            {
                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = p.Id,
                    ProductNameFr = p.NameFr,
                    ProductNameEn = p.NameEn,
                    Quantity = q,
                    PriceHT = p.PriceHT,
                    VatRate = p.VatRate,
                });
            }

            // Synthetic status history: every transition from Pending to the
            // current status with a one-hour gap. Good enough for the admin
            // timeline UI to show motion.
            var transitions = StatusPath(status);
            for (int i = 0; i < transitions.Length - 1; i++)
            {
                statusChanges.Add(new OrderStatusChange
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    From = transitions[i],
                    To = transitions[i + 1],
                    Date = placedAt.AddHours(i + 1),
                    UserId = adminId,
                });
            }
        }

        var c1Addr = addresses.First(a => a.UserId == c1.Id);
        var c3Cabinet = addresses.First(a => a.UserId == c3.Id && a.IsDefault);
        var c3Billing = addresses.First(a => a.UserId == c3.Id && !a.IsDefault);
        var c4Main = addresses.First(a => a.UserId == c4.Id && a.IsDefault);
        var c4Annex = addresses.First(a => a.UserId == c4.Id && !a.IsDefault);
        var c5Addr = addresses.First(a => a.UserId == c5.Id);
        var c6Addr = addresses.First(a => a.UserId == c6.Id);
        var c7Main = addresses.First(a => a.UserId == c7.Id && a.IsDefault);
        var c8Addr = addresses.First(a => a.UserId == c8.Id);
        var c9Addr = addresses.First(a => a.UserId == c9.Id);

        // -- Delivered (5) -----------------------------------
        AddOrder(c4, c4Main, null, OrderStatus.Delivered, PaymentStatus.Validated, PaymentMethod.BankTransfer,
            ShippingMethod.Standard, 15, now.AddDays(-7),
            (pVitalGuard, 4), (pPulseOx, 10), (pGantsNitrile, 20));

        AddOrder(c5, c5Addr, null, OrderStatus.Delivered, PaymentStatus.Validated, PaymentMethod.Card,
            ShippingMethod.Express, 35, now.AddDays(-7).AddHours(4),
            (pSpinPro, 1), (pCompresses, 5));

        AddOrder(c1, c1Addr, null, OrderStatus.Delivered, PaymentStatus.Validated, PaymentMethod.Card,
            ShippingMethod.Standard, 15, now.AddDays(-6),
            (pPrecisionCut, 2), (pMayoSet, 4), (pGantsNitrile, 6));

        AddOrder(c7, c7Main, null, OrderStatus.Delivered, PaymentStatus.Validated, PaymentMethod.BankTransfer,
            ShippingMethod.Standard, 15, now.AddDays(-6).AddHours(2),
            (pFlexiBed, 2), (pErgoSit, 4));

        AddOrder(c8, c8Addr, null, OrderStatus.Delivered, PaymentStatus.Validated, PaymentMethod.Card,
            ShippingMethod.Express, 35, now.AddDays(-5),
            (pCardioTrace, 1), (pPulseOx, 5));

        // -- Returned (1) ------------------------------------
        AddOrder(c6, c6Addr, null, OrderStatus.Returned, PaymentStatus.Refunded, PaymentMethod.Card,
            ShippingMethod.Standard, 15, now.AddDays(-5).AddHours(6),
            (pSoniClean, 1), (pBactoCide, 2));

        // -- Shipped (4) -------------------------------------
        AddOrder(c4, c4Annex, null, OrderStatus.Shipped, PaymentStatus.Validated, PaymentMethod.BankTransfer,
            ShippingMethod.Standard, 15, now.AddDays(-4),
            (pSteriliPro, 1), (pBactoCide, 4));

        AddOrder(c3, c3Billing, c3Cabinet, OrderStatus.Shipped, PaymentStatus.Validated, PaymentMethod.Card,
            ShippingMethod.Express, 35, now.AddDays(-4).AddHours(3),
            (pVitalGuard, 1), (pPressureTrack, 3));

        AddOrder(c9, c9Addr, null, OrderStatus.Shipped, PaymentStatus.Validated, PaymentMethod.Card,
            ShippingMethod.Standard, 15, now.AddDays(-3),
            (pErgoSit, 2), (pCompresses, 8), (pVelpeau, 10));

        AddOrder(c5, c5Addr, null, OrderStatus.Shipped, PaymentStatus.Validated, PaymentMethod.BankTransfer,
            ShippingMethod.Overnight, 75, now.AddDays(-3).AddHours(5),
            (pOptiView, 1), (pSeringues, 12));

        // -- Processing (3) ----------------------------------
        AddOrder(c1, c1Addr, null, OrderStatus.Processing, PaymentStatus.Validated, PaymentMethod.Card,
            ShippingMethod.Standard, 15, now.AddDays(-2).AddHours(2),
            (pDentalView, 1));

        AddOrder(c7, c7Main, null, OrderStatus.Processing, PaymentStatus.Validated, PaymentMethod.BankTransfer,
            ShippingMethod.Standard, 15, now.AddDays(-2).AddHours(8),
            (pFlexiBed, 1), (pPrivacy, 3), (pMedCart, 1));

        AddOrder(c8, c8Addr, null, OrderStatus.Processing, PaymentStatus.Validated, PaymentMethod.Card,
            ShippingMethod.Express, 35, now.AddDays(-2).AddHours(14),
            (pKingVision, 1));

        // -- Confirmed (3) -----------------------------------
        AddOrder(c5, c5Addr, null, OrderStatus.Confirmed, PaymentStatus.Validated, PaymentMethod.BankTransfer,
            ShippingMethod.Standard, 15, now.AddDays(-1).AddHours(1),
            (pBioAnalyzer, 1));

        AddOrder(c3, c3Billing, c3Cabinet, OrderStatus.Confirmed, PaymentStatus.Validated, PaymentMethod.Card,
            ShippingMethod.Express, 35, now.AddDays(-1).AddHours(5),
            (pEchoView, 1), (pPulseOx, 4));

        AddOrder(c6, c6Addr, null, OrderStatus.Confirmed, PaymentStatus.Validated, PaymentMethod.Card,
            ShippingMethod.Standard, 15, now.AddDays(-1).AddHours(9),
            (pAeroMist, 2), (pSilentAir, 5));

        // -- Pending (2) -------------------------------------
        AddOrder(c4, c4Main, null, OrderStatus.Pending, PaymentStatus.Pending, PaymentMethod.BankTransfer,
            ShippingMethod.Standard, 15, now.AddHours(-18),
            (pRespiraCare, 1), (pOxyFlow, 2));

        AddOrder(c9, c9Addr, null, OrderStatus.Pending, PaymentStatus.Pending, PaymentMethod.Card,
            ShippingMethod.Standard, 15, now.AddHours(-3),
            (pCompresses, 3), (pVelpeau, 5), (pGantsNitrile, 4));

        // -- Cancelled (2) -----------------------------------
        // 1: customer cancelled before payment
        AddOrder(c1, c1Addr, null, OrderStatus.Cancelled, PaymentStatus.Pending, PaymentMethod.Card,
            ShippingMethod.Standard, 15, now.AddDays(-3).AddHours(10),
            (pSpectraLab, 1));

        // 2: payment failed (Stripe 3-DS challenge timed out)
        AddOrder(c8, c8Addr, null, OrderStatus.Cancelled, PaymentStatus.Failed, PaymentMethod.Card,
            ShippingMethod.Express, 35, now.AddDays(-2).AddHours(20),
            (pMagniScan, 1));

        context.Orders.AddRange(orders);
        context.OrderItems.AddRange(orderItems);
        context.OrderStatusChanges.AddRange(statusChanges);

        // ── Contact messages ─────────────────────────────
        // Visitors (no account) writing through the public contact form.
        // Mix of statuses + one that will be escalated to a ticket below.
        var contactEscalateId = Guid.NewGuid();
        var contactMessages = new List<ContactMessage>
        {
            new() { Id = Guid.NewGuid(), Email = "contact@cabinetduvieuxport.fr",
                Subject = "Demande de devis — 3 autoclaves",
                Message = "Bonjour, nous équipons un nouveau cabinet dentaire à Marseille (3 fauteuils). Pourriez-vous nous transmettre un devis pour 3 SteriliPro Autoclave avec installation ? Merci.",
                Status = MessageStatus.Treated, CreatedAt = now.AddDays(-6).AddHours(3) },

            new() { Id = Guid.NewGuid(), Email = "responsable.achats@ch-loire.fr",
                Subject = "Disponibilité MagniScan IRM",
                Message = "Quels sont vos délais de livraison sur l'IRM compact 0.5T ? Nous serions intéressés pour un hôpital de proximité dans la Loire.",
                Status = MessageStatus.Read, CreatedAt = now.AddDays(-4).AddHours(7) },

            new() { Id = Guid.NewGuid(), Email = "j.dubois@infirmier-libre.fr",
                Subject = "Erreur de facturation",
                Message = "Ma dernière facture indique 6 boîtes de gants alors que j'en ai commandé 3. Pouvez-vous corriger ?",
                Status = MessageStatus.Treated, CreatedAt = now.AddDays(-3).AddHours(5) },

            new() { Id = contactEscalateId, Email = "direction@laboanalyse-sud.fr",
                Subject = "Problème centrifugeuse SpinPro",
                Message = "Notre SpinPro reçue il y a 10 jours déclenche systématiquement une alarme de déséquilibre. Pouvez-vous nous mettre en relation avec votre SAV ?",
                Status = MessageStatus.Read, CreatedAt = now.AddDays(-2).AddHours(10) },

            new() { Id = Guid.NewGuid(), Email = "info@pharmaroselyne.fr",
                Subject = "Catalogue 2026 papier",
                Message = "Bonjour, est-il possible de recevoir votre catalogue 2026 en version imprimée ? Adresse : 4 rue de la Paix, 75002 Paris. Merci !",
                Status = MessageStatus.Unread, CreatedAt = now.AddDays(-1).AddHours(2) },

            new() { Id = Guid.NewGuid(), Email = "achats@maisonretraite-vert-pre.fr",
                Subject = "Question gants nitrile taille XL",
                Message = "Les gants nitrile sont-ils disponibles en taille XL ? Nos soignants en demandent.",
                Status = MessageStatus.Unread, CreatedAt = now.AddHours(-6) },
        };
        context.ContactMessages.AddRange(contactMessages);

        // ── Chat conversations ───────────────────────────
        // Four conversations: two simple Q&A, one escalated to a ticket,
        // one ongoing. Messages alternate User → Bot.
        var conv1Id = Guid.NewGuid();
        var conv2Id = Guid.NewGuid();
        var conv3Id = Guid.NewGuid();
        var conv4Id = Guid.NewGuid();

        // conv3 will be escalated → ticket
        var conv3TicketId = Guid.NewGuid();

        var conversations = new List<ChatConversation>
        {
            new() { Id = conv1Id, UserId = c1.Id, Email = c1.Email, Escalated = false,
                CreatedAt = now.AddDays(-5).AddHours(2) },
            new() { Id = conv2Id, UserId = c4.Id, Email = c4.Email, Escalated = false,
                CreatedAt = now.AddDays(-3).AddHours(8) },
            new() { Id = conv3Id, UserId = c6.Id, Email = c6.Email, Escalated = true,
                TicketId = conv3TicketId, CreatedAt = now.AddDays(-2).AddHours(11) },
            new() { Id = conv4Id, UserId = c3.Id, Email = c3.Email, Escalated = false,
                CreatedAt = now.AddHours(-4) },
        };
        context.ChatConversations.AddRange(conversations);

        var chatMessages = new List<ChatMessage>
        {
            // conv1 — quick stock question
            new() { Id = Guid.NewGuid(), ConversationId = conv1Id, Role = ChatRole.Bot,
                Content = "Bonjour ! Je suis l'assistant virtuel d'Althea Systems. Comment puis-je vous aider ?",
                Timestamp = now.AddDays(-5).AddHours(2) },
            new() { Id = Guid.NewGuid(), ConversationId = conv1Id, Role = ChatRole.User,
                Content = "Vous avez les ciseaux Mayo en stock ?",
                Timestamp = now.AddDays(-5).AddHours(2).AddMinutes(1) },
            new() { Id = Guid.NewGuid(), ConversationId = conv1Id, Role = ChatRole.Bot,
                Content = "Les Ciseaux Mayo droits 16 cm sont actuellement épuisés. Réapprovisionnement prévu sous 5 jours. Souhaitez-vous être notifié à la disponibilité ?",
                Timestamp = now.AddDays(-5).AddHours(2).AddMinutes(2) },

            // conv2 — payment question
            new() { Id = Guid.NewGuid(), ConversationId = conv2Id, Role = ChatRole.Bot,
                Content = "Bonjour ! Je suis l'assistant virtuel d'Althea Systems. Comment puis-je vous aider ?",
                Timestamp = now.AddDays(-3).AddHours(8) },
            new() { Id = Guid.NewGuid(), ConversationId = conv2Id, Role = ChatRole.User,
                Content = "Acceptez-vous le paiement par virement pour les commandes supérieures à 10 000 € ?",
                Timestamp = now.AddDays(-3).AddHours(8).AddMinutes(2) },
            new() { Id = Guid.NewGuid(), ConversationId = conv2Id, Role = ChatRole.Bot,
                Content = "Oui, le virement bancaire est disponible au checkout pour les commandes professionnelles. La commande est confirmée à réception du virement (RIB envoyé par email).",
                Timestamp = now.AddDays(-3).AddHours(8).AddMinutes(3) },

            // conv3 — SAV escalated
            new() { Id = Guid.NewGuid(), ConversationId = conv3Id, Role = ChatRole.Bot,
                Content = "Bonjour ! Je suis l'assistant virtuel d'Althea Systems. Comment puis-je vous aider ?",
                Timestamp = now.AddDays(-2).AddHours(11) },
            new() { Id = Guid.NewGuid(), ConversationId = conv3Id, Role = ChatRole.User,
                Content = "Mon bac SoniClean ne chauffe plus depuis hier, j'ai essayé un autre câble sans succès.",
                Timestamp = now.AddDays(-2).AddHours(11).AddMinutes(1) },
            new() { Id = Guid.NewGuid(), ConversationId = conv3Id, Role = ChatRole.Bot,
                Content = "Je vais transmettre votre demande au support technique. Un membre de notre équipe vous contactera sous 24 h ouvrées.",
                Timestamp = now.AddDays(-2).AddHours(11).AddMinutes(2) },
            new() { Id = Guid.NewGuid(), ConversationId = conv3Id, Role = ChatRole.User,
                Content = "Merci, j'ai besoin de cet appareil en urgence.",
                Timestamp = now.AddDays(-2).AddHours(11).AddMinutes(3) },

            // conv4 — ongoing, recent
            new() { Id = Guid.NewGuid(), ConversationId = conv4Id, Role = ChatRole.Bot,
                Content = "Bonjour ! Je suis l'assistant virtuel d'Althea Systems. Comment puis-je vous aider ?",
                Timestamp = now.AddHours(-4) },
            new() { Id = Guid.NewGuid(), ConversationId = conv4Id, Role = ChatRole.User,
                Content = "Parle-moi du EchoView Pro portable",
                Timestamp = now.AddHours(-4).AddMinutes(1) },
            new() { Id = Guid.NewGuid(), ConversationId = conv4Id, Role = ChatRole.Bot,
                Content = "L'EchoView Pro portable est un échographe couleur Doppler avec écran 12\" et sondes interchangeables. Prix : 5 500 € HT, en stock. Souhaitez-vous plus d'informations ?",
                Timestamp = now.AddHours(-4).AddMinutes(2) },
        };
        context.ChatMessages.AddRange(chatMessages);

        // ── Support tickets ──────────────────────────────
        // Three tickets: one from the escalated chat, one from a contact
        // message, one standalone (visitor opened a ticket directly via the
        // admin reply tooling).
        var tickets = new List<SupportTicket>
        {
            new() { Id = conv3TicketId, ConversationId = conv3Id, ContactMessageId = null,
                Email = c6.Email, Subject = "Panne SoniClean — chauffe ne fonctionne plus",
                Status = TicketStatus.InProgress,
                CreatedAt = now.AddDays(-2).AddHours(11).AddMinutes(2),
                UpdatedAt = now.AddDays(-1).AddHours(3) },

            new() { Id = Guid.NewGuid(), ConversationId = null, ContactMessageId = contactEscalateId,
                Email = "direction@laboanalyse-sud.fr", Subject = "SAV SpinPro — alarme déséquilibre",
                Status = TicketStatus.Open,
                CreatedAt = now.AddDays(-2).AddHours(11),
                UpdatedAt = now.AddDays(-2).AddHours(11) },

            new() { Id = Guid.NewGuid(), ConversationId = null, ContactMessageId = null,
                Email = "comptabilite@clinique-sainte-anne.fr",
                Subject = "Demande facture acquittée commande #ALT-2026-0142",
                Status = TicketStatus.Closed,
                CreatedAt = now.AddDays(-6).AddHours(4),
                UpdatedAt = now.AddDays(-5).AddHours(2) },
        };
        context.SupportTickets.AddRange(tickets);

        // ── Hero Slides ──────────────────────────────────
        var heroSlides = new List<HeroSlide>
        {
            new() { Id = Guid.NewGuid(), Image = "hero-1",
                TitleFr = "Équipement Médical de Pointe", TitleEn = "Cutting-Edge Medical Equipment",
                TitleMs = "Peralatan Perubatan Terkini", TitleAr = "معدات طبية متطورة",
                SubtitleFr = "Althea Systems", SubtitleEn = "Althea Systems",
                SubtitleMs = "Althea Systems", SubtitleAr = "Althea Systems",
                DescriptionFr = "Solutions professionnelles pour les établissements de santé",
                DescriptionEn = "Professional solutions for healthcare facilities",
                DescriptionMs = "Penyelesaian profesional untuk kemudahan penjagaan kesihatan",
                DescriptionAr = "حلول احترافية لمرافق الرعاية الصحية",
                CtaFr = "Découvrir", CtaEn = "Discover",
                CtaMs = "Terokai", CtaAr = "اكتشف",
                Link = "/categories", DisplayOrder = 1 },
            new() { Id = Guid.NewGuid(), Image = "hero-2",
                TitleFr = "Nouveautés 2026", TitleEn = "New in 2026",
                TitleMs = "Terbaru 2026", TitleAr = "جديد 2026",
                SubtitleFr = "Innovation", SubtitleEn = "Innovation",
                SubtitleMs = "Inovasi", SubtitleAr = "ابتكار",
                DescriptionFr = "Découvrez nos dernières innovations en imagerie médicale",
                DescriptionEn = "Discover our latest innovations in medical imaging",
                DescriptionMs = "Terokai inovasi terkini kami dalam pengimejan perubatan",
                DescriptionAr = "اكتشف أحدث ابتكاراتنا في التصوير الطبي",
                CtaFr = "Voir les nouveautés", CtaEn = "See what's new",
                CtaMs = "Lihat yang baru", CtaAr = "اطّلع على الجديد",
                Link = "/categories/imagerie-medicale", DisplayOrder = 2 },
            new() { Id = Guid.NewGuid(), Image = "hero-3",
                TitleFr = "Service & Support", TitleEn = "Service & Support",
                TitleMs = "Servis & Sokongan", TitleAr = "الخدمة والدعم",
                SubtitleFr = "Accompagnement", SubtitleEn = "Support",
                SubtitleMs = "Sokongan", SubtitleAr = "المرافقة",
                DescriptionFr = "Un accompagnement technique dédié pour votre établissement",
                DescriptionEn = "Dedicated technical support for your facility",
                DescriptionMs = "Sokongan teknikal khusus untuk kemudahan anda",
                DescriptionAr = "دعم تقني مخصص لمنشأتك",
                CtaFr = "Nous contacter", CtaEn = "Contact us",
                CtaMs = "Hubungi kami", CtaAr = "اتصل بنا",
                Link = "/contact", DisplayOrder = 3 }
        };
        context.HeroSlides.AddRange(heroSlides);

        // ── Static Pages ─────────────────────────────────
        // Static pages — full editorial content in all 4 locales. Sample
        // texts plausible for a French B2B medical equipment site; for real
        // production these MUST be reviewed by a lawyer (RGPD, LCEN, MDR
        // EU 2017/745). The text content lives in StaticPageContent below
        // to keep the seed concise.
        var staticPages = new List<Models.Content.StaticPage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "cgu",
                TitleFr = "Conditions Générales d'Utilisation",
                TitleEn = "Terms of Service",
                TitleMs = "Terma Perkhidmatan",
                TitleAr = "شروط الاستخدام",
                ContentFr = StaticPageContent.CguFr,
                ContentEn = StaticPageContent.CguEn,
                ContentMs = StaticPageContent.CguMs,
                ContentAr = StaticPageContent.CguAr,
            },
            // CGV — distinct page covering the SALES side (vs CGU which
            // covers the USE side). French law (Code de commerce art L.441-1)
            // requires CGV to be the unique basis of commercial negotiation
            // in B2B — they need to live as a clearly identified document.
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "cgv",
                TitleFr = "Conditions Générales de Vente",
                TitleEn = "Terms of Sale",
                TitleMs = "Terma Jualan",
                TitleAr = "شروط البيع",
                ContentFr = StaticPageContent.CgvFr,
                ContentEn = StaticPageContent.CgvEn,
                ContentMs = StaticPageContent.CgvMs,
                ContentAr = StaticPageContent.CgvAr,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "mentions-legales",
                TitleFr = "Mentions Légales",
                TitleEn = "Legal Notice",
                TitleMs = "Notis Undang-undang",
                TitleAr = "الإشعار القانوني",
                ContentFr = StaticPageContent.LegalFr,
                ContentEn = StaticPageContent.LegalEn,
                ContentMs = StaticPageContent.LegalMs,
                ContentAr = StaticPageContent.LegalAr,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Slug = "a-propos",
                TitleFr = "À Propos",
                TitleEn = "About Us",
                TitleMs = "Tentang Kami",
                TitleAr = "من نحن",
                ContentFr = StaticPageContent.AboutFr,
                ContentEn = StaticPageContent.AboutEn,
                ContentMs = StaticPageContent.AboutMs,
                ContentAr = StaticPageContent.AboutAr,
            },
        };
        context.StaticPages.AddRange(staticPages);

        await context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────
    //  Factories
    // ─────────────────────────────────────────────────────

    private static User NewCustomer(string name, string email, string hash, DateTime createdAt, DateTime? lastLogin)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = hash,
            Role = UserRole.Customer,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            LastLogin = lastLogin,
        };

    private static Address NewAddress(
        Guid userId, string label, string firstName, string lastName, string? company,
        string street, string? street2, string city, string postal, string? phone, bool isDefault)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Label = label,
            FirstName = firstName,
            LastName = lastName,
            Company = company,
            Street = street,
            Street2 = street2,
            City = city,
            PostalCode = postal,
            Country = "France",
            Phone = phone,
            IsDefault = isDefault,
            Archived = false,
            CreatedAt = DateTime.UtcNow,
        };

    /// <summary>
    /// 4-language tuple used to populate Product and ProductSpec rows in
    /// the seeder. Fr/En are required, Ms/Ar are optional — when null, the
    /// front falls back to the French value. Many ProductSpec values are
    /// language-agnostic (e.g. "12 L", "3 008 × 3 008 px") and use null for
    /// Ms/Ar by design.
    /// </summary>
    private record TextL10n(string Fr, string En, string? Ms = null, string? Ar = null);

    private static Product NewProduct(
        string slug, TextL10n name, TextL10n desc, TextL10n longDesc,
        decimal priceHT, VatRate vat,
        int stockQty, StockStatus stockStatus, bool isNew, int priorityRank, string image)
        => new()
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            NameFr = name.Fr,
            NameEn = name.En,
            NameMs = name.Ms,
            NameAr = name.Ar,
            DescriptionFr = desc.Fr,
            DescriptionEn = desc.En,
            DescriptionMs = desc.Ms,
            DescriptionAr = desc.Ar,
            LongDescriptionFr = longDesc.Fr,
            LongDescriptionEn = longDesc.En,
            LongDescriptionMs = longDesc.Ms,
            LongDescriptionAr = longDesc.Ar,
            PriceHT = priceHT,
            VatRate = vat,
            StockQty = stockQty,
            StockStatus = stockStatus,
            IsNew = isNew,
            PriorityRank = priorityRank,
            Images = [image],
            Status = ProductStatus.Active,
        };

    /// <summary>
    /// Canonical happy-path transition list ending on the given status. Used
    /// to synthesise plausible status-history rows for the admin timeline.
    /// </summary>
    private static OrderStatus[] StatusPath(OrderStatus target) => target switch
    {
        OrderStatus.Pending => [OrderStatus.Pending],
        OrderStatus.Confirmed => [OrderStatus.Pending, OrderStatus.Confirmed],
        OrderStatus.Processing => [OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing],
        OrderStatus.Shipped => [OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped],
        OrderStatus.Delivered => [OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered],
        OrderStatus.Cancelled => [OrderStatus.Pending, OrderStatus.Cancelled],
        OrderStatus.Returned => [OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.Returned],
        _ => [OrderStatus.Pending],
    };
}
