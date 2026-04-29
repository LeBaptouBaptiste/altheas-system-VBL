import { VAT_RATES, type VatRateKey } from './constants';

export const INTL_LOCALE: Record<string, string> = {
  fr: 'fr-FR',
  en: 'en-US',
  ms: 'ms-MY',
  ar: 'ar-MA',
};

export function toIntlLocale(appLocale: string): string {
  return INTL_LOCALE[appLocale] ?? 'en-US';
}

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

/**
 * Get HT from TTC
 */
export function calculateHT(priceTTC: number, vatRate: number): number {
  return Math.round((priceTTC / (1 + vatRate)) * 100) / 100;
}

/**
 * Get VAT rate label
 */
export function getVatRateLabel(key: VatRateKey): string {
  const rate = VAT_RATES[key];
  return `${(rate * 100).toFixed(1)}%`;
}

/**
 * Format a percentage for display
 */
export function formatPercent(rate: number): string {
  return `${(rate * 100).toFixed(1)}%`;
}
