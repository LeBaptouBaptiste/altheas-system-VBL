'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Check, CreditCard, Building2, FileText, Truck, MapPin, ArrowLeft, Download, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent } from '@/components/ui/card';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Checkbox } from '@/components/ui/checkbox';
import { Separator } from '@/components/ui/separator';
import { useI18n } from '@/context/i18n-context';
import { useAuth } from '@/context/auth-context';
import { useCart } from '@/context/cart-context';
import { ordersService, usersService } from '@/lib/api-services';
import { formatPrice } from '@/lib/money';
import { SHIPPING_METHODS } from '@/lib/constants';
import { ShippingMethod, PaymentMethod } from '@/lib/enums';
import { toast } from 'sonner';

const SHIPPING_MAP: Record<string, number> = {
  standard: ShippingMethod.Standard,
  express: ShippingMethod.Express,
  overnight: ShippingMethod.Overnight,
};

const PAYMENT_MAP: Record<string, number> = {
  card: PaymentMethod.Card,
  bank_transfer: PaymentMethod.BankTransfer,
  admin_mandate: PaymentMethod.PayPal, // PayPal used as placeholder for admin mandate
};

interface AddressForm {
  firstName: string;
  lastName: string;
  company: string;
  street: string;
  city: string;
  postalCode: string;
  country: string;
  phone: string;
}

const emptyAddress: AddressForm = { firstName: '', lastName: '', company: '', street: '', city: '', postalCode: '', country: 'France', phone: '' };

