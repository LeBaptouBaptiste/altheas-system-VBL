# Althea Systems

> Plateforme e-commerce **B2B** de matériel médical pour professionnels de santé (cabinets, cliniques, laboratoires, pharmacies, EHPAD).

API **ASP.NET Core 10** · Front **Next.js 16 / React 19** · **PostgreSQL · MongoDB · Redis** · chatbot **IA auto-hébergée (Ollama)** · paiements **Stripe** · le tout orchestré avec **Docker Compose**.

---

## Sommaire

- [Fonctionnalités](#fonctionnalités)
- [Architecture](#architecture)
- [Stack technique](#stack-technique)
- [Prérequis](#prérequis)
- [Démarrage rapide (Docker)](#démarrage-rapide-docker)
- [Configuration (.env)](#configuration-env)
- [Services & ports](#services--ports)
- [Comptes de démonstration](#comptes-de-démonstration)
- [Webhooks Stripe (dev)](#webhooks-stripe-dev)
- [Chatbot IA (Ollama)](#chatbot-ia-ollama)
- [Développement local (hors Docker)](#développement-local-hors-docker)
- [Tests & intégration continue](#tests--intégration-continue)
- [Structure du dépôt](#structure-du-dépôt)
- [Dépannage](#dépannage)

---

## Fonctionnalités

- **Catalogue** multi-catégories avec fiches produit détaillées (spécifications techniques) et recherche.
- **Parcours d'achat complet** : panier, commande invité ou authentifiée, adresses, livraison, paiement.
- **Paiements Stripe** : PaymentIntents, authentification forte 3DS / DSP2 (SCA), webhooks signés et idempotents.
- **Facturation** : factures numérotées (`{CodeClient}-{AAAA}-{MM}-{NNNN}`), avoirs (remboursement Stripe ou crédit boutique), génération PDF.
- **Comptes & sécurité** : inscription avec confirmation e-mail, JWT, **2FA** (TOTP + e-mail), **step-up authentication** (ré-authentification pour actions sensibles), contrôle d'accès et de propriété (anti-IDOR).
- **Back-office admin** : gestion catalogue, commandes, clients, contenus (carrousel, pages statiques), tickets support, analytics.
- **Chatbot IA** auto-hébergé : assistant catalogue / suivi de commande ancré sur les données réelles (RAG), multilingue, avec escalade vers un ticket support. **Ne fournit jamais de conseil médical.**
- **Internationalisation** : 4 langues — **français, anglais, malais, arabe** — avec support **RTL** complet pour l'arabe.
- **E-mails transactionnels** multilingues (confirmation de compte, réinitialisation de mot de passe, etc.).

---

## Architecture

```
                         +------------------------+
        Internet ----->  |  front (Next.js 16)    |
                         |  :3000                 |
                         +-----------+------------+
                                     |  HTTPS / JSON
                                     v
                         +------------------------+
                         |  api (ASP.NET Core 10)  |
                         |  :8080                 |
                         +--+------+------+----+---+
                            |      |      |    |
              +-------------+   +--+--+ +-+--+ +----------+
              v                 v     v v    v            v
        +-----------+    +------------+ +-------+   +---------------+
        | PostgreSQL|    |  MongoDB   | | Redis |   |  Ollama       |
        | (métier,  |    | (logs /    | | (cache|   |  qwen2.5:3b   |
        |  EF Core) |    |  documents)| | step-up)  |  (chatbot)    |
        +-----------+    +------------+ +-------+   +---------------+
```

L'API suit une **architecture en couches** : `Controllers` → `Services` → `Repositories` → `Models`, avec des `DTOs` pour le contrat d'API et une configuration Fluent API pour EF Core. Le front Next.js (App Router) sépare **Server Components** (contenu, SEO) et **Client Components** (interactivité), organisés en *route groups* `(store)` et `(admin)`.

---

## Stack technique

**Back-end** — .NET 10, ASP.NET Core, EF Core 10 (Npgsql), MongoDB.Driver, StackExchange.Redis, JWT Bearer, BCrypt, Otp.NET (TOTP), FluentValidation, Stripe.net, QuestPDF, MailKit/MimeKit, Swashbuckle (OpenAPI/Swagger).

**Front-end** — Next.js 16, React 19, TypeScript, Tailwind CSS 4, shadcn/ui (Radix UI), React Hook Form + Zod, Stripe.js / React Stripe.js, Recharts, Sonner, support RTL.

**Données & infra** — PostgreSQL 16, MongoDB 7, Redis 7, Ollama, Docker & Docker Compose. CI : GitHub Actions (build + tests back, type-check + build front).

---

## Prérequis

- [Docker](https://docs.docker.com/get-docker/) et Docker Compose (v2).
- ~4 Go de RAM libres (le modèle du chatbot tourne sur CPU, sans GPU).
- Un compte **Stripe** en mode test (clés gratuites) si vous voulez tester les paiements.
- *(Optionnel, pour le dev hors Docker)* : **.NET 10 SDK** et **Node.js 22+**.

---

## Démarrage rapide (Docker)

```bash
# 1. Cloner le dépôt
git clone <url-du-depot> althea-systems && cd althea-systems

# 2. Créer le fichier d'environnement à partir de l'exemple
cp .env.example .env
#   → éditer .env : mots de passe, clés JWT/chiffrement, clés Stripe de test, SMTP

# 3. Lancer toute la stack
docker compose up -d --build
```

Au premier démarrage, le sidecar `ollama-init` télécharge le modèle (`qwen2.5:3b`, quelques minutes) ; pendant ce temps le chatbot renvoie un message de repli poli. Une fois prêt :

- **Front** : http://localhost:3000
- **API (Swagger)** : http://localhost:8080/swagger
- **Healthcheck API** : http://localhost:8080/api/health

> La base est **peuplée automatiquement** (seed) au démarrage : catalogue, clients de démo, commandes, conversations et tickets.

Pour arrêter / réinitialiser :

```bash
docker compose down          # arrêt (conserve les données)
docker compose down -v       # arrêt + suppression des volumes (reset complet)
```

---

## Configuration (.env)

Le fichier `.env` (gabarit : `.env.example`) regroupe toute la configuration. Variables clés :

| Variable | Rôle |
|---|---|
| `POSTGRES_DB` / `POSTGRES_USER` / `POSTGRES_PASSWORD` | Base de données PostgreSQL |
| `JWT_SECRET_KEY` / `JWT_ISSUER` / `JWT_AUDIENCE` | Signature des jetons d'authentification (clé ≥ 32 caractères) |
| `ENCRYPTION_KEY` | Chiffrement des secrets au repos (TOTP). Clé **base64 de 32 octets** : `openssl rand -base64 32` |
| `STRIPE_SECRET_KEY` / `STRIPE_WEBHOOK_SECRET` | Paiements (clés **test**). Les deux clés Stripe doivent venir du **même compte**. |
| `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY` | Clé publique Stripe (front) |
| `SMTP_*` | Envoi d'e-mails (laisser `SMTP_HOST` vide pour désactiver l'envoi) |
| `OLLAMA_MODEL` | Modèle du chatbot (défaut : `qwen2.5:3b`) |
| `NEXT_PUBLIC_API_URL` | URL de l'API consommée par le front |

> ⚠️ Le `.env` contient des secrets : il est ignoré par Git. Ne jamais committer de vraies clés.

---

## Services & ports

| Service | Conteneur | Port hôte | Description |
|---|---|---|---|
| Front | `althea-front` | `3000` | Application Next.js |
| API | `althea-api` | `8080` | API REST ASP.NET Core (+ Swagger) |
| PostgreSQL | `althea-postgres` | — | Données métier (EF Core) |
| MongoDB | `althea-mongodb` | — | Logs / documents |
| Redis | `althea-redis` | — | Cache, jetons de step-up |
| Ollama | `althea-ollama` | `11434` | Moteur d'inférence du chatbot |
| Ollama init | `althea-ollama-init` | — | Sidecar one-shot : télécharge le modèle |
| Stripe CLI | `althea-stripe-cli` | — | Relais de webhooks (profil `dev` uniquement) |

---

## Comptes de démonstration

Créés par le seed au premier démarrage :

| Rôle | E-mail | Mot de passe |
|---|---|---|
| **Administrateur** | `admin@altheasystems.com` | `Admin1234!` |
| **Client** (plusieurs) | ex. `contact@cabinetduvieuxport.fr` | `Demo1234!` |

> Tous les clients de démonstration partagent le mot de passe `Demo1234!`. À usage local uniquement.

---

## Webhooks Stripe (dev)

En développement, un sidecar **Stripe CLI** relaie les événements de votre compte de test vers l'API. Il ne démarre qu'avec le profil `dev` :

```bash
# 1. Démarrer avec le profil dev
docker compose --profile dev up -d

# 2. Récupérer le secret de webhook affiché par la CLI
docker logs althea-stripe-cli      # copier le whsec_...

# 3. Le renseigner dans .env puis redémarrer l'API
#    STRIPE_WEBHOOK_SECRET=whsec_...
docker compose restart api

# 4. Déclencher un événement de test
docker exec althea-stripe-cli stripe trigger payment_intent.succeeded
```

> Le secret est régénéré à chaque `stripe listen`. En production, Stripe POST directement vers l'URL de webhook publique enregistrée dans le dashboard.

**Cartes de test Stripe** : `4242 4242 4242 4242` (succès), `4000 0027 6000 3184` (3DS requis) — date d'expiration future quelconque, CVC à 3 chiffres.

---

## Chatbot IA (Ollama)

Le chatbot s'appuie sur un modèle **auto-hébergé** via Ollama (aucun coût par requête, données client non externalisées — critère fort en contexte médical). Approche **RAG** : à chaque message, un contexte factuel (profil client, commandes, catalogue, produits pertinents) est injecté pour ancrer la réponse dans des données réelles.

- Modèle par défaut : `qwen2.5:3b` (modifiable via `OLLAMA_MODEL`).
- Multilingue (fr / en / ms / ar), répond dans la langue de l'interface.
- Garde-fous : **aucun conseil médical**, repli localisé si le service est indisponible, escalade vers un ticket support si le bot ne peut pas répondre.

---

## Développement local (hors Docker)

Utile pour itérer sans rebuild d'image. Les services de données (PostgreSQL, MongoDB, Redis, Ollama) peuvent rester dans Docker.

**Back-end** (.NET 10 SDK requis) :

```bash
cd API_Althea-systems
dotnet restore API_Althea-systems.slnx
dotnet run                      # API sur http://localhost:8080
# Migrations EF Core :
dotnet ef database update
```

**Front-end** (Node.js 22+ requis) :

```bash
cd front
npm install
npm run dev                     # front sur http://localhost:3000
```

---

## Tests & intégration continue

```bash
# Tests back-end
dotnet test API_Althea-systems/API_Althea-systems.slnx

# Vérification de types front
cd front && npx tsc --noEmit
```

La CI **GitHub Actions** (`.github/workflows/ci.yml`) s'exécute à chaque push :

- **back-end** : restore → build (Release) → tests ;
- **front-end** : `npm ci` → type-check (`tsc --noEmit`) → build.

---

## Structure du dépôt

```
.
├── API_Althea-systems/        # API ASP.NET Core 10 (Controllers, Services, Repositories,
│                              #   Models, DTOs, Data/Seed, Migrations, Auth, Validators…)
├── API_Althea-systems.Tests/  # Tests back-end
├── front/                     # Application Next.js 16 (src/app, components, context, lib)
├── scripts/                   # Outillage (génération de diagramme)
├── .github/workflows/         # CI GitHub Actions
├── docker-compose.yml         # Orchestration de toute la stack
└── .env.example               # Gabarit de configuration
```

---

## Dépannage

| Symptôme | Cause / solution |
|---|---|
| Le chatbot répond « service indisponible » juste après le démarrage | Le modèle est encore en cours de téléchargement par `ollama-init`. Suivre la progression : `docker logs -f althea-ollama-init`. |
| Les paiements échouent / les webhooks n'arrivent pas | Vérifier que `STRIPE_WEBHOOK_SECRET` correspond bien au secret affiché par `docker logs althea-stripe-cli`, puis `docker compose restart api`. |
| Aucun e-mail n'est envoyé | `SMTP_HOST` est vide (envoi désactivé) ou identifiants SMTP incorrects. |
| L'API ne démarre pas | Vérifier que `JWT_SECRET_KEY` fait ≥ 32 caractères et que `ENCRYPTION_KEY` est bien une clé base64 de 32 octets. |
| Repartir d'une base vierge | `docker compose down -v` (supprime tous les volumes) puis `docker compose up -d --build`. |

---

> Projet de démonstration. Toutes les clés Stripe sont en **mode test** ; aucune transaction réelle n'est effectuée.
