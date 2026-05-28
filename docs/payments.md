# Paiement Stripe — guide d'intégration

Architecture, setup local, debug et déploiement prod.

---

## Architecture

```
┌─────────┐  pk_test_*  ┌──────────┐  sk_test_*  ┌──────────┐
│ Browser ├────────────►│ Front    ├────────────►│ Stripe   │
│ (CB)    │             │ Next.js  │             │ API      │
└─────────┘             └──────────┘             └─────┬────┘
                              │                        │
                              │ POST /api/payments     │ POST /api/webhooks/stripe
                              │ /intents               │ (signature HMAC)
                              ▼                        ▼
                        ┌──────────────────────────────┐
                        │  API .NET 10                  │
                        │  ├ PaymentIntentController    │  ← user lance le paiement
                        │  ├ StripeWebhookController    │  ← Stripe confirme async
                        │  └ StripeWebhookProcessor     │
                        └────────────┬──────────────────┘
                                     ▼
                              ┌─────────────┐
                              │ PostgreSQL  │
                              │ orders +    │
                              │ webhook_evt │
                              └─────────────┘
```

**2 sources de vérité, dans cet ordre** :

1. **Frontend `confirmPayment()`** : UX immédiate. Le user voit "payé" en
   ~1 seconde, l'écran de confirmation s'affiche.
2. **Webhook serveur** : autorité métier. C'est lui qui flip
   `Order.PaymentStatus = Validated` en DB. Si le user ferme l'onglet
   avant la confirmation côté front, le webhook règle l'order quand même.

Tout passe par le **test mode Stripe** : `sk_test_*` + `pk_test_*` +
cartes de test. **Aucun euro réel ne bouge.**

---

## Setup initial

### 1. Récupérer les clés Stripe

