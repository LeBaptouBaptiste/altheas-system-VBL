/**
 * Format a price for display in the user's locale.
 * Payment is always in EUR, but display uses locale formatting.
 */
export function formatPrice(amountInEuros: number, locale: string = 'fr-FR'): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: 'EUR',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amountInEuros);
}

/**
 * Calculate TTC from HT price and VAT rate
 */
export function calculateTTC(priceHT: number, vatRate: number): number {
  return Math.round(priceHT * (1 + vatRate) * 100) / 100;
}

/**
 * Calculate VAT amount from HT price
 */
export function calculateVAT(priceHT: number, vatRate: number): number {
  return Math.round(priceHT * vatRate * 100) / 100;
}
