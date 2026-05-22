namespace API_Althea_systems.Data.Seed;

/// <summary>
/// Editorial content for the three static pages (CGU, Legal, About) in
/// 4 languages. Kept in its own file because each block is several
/// hundred words long and would otherwise drown the DataSeeder.
///
/// <para><b>Disclaimer</b>: these are <b>sample texts</b> plausible for a
/// French B2B medical equipment supplier. They are NOT legal advice. Any
/// production deployment must have the CGU and Mentions Légales reviewed
/// by a lawyer to ensure compliance with current French / EU regulations
/// (RGPD, LCEN, MDR EU 2017/745, etc.).</para>
///
/// <para>Format: markdown-flavoured plain text. The front's static page
/// renderer treats <c>##</c> as section headers and bullet lists with
/// <c>-</c> as ULs. Keep the structure parallel across the 4 languages
/// so a customer switching locale gets the same sections in the same
/// order.</para>
/// </summary>
internal static class StaticPageContent
{
    // ═════════════════════════════════════════════════════════════════════
    //  CGU — Conditions Générales d'Utilisation / Terms of Service
    // ═════════════════════════════════════════════════════════════════════

    public const string CguFr = """
        # Conditions Générales d'Utilisation

        *Dernière mise à jour : mai 2026*

        ## 1. Objet
        Les présentes Conditions Générales d'Utilisation (« CGU ») régissent l'utilisation du site altheasystems.com (le « Site ») édité par Althea Systems SAS et destiné exclusivement aux professionnels de santé pour la commercialisation de matériel médical.

        ## 2. Acceptation
        L'accès au Site et la création d'un compte valent acceptation pleine et entière des présentes CGU. L'utilisateur reconnaît disposer de la capacité juridique pour s'engager dans le cadre de son activité professionnelle. Les CGU sont opposables dès leur acceptation, y compris pour les commandes ultérieures.

        ## 3. Public concerné
        Le Site est strictement réservé aux professionnels de santé, établissements de soins, laboratoires, pharmacies, cabinets médicaux, vétérinaires, EHPAD et plus généralement toute personne morale ou physique exerçant une activité réglementée dans le secteur médical. L'utilisateur s'engage à fournir des informations exactes et à justifier de sa qualité professionnelle sur demande.

        ## 4. Compte utilisateur
        L'inscription requiert un nom complet, une adresse e-mail professionnelle valide et un mot de passe respectant les recommandations CNIL (12 caractères minimum, mélange de types). L'utilisateur est responsable de la confidentialité de ses identifiants et de toute activité réalisée depuis son compte. En cas de soupçon d'accès non autorisé, il doit immédiatement modifier son mot de passe et contacter le support.

        ## 5. Commandes et prix
        Les prix sont indiqués en euros, hors taxes (HT) et toutes taxes comprises (TTC). La TVA française applicable est calculée automatiquement. Les paiements sont acceptés par carte bancaire (via Stripe), virement bancaire ou mandat administratif pour les établissements publics. Le contrat de vente est formé dès validation du paiement et confirmation par e-mail.

        ## 6. Livraison
        Trois modes de livraison sont proposés : Standard (5-7 jours, 15 € HT), Express (2-3 jours, 35 € HT) et 24 h (75 € HT). Les délais courent à compter de la confirmation de paiement. Les risques sont transférés à l'acheteur dès remise au transporteur ; toute réserve doit être notée sur le bordereau de livraison.

        ## 7. Droit de rétractation
        Conformément à l'article L.221-3 du Code de la consommation, le droit légal de rétractation de 14 jours **ne s'applique pas** aux contrats conclus entre professionnels. À titre commercial, Althea Systems peut accepter le retour de produits sous 7 jours, dans leur emballage d'origine, non utilisés, sous réserve d'accord préalable. Les produits stériles ouverts, personnalisés ou sur commande spéciale ne sont en aucun cas repris.

        ## 8. Garanties
        Les produits bénéficient de la garantie légale de conformité (art. L.217-4 et suivants du Code de la consommation) et de la garantie des vices cachés (art. 1641 et suivants du Code civil). Les durées de garantie commerciale sont précisées sur chaque fiche produit. Le service après-vente est assuré directement par Althea Systems ou par le fabricant selon les cas.

        ## 9. Responsabilité
        Althea Systems s'engage à fournir des produits conformes aux normes médicales européennes (marquage CE, Règlement UE 2017/745). L'éditeur ne saurait être tenu responsable de l'usage des produits par les professionnels, qui agissent sous leur propre responsabilité médicale et engagent leur assurance RC professionnelle. La responsabilité d'Althea Systems est limitée au montant de la commande concernée.

        ## 10. Propriété intellectuelle
        L'ensemble du contenu du Site (textes, images, logos, marques, code source) est protégé par le droit d'auteur et la propriété industrielle. Toute reproduction, même partielle, est strictement interdite sans autorisation écrite préalable.

        ## 11. Données personnelles
        Le traitement des données personnelles est régi par notre Politique de Confidentialité, conforme au RGPD et à la loi Informatique et Libertés. L'utilisateur dispose des droits d'accès, rectification, effacement, portabilité, limitation et opposition, exerçables à l'adresse dpo@altheasystems.com.

        ## 12. Modification des CGU
        Althea Systems se réserve le droit de modifier les présentes CGU à tout moment. Les utilisateurs sont informés par e-mail des modifications substantielles. Les CGU applicables sont celles en vigueur à la date de validation de la commande.

        ## 13. Droit applicable et litiges
        Les présentes CGU sont régies par le droit français. Tout litige fera l'objet d'une tentative de résolution amiable préalable. À défaut, le Tribunal de Commerce de Paris sera seul compétent, nonobstant pluralité de défendeurs ou appel en garantie.

        Pour toute question : legal@altheasystems.com
        """;

    public const string CguEn = """
        # Terms of Service

        *Last updated: May 2026*

        ## 1. Purpose
        These Terms of Service ("Terms") govern the use of altheasystems.com (the "Site") published by Althea Systems SAS and intended exclusively for healthcare professionals for the sale of medical equipment.

        ## 2. Acceptance
        Accessing the Site and creating an account constitutes full acceptance of these Terms. The user acknowledges having the legal capacity to commit within the framework of their professional activity. The Terms apply from the moment of acceptance, including for subsequent orders.

        ## 3. Target audience
        The Site is strictly reserved for healthcare professionals, care facilities, laboratories, pharmacies, medical practices, veterinarians, retirement homes and more generally any legal entity or natural person carrying out a regulated activity in the medical sector. The user undertakes to provide accurate information and to justify their professional status on request.

        ## 4. User account
        Registration requires a full name, a valid professional email address and a password complying with French CNIL guidelines (minimum 12 characters, mixed character types). The user is responsible for the confidentiality of their credentials and for any activity carried out from their account. In case of suspected unauthorised access, the user must immediately change their password and contact support.

        ## 5. Orders and pricing
        Prices are displayed in euros, both excluding tax (HT) and including tax (TTC). Applicable French VAT is calculated automatically. Payments are accepted via credit card (through Stripe), bank transfer, or administrative purchase order for public institutions. The sales contract is formed upon payment validation and email confirmation.

        ## 6. Delivery
        Three shipping methods are offered: Standard (5-7 days, €15 HT), Express (2-3 days, €35 HT), and Overnight (€75 HT). Lead times run from payment confirmation. Risk is transferred to the buyer upon handover to the carrier; any reservation must be noted on the delivery slip.

        ## 7. Right of withdrawal
        In accordance with article L.221-3 of the French Consumer Code, the statutory 14-day right of withdrawal **does not apply** to contracts concluded between professionals. As a commercial gesture, Althea Systems may accept the return of products within 7 days, in their original packaging and unused, subject to prior agreement. Opened sterile, personalised or special-order products are non-returnable.

        ## 8. Warranties
        Products benefit from the statutory warranty of conformity (art. L.217-4 et seq. of the French Consumer Code) and from the warranty against latent defects (art. 1641 et seq. of the French Civil Code). Commercial warranty durations are specified on each product page. After-sales service is provided directly by Althea Systems or by the manufacturer depending on the case.

        ## 9. Liability
        Althea Systems commits to supplying products compliant with European medical standards (CE marking, EU Regulation 2017/745). The publisher cannot be held liable for the use of products by professionals, who act under their own medical responsibility and engage their professional liability insurance. Althea Systems' liability is limited to the amount of the order concerned.

        ## 10. Intellectual property
        All content on the Site (texts, images, logos, trademarks, source code) is protected by copyright and industrial property. Any reproduction, even partial, is strictly prohibited without prior written authorisation.

        ## 11. Personal data
        The processing of personal data is governed by our Privacy Policy, compliant with GDPR and the French Data Protection Act. Users have rights of access, rectification, erasure, portability, restriction and objection, exercisable at dpo@altheasystems.com.

        ## 12. Modification of the Terms
        Althea Systems reserves the right to modify these Terms at any time. Users are informed by email of substantial modifications. The Terms applicable are those in force on the date of order validation.

        ## 13. Applicable law and disputes
        These Terms are governed by French law. Any dispute will be subject to an attempt at amicable resolution beforehand. Failing that, the Commercial Court of Paris will have sole jurisdiction, notwithstanding multiple defendants or warranty claims.

        For any question: legal@altheasystems.com
        """;

