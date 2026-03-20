import type { ContactMessage, ChatConversation, SupportTicket } from './types';

export const contactMessages: ContactMessage[] = [
  { id: 'msg-1', email: 'dr.lemaire@hopital.fr', subject: 'Demande de devis pour équipement imagerie', message: 'Bonjour, nous souhaitons équiper notre nouveau service de radiologie. Pourriez-vous nous faire parvenir un devis pour 2 systèmes ProScan et 1 échographe UltraView ?', status: 'unread', createdAt: '2026-02-18T08:30:00Z' },
  { id: 'msg-2', email: 'achat@clinique-etoile.fr', subject: 'Problème de livraison commande ORD-2026-003', message: 'Notre commande devait arriver le 15 février mais nous n\'avons toujours rien reçu. Merci de nous tenir informés.', status: 'unread', createdAt: '2026-02-17T14:20:00Z' },
  { id: 'msg-3', email: 'sophie.martin@hopital-lyon.fr', subject: 'Retour SAV - Moniteur VitalGuard défectueux', message: 'Un de nos moniteurs VitalGuard présente un dysfonctionnement de l\'écran tactile après 6 mois d\'utilisation. Comment procéder pour le SAV ?', status: 'read', createdAt: '2026-02-16T10:15:00Z' },
  { id: 'msg-4', email: 'formation@chu-marseille.fr', subject: 'Formation utilisation BioAnalyzer', message: 'Proposez-vous des formations pour l\'utilisation du BioAnalyzer Blood Chemistry System ? Pour une équipe de 8 techniciens.', status: 'treated', createdAt: '2026-02-15T16:45:00Z' },
  { id: 'msg-5', email: 'comptabilite@hopital-nice.fr', subject: 'Facture manquante', message: 'Nous n\'avons pas reçu la facture pour notre dernière commande. Pouvez-vous nous la renvoyer par email ?', status: 'treated', createdAt: '2026-02-14T09:00:00Z' },
  { id: 'msg-6', email: 'urgences@chr-lille.fr', subject: 'Disponibilité défibrillateurs', message: 'Nous devons renouveler 10 défibrillateurs AED. Quel est le délai de livraison actuel ?', status: 'unread', createdAt: '2026-02-13T11:30:00Z' },
  { id: 'msg-7', email: 'pharmacie@hopital-nantes.fr', subject: 'Compatibilité consommables autoclave', message: 'Les consommables de notre ancien autoclave sont-ils compatibles avec le SteriliPro 450L ?', status: 'read', createdAt: '2026-02-12T15:00:00Z' },
  { id: 'msg-8', email: 'direction@clinique-ocean.fr', subject: 'Partenariat long terme', message: 'Nous souhaitons établir un partenariat d\'approvisionnement sur 3 ans. Proposez-vous des tarifs préférentiels ?', status: 'unread', createdAt: '2026-02-11T08:45:00Z' },
  { id: 'msg-9', email: 'tech@labo-pasteur.fr', subject: 'Mise à jour logiciel microscope', message: 'Y a-t-il une mise à jour du logiciel d\'analyse pour le MicroScope Digital ? Notre version date de 6 mois.', status: 'treated', createdAt: '2026-02-10T13:20:00Z' },
  { id: 'msg-10', email: 'achat@hopital-strasbourg.fr', subject: 'Demande catalogue 2026', message: 'Pourriez-vous nous envoyer votre catalogue produits 2026 au format PDF ?', status: 'treated', createdAt: '2026-02-09T10:10:00Z' },
  { id: 'msg-11', email: 'maintenance@chr-toulouse.fr', subject: 'Pièces détachées ventilateur', message: 'Nous recherchons des filtres de remplacement pour le RespiraCare Ventilator Pro. Disponibilité ?', status: 'unread', createdAt: '2026-02-08T14:30:00Z' },
  { id: 'msg-12', email: 'infirmiere@maison-retraite.fr', subject: 'Recommandation lit médicalisé', message: 'Quel lit recommandez-vous pour une maison de retraite de 80 résidents ? Budget limité.', status: 'read', createdAt: '2026-02-07T09:15:00Z' },
  { id: 'msg-13', email: 'chef.service@hopital-grenoble.fr', subject: 'Démonstration ECG CardioMax', message: 'Serait-il possible d\'organiser une démonstration de l\'ECG CardioMax dans notre service de cardiologie ?', status: 'treated', createdAt: '2026-02-06T16:00:00Z' },
  { id: 'msg-14', email: 'logistique@clinique-sud.fr', subject: 'Conditions de livraison matériel lourd', message: 'Comment se passe la livraison et l\'installation d\'un CT-Scan ? Besoin de préparer nos locaux.', status: 'unread', createdAt: '2026-02-05T11:45:00Z' },
  { id: 'msg-15', email: 'qualite@hopital-rennes.fr', subject: 'Certificats de conformité', message: 'Pouvez-vous nous fournir les certificats CE et les fiches de données de sécurité pour les EPI commandés ?', status: 'read', createdAt: '2026-02-04T08:30:00Z' },
];

