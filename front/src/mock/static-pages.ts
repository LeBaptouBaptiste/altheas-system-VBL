import type { StaticPage, HeroSlide } from './types';

export const heroSlides: HeroSlide[] = [
  {
    id: 'slide-1',
    image: 'slide1',
    title: { fr: 'Solutions d\'Imagerie Avancées', en: 'Advanced Imaging Solutions', ms: 'Penyelesaian Pengimejan Termaju', ar: 'حلول التصوير المتقدمة' },
    subtitle: { fr: 'Système ProScan Radiographie Numérique', en: 'ProScan Digital X-Ray System', ms: 'Sistem Radiografi Digital ProScan', ar: 'نظام الأشعة السينية الرقمية ProScan' },
    description: { fr: 'Découvrez un diagnostic de précision avec nos équipements d\'imagerie de pointe', en: 'Experience precision diagnostics with our state-of-the-art imaging equipment', ms: 'Alami diagnostik tepat dengan peralatan pengimejan canggih kami', ar: 'اختبر التشخيص الدقيق مع أحدث معدات التصوير لدينا' },
    cta: { fr: 'Découvrir', en: 'Explore Now', ms: 'Terokai Kini', ar: 'استكشف الآن' },
    link: '/product/proscan-digital-xray',
  },
  {
    id: 'slide-2',
    image: 'slide2',
    title: { fr: 'Monitoring Patient d\'Excellence', en: 'Patient Monitoring Excellence', ms: 'Kecemerlangan Pemantauan Pesakit', ar: 'تميز مراقبة المريض' },
    subtitle: { fr: 'Série VitalGuard Monitor', en: 'VitalGuard Monitor Series', ms: 'Siri Monitor VitalGuard', ar: 'سلسلة مراقب VitalGuard' },
    description: { fr: 'Surveillance des signes vitaux en temps réel avec systèmes d\'alarme avancés', en: 'Real-time vital sign monitoring with advanced alarm systems', ms: 'Pemantauan tanda-tanda vital masa nyata dengan sistem penggera termaju', ar: 'مراقبة العلامات الحيوية في الوقت الفعلي مع أنظمة الإنذار المتقدمة' },
    cta: { fr: 'En savoir plus', en: 'Learn More', ms: 'Ketahui Lebih Lanjut', ar: 'اعرف المزيد' },
    link: '/product/vitalguard-patient-monitor',
  },
  {
    id: 'slide-3',
    image: 'slide3',
    title: { fr: 'Instruments Chirurgicaux de Précision', en: 'Surgical Precision Tools', ms: 'Instrumen Pembedahan Berpresisi', ar: 'أدوات الجراحة الدقيقة' },
    subtitle: { fr: 'Instruments de Qualité Professionnelle', en: 'Professional Grade Instruments', ms: 'Instrumen Gred Profesional', ar: 'أدوات بجودة احترافية' },
    description: { fr: 'La confiance des professionnels de santé dans le monde entier', en: 'Trusted by healthcare professionals worldwide', ms: 'Dipercayai oleh profesional kesihatan di seluruh dunia', ar: 'موثوق به من قبل المتخصصين في الرعاية الصحية حول العالم' },
    cta: { fr: 'Voir la collection', en: 'View Collection', ms: 'Lihat Koleksi', ar: 'عرض المجموعة' },
    link: '/category/instruments-chirurgicaux',
  },
];