    public const string CguMs = """
        # Terma Perkhidmatan

        *Kemas kini terakhir: Mei 2026*

        ## 1. Tujuan
        Terma Perkhidmatan ini ("Terma") mengawal penggunaan laman altheasystems.com ("Laman") yang diterbitkan oleh Althea Systems SAS dan ditujukan secara eksklusif untuk profesional penjagaan kesihatan bagi pemasaran peralatan perubatan.

        ## 2. Penerimaan
        Mengakses Laman dan mencipta akaun bermakna penerimaan penuh terhadap Terma ini. Pengguna mengakui mempunyai keupayaan undang-undang untuk komited dalam rangka aktiviti profesionalnya. Terma terpakai sebaik sahaja diterima, termasuk untuk pesanan seterusnya.

        ## 3. Khalayak sasaran
        Laman ini dikhaskan untuk profesional penjagaan kesihatan, kemudahan rawatan, makmal, farmasi, klinik perubatan, doktor haiwan, rumah penjagaan warga emas dan secara umumnya mana-mana entiti undang-undang atau individu yang menjalankan aktiviti dikawal selia dalam sektor perubatan. Pengguna berjanji untuk memberikan maklumat yang tepat dan membuktikan status profesionalnya apabila diminta.

        ## 4. Akaun pengguna
        Pendaftaran memerlukan nama penuh, alamat e-mel profesional yang sah dan kata laluan yang mematuhi garis panduan CNIL Perancis (minimum 12 aksara, campuran jenis aksara). Pengguna bertanggungjawab untuk kerahsiaan kelayakannya dan untuk sebarang aktiviti yang dijalankan dari akaunnya. Jika terdapat sebarang sangkaan akses tanpa kebenaran, pengguna mesti segera menukar kata laluannya dan menghubungi sokongan.

        ## 5. Pesanan dan harga
        Harga dipaparkan dalam euro, kedua-dua tidak termasuk cukai (HT) dan termasuk cukai (TTC). VAT Perancis yang berkenaan dikira secara automatik. Pembayaran diterima melalui kad kredit (melalui Stripe), pindahan bank, atau pesanan pembelian pentadbiran untuk institusi awam. Kontrak jualan terbentuk apabila pembayaran disahkan dan disusuli pengesahan e-mel.

        ## 6. Penghantaran
        Tiga kaedah penghantaran ditawarkan: Standard (5-7 hari, €15 HT), Ekspres (2-3 hari, €35 HT), dan Semalaman (€75 HT). Tempoh masa bermula dari pengesahan pembayaran. Risiko dipindahkan kepada pembeli setelah diserahkan kepada syarikat penghantaran; sebarang tempahan mesti dicatatkan pada slip penghantaran.

        ## 7. Hak penarikan
        Selaras dengan artikel L.221-3 Kod Pengguna Perancis, hak penarikan berkanun selama 14 hari **tidak terpakai** untuk kontrak yang dibuat antara profesional. Sebagai gerak isyarat komersial, Althea Systems boleh menerima pemulangan produk dalam tempoh 7 hari, dalam pembungkusan asal dan tidak digunakan, tertakluk kepada persetujuan terlebih dahulu. Produk steril yang dibuka, diperibadikan atau pesanan khas tidak boleh dipulangkan.

        ## 8. Jaminan
        Produk mendapat manfaat daripada jaminan keserasian berkanun dan jaminan terhadap kecacatan tersembunyi. Tempoh jaminan komersial dinyatakan pada setiap halaman produk. Perkhidmatan selepas jualan disediakan secara langsung oleh Althea Systems atau oleh pengeluar bergantung kepada kes.

        ## 9. Liabiliti
        Althea Systems komited untuk membekalkan produk yang mematuhi piawaian perubatan Eropah (tanda CE, Peraturan EU 2017/745). Penerbit tidak boleh dipertanggungjawabkan atas penggunaan produk oleh profesional, yang bertindak di bawah tanggungjawab perubatan mereka sendiri dan melibatkan insurans liabiliti profesional mereka. Liabiliti Althea Systems terhad kepada jumlah pesanan yang berkenaan.

        ## 10. Harta intelek
        Semua kandungan Laman (teks, imej, logo, tanda dagangan, kod sumber) dilindungi oleh hak cipta dan harta perindustrian. Sebarang pengeluaran semula, walaupun sebahagiannya, adalah dilarang sama sekali tanpa kebenaran bertulis terlebih dahulu.

        ## 11. Data peribadi
        Pemprosesan data peribadi dikawal oleh Dasar Privasi kami, yang mematuhi GDPR dan Akta Perlindungan Data Perancis. Pengguna mempunyai hak akses, pembetulan, pemadaman, mudah alih, sekatan dan bantahan, boleh dilaksanakan di dpo@altheasystems.com.

        ## 12. Pengubahsuaian Terma
        Althea Systems berhak mengubah Terma ini pada bila-bila masa. Pengguna dimaklumkan melalui e-mel tentang perubahan ketara. Terma yang terpakai adalah yang berkuat kuasa pada tarikh pengesahan pesanan.

        ## 13. Undang-undang yang berkenaan dan pertikaian
        Terma ini dikawal oleh undang-undang Perancis. Sebarang pertikaian akan tertakluk kepada percubaan penyelesaian secara mesra terlebih dahulu. Sekiranya gagal, Mahkamah Komersial Paris akan mempunyai bidang kuasa tunggal.

        Untuk sebarang pertanyaan: legal@altheasystems.com
        """;

    public const string CguAr = """
        # شروط الاستخدام

        *آخر تحديث: مايو 2026*

        ## 1. الغرض
        تحكم شروط الاستخدام هذه ("الشروط") استخدام موقع altheasystems.com ("الموقع") الذي تنشره شركة Althea Systems SAS والمخصص حصرياً للمهنيين في مجال الرعاية الصحية لتسويق المعدات الطبية.

        ## 2. القبول
        يشكّل الوصول إلى الموقع وإنشاء حساب قبولاً كاملاً لهذه الشروط. يقر المستخدم بأنه يتمتع بالأهلية القانونية للالتزام في إطار نشاطه المهني. تسري الشروط فور قبولها، بما في ذلك للطلبات اللاحقة.

        ## 3. الجمهور المستهدف
        الموقع مخصص حصرياً للمهنيين الصحيين ومرافق الرعاية والمختبرات والصيدليات والعيادات الطبية والأطباء البيطريين ودور رعاية المسنين، وبشكل عام أي كيان قانوني أو شخص طبيعي يمارس نشاطاً منظماً في القطاع الطبي. يلتزم المستخدم بتقديم معلومات دقيقة وإثبات صفته المهنية عند الطلب.

        ## 4. حساب المستخدم
        يتطلب التسجيل اسماً كاملاً وعنوان بريد إلكتروني مهني صالح وكلمة مرور تتوافق مع توجيهات CNIL الفرنسية (12 حرفاً على الأقل، مزيج من أنواع الأحرف). المستخدم مسؤول عن سرية بيانات اعتماده وعن أي نشاط يتم من حسابه. في حالة الاشتباه بوصول غير مصرّح به، يجب على المستخدم تغيير كلمة المرور فوراً والاتصال بالدعم.

        ## 5. الطلبات والأسعار
        تُعرض الأسعار باليورو، صافي الضريبة (HT) وشامل الضريبة (TTC). تُحتسب ضريبة القيمة المضافة الفرنسية المعمول بها تلقائياً. تُقبل المدفوعات عبر بطاقة الائتمان (من خلال Stripe)، التحويل المصرفي، أو أمر الشراء الإداري للمؤسسات العامة. يتم تكوين عقد البيع عند التحقق من الدفع وتأكيده بالبريد الإلكتروني.

        ## 6. التسليم
        تُقدَّم ثلاث طرق شحن: عادي (5-7 أيام، 15 يورو HT)، سريع (2-3 أيام، 35 يورو HT)، و24 ساعة (75 يورو HT). تبدأ المهلة من تأكيد الدفع. تنتقل المخاطر إلى المشتري عند التسليم إلى شركة الشحن؛ يجب تسجيل أي تحفظ على وثيقة التسليم.

        ## 7. حق الانسحاب
        وفقاً للمادة L.221-3 من قانون المستهلك الفرنسي، **لا ينطبق** حق الانسحاب القانوني لمدة 14 يوماً على العقود المبرمة بين المهنيين. كبادرة تجارية، يمكن لشركة Althea Systems قبول إرجاع المنتجات خلال 7 أيام، في عبواتها الأصلية وغير المستخدمة، رهناً بالاتفاق المسبق. المنتجات المعقّمة المفتوحة أو المخصصة أو ذات الطلب الخاص لا تُرتجَع.

        ## 8. الضمانات
        تستفيد المنتجات من الضمان القانوني للمطابقة وضمان العيوب الخفية وفقاً للقانون الفرنسي. تُحدَّد مدد الضمان التجاري في كل صفحة منتج. تُقدَّم خدمة ما بعد البيع مباشرةً من قبل Althea Systems أو من قبل الشركة المصنّعة حسب الحالة.

        ## 9. المسؤولية
        تلتزم Althea Systems بتوريد منتجات متوافقة مع المعايير الطبية الأوروبية (علامة CE، اللائحة الأوروبية 2017/745). لا يمكن تحميل الناشر المسؤولية عن استخدام المنتجات من قبل المهنيين، الذين يتصرفون تحت مسؤوليتهم الطبية الخاصة ويلتزمون بتأمين مسؤوليتهم المهنية. مسؤولية Althea Systems محدودة بمبلغ الطلب المعني.

        ## 10. الملكية الفكرية
        جميع محتويات الموقع (النصوص، الصور، الشعارات، العلامات التجارية، الكود المصدري) محمية بموجب حقوق التأليف والنشر والملكية الصناعية. يُحظر أي استنساخ، حتى ولو جزئي، دون إذن كتابي مسبق.

        ## 11. البيانات الشخصية
        تخضع معالجة البيانات الشخصية لسياسة الخصوصية الخاصة بنا، والتي تتوافق مع GDPR وقانون حماية البيانات الفرنسي. للمستخدمين حقوق الوصول والتصحيح والمحو والنقل والتقييد والاعتراض، يمكن ممارستها على dpo@altheasystems.com.

        ## 12. تعديل الشروط
        تحتفظ Althea Systems بالحق في تعديل هذه الشروط في أي وقت. يتم إبلاغ المستخدمين بالبريد الإلكتروني بالتعديلات الجوهرية. الشروط المعمول بها هي تلك السارية في تاريخ التحقق من الطلب.

        ## 13. القانون المعمول به والنزاعات
        تخضع هذه الشروط للقانون الفرنسي. سيخضع أي نزاع لمحاولة حل ودي مسبقة. في حالة الإخفاق، تختص محكمة باريس التجارية وحدها بالنظر فيها.

        لأي استفسار: legal@altheasystems.com
        """;

    // ═════════════════════════════════════════════════════════════════════
    //  CGV — Conditions Générales de Vente / Terms of Sale
    // ═════════════════════════════════════════════════════════════════════