export const chatConversations: ChatConversation[] = [
  {
    id: 'chat-1', email: 'dr.petit@cabinet.fr', messages: [
      { id: 'cm-1', role: 'bot', content: 'Bonjour ! Je suis l\'assistant virtuel d\'Althea Systems. Comment puis-je vous aider ?', timestamp: '2026-02-18T10:00:00Z' },
      { id: 'cm-2', role: 'user', content: 'Quel est le prix du moniteur VitalGuard ?', timestamp: '2026-02-18T10:01:00Z' },
      { id: 'cm-3', role: 'bot', content: 'Le VitalGuard Patient Monitor est proposé à 8 500 € HT (10 200 € TTC). Il est actuellement en stock avec 25 unités disponibles. Souhaitez-vous plus d\'informations ?', timestamp: '2026-02-18T10:01:30Z' },
      { id: 'cm-4', role: 'user', content: 'Il est compatible avec les systèmes PACS ?', timestamp: '2026-02-18T10:02:00Z' },
      { id: 'cm-5', role: 'bot', content: 'Le VitalGuard dispose de connectivité WiFi, USB et LAN pour l\'export de données. Pour la compatibilité PACS spécifique, je vous recommande de contacter notre équipe technique pour une réponse détaillée. Souhaitez-vous que je crée un ticket support ?', timestamp: '2026-02-18T10:02:30Z' },
    ], escalated: false, createdAt: '2026-02-18T10:00:00Z',
  },
  {
    id: 'chat-2', email: 'infirmiere.dupont@hopital.fr', messages: [
      { id: 'cm-6', role: 'bot', content: 'Bonjour ! Comment puis-je vous aider aujourd\'hui ?', timestamp: '2026-02-17T14:00:00Z' },
      { id: 'cm-7', role: 'user', content: 'J\'ai un problème avec ma commande ORD-2026-003', timestamp: '2026-02-17T14:01:00Z' },
      { id: 'cm-8', role: 'bot', content: 'Je comprends. Pouvez-vous me préciser le problème rencontré avec votre commande ?', timestamp: '2026-02-17T14:01:30Z' },
      { id: 'cm-9', role: 'user', content: 'Le colis est arrivé abîmé, l\'emballage était déchiré', timestamp: '2026-02-17T14:02:00Z' },
      { id: 'cm-10', role: 'bot', content: 'Je suis désolé pour ce désagrément. Ce type de problème nécessite l\'intervention de notre service client. Souhaitez-vous que je crée un ticket pour qu\'un conseiller vous contacte ?', timestamp: '2026-02-17T14:02:30Z' },
      { id: 'cm-11', role: 'user', content: 'Oui, merci', timestamp: '2026-02-17T14:03:00Z' },
      { id: 'cm-12', role: 'bot', content: 'Ticket créé ! Notre équipe vous contactera par email dans les plus brefs délais. Référence ticket : TK-003.', timestamp: '2026-02-17T14:03:30Z' },
    ], escalated: true, ticketId: 'tk-3', createdAt: '2026-02-17T14:00:00Z',
  },
  {
    id: 'chat-3', email: 'achat@clinique.fr', messages: [
      { id: 'cm-13', role: 'bot', content: 'Bonjour ! Comment puis-je vous aider ?', timestamp: '2026-02-16T09:00:00Z' },
      { id: 'cm-14', role: 'user', content: 'Quels sont vos délais de livraison ?', timestamp: '2026-02-16T09:01:00Z' },
      { id: 'cm-15', role: 'bot', content: 'Nos délais de livraison sont : Standard (5-7 jours ouvrés), Express (2-3 jours ouvrés), Livraison 24h pour les urgences. Les frais varient de 15€ à 75€ selon l\'option choisie.', timestamp: '2026-02-16T09:01:30Z' },
    ], escalated: false, createdAt: '2026-02-16T09:00:00Z',
  },
  {
    id: 'chat-4', userId: 'user-1', email: 'sophie.martin@hopital-lyon.fr', messages: [
      { id: 'cm-16', role: 'bot', content: 'Bonjour Dr. Martin ! Comment puis-je vous aider ?', timestamp: '2026-02-15T11:00:00Z' },
      { id: 'cm-17', role: 'user', content: 'Je cherche un spiromètre portable pour mon cabinet', timestamp: '2026-02-15T11:01:00Z' },
      { id: 'cm-18', role: 'bot', content: 'Je vous recommande le SpiroMax Digital Spirometer. C\'est un spiromètre numérique portable avec logiciel d\'analyse intégré, proposé à 3 200 € HT. Il est nouveau dans notre catalogue et en stock.', timestamp: '2026-02-15T11:01:30Z' },
      { id: 'cm-19', role: 'user', content: 'Peut-il se connecter à mon logiciel médical ?', timestamp: '2026-02-15T11:02:00Z' },
      { id: 'cm-20', role: 'bot', content: 'Le SpiroMax dispose d\'une connectivité USB et Bluetooth. Pour la compatibilité avec votre logiciel médical spécifique, je vous recommande de contacter notre support technique. Voulez-vous créer un ticket ?', timestamp: '2026-02-15T11:02:30Z' },
      { id: 'cm-21', role: 'user', content: 'Oui, créez un ticket svp', timestamp: '2026-02-15T11:03:00Z' },
      { id: 'cm-22', role: 'bot', content: 'Ticket créé avec la référence TK-004. Un technicien vous contactera sous 24h.', timestamp: '2026-02-15T11:03:30Z' },
    ], escalated: true, ticketId: 'tk-4', createdAt: '2026-02-15T11:00:00Z',
  },
  {
    id: 'chat-5', email: 'pharmacien@officine.fr', messages: [
      { id: 'cm-23', role: 'bot', content: 'Bonjour ! Comment puis-je vous aider ?', timestamp: '2026-02-14T16:00:00Z' },
      { id: 'cm-24', role: 'user', content: 'Avez-vous des nébuliseurs en stock ?', timestamp: '2026-02-14T16:01:00Z' },
      { id: 'cm-25', role: 'bot', content: 'Oui ! Le MistPro Nébuliseur Ultrasonique est en stock (50 unités) à 450 € HT. Il est silencieux (<30 dB) et très efficace pour l\'aérosolthérapie.', timestamp: '2026-02-14T16:01:30Z' },
    ], escalated: false, createdAt: '2026-02-14T16:00:00Z',
  },
  { id: 'chat-6', email: 'etudiant@univ.fr', messages: [{ id: 'cm-26', role: 'bot', content: 'Bonjour ! Comment puis-je vous aider ?', timestamp: '2026-02-13T10:00:00Z' }, { id: 'cm-27', role: 'user', content: 'C\'est quoi un autoclave ?', timestamp: '2026-02-13T10:01:00Z' }, { id: 'cm-28', role: 'bot', content: 'Un autoclave est un appareil de stérilisation qui utilise la vapeur sous pression pour éliminer les micro-organismes. Il est essentiel dans les environnements médicaux pour stériliser les instruments chirurgicaux.', timestamp: '2026-02-13T10:01:30Z' }], escalated: false, createdAt: '2026-02-13T10:00:00Z' },
  { id: 'chat-7', email: 'directeur@ehpad.fr', messages: [{ id: 'cm-29', role: 'bot', content: 'Bonjour ! Comment puis-je vous aider ?', timestamp: '2026-02-12T14:00:00Z' }, { id: 'cm-30', role: 'user', content: 'Je veux équiper 20 chambres en lits médicalisés', timestamp: '2026-02-12T14:01:00Z' }, { id: 'cm-31', role: 'bot', content: 'Pour un projet de cette envergure, je vous recommande de contacter notre équipe commerciale qui pourra vous proposer un devis personnalisé. Voulez-vous créer un ticket ?', timestamp: '2026-02-12T14:01:30Z' }, { id: 'cm-32', role: 'user', content: 'Oui', timestamp: '2026-02-12T14:02:00Z' }, { id: 'cm-33', role: 'bot', content: 'Ticket TK-005 créé. Notre équipe commerciale vous contactera.', timestamp: '2026-02-12T14:02:30Z' }], escalated: true, ticketId: 'tk-5', createdAt: '2026-02-12T14:00:00Z' },
  { id: 'chat-8', email: 'achat@veterinaire.fr', messages: [{ id: 'cm-34', role: 'bot', content: 'Bonjour !', timestamp: '2026-02-11T09:00:00Z' }, { id: 'cm-35', role: 'user', content: 'Vos produits sont-ils adaptés à l\'usage vétérinaire ?', timestamp: '2026-02-11T09:01:00Z' }, { id: 'cm-36', role: 'bot', content: 'Nos équipements sont conçus pour un usage médical humain. Certains peuvent être utilisés en contexte vétérinaire, mais je vous recommande de vérifier les spécifications. Voulez-vous contacter un conseiller ?', timestamp: '2026-02-11T09:01:30Z' }], escalated: false, createdAt: '2026-02-11T09:00:00Z' },
  { id: 'chat-9', email: 'technicien@hopital.fr', messages: [{ id: 'cm-37', role: 'bot', content: 'Bonjour !', timestamp: '2026-02-10T15:00:00Z' }, { id: 'cm-38', role: 'user', content: 'Comment calibrer le BioAnalyzer ?', timestamp: '2026-02-10T15:01:00Z' }, { id: 'cm-39', role: 'bot', content: 'Le BioAnalyzer dispose d\'un système d\'auto-calibration. Pour une calibration manuelle, consultez le manuel technique section 5.2. Si vous rencontrez des difficultés, créez un ticket support.', timestamp: '2026-02-10T15:01:30Z' }, { id: 'cm-40', role: 'user', content: 'OK je vais créer un ticket, l\'auto-calibration ne fonctionne pas', timestamp: '2026-02-10T15:02:00Z' }, { id: 'cm-41', role: 'bot', content: 'Ticket TK-006 créé. Un technicien spécialisé vous contactera.', timestamp: '2026-02-10T15:02:30Z' }], escalated: true, ticketId: 'tk-6', createdAt: '2026-02-10T15:00:00Z' },
  { id: 'chat-10', email: 'secretaire@medecin.fr', messages: [{ id: 'cm-42', role: 'bot', content: 'Bonjour !', timestamp: '2026-02-09T11:00:00Z' }, { id: 'cm-43', role: 'user', content: 'Quels sont vos horaires ?', timestamp: '2026-02-09T11:01:00Z' }, { id: 'cm-44', role: 'bot', content: 'Notre service client est disponible du lundi au vendredi de 8h à 18h. Je suis disponible 24h/24 pour répondre à vos questions !', timestamp: '2026-02-09T11:01:30Z' }], escalated: false, createdAt: '2026-02-09T11:00:00Z' },
];