export const staticPages: StaticPage[] = [
  {
    id: 'page-cgu',
    slug: 'cgu',
    title: { fr: 'Conditions Générales d\'Utilisation', en: 'Terms & Conditions', ms: 'Terma & Syarat', ar: 'الشروط والأحكام' },
    content: {
      fr: `# Conditions Générales d'Utilisation

## Article 1 - Objet
Les présentes CGU régissent l'utilisation du site web Althea Systems et la vente de matériel médical professionnel.

## Article 2 - Accès au site
L'accès au site est gratuit. L'utilisateur assume la responsabilité de disposer des moyens nécessaires pour accéder au service.

## Article 3 - Propriété intellectuelle
L'ensemble des contenus du site (textes, images, logos) sont la propriété exclusive d'Althea Systems ou de ses partenaires.

## Article 4 - Protection des données
Conformément au RGPD, les données personnelles collectées sont traitées de manière sécurisée. L'utilisateur dispose d'un droit d'accès, de modification et de suppression de ses données.

## Article 5 - Responsabilité
Althea Systems s'engage à fournir des produits conformes aux normes médicales en vigueur. La responsabilité est limitée au montant de la commande.

## Article 6 - Droit applicable
Les présentes CGU sont soumises au droit français. En cas de litige, les tribunaux de Paris seront compétents.

*Dernière mise à jour : 1er janvier 2026*`,
      en: `# Terms & Conditions

## Article 1 - Purpose
These T&C govern the use of the Althea Systems website and the sale of professional medical equipment.

## Article 2 - Site Access
Access to the site is free. The user assumes responsibility for having the necessary means to access the service.

## Article 3 - Intellectual Property
All site content (texts, images, logos) are the exclusive property of Althea Systems or its partners.

## Article 4 - Data Protection
In accordance with GDPR, personal data collected is processed securely. Users have the right to access, modify and delete their data.

## Article 5 - Liability
Althea Systems is committed to providing products that comply with current medical standards. Liability is limited to the order amount.

## Article 6 - Applicable Law
These T&C are subject to French law. In case of dispute, the courts of Paris shall have jurisdiction.

*Last updated: January 1, 2026*`,
      ms: `# Terma & Syarat

## Artikel 1 - Tujuan
Terma & Syarat ini mengawal penggunaan laman web Althea Systems dan penjualan peralatan perubatan profesional.

## Artikel 2 - Akses Laman
Akses ke laman adalah percuma. Pengguna bertanggungjawab untuk mempunyai cara yang diperlukan bagi mengakses perkhidmatan.

## Artikel 3 - Harta Intelek
Semua kandungan laman (teks, imej, logo) adalah milik eksklusif Althea Systems atau rakan kongsinya.

## Artikel 4 - Perlindungan Data
Selaras dengan GDPR, data peribadi yang dikumpul diproses dengan selamat. Pengguna berhak untuk mengakses, mengubah dan memadam data mereka.

## Artikel 5 - Liabiliti
Althea Systems komited untuk menyediakan produk yang mematuhi piawaian perubatan semasa. Liabiliti terhad kepada jumlah pesanan.

## Artikel 6 - Undang-Undang Berkenaan
Terma & Syarat ini tertakluk kepada undang-undang Perancis. Sekiranya berlaku pertikaian, mahkamah Paris akan mempunyai bidang kuasa.

*Kemas kini terakhir: 1 Januari 2026*`,
      ar: `# الشروط والأحكام

## المادة 1 - الغرض
تحكم هذه الشروط والأحكام استخدام موقع Althea Systems وبيع المعدات الطبية المهنية.

## المادة 2 - الوصول إلى الموقع
الوصول إلى الموقع مجاني. يتحمل المستخدم مسؤولية امتلاك الوسائل اللازمة للوصول إلى الخدمة.

## المادة 3 - الملكية الفكرية
جميع محتويات الموقع (نصوص، صور، شعارات) هي ملكية حصرية لـ Althea Systems أو شركائها.

## المادة 4 - حماية البيانات
وفقاً للائحة GDPR، تتم معالجة البيانات الشخصية المجمعة بشكل آمن. يحق للمستخدمين الوصول إلى بياناتهم وتعديلها وحذفها.

## المادة 5 - المسؤولية
تلتزم Althea Systems بتقديم منتجات تمتثل للمعايير الطبية المعمول بها. تقتصر المسؤولية على قيمة الطلب.

## المادة 6 - القانون المطبق
تخضع هذه الشروط والأحكام للقانون الفرنسي. في حالة النزاع، تختص محاكم باريس بالفصل فيه.

*آخر تحديث: 1 يناير 2026*`,
    },
    updatedAt: '2026-01-01',
  },
  {
    id: 'page-legal',
    slug: 'mentions-legales',
    title: { fr: 'Mentions Légales', en: 'Legal Notice', ms: 'Notis Undang-Undang', ar: 'إشعار قانوني' },
    content: {
      fr: `# Mentions Légales

## Éditeur du site
**Althea Systems SAS**
Capital social : 500 000 €
RCS Paris B 123 456 789
SIRET : 123 456 789 00012
TVA intracommunautaire : FR 12 345678901

## Siège social
1234 Medical Plaza
75008 Paris, France

## Directeur de la publication
M. Jean-Pierre Althea, Président

## Hébergement
Vercel Inc.
440 N Barranca Ave #4133
Covina, CA 91723, USA

## Contact
Email : contact@altheasystems.com
Téléphone : +33 (0)1 23 45 67 89

## Protection des données (DPO)
M. Thomas Privacy
dpo@altheasystems.com

*Dernière mise à jour : 1er janvier 2026*`,
      en: `# Legal Notice

## Site Publisher
**Althea Systems SAS**
Share capital: €500,000
RCS Paris B 123 456 789
SIRET: 123 456 789 00012
EU VAT: FR 12 345678901

## Registered Office
1234 Medical Plaza
75008 Paris, France

## Publication Director
Mr. Jean-Pierre Althea, President

## Hosting
Vercel Inc.
440 N Barranca Ave #4133
Covina, CA 91723, USA

## Contact
Email: contact@altheasystems.com
Phone: +33 (0)1 23 45 67 89

## Data Protection Officer
Mr. Thomas Privacy
dpo@altheasystems.com

*Last updated: January 1, 2026*`,
      ms: `# Notis Undang-Undang

## Penerbit Laman
**Althea Systems SAS**
Modal syer: €500,000
RCS Paris B 123 456 789
SIRET: 123 456 789 00012
VAT EU: FR 12 345678901

## Pejabat Berdaftar
1234 Medical Plaza
75008 Paris, Perancis

## Pengarah Penerbitan
En. Jean-Pierre Althea, Presiden

## Pengehosan
Vercel Inc.
440 N Barranca Ave #4133
Covina, CA 91723, USA

## Hubungi
Emel: contact@altheasystems.com
Tel: +33 (0)1 23 45 67 89

## Pegawai Perlindungan Data
En. Thomas Privacy
dpo@altheasystems.com

*Kemas kini terakhir: 1 Januari 2026*`,
      ar: `# إشعار قانوني

## ناشر الموقع
**Althea Systems SAS**
رأس المال: 500,000 يورو
RCS Paris B 123 456 789
SIRET: 123 456 789 00012
رقم VAT الأوروبي: FR 12 345678901

## المقر الرسمي
1234 Medical Plaza
75008 باريس، فرنسا

## مدير النشر
السيد Jean-Pierre Althea، الرئيس

## الاستضافة
Vercel Inc.
440 N Barranca Ave #4133
Covina, CA 91723, USA

## التواصل
البريد الإلكتروني: contact@altheasystems.com
الهاتف: +33 (0)1 23 45 67 89

## مسؤول حماية البيانات
السيد Thomas Privacy
dpo@altheasystems.com

*آخر تحديث: 1 يناير 2026*`,
    },
    updatedAt: '2026-01-01',
  },
  {
    id: 'page-about',
    slug: 'a-propos',
    title: { fr: 'À Propos', en: 'About Us', ms: 'Tentang Kami', ar: 'نبذة عنا' },
    content: {
      fr: `# À Propos d'Althea Systems

## Notre Mission
Depuis 2005, Althea Systems fournit aux professionnels de santé des équipements médicaux de pointe. Notre mission est de rendre les technologies médicales les plus avancées accessibles à tous les établissements de santé.

## Notre Expertise
Avec plus de 20 ans d'expérience dans le secteur du matériel médical, nous accompagnons nos clients dans le choix, l'installation et la maintenance de leurs équipements.

## Nos Valeurs
- **Excellence** : Nous sélectionnons uniquement des produits répondant aux plus hautes normes de qualité.
- **Innovation** : Nous restons à la pointe de la technologie médicale.
- **Service** : Un accompagnement personnalisé avant, pendant et après l'achat.
- **Confiance** : Des partenariats durables avec nos clients et fournisseurs.

## Nos Chiffres
- 500+ établissements de santé clients
- 10 000+ produits livrés par an
- 98% de satisfaction client
- 50+ collaborateurs dédiés

## Certifications
- ISO 13485 (Dispositifs médicaux)
- ISO 9001 (Management de la qualité)
- Marquage CE sur tous nos produits`,
      en: `# About Althea Systems

## Our Mission
Since 2005, Althea Systems has been providing healthcare professionals with cutting-edge medical equipment. Our mission is to make the most advanced medical technologies accessible to all healthcare facilities.

## Our Expertise
With over 20 years of experience in the medical equipment sector, we support our clients in choosing, installing and maintaining their equipment.

## Our Values
- **Excellence**: We select only products that meet the highest quality standards.
- **Innovation**: We stay at the forefront of medical technology.
- **Service**: Personalized support before, during and after purchase.
- **Trust**: Lasting partnerships with our clients and suppliers.

## Our Numbers
- 500+ healthcare facility clients
- 10,000+ products delivered per year
- 98% customer satisfaction
- 50+ dedicated employees

## Certifications
- ISO 13485 (Medical Devices)
- ISO 9001 (Quality Management)
- CE marking on all products`,
      ms: `# Tentang Althea Systems

## Misi Kami
Sejak 2005, Althea Systems telah menyediakan profesional kesihatan dengan peralatan perubatan termaju. Misi kami adalah menjadikan teknologi perubatan paling canggih dapat diakses oleh semua kemudahan penjagaan kesihatan.

## Kepakaran Kami
Dengan pengalaman lebih 20 tahun dalam sektor peralatan perubatan, kami menyokong pelanggan dalam memilih, memasang dan menyelenggara peralatan mereka.

## Nilai Kami
- **Kecemerlangan**: Kami hanya memilih produk yang memenuhi piawaian kualiti tertinggi.
- **Inovasi**: Kami sentiasa berada di hadapan teknologi perubatan.
- **Perkhidmatan**: Sokongan peribadi sebelum, semasa dan selepas pembelian.
- **Kepercayaan**: Perkongsian berkekalan dengan pelanggan dan pembekal kami.

## Angka Kami
- 500+ kemudahan penjagaan kesihatan sebagai pelanggan
- 10,000+ produk dihantar setahun
- 98% kepuasan pelanggan
- 50+ pekerja berdedikasi

## Pensijilan
- ISO 13485 (Peranti Perubatan)
- ISO 9001 (Pengurusan Kualiti)
- Penandaan CE pada semua produk`,
      ar: `# نبذة عن Althea Systems

## مهمتنا
منذ عام 2005، تقدم Althea Systems للمتخصصين في الرعاية الصحية أحدث المعدات الطبية. مهمتنا هي جعل أكثر تقنيات الطب تقدماً في متناول جميع مرافق الرعاية الصحية.

## خبرتنا
بخبرة تتجاوز 20 عاماً في قطاع المعدات الطبية، ندعم عملاءنا في اختيار معداتهم وتركيبها وصيانتها.

## قيمنا
- **التميز**: نختار فقط المنتجات التي تلبي أعلى معايير الجودة.
- **الابتكار**: نبقى في طليعة التكنولوجيا الطبية.
- **الخدمة**: دعم شخصي قبل الشراء وأثناءه وبعده.
- **الثقة**: شراكات دائمة مع عملائنا ومورّدينا.

## أرقامنا
- أكثر من 500 مرفق رعاية صحية كعملاء
- أكثر من 10,000 منتج مسلّم سنوياً
- 98% رضا العملاء
- أكثر من 50 موظفاً متخصصاً

## الشهادات
- ISO 13485 (الأجهزة الطبية)
- ISO 9001 (إدارة الجودة)
- علامة CE على جميع المنتجات`,
    },
    updatedAt: '2026-01-15',
  },
];