    public const string CgvFr = """
        # Conditions Générales de Vente

        *Dernière mise à jour : mai 2026*

        ## 1. Champ d'application
        Les présentes Conditions Générales de Vente (« CGV ») régissent l'ensemble des ventes conclues entre Althea Systems SAS (« le Vendeur ») et tout professionnel acheteur (« l'Acheteur ») via le site altheasystems.com. Conformément à l'article L.441-1 du Code de commerce, les CGV constituent le socle unique de la négociation commerciale. Elles prévalent sur toutes Conditions Générales d'Achat de l'Acheteur, sauf accord écrit contraire.

        ## 2. Définitions
        **« Site »** : la plateforme e-commerce altheasystems.com.
        **« Produit »** : tout article de matériel médical proposé à la vente sur le Site.
        **« Commande »** : tout achat passé via le Site et accepté par le Vendeur.

        ## 3. Acceptation des CGV
        La passation d'une Commande implique l'acceptation pleine et entière des présentes CGV par l'Acheteur, qui déclare en avoir pris connaissance et en accepter sans réserve les termes. Une case à cocher au moment du paiement matérialise cette acceptation.

        ## 4. Produits
        Les caractéristiques des Produits sont décrites sur chaque fiche produit du Site. Les Produits sont des dispositifs médicaux ou consommables conformes au Règlement européen (UE) 2017/745 (« MDR ») et bénéficient du marquage CE. Les photographies et illustrations sont fournies à titre indicatif et n'engagent pas le Vendeur en cas de variation mineure.

        Le Vendeur se réserve le droit de modifier sans préavis les caractéristiques techniques d'un Produit dès lors que cette modification ne porte pas atteinte à ses fonctions essentielles.

        ## 5. Commande
        L'Acheteur sélectionne ses Produits, valide son panier, fournit ses informations de facturation et livraison, choisit son mode de paiement et confirme sa Commande. Un e-mail de confirmation est envoyé à l'Acheteur dès validation du paiement.

        Le Vendeur se réserve le droit de refuser toute Commande pour motif légitime, notamment en cas d'indisponibilité du Produit, de défaut de paiement antérieur, ou si l'Acheteur ne justifie pas de sa qualité de professionnel de santé.

        ## 6. Prix
        Les prix sont indiqués en euros, hors taxes (HT) et toutes taxes comprises (TTC). La TVA française applicable est calculée automatiquement selon le taux en vigueur (20 % standard, 5,5 % pour les consommables médicaux le cas échéant). Les frais de livraison s'ajoutent et sont précisés au moment de la Commande.

        Les prix sont valables tels qu'indiqués sur le Site à la date de la Commande. Le Vendeur se réserve le droit de modifier ses prix à tout moment, étant entendu que les Commandes en cours seront facturées au prix en vigueur lors de leur validation.

        ## 7. Modalités de paiement
        Le paiement s'effectue au moment de la Commande par l'un des moyens suivants :
        - **Carte bancaire** via notre prestataire Stripe (Visa, Mastercard, American Express)
        - **Virement bancaire** sous 8 jours sur le compte indiqué dans la confirmation
        - **Mandat administratif** pour les établissements publics et collectivités

        Les paiements par carte bancaire sont sécurisés par le protocole 3D Secure (DSP2) imposé par la directive européenne sur les services de paiement.

        En cas de retard de paiement, des pénalités calculées au taux directeur de la BCE majoré de 10 points et une indemnité forfaitaire de recouvrement de 40 € (article L.441-10 du Code de commerce) seront appliquées de plein droit.

        ## 8. Livraison
        La livraison est effectuée à l'adresse renseignée par l'Acheteur lors de la Commande. Trois modes sont proposés :

        - **Standard** : 5-7 jours ouvrés, 15 € HT
        - **Express** : 2-3 jours ouvrés, 35 € HT
        - **24h** : livraison sous 24 h ouvrées, 75 € HT

        Les délais courent à compter de la confirmation du paiement. Ils sont donnés à titre indicatif. Un retard de livraison ne peut donner lieu à aucune indemnité ni annulation de la Commande, sauf retard supérieur à 30 jours par rapport à la date convenue.

        ## 9. Transfert des risques et réception
        Les risques de perte et de détérioration des Produits sont transférés à l'Acheteur dès la remise au transporteur. L'Acheteur doit vérifier l'état des colis à la livraison et émettre toute réserve écrite sur le bordereau du transporteur. Toute anomalie doit être signalée au Vendeur dans les 48 heures par e-mail à support@altheasystems.com.

        ## 10. Droit de rétractation
        Conformément à l'article L.221-3 du Code de la consommation, le droit légal de rétractation de 14 jours **ne s'applique pas** aux contrats conclus entre professionnels. À titre commercial, le Vendeur peut accepter le retour de Produits non utilisés et dans leur emballage d'origine sous 7 jours, sous réserve d'accord préalable. Les Produits stériles ouverts, personnalisés ou commandés spécialement ne sont en aucun cas repris.

        ## 11. Garanties
        Les Produits bénéficient :
        - De la **garantie légale de conformité** (articles L.217-4 et suivants du Code de la consommation)
        - De la **garantie des vices cachés** (articles 1641 et suivants du Code civil)
        - De la **garantie commerciale du fabricant** dont la durée est précisée sur chaque fiche produit

        ## 12. Service après-vente
        Toute demande de SAV doit être adressée à support@altheasystems.com avec le numéro de facture et la description du défaut. Le Vendeur s'engage à répondre sous 24 h ouvrées. Les retours sont organisés à la charge du Vendeur en cas de défaut avéré couvert par la garantie.

        ## 13. Responsabilité
        Le Vendeur ne saurait être tenu responsable de l'usage des Produits par les professionnels acheteurs, qui agissent sous leur propre responsabilité médicale. La responsabilité du Vendeur est en tout état de cause **limitée au montant HT de la Commande** concernée.

        ## 14. Réserve de propriété
        Les Produits restent la propriété exclusive du Vendeur jusqu'au paiement intégral de leur prix. À défaut de paiement à l'échéance, le Vendeur pourra exiger la restitution des Produits aux frais et risques de l'Acheteur.

        ## 15. Force majeure
        La responsabilité du Vendeur ne pourra être engagée en cas d'inexécution due à un cas de force majeure au sens de l'article 1218 du Code civil (pandémie, catastrophe naturelle, grève des transporteurs, rupture d'approvisionnement, etc.).

        ## 16. Données personnelles
        Le traitement des données personnelles est régi par notre Politique de Confidentialité, conforme au RGPD. Pour exercer vos droits : dpo@altheasystems.com.

        ## 17. Médiation
        Conformément à l'article L.612-1 du Code de la consommation, le Vendeur adhère au dispositif de médiation de la consommation. Pour les litiges entre professionnels, l'Acheteur peut saisir le Médiateur des entreprises (mediateur-des-entreprises.fr) avant toute action judiciaire.

        ## 18. Loi applicable et juridiction
        Les présentes CGV sont régies par le droit français. **Tout litige** sera soumis à une tentative de résolution amiable préalable. À défaut, **le Tribunal de Commerce de Paris sera seul compétent**, nonobstant pluralité de défendeurs ou appel en garantie.

        Pour toute question : legal@altheasystems.com
        """;

    public const string CgvEn = """
        # Terms of Sale

        *Last updated: May 2026*

        ## 1. Scope
        These Terms of Sale ("Terms") govern all sales concluded between Althea Systems SAS ("the Seller") and any professional buyer ("the Buyer") via altheasystems.com. Pursuant to article L.441-1 of the French Commercial Code, these Terms constitute the sole basis for commercial negotiation. They prevail over any General Purchase Conditions of the Buyer, unless otherwise agreed in writing.

        ## 2. Definitions
        **"Site"**: the e-commerce platform altheasystems.com.
        **"Product"**: any medical equipment item offered for sale on the Site.
        **"Order"**: any purchase placed via the Site and accepted by the Seller.

        ## 3. Acceptance of the Terms
        Placing an Order implies the Buyer's full acceptance of these Terms. The Buyer declares having read them and accepts them without reservation. A checkbox at the payment stage materialises this acceptance.

        ## 4. Products
        Product characteristics are described on each Product sheet. The Products are medical devices or consumables compliant with European Regulation (EU) 2017/745 ("MDR") and bear the CE marking. Photographs and illustrations are provided for information purposes and do not bind the Seller in case of minor variation.

        The Seller reserves the right to modify a Product's technical characteristics without notice, provided this does not impair its essential functions.

        ## 5. Order
        The Buyer selects Products, validates the cart, provides billing and delivery information, chooses a payment method and confirms the Order. A confirmation email is sent to the Buyer upon payment validation.

        The Seller reserves the right to refuse any Order on legitimate grounds, in particular Product unavailability, prior payment default, or where the Buyer fails to justify their status as a healthcare professional.

        ## 6. Pricing
        Prices are displayed in euros, both excluding tax (HT) and including tax (TTC). Applicable French VAT is calculated automatically based on the current rate (20% standard, 5.5% for medical consumables where applicable). Shipping costs are added and specified at the time of the Order.

        Prices are valid as displayed on the Site at the time of the Order. The Seller reserves the right to modify prices at any time; Orders in progress will be billed at the price in effect at the time of their validation.

        ## 7. Payment terms
        Payment is made at the time of the Order using one of the following methods:
        - **Credit card** via our provider Stripe (Visa, Mastercard, American Express)
        - **Bank transfer** within 8 days to the account specified in the confirmation
        - **Administrative purchase order** for public institutions and local authorities

        Card payments are secured by the 3D Secure protocol (PSD2) mandated by the European Payment Services Directive.

        In case of late payment, penalties calculated at the ECB key rate plus 10 points and a flat-rate recovery indemnity of €40 (article L.441-10 of the French Commercial Code) will apply automatically.

        ## 8. Delivery
        Delivery is made to the address provided by the Buyer at the time of the Order. Three modes are offered:

        - **Standard**: 5-7 business days, €15 HT
        - **Express**: 2-3 business days, €35 HT
        - **Overnight**: delivery within 24 business hours, €75 HT

        Lead times run from payment confirmation. They are given as a guide. A delivery delay does not entitle the Buyer to compensation or cancellation of the Order, except for delays exceeding 30 days from the agreed date.

        ## 9. Risk transfer and acceptance
        The risks of loss and deterioration of Products are transferred to the Buyer upon handover to the carrier. The Buyer must check the condition of the packages on delivery and issue any written reservation on the carrier's slip. Any anomaly must be reported to the Seller within 48 hours by email to support@altheasystems.com.

        ## 10. Right of withdrawal
        Pursuant to article L.221-3 of the French Consumer Code, the statutory 14-day right of withdrawal **does not apply** to contracts concluded between professionals. As a commercial gesture, the Seller may accept the return of unused Products in their original packaging within 7 days, subject to prior agreement. Opened sterile, personalised or specially ordered Products are non-returnable.

        ## 11. Warranties
        Products benefit from:
        - The **statutory warranty of conformity** (articles L.217-4 et seq. of the French Consumer Code)
        - The **warranty against latent defects** (articles 1641 et seq. of the French Civil Code)
        - The **manufacturer's commercial warranty** whose duration is specified on each Product sheet

        ## 12. After-sales service
        Any SAV request must be sent to support@altheasystems.com with the invoice number and a description of the defect. The Seller commits to respond within 24 business hours. Returns are organised at the Seller's expense in case of proven defect covered by the warranty.

        ## 13. Liability
        The Seller cannot be held liable for the use of Products by professional buyers, who act under their own medical responsibility. The Seller's liability is in any event **limited to the HT amount of the Order** concerned.

        ## 14. Retention of title
        Products remain the exclusive property of the Seller until full payment of their price. In case of non-payment at maturity, the Seller may demand the return of the Products at the Buyer's cost and risk.

        ## 15. Force majeure
        The Seller's liability cannot be engaged in case of non-performance due to force majeure within the meaning of article 1218 of the French Civil Code (pandemic, natural disaster, carrier strike, supply disruption, etc.).

        ## 16. Personal data
        Personal data processing is governed by our Privacy Policy, compliant with GDPR. To exercise your rights: dpo@altheasystems.com.

        ## 17. Mediation
        Pursuant to article L.612-1 of the French Consumer Code, the Seller adheres to a consumer mediation scheme. For disputes between professionals, the Buyer may refer to the Médiateur des entreprises (mediateur-des-entreprises.fr) before any judicial action.

        ## 18. Applicable law and jurisdiction
        These Terms are governed by French law. **Any dispute** will be subject to an attempt at amicable resolution beforehand. Failing that, **the Commercial Court of Paris will have sole jurisdiction**, notwithstanding multiple defendants or warranty claims.

        For any question: legal@altheasystems.com
        """;

