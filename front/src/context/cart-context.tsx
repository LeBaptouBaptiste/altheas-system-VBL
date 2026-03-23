'use client';

import { createContext, useContext, useState, useCallback, useMemo, type ReactNode } from 'react';
import type { CartItem } from '@/mock/types';
import { products } from '@/mock/products';
import { VAT_RATES } from '@/lib/constants';

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

  const persist = (newItems: CartItem[]) => {
    setItems(newItems);
    saveCart(newItems);
  };

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

  const clearCart = useCallback(() => persist([]), []);

  const { subtotalHT, totalVAT, totalTTC, hasUnavailableItems } = useMemo(() => {
    let ht = 0;
    let vat = 0;
    let unavailable = false;

    for (const item of items) {
      const product = products.find(p => p.id === item.productId);
      if (!product) continue;
      if (product.stockStatus === 'out_of_stock') unavailable = true;
      const lineHT = product.priceHT * item.quantity;
      const lineVAT = lineHT * VAT_RATES[product.vatRate];
      ht += lineHT;
      vat += lineVAT;
    }

    return {
      subtotalHT: Math.round(ht * 100) / 100,
      totalVAT: Math.round(vat * 100) / 100,
      totalTTC: Math.round((ht + vat) * 100) / 100,
      hasUnavailableItems: unavailable,
    };
  }, [items]);

  const itemCount = useMemo(() => items.reduce((sum, i) => sum + i.quantity, 0), [items]);

  return (
    <CartContext.Provider value={{
      items, addItem, removeItem, updateQuantity, clearCart,
      itemCount, subtotalHT, totalVAT, totalTTC, hasUnavailableItems,
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