Va sur [dashboard.stripe.com/test/apikeys](https://dashboard.stripe.com/test/apikeys).
**Active le mode Test** (toggle en haut à droite si tu vois "Live").

Tu as besoin de 2 clés :
- **Secret key** (`sk_test_…`) — backend, à garder secrète
- **Publishable key** (`pk_test_…`) — frontend, OK exposée

### 2. Configurer le `.env`

```bash
# Backend
STRIPE_SECRET_KEY=sk_test_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

# Frontend (baked dans le bundle au build, safe)
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_xxxxxxxxxxxxxxxxxxxxxxxxxxxx

# Webhook signing secret — voir section ci-dessous
STRIPE_WEBHOOK_SECRET=whsec_REPLACE_ME
```

### 3. Webhook signing secret

Stripe signe chaque webhook avec un HMAC. Sans le bon secret, l'API rejette
en `400 Bad Request`. Deux options selon ton workflow dev :

#### Option A (recommandée) — secret stable via Dashboard

Setup une fois, marche pour tout le projet ensuite.

1. [dashboard.stripe.com/test/webhooks](https://dashboard.stripe.com/test/webhooks)
   → **+ Add endpoint**
2. URL : `https://localhost:8080/api/webhooks/stripe`
   (peu importe que ce ne soit pas accessible — c'est juste un placeholder
   pour obtenir un secret stable)
3. Events à sélectionner :
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `payment_method.attached` *(pour Phase 4 save-card)*
4. Stripe affiche **"Signing secret"** : `whsec_xxxx...` → copie-le dans `.env`
5. Modifie la commande `stripe-cli` dans `docker-compose.yml` pour ajouter
   `--use-configured-webhooks` :
   ```yaml
   command:
     - listen
     - --api-key=${STRIPE_SECRET_KEY}
     - --forward-to=http://api:8080/api/webhooks/stripe
     - --use-configured-webhooks
     - --skip-verify
   ```

✅ Secret stable, jamais besoin de recopier.

#### Option B — secret éphémère via Stripe CLI

Plus rapide à setup, mais re-copy à chaque restart du sidecar.

1. `docker compose --profile dev up -d`
2. `docker logs althea-stripe-cli` → cherche la ligne `Your webhook signing
   secret is whsec_...`
3. `STRIPE_WEBHOOK_SECRET=whsec_xxxx` dans `.env`
4. `docker compose restart api` pour qu'il prenne le nouveau secret

---

## Workflow dev local

### Démarrer la stack avec webhook

```bash
docker compose --profile dev up -d
```

Le sidecar `stripe-cli` se connecte à Stripe via WebSocket et forward
tous les events `test_mode` vers `http://api:8080/api/webhooks/stripe`.

### Tester un paiement de bout en bout

1. **Front** : ouvre [localhost:3000](http://localhost:3000), ajoute un produit
   au panier, va sur `/checkout`
2. Remplis adresse + shipping, arrive à **Step 3 — Paiement**
3. Le PaymentElement affiche un formulaire CB. Utilise une carte test :

| Carte | Comportement |
|---|---|
| `4242 4242 4242 4242` | Succès sans 3DS |
| `4000 0027 6000 3184` | Succès avec 3DS (challenge popup) |
| `4000 0000 0000 9995` | `insufficient_funds` (decline_code) |
| `4000 0000 0000 0002` | `card_declined` / `generic_decline` |
| `4000 0000 0000 0069` | `expired_card` |
| `4000 0000 0000 0127` | `incorrect_cvc` |

Date d'expiration : n'importe quelle date future (`12/30`).
CVC : n'importe quel 3 chiffres (`123`).
Code postal : n'importe quoi (`75001`).

4. Click **Confirmer**.
5. Tu verras dans `docker logs althea-api` :
   ```
   info: ... Order <guid> marked as Validated via PaymentIntent pi_xxx
   ```
6. L'order est `PaymentStatus.Validated` en DB.

### Trigger un event sans aller dans le UI

Utile pour tester les handlers sans passer par /checkout :

```bash
docker exec althea-stripe-cli stripe trigger payment_intent.succeeded
docker exec althea-stripe-cli stripe trigger payment_intent.payment_failed
```

Stripe génère un PaymentIntent fictif et l'envoie au webhook. Note : ces
events n'ont pas `metadata.orderId`, donc le handler les loggera comme
"ignored" — c'est attendu. Pour un test bout-en-bout réaliste, passe par
le UI.

---

## Debug

### Le webhook répond `400 Bad Request`

→ Mauvais `STRIPE_WEBHOOK_SECRET`. Vérifie qu'il correspond à celui du
sidecar (Option B) ou du dashboard (Option A). Restart `api` après modif
du `.env`.

### Le webhook répond `500 Internal Server Error`

→ Vérifie les logs : `docker logs althea-api`. Probablement
`Stripe:WebhookSecret is not configured` (placeholder `REPLACE_ME` toujours
en place) ou une erreur EF Core.

### L'order reste `PaymentStatus.Pending` après paiement

→ Le webhook n'arrive pas, OU le handler ne trouve pas l'order.

Vérif :
1. `docker logs althea-stripe-cli` → tu vois bien
   `--> payment_intent.succeeded [evt_xxx]` ?
2. `docker logs althea-api | grep webhook` → tu vois
   `Order <guid> marked as Validated` ?
3. Si l'event passe mais ne match aucun order : vérifie que la création
   du PaymentIntent côté front a bien set `metadata.orderId` (côté code
   c'est fait par `StripeService.CreatePaymentIntentAsync`).

### Le sidecar ne se connecte pas

→ `docker logs althea-stripe-cli`. Erreur fréquente :
`API key invalid: sk_test_REPLACE_ME` → tu as oublié de mettre la vraie clé
dans `.env`.

---

## Production

### Webhook URL

Quand tu déploies, tu **n'utilises plus la Stripe CLI**. Tu enregistres un
endpoint webhook dans le dashboard pointant directement vers ton API
publique :

1. [dashboard.stripe.com/webhooks](https://dashboard.stripe.com/webhooks) *(mode live)*
2. **+ Add endpoint** → `https://api.tondomaine.com/api/webhooks/stripe`
3. Sélectionne les mêmes events qu'en dev
4. Récupère le `whsec_...` de prod → mets-le dans le secret manager
   (GitHub Secrets, Vault, AWS Secrets Manager…)

### Switch test → live

| Variable | Test | Prod |
|---|---|---|
| `STRIPE_SECRET_KEY` | `sk_test_…` | `sk_live_…` |
| `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY` | `pk_test_…` | `pk_live_…` |
| `STRIPE_WEBHOOK_SECRET` | (CLI / dashboard test) | (dashboard live) |

Le code applicatif est strictement identique — seules les clés changent.
La validation au démarrage refuse les clés avec mauvais préfixe
(`sk_test_*` / `sk_live_*` only — pas de typo, pas de `pk_*` collé par
erreur).

### Désactiver le sidecar

Le sidecar `stripe-cli` est en `profiles: [dev]`, donc absent par défaut
de `docker compose up`. Aucune action nécessaire — il ne tournera jamais
en prod tant que personne ne passe `--profile dev`.

---

## Références

- Stripe API : [docs.stripe.com/api](https://docs.stripe.com/api)
- Cartes de test : [docs.stripe.com/testing](https://docs.stripe.com/testing)
- Stripe CLI : [docs.stripe.com/stripe-cli](https://docs.stripe.com/stripe-cli)
- Webhook signature : [docs.stripe.com/webhooks/signatures](https://docs.stripe.com/webhooks/signatures)
- PaymentIntent flow : [docs.stripe.com/payments/payment-intents](https://docs.stripe.com/payments/payment-intents)