    public const string CgvMs = """
        # Terma Jualan

        *Kemas kini terakhir: Mei 2026*

        ## 1. Skop
        Terma Jualan ini ("Terma") mengawal semua jualan yang dibuat antara Althea Systems SAS ("Penjual") dan mana-mana pembeli profesional ("Pembeli") melalui altheasystems.com. Mengikut artikel L.441-1 Kod Komersial Perancis, Terma ini merupakan asas tunggal untuk rundingan komersial. Ia mengatasi mana-mana Syarat Pembelian Am Pembeli, melainkan dipersetujui secara bertulis.

        ## 2. Definisi
        **"Laman"**: platform e-dagang altheasystems.com.
        **"Produk"**: mana-mana item peralatan perubatan yang ditawarkan untuk dijual di Laman.
        **"Pesanan"**: mana-mana pembelian yang dibuat melalui Laman dan diterima oleh Penjual.

        ## 3. Penerimaan Terma
        Membuat Pesanan menyiratkan penerimaan penuh Terma ini oleh Pembeli. Pembeli mengisytiharkan telah membacanya dan menerimanya tanpa keraguan. Kotak pilihan pada peringkat pembayaran membuktikan penerimaan ini.

        ## 4. Produk
        Ciri-ciri Produk diterangkan pada setiap helaian Produk. Produk adalah peranti perubatan atau bahan habis pakai yang mematuhi Peraturan Eropah (EU) 2017/745 ("MDR") dan mempunyai tanda CE. Fotografi dan ilustrasi disediakan untuk tujuan maklumat dan tidak mengikat Penjual sekiranya berlaku variasi kecil.

        Penjual berhak mengubah ciri teknikal Produk tanpa notis, dengan syarat ini tidak menjejaskan fungsi pentingnya.

        ## 5. Pesanan
        Pembeli memilih Produk, mengesahkan troli, memberikan maklumat pengebilan dan penghantaran, memilih kaedah pembayaran dan mengesahkan Pesanan. E-mel pengesahan dihantar kepada Pembeli sebaik sahaja pembayaran disahkan.

        Penjual berhak menolak mana-mana Pesanan atas alasan yang sah, terutamanya jika Produk tidak tersedia, jika terdapat kegagalan pembayaran sebelumnya, atau jika Pembeli gagal membuktikan statusnya sebagai profesional penjagaan kesihatan.

        ## 6. Harga
        Harga dipaparkan dalam euro, tidak termasuk cukai (HT) dan termasuk cukai (TTC). VAT Perancis yang berkenaan dikira secara automatik berdasarkan kadar semasa (20% standard, 5.5% untuk bahan habis pakai perubatan jika berkenaan). Kos penghantaran ditambah dan dinyatakan pada masa Pesanan.

        Harga adalah sah seperti yang dipaparkan pada Laman pada masa Pesanan. Penjual berhak mengubah harga pada bila-bila masa; Pesanan dalam proses akan dibilkan pada harga yang berkuat kuasa pada masa pengesahan.

        ## 7. Syarat pembayaran
        Pembayaran dibuat pada masa Pesanan menggunakan salah satu kaedah berikut:
        - **Kad kredit** melalui pembekal kami Stripe (Visa, Mastercard, American Express)
        - **Pindahan bank** dalam tempoh 8 hari ke akaun yang dinyatakan dalam pengesahan
        - **Pesanan pembelian pentadbiran** untuk institusi awam dan pihak berkuasa tempatan

        Pembayaran kad dilindungi oleh protokol 3D Secure (PSD2) yang dimandatkan oleh Arahan Perkhidmatan Pembayaran Eropah.

        Sekiranya berlaku kelewatan pembayaran, penalti dikira pada kadar utama ECB ditambah 10 mata dan indemniti pemulihan kadar rata €40 (artikel L.441-10 Kod Komersial Perancis) akan dikenakan secara automatik.

        ## 8. Penghantaran
        Penghantaran dibuat ke alamat yang disediakan oleh Pembeli pada masa Pesanan. Tiga mod ditawarkan:

        - **Standard**: 5-7 hari bekerja, €15 HT
        - **Ekspres**: 2-3 hari bekerja, €35 HT
        - **Semalaman**: penghantaran dalam masa 24 jam bekerja, €75 HT

        Tempoh masa bermula dari pengesahan pembayaran. Ia diberikan sebagai panduan. Kelewatan penghantaran tidak melayakkan Pembeli kepada pampasan atau pembatalan Pesanan, kecuali kelewatan melebihi 30 hari dari tarikh yang dipersetujui.

        ## 9. Pemindahan risiko dan penerimaan
        Risiko kehilangan dan kerosakan Produk dipindahkan kepada Pembeli sebaik sahaja diserahkan kepada syarikat penghantaran. Pembeli mesti menyemak keadaan bungkusan semasa penghantaran dan mengeluarkan sebarang tempahan bertulis pada slip pembawa. Sebarang anomali mesti dilaporkan kepada Penjual dalam tempoh 48 jam melalui e-mel kepada support@altheasystems.com.

        ## 10. Hak penarikan
        Mengikut artikel L.221-3 Kod Pengguna Perancis, hak penarikan berkanun selama 14 hari **tidak terpakai** untuk kontrak yang dibuat antara profesional. Sebagai gerak isyarat komersial, Penjual boleh menerima pemulangan Produk yang tidak digunakan dalam pembungkusan asalnya dalam tempoh 7 hari, tertakluk kepada persetujuan terlebih dahulu. Produk steril yang dibuka, diperibadikan atau dipesan khas tidak boleh dipulangkan.

        ## 11. Jaminan
        Produk mendapat manfaat daripada:
        - **Jaminan keserasian berkanun** (artikel L.217-4 dan seterusnya Kod Pengguna Perancis)
        - **Jaminan terhadap kecacatan tersembunyi** (artikel 1641 dan seterusnya Kod Sivil Perancis)
        - **Jaminan komersial pengilang** yang tempohnya dinyatakan pada setiap helaian Produk

        ## 12. Perkhidmatan selepas jualan
        Sebarang permintaan SAV mesti dihantar ke support@altheasystems.com dengan nombor invois dan penerangan kecacatan. Penjual komited untuk membalas dalam tempoh 24 jam bekerja. Pulangan dianjurkan dengan perbelanjaan Penjual sekiranya berlaku kecacatan yang terbukti dilindungi oleh jaminan.

        ## 13. Liabiliti
        Penjual tidak boleh dipertanggungjawabkan atas penggunaan Produk oleh pembeli profesional, yang bertindak di bawah tanggungjawab perubatan mereka sendiri. Liabiliti Penjual dalam apa keadaan sekalipun **terhad kepada jumlah HT Pesanan** yang berkenaan.

        ## 14. Pengekalan hak milik
        Produk kekal sebagai harta eksklusif Penjual sehingga pembayaran penuh harganya. Sekiranya tiada pembayaran pada tarikh matang, Penjual boleh menuntut pemulangan Produk dengan kos dan risiko Pembeli.

        ## 15. Force majeure
        Liabiliti Penjual tidak boleh dikenakan dalam kes ketidaksempurnaan disebabkan oleh force majeure dalam erti kata artikel 1218 Kod Sivil Perancis (pandemik, bencana alam, mogok pembawa, gangguan bekalan, dll.).

        ## 16. Data peribadi
        Pemprosesan data peribadi dikawal oleh Dasar Privasi kami, yang mematuhi GDPR. Untuk melaksanakan hak anda: dpo@altheasystems.com.

        ## 17. Pengantaraan
        Mengikut artikel L.612-1 Kod Pengguna Perancis, Penjual mematuhi skim pengantaraan pengguna. Untuk pertikaian antara profesional, Pembeli boleh merujuk kepada Médiateur des entreprises (mediateur-des-entreprises.fr) sebelum sebarang tindakan kehakiman.

        ## 18. Undang-undang yang berkenaan dan bidang kuasa
        Terma ini dikawal oleh undang-undang Perancis. **Sebarang pertikaian** akan tertakluk kepada percubaan penyelesaian secara mesra terlebih dahulu. Sekiranya gagal, **Mahkamah Komersial Paris akan mempunyai bidang kuasa tunggal**, walaupun terdapat berbilang defendan atau tuntutan jaminan.

        Untuk sebarang pertanyaan: legal@altheasystems.com
        """;

