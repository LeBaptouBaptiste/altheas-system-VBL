/**
 * Chatbot localized strings + response templates.
 *
 * Split into two structures because they have different shapes:
 *  - `BOT_STRINGS`  → simple strings keyed by intent (greeting, price, …)
 *  - `PRODUCT_TPL`  → functions that build a per-product reply from the
 *                     product's localized fields. Keeping it separate
 *                     avoids the need for `as unknown as Record<...>`
 *                     hacks that would erase the function signature.
 */

export type BotLocale = 'fr' | 'en' | 'ms' | 'ar';

export const BOT_STRINGS: Record<string, Record<BotLocale, string>> = {
  stock_in:  { fr: 'en stock',     en: 'in stock',     ms: 'dalam stok',     ar: 'متوفر في المخزون' },
  stock_low: { fr: 'stock faible', en: 'low stock',    ms: 'stok rendah',    ar: 'مخزون منخفض' },
  stock_out: { fr: 'rupture',      en: 'out of stock', ms: 'kehabisan stok', ar: 'نفاد المخزون' },

  price: {
    fr: 'Nos prix sont affichés TTC sur chaque fiche produit. Pour un devis personnalisé, contactez notre équipe commerciale.',
    en: 'Our prices are displayed including VAT on each product page. For a custom quote, contact our sales team.',
    ms: 'Harga kami dipaparkan termasuk cukai pada setiap halaman produk. Untuk sebut harga, hubungi pasukan jualan kami.',
    ar: 'تُعرض أسعارنا شاملة الضريبة على كل صفحة منتج. للحصول على عرض سعر مخصص، تواصل مع فريق المبيعات.',
  },
  shipping: {
    fr: 'Nous proposons 3 modes de livraison : Standard (5-7 jours, 15€), Express (2-3 jours, 35€), 24h (75€).',
    en: 'We offer 3 shipping methods: Standard (5-7 days, €15), Express (2-3 days, €35), Overnight (€75).',
    ms: 'Kami menawarkan 3 kaedah penghantaran: Standard (5-7 hari, €15), Ekspres (2-3 hari, €35), 24 jam (€75).',
    ar: 'نقدم 3 طرق شحن: عادي (5-7 أيام، 15€)، سريع (2-3 أيام، 35€)، 24 ساعة (75€).',
  },
  returns: {
    fr: 'Pour tout retour ou SAV, contactez notre service client. Je peux créer un ticket si vous le souhaitez.',
    en: 'For any returns or after-sales service, contact our customer service. I can create a ticket if you wish.',
    ms: 'Untuk sebarang pemulangan atau perkhidmatan selepas jualan, hubungi khidmat pelanggan kami. Saya boleh buat tiket jika anda mahu.',
    ar: 'لأي إرجاع أو خدمة ما بعد البيع، تواصل مع خدمة العملاء. يمكنني إنشاء تذكرة دعم إن أردت.',
  },
  hours: {
    fr: 'Notre service client est disponible du lundi au vendredi de 8h à 18h. Je suis disponible 24h/24 !',
    en: "Our customer service is available Monday to Friday from 8am to 6pm. I'm available 24/7!",
    ms: 'Khidmat pelanggan kami tersedia Isnin hingga Jumaat 8 pagi hingga 6 petang. Saya sedia 24/7!',
    ar: 'خدمة العملاء متاحة من الإثنين إلى الجمعة من 8 صباحاً حتى 6 مساءً. أنا متاح على مدار الساعة!',
  },
  greeting: {
    fr: "Bonjour ! Comment puis-je vous aider aujourd'hui ?",
    en: 'Hello! How can I help you today?',
    ms: 'Helo! Bagaimana saya boleh membantu anda hari ini?',
    ar: 'مرحباً! كيف يمكنني مساعدتك اليوم؟',
  },
  fallback: {
    fr: "Je ne suis pas sûr de pouvoir répondre à cette question. Souhaitez-vous contacter notre support technique ? Je peux créer un ticket pour vous.",
    en: "I'm not sure I can answer this question. Would you like to contact our technical support? I can create a ticket for you.",
    ms: 'Saya tidak pasti boleh menjawab soalan ini. Adakah anda ingin menghubungi sokongan teknikal kami? Saya boleh buat tiket untuk anda.',
    ar: 'لست متأكداً من إمكانية الإجابة على هذا السؤال. هل تريد التواصل مع الدعم الفني؟ يمكنني إنشاء تذكرة لك.',
  },
};

/** Per-product reply template — proper function type, no `as unknown as` hack. */
export type ProductTpl = (name: string, price: string, stock: string, desc: string) => string;

export const PRODUCT_TPL: Record<BotLocale, ProductTpl> = {
  fr: (name, price, stock, desc) => `Le ${name} est proposé à ${price} TTC. Statut : ${stock}. ${desc}`,
  en: (name, price, stock, desc) => `The ${name} is available at ${price} incl. VAT. Status: ${stock}. ${desc}`,
  ms: (name, price, stock, desc) => `${name} ditawarkan pada ${price} termasuk CBP. Status: ${stock}. ${desc}`,
  ar: (name, price, stock, desc) => `${name} متاح بسعر ${price} شامل ضريبة القيمة المضافة. الحالة: ${stock}. ${desc}`,
};

/** Helper for simple string lookups, with English as the safe fallback. */
export function botString(key: string, locale: string): string {
  return BOT_STRINGS[key]?.[locale as BotLocale] ?? BOT_STRINGS[key]?.en ?? '';
}

/** Helper for the per-product template, with English as the safe fallback. */
export function productTpl(locale: string): ProductTpl {
  return PRODUCT_TPL[locale as BotLocale] ?? PRODUCT_TPL.en;
}
