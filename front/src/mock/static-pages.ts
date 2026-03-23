import type { StaticPage, HeroSlide } from './types';

export const heroSlides: HeroSlide[] = [
  {
    id: 'slide-1',
    image: 'slide1',
    title: { fr: 'Solutions d\'Imagerie Avancées', en: 'Advanced Imaging Solutions' },
    subtitle: { fr: 'Système ProScan Radiographie Numérique', en: 'ProScan Digital X-Ray System' },
    description: { fr: 'Découvrez un diagnostic de précision avec nos équipements d\'imagerie de pointe', en: 'Experience precision diagnostics with our state-of-the-art imaging equipment' },
    cta: { fr: 'Découvrir', en: 'Explore Now' },
    link: '/product/proscan-digital-xray',
  },
  {
    id: 'slide-2',
    image: 'slide2',
    title: { fr: 'Monitoring Patient d\'Excellence', en: 'Patient Monitoring Excellence' },
    subtitle: { fr: 'Série VitalGuard Monitor', en: 'VitalGuard Monitor Series' },
    description: { fr: 'Surveillance des signes vitaux en temps réel avec systèmes d\'alarme avancés', en: 'Real-time vital sign monitoring with advanced alarm systems' },
    cta: { fr: 'En savoir plus', en: 'Learn More' },
    link: '/product/vitalguard-patient-monitor',
  },
  {
    id: 'slide-3',
    image: 'slide3',
    title: { fr: 'Instruments Chirurgicaux de Précision', en: 'Surgical Precision Tools' },
    subtitle: { fr: 'Instruments de Qualité Professionnelle', en: 'Professional Grade Instruments' },
    description: { fr: 'La confiance des professionnels de santé dans le monde entier', en: 'Trusted by healthcare professionals worldwide' },
    cta: { fr: 'Voir la collection', en: 'View Collection' },
    link: '/category/instruments-chirurgicaux',
  },
];

export const staticPages: StaticPage[] = [
  {
    id: 'page-cgu',
    slug: 'cgu',
    title: { fr: 'Conditions Générales d\'Utilisation', en: 'Terms & Conditions' },
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
    },
    updatedAt: '2026-01-01',
  },
  {
    id: 'page-legal',
    slug: 'mentions-legales',
    title: { fr: 'Mentions Légales', en: 'Legal Notice' },
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
    },
    updatedAt: '2026-01-01',
  },
  {
    id: 'page-about',
    slug: 'a-propos',
    title: { fr: 'À Propos', en: 'About Us' },
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
    },
    updatedAt: '2026-01-15',
  },
];

export const marketingText = {
  fr: 'Althea Systems est votre partenaire de confiance pour l\'équipement médical de pointe. Nous proposons une gamme complète de dispositifs médicaux certifiés, des systèmes d\'imagerie aux instruments chirurgicaux, en passant par le mobilier hospitalier et les équipements de protection. **Qualité certifiée. Livraison rapide. Support expert.**',
  en: 'Althea Systems is your trusted partner for cutting-edge medical equipment. We offer a comprehensive range of certified medical devices, from imaging systems to surgical instruments, hospital furniture and protective equipment. **Certified quality. Fast delivery. Expert support.**',
};