    public const string CgvAr = """
        # شروط البيع

        *آخر تحديث: مايو 2026*

        ## 1. النطاق
        تحكم شروط البيع هذه ("الشروط") جميع عمليات البيع المبرمة بين شركة Althea Systems SAS ("البائع") وأي مشترٍ مهني ("المشتري") عبر موقع altheasystems.com. وفقاً للمادة L.441-1 من قانون التجارة الفرنسي، تُشكل هذه الشروط الأساس الوحيد للتفاوض التجاري. تسود على أي شروط شراء عامة للمشتري، ما لم يُتفق على خلاف ذلك كتابياً.

        ## 2. التعريفات
        **"الموقع"**: منصة التجارة الإلكترونية altheasystems.com.
        **"المنتج"**: أي عنصر من معدات طبية معروض للبيع على الموقع.
        **"الطلب"**: أي عملية شراء تتم عبر الموقع ويقبلها البائع.

        ## 3. قبول الشروط
        يستلزم تقديم الطلب القبول الكامل من المشتري لهذه الشروط. يُصرح المشتري بأنه قرأها وقبلها دون تحفظ. يُجسد مربع اختيار في مرحلة الدفع هذا القبول.

        ## 4. المنتجات
        تُوصف خصائص المنتجات على كل صفحة منتج. المنتجات هي أجهزة طبية أو مستهلكات متوافقة مع اللائحة الأوروبية (EU) 2017/745 ("MDR") وتحمل علامة CE. تُقدَّم الصور والرسوم التوضيحية لأغراض إعلامية ولا تُلزم البائع في حالة وجود تباين طفيف.

        يحتفظ البائع بالحق في تعديل الخصائص التقنية للمنتج دون إشعار، شريطة ألا يُضعف ذلك من وظائفه الأساسية.

        ## 5. الطلب
        يختار المشتري المنتجات، ويصادق على السلة، ويقدم معلومات الفوترة والتسليم، ويختار طريقة الدفع ويؤكد الطلب. يُرسَل بريد إلكتروني للتأكيد إلى المشتري عند التحقق من الدفع.

        يحتفظ البائع بالحق في رفض أي طلب لأسباب مشروعة، ولا سيما عدم توفر المنتج، أو التخلف عن الدفع السابق، أو عدم قدرة المشتري على إثبات صفته كمهني صحي.

        ## 6. الأسعار
        تُعرض الأسعار باليورو، صافي الضريبة (HT) وشامل الضريبة (TTC). تُحتسب ضريبة القيمة المضافة الفرنسية المعمول بها تلقائياً وفقاً للمعدل الحالي (20٪ قياسي، 5.5٪ للمستهلكات الطبية حيث ينطبق). تُضاف تكاليف الشحن وتُحدد وقت الطلب.

        الأسعار صالحة كما هي معروضة على الموقع وقت الطلب. يحتفظ البائع بالحق في تعديل الأسعار في أي وقت؛ تُفوتر الطلبات الجارية بالسعر الساري وقت التحقق منها.

        ## 7. شروط الدفع
        يتم الدفع وقت الطلب باستخدام إحدى الطرق التالية:
        - **بطاقة الائتمان** عبر مزودنا Stripe (Visa، Mastercard، American Express)
        - **التحويل المصرفي** خلال 8 أيام إلى الحساب المحدد في التأكيد
        - **أمر الشراء الإداري** للمؤسسات العامة والسلطات المحلية

        مدفوعات البطاقة مؤمنة ببروتوكول 3D Secure (PSD2) الذي تفرضه التوجيهات الأوروبية لخدمات الدفع.

        في حالة التأخر في الدفع، تُطبَّق تلقائياً عقوبات محتسبة على معدل البنك المركزي الأوروبي زائد 10 نقاط وتعويض استرداد بمبلغ ثابت قدره 40 يورو (المادة L.441-10 من قانون التجارة الفرنسي).

        ## 8. التسليم
        يتم التسليم إلى العنوان المقدم من المشتري وقت الطلب. تُقدَّم ثلاث طرق:

        - **عادي**: 5-7 أيام عمل، 15 يورو HT
        - **سريع**: 2-3 أيام عمل، 35 يورو HT
        - **24 ساعة**: التسليم خلال 24 ساعة عمل، 75 يورو HT

        تبدأ المهل من تأكيد الدفع. تُعطى كدليل. لا يخوّل تأخر التسليم المشتري للحصول على تعويض أو إلغاء الطلب، باستثناء التأخيرات التي تتجاوز 30 يوماً من التاريخ المتفق عليه.

        ## 9. نقل المخاطر والاستلام
        تُنقل مخاطر فقدان وتلف المنتجات إلى المشتري عند التسليم إلى شركة الشحن. يجب على المشتري فحص حالة الطرود عند التسليم وإصدار أي تحفظ كتابي على وثيقة الشاحن. يجب الإبلاغ عن أي شذوذ إلى البائع خلال 48 ساعة عبر البريد الإلكتروني إلى support@altheasystems.com.

        ## 10. حق الانسحاب
        وفقاً للمادة L.221-3 من قانون المستهلك الفرنسي، **لا ينطبق** حق الانسحاب القانوني لمدة 14 يوماً على العقود المبرمة بين المهنيين. كبادرة تجارية، يجوز للبائع قبول إرجاع المنتجات غير المستخدمة في عبواتها الأصلية خلال 7 أيام، رهناً بالاتفاق المسبق. المنتجات المعقمة المفتوحة أو المخصصة أو المطلوبة خصيصاً لا تُرتجَع.

        ## 11. الضمانات
        تستفيد المنتجات من:
        - **الضمان القانوني للمطابقة** (المادتان L.217-4 وما يليهما من قانون المستهلك الفرنسي)
        - **ضمان العيوب الخفية** (المادتان 1641 وما يليهما من القانون المدني الفرنسي)
        - **الضمان التجاري للشركة المصنّعة** تُحدَّد مدته في كل صفحة منتج

        ## 12. خدمة ما بعد البيع
        يجب إرسال أي طلب خدمة ما بعد البيع إلى support@altheasystems.com مع رقم الفاتورة ووصف العيب. يلتزم البائع بالرد خلال 24 ساعة عمل. تُنظَّم عمليات الإرجاع على نفقة البائع في حالة وجود عيب ثابت يغطيه الضمان.

        ## 13. المسؤولية
        لا يمكن تحميل البائع المسؤولية عن استخدام المنتجات من قبل المشترين المهنيين، الذين يتصرفون تحت مسؤوليتهم الطبية الخاصة. مسؤولية البائع في جميع الأحوال **محدودة بمبلغ HT الطلب** المعني.

        ## 14. الاحتفاظ بحق الملكية
        تظل المنتجات الملكية الحصرية للبائع حتى السداد الكامل لسعرها. في حالة عدم السداد عند الاستحقاق، يحق للبائع المطالبة بإعادة المنتجات على نفقة المشتري وعلى مسؤوليته.

        ## 15. القوة القاهرة
        لا يمكن إشراك مسؤولية البائع في حالة عدم التنفيذ بسبب القوة القاهرة بالمعنى المقصود في المادة 1218 من القانون المدني الفرنسي (وباء، كارثة طبيعية، إضراب شركات الشحن، انقطاع الإمدادات، إلخ).

        ## 16. البيانات الشخصية
        تخضع معالجة البيانات الشخصية لسياسة الخصوصية الخاصة بنا، المتوافقة مع GDPR. لممارسة حقوقك: dpo@altheasystems.com.

        ## 17. الوساطة
        وفقاً للمادة L.612-1 من قانون المستهلك الفرنسي، يلتزم البائع بنظام وساطة المستهلك. للنزاعات بين المهنيين، يمكن للمشتري الرجوع إلى Médiateur des entreprises (mediateur-des-entreprises.fr) قبل أي إجراء قضائي.

        ## 18. القانون المعمول به والاختصاص القضائي
        تخضع هذه الشروط للقانون الفرنسي. **سيخضع أي نزاع** لمحاولة حل ودي مسبقة. في حالة الإخفاق، **تختص محكمة باريس التجارية وحدها بالنظر فيه**، بصرف النظر عن تعدد المدعى عليهم أو طلبات الضمان.

        لأي استفسار: legal@altheasystems.com
        """;

    // ═════════════════════════════════════════════════════════════════════
    //  Mentions Légales / Legal Notice
    // ═════════════════════════════════════════════════════════════════════

