// Shipping methods (used in checkout)
export const SHIPPING_METHODS = [
  { id: 'standard', label: { fr: 'Livraison standard (5-7 jours)', en: 'Standard shipping (5-7 days)' }, price: 15.00 },
  { id: 'express', label: { fr: 'Livraison express (2-3 jours)', en: 'Express shipping (2-3 days)' }, price: 35.00 },
  { id: 'overnight', label: { fr: 'Livraison 24h', en: 'Overnight delivery' }, price: 75.00 },
] as const;
