'use client';

import { createContext, useContext, useState, useCallback, useMemo, useEffect, type ReactNode } from 'react';
import type { CartItem, ProductDto } from '@/lib/api-types';
import { StockStatus, VatRate } from '@/lib/enums';
import { productsService } from '@/lib/api-services';

// Map API enum values to VAT rate percentages
const VAT_RATE_VALUES: Record<number, number> = {
  [VatRate.Standard]: 0.20,
  [VatRate.Intermediate]: 0.10,
  [VatRate.Reduced]: 0.055,
  [VatRate.Zero]: 0,
};

interface CartContextType {
  items: CartItem[];
  addItem: (productId: string, qty?: number) => void;
  removeItem: (productId: string) => void;
  updateQuantity: (productId: string, qty: number) => void;
  clearCart: () => void;
  itemCount: number;
  subtotalHT: number;
  totalVAT: number;
  totalTTC: number;
  hasUnavailableItems: boolean;
  loading: boolean;
  productCache: Map<string, ProductDto>;
}

const CartContext = createContext<CartContextType | null>(null);

function loadCart(): CartItem[] {
  if (typeof window !== 'undefined') {
    const saved = localStorage.getItem('althea-cart');
    if (saved) {
      try { return JSON.parse(saved); } catch { /* ignore */ }
    }
  }
  return [];
}

function saveCart(items: CartItem[]) {
  if (typeof window !== 'undefined') {
    localStorage.setItem('althea-cart', JSON.stringify(items));
  }
}

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartItem[]>(loadCart);
  const [productCache, setProductCache] = useState<Map<string, ProductDto>>(new Map());
  const [loading, setLoading] = useState(false);

  // Fetch product details for items not yet in cache
  useEffect(() => {
    const missingIds = items
      .map(i => i.productId)
      .filter(id => !productCache.has(id));

    if (missingIds.length === 0) return;

    setLoading(true);
    Promise.all(missingIds.map(id => productsService.getById(id).catch(() => null)))
      .then(results => {
        setProductCache(prev => {
          const next = new Map(prev);
          for (const product of results) {
            if (product) next.set(product.id, product);
          }
          return next;
        });
      })
      .finally(() => setLoading(false));
  }, [items, productCache]);

  const addItem = useCallback((productId: string, qty: number = 1) => {
    setItems(prev => {
      const existing = prev.find(i => i.productId === productId);
      const next = existing
        ? prev.map(i => i.productId === productId ? { ...i, quantity: i.quantity + qty } : i)
        : [...prev, { productId, quantity: qty }];
      saveCart(next);
      return next;
    });
  }, []);

  const removeItem = useCallback((productId: string) => {
    setItems(prev => {
      const next = prev.filter(i => i.productId !== productId);
      saveCart(next);
      return next;
    });
  }, []);

  const updateQuantity = useCallback((productId: string, qty: number) => {
    if (qty <= 0) {
      removeItem(productId);
      return;
    }
    setItems(prev => {
      const next = prev.map(i => i.productId === productId ? { ...i, quantity: qty } : i);
      saveCart(next);
      return next;
    });
  }, [removeItem]);

  const clearCart = useCallback(() => {
    setItems([]);
    saveCart([]);
  }, []);

  const { subtotalHT, totalVAT, totalTTC, hasUnavailableItems } = useMemo(() => {
    let ht = 0;
    let vat = 0;
    let unavailable = false;

    for (const item of items) {
      const product = productCache.get(item.productId);
      if (!product) continue;
      if (product.stockStatus === StockStatus.OutOfStock) unavailable = true;
      const lineHT = product.priceHT * item.quantity;
      const vatRate = VAT_RATE_VALUES[product.vatRate] ?? 0.20;
      const lineVAT = lineHT * vatRate;
      ht += lineHT;
      vat += lineVAT;
    }

    return {
      subtotalHT: Math.round(ht * 100) / 100,
      totalVAT: Math.round(vat * 100) / 100,
      totalTTC: Math.round((ht + vat) * 100) / 100,
      hasUnavailableItems: unavailable,
    };
  }, [items, productCache]);

  const itemCount = useMemo(() => items.reduce((sum, i) => sum + i.quantity, 0), [items]);

  return (
    <CartContext.Provider value={{
      items, addItem, removeItem, updateQuantity, clearCart,
      itemCount, subtotalHT, totalVAT, totalTTC, hasUnavailableItems,
      loading, productCache,
    }}>
      {children}
    </CartContext.Provider>
  );
}

export function useCart() {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error('useCart must be used within CartProvider');
  return ctx;
}