    public const string LegalFr = """
        # Mentions Légales

        Conformément aux dispositions de la loi n° 2004-575 du 21 juin 2004 pour la confiance dans l'économie numérique (LCEN), les utilisateurs du site altheasystems.com sont informés de l'identité des différents intervenants dans le cadre de sa réalisation et de son suivi.

        ## Éditeur du site
        Le site altheasystems.com est édité par :

        **Althea Systems SAS**
        Société par actions simplifiée au capital de 100 000 €
        Siège social : 1 rue de la Santé, 75013 Paris, France
        RCS Paris B 123 456 789
        SIRET : 123 456 789 00012
        N° TVA intracommunautaire : FR12 345678900
        Code APE : 4646Z — Commerce de gros de produits pharmaceutiques

        **Téléphone** : +33 1 23 45 67 89
        **E-mail** : contact@altheasystems.com

        ## Directeur de la publication
        Le Président d'Althea Systems SAS, en sa qualité de représentant légal.

        ## Hébergement
        Le site est hébergé par :

        **OVHcloud SAS**
        2 rue Kellermann, 59100 Roubaix, France
        Téléphone : +33 9 72 10 10 07
        Site web : ovhcloud.com

        ## Activité réglementée
        Althea Systems est distributeur de dispositifs médicaux conformes au Règlement européen (UE) 2017/745 relatif aux dispositifs médicaux (« MDR »). Les produits proposés bénéficient du marquage CE conforme aux exigences essentielles applicables. La vente est strictement réservée aux professionnels de santé habilités.

        Autorité de contrôle compétente pour les dispositifs médicaux :
        **ANSM** — Agence nationale de sécurité du médicament et des produits de santé
        143-147 boulevard Anatole France, 93285 Saint-Denis Cedex

        ## Propriété intellectuelle
        L'ensemble du contenu présent sur le site (textes, images, vidéos, logos, marques, code source) est la propriété exclusive d'Althea Systems SAS ou de ses partenaires. Toute reproduction, représentation, modification, publication ou adaptation totale ou partielle des éléments du site, par quelque procédé que ce soit, est interdite sans l'autorisation écrite préalable d'Althea Systems.

        ## Marques
        « Althea Systems » et le logo associé sont des marques déposées d'Althea Systems SAS auprès de l'INPI. Toute utilisation non autorisée constitue une contrefaçon sanctionnée par les articles L.713-2 et suivants du Code de la propriété intellectuelle.

        ## Données personnelles
        Le traitement des données personnelles est régi par notre Politique de Confidentialité conforme au Règlement Général sur la Protection des Données (RGPD) et à la loi française Informatique et Libertés modifiée. Le responsable du traitement est Althea Systems SAS.

        Délégué à la protection des données (DPO) : dpo@altheasystems.com

        L'utilisateur dispose des droits d'accès, de rectification, d'effacement, de portabilité, de limitation et d'opposition au traitement de ses données. Il peut introduire une réclamation auprès de la CNIL — Commission Nationale de l'Informatique et des Libertés (3 place de Fontenoy, 75007 Paris, www.cnil.fr).

        ## Cookies
        Le site utilise des cookies strictement nécessaires au fonctionnement (session, authentification) et des cookies de mesure d'audience anonymisés. Aucun cookie publicitaire ou de tracking tiers n'est utilisé. Le détail est disponible dans notre Politique de Cookies.

        ## Crédits
        Conception et développement : équipe Althea Systems.
        Iconographie : Unsplash, Lucide Icons.
        Polices : Poppins, Inter (Google Fonts).

        ## Contact
        Pour toute question concernant le site ou les présentes mentions :
        **E-mail** : contact@altheasystems.com
        **Courrier** : Althea Systems SAS, 1 rue de la Santé, 75013 Paris, France
        """;

    public const string LegalEn = """
        # Legal Notice

        Pursuant to French Law No. 2004-575 of 21 June 2004 on Trust in the Digital Economy (LCEN), users of altheasystems.com are informed of the identity of the different parties involved in its production and management.

        ## Publisher
        The altheasystems.com website is published by:

        **Althea Systems SAS**
        Simplified joint-stock company with capital of €100,000
        Registered office: 1 rue de la Santé, 75013 Paris, France
        Paris Trade and Companies Register: B 123 456 789
        SIRET: 123 456 789 00012
        EU VAT number: FR12 345678900
        APE code: 4646Z — Wholesale of pharmaceutical products

        **Phone**: +33 1 23 45 67 89
        **Email**: contact@altheasystems.com

        ## Publication Director
        The President of Althea Systems SAS, in their capacity as legal representative.

        ## Hosting
        The website is hosted by:

        **OVHcloud SAS**
        2 rue Kellermann, 59100 Roubaix, France
        Phone: +33 9 72 10 10 07
        Website: ovhcloud.com

        ## Regulated activity
        Althea Systems is a distributor of medical devices compliant with European Regulation (EU) 2017/745 on medical devices ("MDR"). The products offered bear the CE marking in compliance with applicable essential requirements. Sales are strictly reserved to authorised healthcare professionals.

        Competent supervisory authority for medical devices:
        **ANSM** — French National Agency for Medicines and Health Products Safety
        143-147 boulevard Anatole France, 93285 Saint-Denis Cedex, France

        ## Intellectual property
        All content on the website (texts, images, videos, logos, trademarks, source code) is the exclusive property of Althea Systems SAS or its partners. Any reproduction, representation, modification, publication or adaptation, in whole or in part, of elements of the site, by any means whatsoever, is prohibited without the prior written authorisation of Althea Systems.

        ## Trademarks
        "Althea Systems" and the associated logo are registered trademarks of Althea Systems SAS with the French INPI. Any unauthorised use constitutes infringement punishable under articles L.713-2 et seq. of the French Intellectual Property Code.

        ## Personal data
        The processing of personal data is governed by our Privacy Policy in compliance with the General Data Protection Regulation (GDPR) and the amended French Data Protection Act. The data controller is Althea Systems SAS.

        Data Protection Officer (DPO): dpo@altheasystems.com

        Users have rights of access, rectification, erasure, portability, restriction and objection to the processing of their data. They may lodge a complaint with the CNIL — French Data Protection Authority (3 place de Fontenoy, 75007 Paris, www.cnil.fr).

        ## Cookies
        The site uses strictly necessary cookies (session, authentication) and anonymised audience measurement cookies. No advertising or third-party tracking cookies are used. Details are available in our Cookie Policy.

        ## Credits
        Design and development: Althea Systems team.
        Iconography: Unsplash, Lucide Icons.
        Fonts: Poppins, Inter (Google Fonts).

        ## Contact
        For any question regarding the site or these notices:
        **Email**: contact@altheasystems.com
        **Mail**: Althea Systems SAS, 1 rue de la Santé, 75013 Paris, France
        """;

    public const string LegalMs = """
        # Notis Undang-undang

        Selaras dengan peruntukan Undang-undang Perancis No. 2004-575 bertarikh 21 Jun 2004 mengenai Kepercayaan dalam Ekonomi Digital (LCEN), pengguna laman altheasystems.com dimaklumkan tentang identiti pihak-pihak yang terlibat dalam penghasilan dan pengurusannya.

        ## Penerbit
        Laman altheasystems.com diterbitkan oleh:

        **Althea Systems SAS**
        Syarikat saham bersama yang dipermudahkan dengan modal €100,000
        Pejabat berdaftar: 1 rue de la Santé, 75013 Paris, Perancis
        Daftar Perdagangan dan Syarikat Paris: B 123 456 789
        SIRET: 123 456 789 00012
        Nombor VAT EU: FR12 345678900
        Kod APE: 4646Z — Borong produk farmaseutikal

        **Telefon**: +33 1 23 45 67 89
        **E-mel**: contact@altheasystems.com

        ## Pengarah Penerbitan
        Presiden Althea Systems SAS, dalam kapasiti sebagai wakil undang-undang.

        ## Pengehosan
        Laman ini dihoskan oleh:

        **OVHcloud SAS**
        2 rue Kellermann, 59100 Roubaix, Perancis
        Telefon: +33 9 72 10 10 07
        Laman web: ovhcloud.com

        ## Aktiviti dikawal selia
        Althea Systems adalah pengedar peranti perubatan yang mematuhi Peraturan Eropah (EU) 2017/745 mengenai peranti perubatan ("MDR"). Produk yang ditawarkan mempunyai tanda CE selaras dengan keperluan asas yang berkenaan. Jualan dikhususkan kepada profesional penjagaan kesihatan yang dibenarkan.

        Pihak berkuasa penyeliaan yang kompeten untuk peranti perubatan:
        **ANSM** — Agensi Kebangsaan Keselamatan Ubat dan Produk Kesihatan Perancis
        143-147 boulevard Anatole France, 93285 Saint-Denis Cedex, Perancis

        ## Harta intelek
        Semua kandungan di laman web (teks, imej, video, logo, tanda dagangan, kod sumber) adalah hak milik eksklusif Althea Systems SAS atau rakan kongsinya. Sebarang pengeluaran semula, perwakilan, pengubahsuaian, penerbitan atau penyesuaian, sepenuhnya atau sebahagiannya, elemen laman, melalui sebarang cara, adalah dilarang tanpa kebenaran bertulis terlebih dahulu daripada Althea Systems.

        ## Tanda dagangan
        "Althea Systems" dan logo yang berkaitan adalah tanda dagangan berdaftar Althea Systems SAS dengan INPI Perancis. Sebarang penggunaan tanpa kebenaran adalah pelanggaran yang boleh dikenakan tindakan undang-undang.

        ## Data peribadi
        Pemprosesan data peribadi dikawal oleh Dasar Privasi kami selaras dengan Peraturan Perlindungan Data Umum (GDPR) dan Akta Perlindungan Data Perancis yang dipinda. Pengawal data adalah Althea Systems SAS.

        Pegawai Perlindungan Data (DPO): dpo@altheasystems.com

        Pengguna mempunyai hak akses, pembetulan, pemadaman, mudah alih, sekatan dan bantahan terhadap pemprosesan data mereka. Mereka boleh memfailkan aduan kepada CNIL — Pihak Berkuasa Perlindungan Data Perancis (3 place de Fontenoy, 75007 Paris, www.cnil.fr).

        ## Kuki
        Laman ini menggunakan kuki yang diperlukan secara ketat (sesi, pengesahan) dan kuki pengukuran khalayak yang dianonimkan. Tiada kuki pengiklanan atau penjejakan pihak ketiga digunakan. Butiran tersedia dalam Dasar Kuki kami.

        ## Kredit
        Reka bentuk dan pembangunan: pasukan Althea Systems.
        Ikonografi: Unsplash, Lucide Icons.
        Fon: Poppins, Inter (Google Fonts).

        ## Hubungi kami
        Untuk sebarang pertanyaan mengenai laman atau notis ini:
        **E-mel**: contact@altheasystems.com
        **Surat**: Althea Systems SAS, 1 rue de la Santé, 75013 Paris, Perancis
        """;