export const supportTickets: SupportTicket[] = [
  { id: 'tk-1', contactMessageId: 'msg-1', email: 'dr.lemaire@hopital.fr', subject: 'Devis équipement imagerie', status: 'open', createdAt: '2026-02-18T08:35:00Z', updatedAt: '2026-02-18T08:35:00Z' },
  { id: 'tk-2', contactMessageId: 'msg-2', email: 'achat@clinique-etoile.fr', subject: 'Problème livraison ORD-2026-003', status: 'open', createdAt: '2026-02-17T14:25:00Z', updatedAt: '2026-02-17T14:25:00Z' },
  { id: 'tk-3', conversationId: 'chat-2', email: 'infirmiere.dupont@hopital.fr', subject: 'Colis endommagé - ORD-2026-003', status: 'in_progress', createdAt: '2026-02-17T14:03:30Z', updatedAt: '2026-02-17T16:00:00Z' },
  { id: 'tk-4', conversationId: 'chat-4', email: 'sophie.martin@hopital-lyon.fr', subject: 'Compatibilité SpiroMax avec logiciel médical', status: 'in_progress', createdAt: '2026-02-15T11:03:30Z', updatedAt: '2026-02-16T09:00:00Z' },
  { id: 'tk-5', conversationId: 'chat-7', email: 'directeur@ehpad.fr', subject: 'Devis 20 lits médicalisés EHPAD', status: 'closed', createdAt: '2026-02-12T14:02:30Z', updatedAt: '2026-02-14T10:00:00Z' },
  { id: 'tk-6', conversationId: 'chat-9', email: 'technicien@hopital.fr', subject: 'Panne auto-calibration BioAnalyzer', status: 'open', createdAt: '2026-02-10T15:02:30Z', updatedAt: '2026-02-10T15:02:30Z' },
];