export default function CheckoutPage() {
  const { t, localized, locale } = useI18n();
  const { user, isAuthenticated } = useAuth();
  const { items, subtotalHT, totalVAT, totalTTC, clearCart } = useCart();
  const fmt = (n: number) => formatPrice(n, locale === 'fr' ? 'fr-FR' : 'en-US');

  const [step, setStep] = useState<number>(isAuthenticated ? 1 : 0);
  const [sameAddress, setSameAddress] = useState(true);
  const [shippingMethod, setShippingMethod] = useState('standard');
  const [paymentMethod, setPaymentMethod] = useState('card');
  const [orderPlaced, setOrderPlaced] = useState(false);
  const [orderId, setOrderId] = useState<string | null>(null);
  const [placing, setPlacing] = useState(false);

  const [billing, setBilling] = useState<AddressForm>(emptyAddress);
  const [shipping, setShipping] = useState<AddressForm>(emptyAddress);

  const shippingCost = SHIPPING_METHODS.find(m => m.id === shippingMethod)?.price || 15;
  const grandTotal = totalTTC + shippingCost;

  const updateBilling = (field: keyof AddressForm, value: string) => setBilling(prev => ({ ...prev, [field]: value }));
  const updateShipping = (field: keyof AddressForm, value: string) => setShipping(prev => ({ ...prev, [field]: value }));

  const isAddressValid = (addr: AddressForm) =>
    addr.firstName && addr.lastName && addr.street && addr.city && addr.postalCode && addr.country;

  const handlePlaceOrder = async () => {
    if (!user) { toast.error(locale === 'fr' ? 'Veuillez vous connecter' : 'Please log in'); return; }

    setPlacing(true);
    try {
      // Create billing address via API
      const billingAddr = await usersService.addAddress(user.id, {
        label: locale === 'fr' ? 'Facturation' : 'Billing',
        ...billing,
        company: billing.company || null,
        phone: billing.phone || null,
        street2: null,
      });

      // Create or reuse shipping address
      let shippingAddrId = billingAddr.id;
      if (!sameAddress) {
        const shipAddr = await usersService.addAddress(user.id, {
          label: locale === 'fr' ? 'Livraison' : 'Shipping',
          ...shipping,
          company: shipping.company || null,
          phone: shipping.phone || null,
          street2: null,
        });
        shippingAddrId = shipAddr.id;
      }

      // Create the order
      const order = await ordersService.create({
        billingAddressId: billingAddr.id,
        shippingAddressId: shippingAddrId,
        shippingMethod: SHIPPING_MAP[shippingMethod] ?? ShippingMethod.Standard,
        paymentMethod: PAYMENT_MAP[paymentMethod] ?? PaymentMethod.Card,
        items: items.map(i => ({ productId: i.productId, quantity: i.quantity })),
      });

      setOrderId(order.id);
      setOrderPlaced(true);
      setStep(4);
      clearCart();
      toast.success(t('checkout.order_confirmed'));
    } catch {
      toast.error(locale === 'fr' ? 'Erreur lors de la commande' : 'Order failed');
    } finally {
      setPlacing(false);
    }
  };

  const stepLabels = [t('checkout.step.auth'), t('checkout.step.address'), t('checkout.step.shipping'), t('checkout.step.payment'), t('checkout.step.confirmation')];

  if (items.length === 0 && !orderPlaced) {
    return <div className="container mx-auto px-4 py-16 text-center"><h1 className="text-2xl mb-4">{t('cart.empty')}</h1><Link href="/"><Button>{t('cart.continue_shopping')}</Button></Link></div>;
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <h1 className="text-3xl text-brand-dark mb-6">{t('checkout.title')}</h1>

      {/* Step indicator */}
      <div className="flex items-center justify-between mb-8 overflow-x-auto">
        {stepLabels.map((label, i) => (
          <div key={i} className="flex items-center">
            <div className={`flex items-center justify-center w-8 h-8 rounded-full text-sm font-medium shrink-0 ${i < step ? 'bg-success text-white' : i === step ? 'bg-brand-primary text-white' : 'bg-gray-200 text-gray-500'}`}>
              {i < step ? <Check className="w-4 h-4" /> : i + 1}
            </div>
            <span className={`ms-2 text-sm hidden sm:inline whitespace-nowrap ${i === step ? 'font-semibold text-brand-dark' : 'text-muted-foreground'}`}>{label}</span>
            {i < stepLabels.length - 1 && <div className="w-8 sm:w-16 h-0.5 bg-gray-200 mx-2 shrink-0" />}
          </div>
        ))}
      </div>

      {/* Step 0: Auth */}
      {step === 0 && (
        <Card><CardContent className="p-6 space-y-4">
          <h2 className="text-xl font-semibold">{t('checkout.step.auth')}</h2>
          {isAuthenticated ? (
            <div><p className="text-success">{t('auth.login')} ✓</p><Button onClick={() => setStep(1)} className="mt-4 bg-brand-primary hover:bg-brand-hover text-white">{t('checkout.next')}</Button></div>
          ) : (
            <div className="space-y-4">
              <Link href="/login"><Button className="w-full bg-brand-primary hover:bg-brand-hover text-white">{t('auth.login')}</Button></Link>
              <Link href="/register"><Button variant="outline" className="w-full">{t('auth.register')}</Button></Link>
            </div>
          )}
        </CardContent></Card>
      )}

      {/* Step 1: Address */}
      {step === 1 && (
        <Card><CardContent className="p-6 space-y-6">
          <h2 className="text-xl font-semibold flex items-center gap-2"><MapPin className="w-5 h-5" />{t('checkout.billing_address')}</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div><Label>{locale === 'fr' ? 'Prénom' : 'First Name'}</Label><Input value={billing.firstName} onChange={e => updateBilling('firstName', e.target.value)} required /></div>
            <div><Label>{locale === 'fr' ? 'Nom' : 'Last Name'}</Label><Input value={billing.lastName} onChange={e => updateBilling('lastName', e.target.value)} required /></div>
            <div className="sm:col-span-2"><Label>{locale === 'fr' ? 'Entreprise' : 'Company'}</Label><Input value={billing.company} onChange={e => updateBilling('company', e.target.value)} /></div>
            <div className="sm:col-span-2"><Label>{locale === 'fr' ? 'Adresse' : 'Address'}</Label><Input value={billing.street} onChange={e => updateBilling('street', e.target.value)} required /></div>
            <div><Label>{locale === 'fr' ? 'Ville' : 'City'}</Label><Input value={billing.city} onChange={e => updateBilling('city', e.target.value)} required /></div>
            <div><Label>{locale === 'fr' ? 'Code postal' : 'Postal Code'}</Label><Input value={billing.postalCode} onChange={e => updateBilling('postalCode', e.target.value)} required /></div>
            <div><Label>{locale === 'fr' ? 'Pays' : 'Country'}</Label><Input value={billing.country} onChange={e => updateBilling('country', e.target.value)} required /></div>
            <div><Label>{locale === 'fr' ? 'Téléphone' : 'Phone'}</Label><Input value={billing.phone} onChange={e => updateBilling('phone', e.target.value)} /></div>
          </div>
          <Separator />
          <div className="flex items-center gap-2">
            <Checkbox id="sameAddr" checked={sameAddress} onCheckedChange={(v) => setSameAddress(!!v)} />
            <Label htmlFor="sameAddr">{t('checkout.same_address')}</Label>
          </div>
          {!sameAddress && (
            <div>
              <h3 className="font-semibold mb-3">{t('checkout.shipping_address')}</h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div><Label>{locale === 'fr' ? 'Prénom' : 'First Name'}</Label><Input value={shipping.firstName} onChange={e => updateShipping('firstName', e.target.value)} /></div>
                <div><Label>{locale === 'fr' ? 'Nom' : 'Last Name'}</Label><Input value={shipping.lastName} onChange={e => updateShipping('lastName', e.target.value)} /></div>
                <div className="sm:col-span-2"><Label>{locale === 'fr' ? 'Adresse' : 'Address'}</Label><Input value={shipping.street} onChange={e => updateShipping('street', e.target.value)} /></div>
                <div><Label>{locale === 'fr' ? 'Ville' : 'City'}</Label><Input value={shipping.city} onChange={e => updateShipping('city', e.target.value)} /></div>
                <div><Label>{locale === 'fr' ? 'Code postal' : 'Postal Code'}</Label><Input value={shipping.postalCode} onChange={e => updateShipping('postalCode', e.target.value)} /></div>
                <div><Label>{locale === 'fr' ? 'Pays' : 'Country'}</Label><Input value={shipping.country} onChange={e => updateShipping('country', e.target.value)} /></div>
              </div>
            </div>
          )}
          <div className="flex gap-4 pt-4">
            <Button variant="outline" onClick={() => setStep(0)}><ArrowLeft className="w-4 h-4 me-2" />{t('checkout.previous')}</Button>
            <Button className="bg-brand-primary hover:bg-brand-hover text-white" disabled={!isAddressValid(billing)} onClick={() => setStep(2)}>{t('checkout.next')}</Button>
          </div>
        </CardContent></Card>
      )}

      {/* Step 2: Shipping */}
      {step === 2 && (
        <Card><CardContent className="p-6 space-y-6">
          <h2 className="text-xl font-semibold flex items-center gap-2"><Truck className="w-5 h-5" />{t('checkout.choose_shipping')}</h2>
          <RadioGroup value={shippingMethod} onValueChange={setShippingMethod}>
            {SHIPPING_METHODS.map(m => (
              <div key={m.id} className="flex items-center justify-between p-4 border rounded-lg hover:border-brand-primary transition-colors">
                <div className="flex items-center gap-3">
                  <RadioGroupItem value={m.id} id={m.id} />
                  <Label htmlFor={m.id} className="cursor-pointer">{localized(m.label as unknown as Record<string, string>)}</Label>
                </div>
                <span className="font-semibold">{fmt(m.price)}</span>
              </div>
            ))}
          </RadioGroup>
          <div className="flex gap-4 pt-4">
            <Button variant="outline" onClick={() => setStep(1)}><ArrowLeft className="w-4 h-4 me-2" />{t('checkout.previous')}</Button>
            <Button className="bg-brand-primary hover:bg-brand-hover text-white" onClick={() => setStep(3)}>{t('checkout.next')}</Button>
          </div>
        </CardContent></Card>
      )}

      {/* Step 3: Payment */}
      {step === 3 && (
        <Card><CardContent className="p-6 space-y-6">
          <h2 className="text-xl font-semibold">{t('checkout.payment_method')}</h2>
          <p className="text-sm text-muted-foreground">{t('checkout.payment_in_eur')}</p>
          <RadioGroup value={paymentMethod} onValueChange={setPaymentMethod}>
            <div className="flex items-center gap-3 p-4 border rounded-lg hover:border-brand-primary">
              <RadioGroupItem value="card" id="pay-card" />
              <CreditCard className="w-5 h-5 text-brand-primary" />
              <Label htmlFor="pay-card" className="cursor-pointer flex-1">{t('checkout.card')}</Label>
            </div>
          </RadioGroup>

          {paymentMethod === 'card' && (
            <div className="bg-gray-50 p-4 rounded-lg space-y-3">
              <p className="text-sm font-medium">{locale === 'fr' ? 'Paiement sécurisé' : 'Secure Payment'}</p>
              <Input placeholder="•••• •••• •••• ••••" className="bg-white" />
              <div className="flex gap-3">
                <Input placeholder="MM/YY" className="bg-white" />
                <Input placeholder="CVC" className="bg-white" />
              </div>
              <p className="text-xs text-muted-foreground">{locale === 'fr' ? 'Aucune donnée de carte n\'est stockée' : 'No card data is stored'}</p>
            </div>
          )}
          {paymentMethod === 'bank_transfer' && (
            <div className="bg-gray-50 p-4 rounded-lg text-sm">
              <p className="font-medium mb-2">{locale === 'fr' ? 'Instructions de virement' : 'Bank Transfer Instructions'}</p>
              <p>IBAN: FR76 1234 5678 9012 3456 7890 123</p>
              <p>BIC: BNPAFRPP</p>
            </div>
          )}
          {paymentMethod === 'admin_mandate' && (
            <div className="bg-gray-50 p-4 rounded-lg text-sm">
              <p>{locale === 'fr' ? 'Veuillez envoyer votre mandat administratif à : commandes@altheasystems.com' : 'Please send your administrative mandate to: orders@altheasystems.com'}</p>
            </div>
          )}

          {/* Order summary */}
          <Separator />
          <div className="space-y-2">
            <div className="flex justify-between text-sm"><span>{t('cart.subtotal')}</span><span>{fmt(subtotalHT)}</span></div>
            <div className="flex justify-between text-sm"><span>{t('cart.vat')}</span><span>{fmt(totalVAT)}</span></div>
            <div className="flex justify-between text-sm"><span>{locale === 'fr' ? 'Livraison' : 'Shipping'}</span><span>{fmt(shippingCost)}</span></div>
            <Separator />
            <div className="flex justify-between font-bold text-lg"><span>{t('cart.total')}</span><span>{fmt(grandTotal)}</span></div>
          </div>

          <div className="flex gap-4 pt-4">
            <Button variant="outline" onClick={() => setStep(2)}><ArrowLeft className="w-4 h-4 me-2" />{t('checkout.previous')}</Button>
            <Button size="lg" className="flex-1 bg-brand-primary hover:bg-brand-hover text-white" onClick={handlePlaceOrder} disabled={placing}>
              {placing ? <Loader2 className="w-5 h-5 animate-spin" /> : t('checkout.place_order')}
            </Button>
          </div>
        </CardContent></Card>
      )}

      {/* Step 4: Confirmation */}
      {step === 4 && (
        <Card><CardContent className="p-6 text-center space-y-4">
          <div className="w-16 h-16 rounded-full bg-success/10 flex items-center justify-center mx-auto">
            <Check className="w-8 h-8 text-success" />
          </div>
          <h2 className="text-2xl font-semibold text-brand-dark">{t('checkout.order_confirmed')}</h2>
          <p className="text-muted-foreground">{t('checkout.email_sent')}</p>
          {orderId && <p className="text-sm text-muted-foreground font-mono">{locale === 'fr' ? 'Commande' : 'Order'}: {orderId.slice(0, 8)}...</p>}
          <div className="pt-4">
            <Link href="/"><Button className="bg-brand-primary hover:bg-brand-hover text-white">{t('checkout.continue_shopping')}</Button></Link>
          </div>
        </CardContent></Card>
      )}
    </div>
  );
}