    public const string LegalAr = """
        # الإشعار القانوني

        وفقاً لأحكام القانون الفرنسي رقم 2004-575 الصادر في 21 يونيو 2004 المتعلق بالثقة في الاقتصاد الرقمي (LCEN)، يُحاط مستخدمو موقع altheasystems.com علماً بهوية الأطراف المختلفة المشاركة في إنتاجه وإدارته.

        ## الناشر
        موقع altheasystems.com تنشره:

        **شركة Althea Systems SAS**
        شركة مساهمة مبسّطة برأس مال قدره 100,000 يورو
        المقر الاجتماعي: 1 rue de la Santé, 75013 Paris, فرنسا
        سجل التجارة والشركات بباريس: B 123 456 789
        رقم SIRET: 123 456 789 00012
        رقم ضريبة القيمة المضافة داخل الاتحاد الأوروبي: FR12 345678900
        رمز APE: 4646Z — تجارة الجملة للمنتجات الصيدلانية

        **الهاتف**: +33 1 23 45 67 89
        **البريد الإلكتروني**: contact@altheasystems.com

        ## مدير النشر
        رئيس شركة Althea Systems SAS، بصفته الممثل القانوني.

        ## الاستضافة
        الموقع مستضاف لدى:

        **OVHcloud SAS**
        2 rue Kellermann, 59100 Roubaix, فرنسا
        الهاتف: +33 9 72 10 10 07
        الموقع: ovhcloud.com

        ## نشاط منظَّم
        Althea Systems موزّع للأجهزة الطبية المتوافقة مع اللائحة الأوروبية (EU) 2017/745 المتعلقة بالأجهزة الطبية ("MDR"). تحمل المنتجات المعروضة علامة CE وفقاً للمتطلبات الأساسية المعمول بها. البيع مخصص حصراً للمهنيين الصحيين المؤهلين.

        السلطة الإشرافية المختصة بالأجهزة الطبية:
        **ANSM** — الوكالة الوطنية الفرنسية لسلامة الأدوية والمنتجات الصحية
        143-147 boulevard Anatole France, 93285 Saint-Denis Cedex, فرنسا

        ## الملكية الفكرية
        جميع محتويات الموقع (النصوص، الصور، مقاطع الفيديو، الشعارات، العلامات التجارية، الكود المصدري) هي ملكية حصرية لشركة Althea Systems SAS أو شركائها. يُحظر أي استنساخ أو تمثيل أو تعديل أو نشر أو تكييف، كلياً أو جزئياً، لعناصر الموقع، بأي وسيلة كانت، دون إذن كتابي مسبق من Althea Systems.

        ## العلامات التجارية
        "Althea Systems" والشعار المرتبط بها هي علامات تجارية مسجلة لشركة Althea Systems SAS لدى INPI الفرنسي. يشكّل أي استخدام غير مصرح به تعدياً يخضع للعقوبات المنصوص عليها قانوناً.

        ## البيانات الشخصية
        تخضع معالجة البيانات الشخصية لسياسة الخصوصية الخاصة بنا المتوافقة مع اللائحة العامة لحماية البيانات (GDPR) وقانون حماية البيانات الفرنسي المعدّل. مراقب البيانات هو Althea Systems SAS.

        مسؤول حماية البيانات (DPO): dpo@altheasystems.com

        للمستخدمين حقوق الوصول والتصحيح والمحو والنقل والتقييد والاعتراض على معالجة بياناتهم. يمكنهم تقديم شكوى إلى CNIL — السلطة الفرنسية لحماية البيانات (3 place de Fontenoy, 75007 Paris, www.cnil.fr).

        ## ملفات تعريف الارتباط
        يستخدم الموقع ملفات تعريف ارتباط ضرورية بصرامة (الجلسة، المصادقة) وملفات تعريف ارتباط لقياس الجمهور مجهولة الهوية. لا تُستخدم أي ملفات تعريف ارتباط إعلانية أو تتبع من أطراف ثالثة. التفاصيل متوفرة في سياسة ملفات تعريف الارتباط الخاصة بنا.

        ## الاعتمادات
        التصميم والتطوير: فريق Althea Systems.
        الرسوم التوضيحية: Unsplash، Lucide Icons.
        الخطوط: Poppins، Inter (Google Fonts).

        ## اتصل بنا
        لأي استفسار بخصوص الموقع أو هذه الإشعارات:
        **البريد الإلكتروني**: contact@altheasystems.com
        **العنوان البريدي**: Althea Systems SAS, 1 rue de la Santé, 75013 Paris, فرنسا
        """;

    // ═════════════════════════════════════════════════════════════════════
    //  À Propos / About Us
    // ═════════════════════════════════════════════════════════════════════

    public const string AboutFr = """
        # À propos d'Althea Systems

        ## Notre mission
        Depuis 2015, Althea Systems est un acteur de référence dans la distribution de matériel médical professionnel en France et en Europe. Notre mission : équiper les professionnels de santé avec les meilleurs outils pour offrir des soins de qualité à leurs patients.

        ## Notre histoire
        Fondée à Paris en 2015 par une équipe de pharmaciens et d'ingénieurs biomédicaux, Althea Systems est née d'un constat simple : les professionnels de santé manquent de temps pour comparer, négocier et acquérir leur équipement. Nous nous sommes donné pour mission de simplifier cet achat en réunissant sur une seule plateforme les meilleurs fournisseurs européens, avec des prix transparents et un service après-vente irréprochable.

        En dix ans, nous avons accompagné plus de 5 000 cabinets, cliniques, laboratoires et établissements de soins dans leur équipement quotidien. Notre catalogue compte aujourd'hui plus de 3 000 références, du gant d'examen au scanner de pointe.

        ## Notre catalogue
        Nous proposons huit grandes catégories de produits :

        - **Imagerie médicale** : radiographie numérique, échographes, scanners
        - **Moniteurs & diagnostics** : ECG, tensiomètres, oxymètres
        - **Stérilisation & hygiène** : autoclaves, laveurs, désinfectants
        - **Instruments chirurgicaux** : scalpels, pinces, ciseaux, laryngoscopes
        - **Mobilier médical** : lits, chariots, tabourets ergonomiques
        - **Équipements respiratoires** : ventilateurs, nébuliseurs, CPAP
        - **Consommables médicaux** : gants, compresses, bandes, seringues
        - **Équipements de laboratoire** : analyseurs, centrifugeuses, microscopes

        Tous nos produits sont conformes aux normes européennes en vigueur (marquage CE, ISO 13485, EN) et proviennent exclusivement de fabricants certifiés.

        ## Nos engagements

        **Qualité et conformité.** Nous ne référençons que des produits certifiés CE médical et issus de fabricants audités selon les normes ISO 13485. Chaque produit est accompagné de sa notice et de ses documents de conformité téléchargeables.

        **Transparence des prix.** Tous nos prix sont affichés HT et TTC, sans frais cachés. Pour les commandes importantes, notre équipe commerciale propose des devis personnalisés avec conditions négociées.

        **Réactivité.** Nos délais d'expédition sont parmi les plus courts du marché (24 h pour les consommables en stock, 5-7 jours pour le gros matériel). Un service après-vente dédié répond sous 24 h ouvrées.

        **Conseil expert.** Notre équipe compte des professionnels de santé qui conseillent personnellement nos clients. Un chatbot intelligent répond aux questions courantes 24/7, et nos commerciaux sont disponibles par téléphone pour les besoins spécifiques.

        ## Notre équipe
        Une vingtaine de collaborateurs basés à Paris, dont :

        - Une équipe commerciale composée de pharmaciens et d'ingénieurs biomédicaux
        - Un service après-vente certifié, formé par les fabricants
        - Une équipe logistique opérant depuis nos entrepôts d'Île-de-France et de Lyon
        - Une équipe technique dédiée à notre plateforme numérique

        ## Notre démarche RSE
        Althea Systems s'engage pour un commerce responsable :

        - Emballages recyclables ou biodégradables pour toutes nos expéditions
        - Programme de reprise des équipements obsolètes pour recyclage
        - Partenariat avec des ONG médicales pour l'envoi d'équipements en fin de vie utile vers des pays en développement
        - Bilan carbone annuel publié sur notre site

        ## Nous contacter
        **Siège social** : 1 rue de la Santé, 75013 Paris
        **Téléphone** : +33 1 23 45 67 89
        **E-mail commercial** : contact@altheasystems.com
        **Support technique** : support@altheasystems.com

        Vous représentez un établissement public ou un grand compte ? Contactez notre équipe Grands Comptes : grands-comptes@altheasystems.com

        Vous êtes fabricant et souhaitez devenir partenaire ? partenaires@altheasystems.com
        """;

    public const string AboutEn = """
        # About Althea Systems

        ## Our mission
        Since 2015, Althea Systems has been a leading distributor of professional medical equipment in France and across Europe. Our mission: equip healthcare professionals with the best tools to deliver quality care to their patients.

        ## Our history
        Founded in Paris in 2015 by a team of pharmacists and biomedical engineers, Althea Systems was born from a simple observation: healthcare professionals lack the time to compare, negotiate and acquire their equipment. We set ourselves the mission of simplifying these purchases by bringing together the best European suppliers on a single platform, with transparent prices and impeccable after-sales service.

        In ten years, we have supported over 5,000 practices, clinics, laboratories and care facilities in their daily equipment needs. Our catalogue now contains more than 3,000 references, from examination gloves to cutting-edge scanners.

        ## Our catalogue
        We offer eight main product categories:

        - **Medical imaging**: digital X-ray, ultrasound, scanners
        - **Monitors & diagnostics**: ECG, blood pressure monitors, oximeters
        - **Sterilization & hygiene**: autoclaves, washers, disinfectants
        - **Surgical instruments**: scalpels, forceps, scissors, laryngoscopes
        - **Medical furniture**: beds, trolleys, ergonomic stools
        - **Respiratory equipment**: ventilators, nebulizers, CPAP
        - **Medical consumables**: gloves, gauze, bandages, syringes
        - **Laboratory equipment**: analyzers, centrifuges, microscopes

        All our products comply with current European standards (CE marking, ISO 13485, EN) and come exclusively from certified manufacturers.

        ## Our commitments

        **Quality and compliance.** We only list products certified to medical CE standards and sourced from manufacturers audited under ISO 13485. Every product comes with its instruction manual and conformity documents available for download.

        **Pricing transparency.** All our prices are displayed both excl. and incl. VAT, with no hidden fees. For large orders, our sales team offers personalized quotes with negotiated terms.

        **Responsiveness.** Our shipping times are among the fastest on the market (24 hours for in-stock consumables, 5-7 days for major equipment). A dedicated after-sales team responds within 24 business hours.

        **Expert advice.** Our team includes healthcare professionals who personally advise our clients. An intelligent chatbot answers common questions 24/7, and our sales representatives are available by phone for specific needs.

        ## Our team
        Around twenty employees based in Paris, including:

        - A sales team composed of pharmacists and biomedical engineers
        - A certified after-sales department, trained directly by manufacturers
        - A logistics team operating from our warehouses in Île-de-France and Lyon
        - A technical team dedicated to our digital platform

        ## Our CSR approach
        Althea Systems is committed to responsible commerce:

        - Recyclable or biodegradable packaging for all our shipments
        - Take-back program for obsolete equipment for recycling
        - Partnership with medical NGOs to send end-of-useful-life equipment to developing countries
        - Annual carbon footprint published on our website

        ## Contact us
        **Head office**: 1 rue de la Santé, 75013 Paris, France
        **Phone**: +33 1 23 45 67 89
        **Sales email**: contact@altheasystems.com
        **Technical support**: support@altheasystems.com

        Representing a public institution or a major account? Contact our Key Accounts team: grands-comptes@altheasystems.com

        Manufacturer interested in becoming a partner? partenaires@altheasystems.com
        """;