export const marketingText = {
  fr: 'Althea Systems est votre partenaire de confiance pour l\'équipement médical de pointe. Nous proposons une gamme complète de dispositifs médicaux certifiés, des systèmes d\'imagerie aux instruments chirurgicaux, en passant par le mobilier hospitalier et les équipements de protection. **Qualité certifiée. Livraison rapide. Support expert.**',
  en: 'Althea Systems is your trusted partner for cutting-edge medical equipment. We offer a comprehensive range of certified medical devices, from imaging systems to surgical instruments, hospital furniture and protective equipment. **Certified quality. Fast delivery. Expert support.**',
  ms: 'Althea Systems adalah rakan kongsi dipercayai anda untuk peralatan perubatan termaju. Kami menawarkan rangkaian lengkap peranti perubatan bersertifikasi, dari sistem pengimejan hingga instrumen pembedahan, perabot hospital dan peralatan perlindungan. **Kualiti bersertifikasi. Penghantaran pantas. Sokongan pakar.**',
  ar: 'Althea Systems هي شريكك الموثوق للمعدات الطبية المتطورة. نقدم مجموعة شاملة من الأجهزة الطبية المعتمدة، من أنظمة التصوير إلى الأدوات الجراحية والأثاث الطبي ومعدات الحماية. **جودة معتمدة. توصيل سريع. دعم متخصص.**',
};
