/**
 * Search matching with tolerance as per CDC:
 * Priority: 1) exact match, 2) 1 char difference, 3) starts with, 4) contains
 */

/**
 * Levenshtein distance between two strings (simple implementation)
 */
function levenshtein(a: string, b: string): number {
  const matrix: number[][] = [];
  for (let i = 0; i <= b.length; i++) matrix[i] = [i];
  for (let j = 0; j <= a.length; j++) matrix[0][j] = j;

  for (let i = 1; i <= b.length; i++) {
    for (let j = 1; j <= a.length; j++) {
      if (b.charAt(i - 1) === a.charAt(j - 1)) {
        matrix[i][j] = matrix[i - 1][j - 1];
      } else {
        matrix[i][j] = Math.min(
          matrix[i - 1][j - 1] + 1,
          matrix[i][j - 1] + 1,
          matrix[i - 1][j] + 1
        );
      }
    }
  }
  return matrix[b.length][a.length];
}

export type MatchPriority = 1 | 2 | 3 | 4 | 0;

/**
 * Determine match priority for a query against text.
 * Returns 0 if no match.
 */
export function getMatchPriority(query: string, text: string): MatchPriority {
  const q = query.toLowerCase().trim();
  const t = text.toLowerCase();

  if (!q) return 0;

  // Priority 1: exact match (text equals query or contains exact word)
  if (t === q || t.split(/\s+/).includes(q)) return 1;

  // Priority 2: 1 character difference (tolerance)
  const words = t.split(/\s+/);
  for (const word of words) {
    if (levenshtein(q, word) <= 1) return 2;
  }

  // Priority 3: starts with
  if (t.startsWith(q) || words.some(w => w.startsWith(q))) return 3;

  // Priority 4: contains
  if (t.includes(q)) return 4;

  return 0;
}

/**
 * Search products with prioritized matching across multiple fields
 */
export interface SearchableProduct {
  id: string;
  name: Record<string, string>;
  description: Record<string, string>;
  specs: { label: string; value: string }[];
}

export function searchProducts<T extends SearchableProduct>(
  products: T[],
  query: string,
  locale: string = 'fr'
): { product: T; priority: MatchPriority }[] {
  if (!query.trim()) return products.map(p => ({ product: p, priority: 1 as MatchPriority }));

  const results: { product: T; priority: MatchPriority }[] = [];

  for (const product of products) {
    const namePriority = getMatchPriority(query, product.name[locale] || product.name.fr);
    const descPriority = getMatchPriority(query, product.description[locale] || product.description.fr);

    // Check specs
    let specPriority: MatchPriority = 0;
    for (const spec of product.specs) {
      const p = getMatchPriority(query, `${spec.label} ${spec.value}`);
      if (p > 0 && (specPriority === 0 || p < specPriority)) {
        specPriority = p as MatchPriority;
      }
    }

    // Best priority across all fields
    const priorities = [namePriority, descPriority, specPriority].filter(p => p > 0);
    if (priorities.length > 0) {
      const bestPriority = Math.min(...priorities) as MatchPriority;
      results.push({ product, priority: bestPriority });
    }
  }

  // Sort by priority (1 first = best match)
  results.sort((a, b) => a.priority - b.priority);
  return results;
}