    public const string AboutMs = """
        # Tentang Althea Systems

        ## Misi kami
        Sejak 2015, Althea Systems adalah pengedar peralatan perubatan profesional terkemuka di Perancis dan seluruh Eropah. Misi kami: melengkapkan profesional penjagaan kesihatan dengan alat terbaik untuk memberikan rawatan berkualiti kepada pesakit mereka.

        ## Sejarah kami
        Diasaskan di Paris pada 2015 oleh sepasukan ahli farmasi dan jurutera bioperubatan, Althea Systems lahir daripada pemerhatian mudah: profesional penjagaan kesihatan tidak mempunyai cukup masa untuk membandingkan, berunding dan memperoleh peralatan mereka. Kami menjadikan misi kami untuk memudahkan pembelian ini dengan menghimpunkan pembekal Eropah terbaik pada satu platform tunggal, dengan harga telus dan perkhidmatan selepas jualan yang sempurna.

        Dalam tempoh sepuluh tahun, kami telah menyokong lebih daripada 5,000 amalan, klinik, makmal dan kemudahan penjagaan dalam keperluan peralatan harian mereka. Katalog kami kini mengandungi lebih daripada 3,000 rujukan, dari sarung tangan pemeriksaan hingga pengimbas canggih.

        ## Katalog kami
        Kami menawarkan lapan kategori produk utama:

        - **Pengimejan perubatan**: sinar-X digital, ultrasound, pengimbas
        - **Monitor & diagnostik**: ECG, monitor tekanan darah, oksimeter
        - **Pensterilan & kebersihan**: autoklaf, pencuci, pembasmi kuman
        - **Alat pembedahan**: skalpel, forsep, gunting, laringoskop
        - **Perabot perubatan**: katil, troli, bangku ergonomik
        - **Peralatan pernafasan**: ventilator, nebulizer, CPAP
        - **Bahan pakai buang perubatan**: sarung tangan, kain kasa, pembalut, picagari
        - **Peralatan makmal**: penganalisis, empar, mikroskop

        Semua produk kami mematuhi piawaian Eropah semasa (tanda CE, ISO 13485, EN) dan datang secara eksklusif daripada pengilang yang diperakui.

        ## Komitmen kami

        **Kualiti dan pematuhan.** Kami hanya menyenaraikan produk yang diperakui dengan piawaian CE perubatan dan diperoleh daripada pengilang yang diaudit di bawah ISO 13485. Setiap produk disertakan dengan manual arahan dan dokumen pematuhan yang boleh dimuat turun.

        **Ketelusan harga.** Semua harga kami dipaparkan tidak termasuk dan termasuk VAT, tanpa yuran tersembunyi. Untuk pesanan besar, pasukan jualan kami menawarkan sebut harga peribadi dengan terma yang dirundingkan.

        **Tindak balas pantas.** Masa penghantaran kami adalah antara yang terpantas di pasaran (24 jam untuk bahan pakai buang dalam stok, 5-7 hari untuk peralatan besar). Pasukan selepas jualan yang khusus membalas dalam masa 24 jam bekerja.

        **Nasihat pakar.** Pasukan kami termasuk profesional penjagaan kesihatan yang memberi nasihat secara peribadi kepada pelanggan kami. Chatbot pintar menjawab soalan biasa 24/7, dan wakil jualan kami tersedia melalui telefon untuk keperluan khusus.

        ## Pasukan kami
        Kira-kira dua puluh kakitangan yang berpangkalan di Paris, termasuk:

        - Pasukan jualan yang terdiri daripada ahli farmasi dan jurutera bioperubatan
        - Jabatan selepas jualan yang diperakui, dilatih terus oleh pengilang
        - Pasukan logistik yang beroperasi dari gudang kami di Île-de-France dan Lyon
        - Pasukan teknikal yang khusus untuk platform digital kami

        ## Pendekatan CSR kami
        Althea Systems komited kepada perdagangan yang bertanggungjawab:

        - Pembungkusan boleh dikitar semula atau terbiodegradasi untuk semua penghantaran kami
        - Program pengambilan semula peralatan usang untuk kitar semula
        - Perkongsian dengan NGO perubatan untuk menghantar peralatan akhir hayat berguna ke negara membangun
        - Jejak karbon tahunan diterbitkan di laman web kami

        ## Hubungi kami
        **Pejabat utama**: 1 rue de la Santé, 75013 Paris, Perancis
        **Telefon**: +33 1 23 45 67 89
        **E-mel jualan**: contact@altheasystems.com
        **Sokongan teknikal**: support@altheasystems.com

        Mewakili institusi awam atau akaun utama? Hubungi pasukan Akaun Utama kami: grands-comptes@altheasystems.com

        Pengilang yang berminat untuk menjadi rakan kongsi? partenaires@altheasystems.com
        """;

    public const string AboutAr = """
        # عن Althea Systems

        ## مهمتنا
        منذ عام 2015، تُعدّ Althea Systems موزّعاً رائداً للمعدات الطبية المهنية في فرنسا وعبر أوروبا. مهمتنا: تجهيز المهنيين الصحيين بأفضل الأدوات لتقديم رعاية عالية الجودة لمرضاهم.

        ## تاريخنا
        تأسست في باريس عام 2015 على يد فريق من الصيادلة ومهندسي الهندسة الطبية الحيوية، وُلدت Althea Systems من ملاحظة بسيطة: المهنيون الصحيون يفتقرون إلى الوقت لمقارنة معداتهم والتفاوض عليها واقتنائها. وقد جعلنا مهمتنا تبسيط عمليات الشراء هذه من خلال جمع أفضل الموردين الأوروبيين على منصة واحدة، بأسعار شفافة وخدمة ما بعد البيع لا تشوبها شائبة.

        خلال عشر سنوات، دعمنا أكثر من 5,000 عيادة ومركز طبي ومختبر ومرفق رعاية في احتياجاتهم اليومية من المعدات. يحتوي كتالوجنا الآن على أكثر من 3,000 مرجع، من قفازات الفحص إلى أحدث أجهزة المسح.

        ## كتالوجنا
        نقدم ثماني فئات منتجات رئيسية:

        - **التصوير الطبي**: الأشعة السينية الرقمية، الموجات فوق الصوتية، أجهزة المسح
        - **أجهزة المراقبة والتشخيص**: تخطيط القلب، أجهزة قياس ضغط الدم، أجهزة قياس التأكسج
        - **التعقيم والنظافة**: الموصدات، الغسالات، المطهرات
        - **الأدوات الجراحية**: المشارط، الملاقط، المقصات، مناظير الحنجرة
        - **الأثاث الطبي**: الأسرّة، العربات، الكراسي المريحة
        - **معدات الجهاز التنفسي**: أجهزة التنفس، البخاخات، CPAP
        - **المستهلكات الطبية**: القفازات، الشاش، الضمادات، الحقن
        - **معدات المختبر**: المحللات، أجهزة الطرد المركزي، المجاهر

        تمتثل جميع منتجاتنا للمعايير الأوروبية الحالية (علامة CE، ISO 13485، EN) وتأتي حصراً من مصنعين معتمدين.

        ## التزاماتنا

        **الجودة والامتثال.** نُدرج فقط المنتجات المعتمدة وفقاً للمعايير الطبية CE والمأخوذة من مصنعين خضعوا للتدقيق وفقاً لـ ISO 13485. يأتي كل منتج مع دليل التعليمات ووثائق المطابقة المتاحة للتنزيل.

        **شفافية الأسعار.** تُعرض جميع أسعارنا صافي الضريبة وشاملها، دون رسوم خفية. للطلبات الكبيرة، يقدم فريق المبيعات لدينا عروض أسعار مخصصة بشروط متفاوض عليها.

        **سرعة الاستجابة.** أوقات الشحن لدينا من بين الأسرع في السوق (24 ساعة للمستهلكات المتوفرة، 5-7 أيام للمعدات الكبيرة). يستجيب فريق ما بعد البيع المخصص في غضون 24 ساعة عمل.

        **نصيحة الخبراء.** يضم فريقنا مهنيين صحيين يقدمون النصيحة شخصياً لعملائنا. يجيب روبوت دردشة ذكي على الأسئلة الشائعة على مدار الساعة طوال أيام الأسبوع، ومندوبو مبيعاتنا متاحون عبر الهاتف للاحتياجات المحددة.

        ## فريقنا
        نحو عشرين موظفاً يتمركزون في باريس، بما في ذلك:

        - فريق مبيعات يتألف من صيادلة ومهندسي هندسة طبية حيوية
        - قسم ما بعد البيع المعتمد، مُدرَّب مباشرة من قبل المصنّعين
        - فريق لوجستي يعمل من مستودعاتنا في إيل-دو-فرانس وليون
        - فريق تقني مخصص لمنصتنا الرقمية

        ## نهجنا في المسؤولية الاجتماعية للشركات
        تلتزم Althea Systems بالتجارة المسؤولة:

        - تغليف قابل لإعادة التدوير أو قابل للتحلل الحيوي لجميع شحناتنا
        - برنامج استرداد المعدات القديمة لإعادة التدوير
        - شراكة مع منظمات غير حكومية طبية لإرسال المعدات في نهاية عمرها المفيد إلى الدول النامية
        - بصمة كربونية سنوية تُنشر على موقعنا

        ## اتصل بنا
        **المقر الرئيسي**: 1 rue de la Santé, 75013 Paris, فرنسا
        **الهاتف**: +33 1 23 45 67 89
        **بريد المبيعات**: contact@altheasystems.com
        **الدعم التقني**: support@altheasystems.com

        تمثل مؤسسة عامة أو حساباً رئيسياً؟ اتصل بفريق الحسابات الرئيسية لدينا: grands-comptes@altheasystems.com

        هل أنت مصنع مهتم بأن تصبح شريكاً؟ partenaires@altheasystems.com
        """;
}
