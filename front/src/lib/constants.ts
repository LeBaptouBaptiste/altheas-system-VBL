// Shipping methods (used in checkout). Each label is in all 4 supported
// locales (fr/en/ms/ar) so the AR-RTL checkout doesn't fall back to French.
// The shape is `{ [locale]: string }` so the front uses `localized(label, locale)`.
export const SHIPPING_METHODS = [
  {
    id: 'standard',
    label: {
      fr: 'Livraison standard (5-7 jours)',
      en: 'Standard shipping (5-7 days)',
      ms: 'Penghantaran standard (5-7 hari)',
      ar: 'شحن عادي (5-7 أيام)',
    },
    price: 15.00,
  },
  {
    id: 'express',
    label: {
      fr: 'Livraison express (2-3 jours)',
      en: 'Express shipping (2-3 days)',
      ms: 'Penghantaran ekspres (2-3 hari)',
      ar: 'شحن سريع (2-3 أيام)',
    },
    price: 35.00,
  },
  {
    id: 'overnight',
    label: {
      fr: 'Livraison 24h',
      en: 'Overnight delivery',
      ms: 'Penghantaran 24 jam',
      ar: 'توصيل خلال 24 ساعة',
    },
    price: 75.00,
  },
] as const;
