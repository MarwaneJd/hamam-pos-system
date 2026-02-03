# 🛁 Système de Gestion Hammam

Un système complet de gestion pour les hammams, comprenant une API backend, un dashboard administrateur web et une application de point de vente desktop.

## 📋 Table des matières

- [Architecture](#architecture)
- [Technologies](#technologies)
- [Structure du projet](#structure-du-projet)
- [Installation](#installation)
- [Configuration](#configuration)
- [Démarrage](#démarrage)
- [API Endpoints](#api-endpoints)
- [Fonctionnalités](#fonctionnalités)

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      SYSTÈME HAMMAM                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────┐  │
│  │  Desktop POS    │    │   Dashboard     │    │ Mobile App  │  │
│  │  (WPF/.NET 8)   │    │   (React/Vite)  │    │  (Flutter)  │  │
│  │                 │    │                 │    │  [Prévu]    │  │
│  └────────┬────────┘    └────────┬────────┘    └──────┬──────┘  │
│           │                      │                     │         │
│           └──────────────────────┼─────────────────────┘         │
│                                  │                               │
│                    ┌─────────────▼─────────────┐                 │
│                    │      Backend API          │                 │
│                    │   (ASP.NET Core 8)        │                 │
│                    │                           │                 │
│                    │  ┌─────────────────────┐  │                 │
│                    │  │    WebAPI Layer     │  │                 │
│                    │  │   (Controllers)     │  │                 │
│                    │  └─────────┬───────────┘  │                 │
│                    │            │              │                 │
│                    │  ┌─────────▼───────────┐  │                 │
│                    │  │  Application Layer  │  │                 │
│                    │  │    (Services)       │  │                 │
│                    │  └─────────┬───────────┘  │                 │
│                    │            │              │                 │
│                    │  ┌─────────▼───────────┐  │                 │
│                    │  │ Infrastructure Layer│  │                 │
│                    │  │   (EF Core/Repos)   │  │                 │
│                    │  └─────────┬───────────┘  │                 │
│                    │            │              │                 │
│                    │  ┌─────────▼───────────┐  │                 │
│                    │  │   Domain Layer      │  │                 │
│                    │  │   (Entities)        │  │                 │
│                    │  └─────────────────────┘  │                 │
│                    └─────────────┬─────────────┘                 │
│                                  │                               │
│                    ┌─────────────▼─────────────┐                 │
│                    │       PostgreSQL          │                 │
│                    │        Database           │                 │
│                    └───────────────────────────┘                 │
└─────────────────────────────────────────────────────────────────┘
```

## 🛠️ Technologies

### Backend API
- **Framework:** ASP.NET Core 8
- **Base de données:** PostgreSQL
- **ORM:** Entity Framework Core 8
- **Authentification:** JWT Bearer
- **Logging:** Serilog
- **Documentation:** Swagger/OpenAPI
- **Export:** EPPlus (Excel), QuestPDF (PDF)

### Dashboard Web
- **Framework:** React 18
- **Build:** Vite 5
- **Styling:** Tailwind CSS 3
- **HTTP Client:** Axios
- **Routing:** React Router v6
- **Formulaires:** React Hook Form + Yup

### Desktop POS
- **Framework:** WPF (.NET 8)
- **Pattern:** MVVM
- **UI:** Material Design in XAML
- **Base locale:** SQLite
- **MVVM Toolkit:** CommunityToolkit.Mvvm
- **Resilience:** Polly

## 📁 Structure du projet

```
systemHammam/
├── src/
│   ├── HammamAPI/                    # Backend API
│   │   ├── HammamAPI.Domain/         # Entités et interfaces
│   │   ├── HammamAPI.Application/    # Services et DTOs
│   │   ├── HammamAPI.Infrastructure/ # DbContext et Repositories
│   │   └── HammamAPI.WebAPI/         # Controllers et Program.cs
│   │
│   ├── HammamDesktop/                # Application Desktop
│   │   └── HammamDesktop.App/        # Projet WPF
│   │       ├── Data/                 # Entités et DbContext SQLite
│   │       ├── Services/             # Services métier
│   │       ├── ViewModels/           # ViewModels MVVM
│   │       ├── Views/                # Fenêtres XAML
│   │       ├── Styles/               # Styles personnalisés
│   │       └── Converters/           # Convertisseurs XAML
│   │
│   └── hammam-dashboard/             # Dashboard React
│       ├── src/
│       │   ├── components/           # Composants réutilisables
│       │   ├── pages/                # Pages de l'app
│       │   ├── context/              # Context API (Auth)
│       │   └── services/             # Services API
│       └── public/
│
├── HammamSystem.sln                  # Solution Visual Studio
└── README.md
```

## ⚙️ Installation

### Prérequis
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) ou VS Code

### 1. Cloner le projet
```bash
git clone https://github.com/votre-repo/systemHammam.git
cd systemHammam
```

### 2. Backend API
```bash
cd src/HammamAPI/HammamAPI.WebAPI
dotnet restore
dotnet ef database update
dotnet run
```

### 3. Dashboard Web
```bash
cd src/hammam-dashboard
npm install
npm run dev
```

### 4. Desktop App
Ouvrir `HammamSystem.sln` dans Visual Studio et exécuter `HammamDesktop.App`.

## 🔧 Configuration

### Backend (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=hammam_db;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "Secret": "VOTRE_CLE_SECRETE_TRES_LONGUE_ET_SECURISEE",
    "Issuer": "HammamAPI",
    "Audience": "HammamClients",
    "ExpirationHours": 8
  }
}
```

### Desktop (`appsettings.json`)
```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.hammam.local"
  },
  "SyncSettings": {
    "IntervalMinutes": 5
  }
}
```

## 🚀 Démarrage

```bash
# Terminal 1 - Backend API
cd src/HammamAPI/HammamAPI.WebAPI
dotnet run

# Terminal 2 - Dashboard
cd src/hammam-dashboard
npm run dev
```

Accès:
- **API:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger
- **Dashboard:** http://localhost:3000

## 📡 API Endpoints

### Authentification
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| POST | `/api/auth/login` | Connexion employé |
| POST | `/api/auth/refresh` | Rafraîchir le token |
| POST | `/api/auth/logout` | Déconnexion |

### Tickets
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| GET | `/api/tickets` | Liste des tickets |
| POST | `/api/tickets` | Créer un ticket |
| POST | `/api/tickets/sync` | Synchroniser des tickets |
| GET | `/api/tickets/daily-count` | Compteur journalier |

### Statistiques
| Méthode | Endpoint | Description |
|---------|----------|-------------|
| GET | `/api/stats/dashboard` | Stats dashboard |
| GET | `/api/stats/hammams` | Stats par hammam |
| GET | `/api/stats/employes` | Stats par employé |
| GET | `/api/stats/ecart` | Calcul des écarts |

## ✨ Fonctionnalités

### Application Desktop (POS)
- ✅ 3 gros boutons de vente (HOMME/FEMME/ENFANT)
- ✅ Confirmation audio et visuelle de chaque vente
- ✅ Compteur de tickets journalier
- ✅ Mode hors ligne avec SQLite
- ✅ Synchronisation automatique toutes les 5 minutes
- ✅ Session de 8 heures avec déconnexion auto

### Dashboard Web
- ✅ Vue en temps réel des statistiques
- ✅ Filtres par période (jour/semaine/mois)
- ✅ Gestion des employés (CRUD)
- ✅ Gestion des hammams
- ✅ Génération de rapports (Excel/PDF/CSV)
- ✅ Détection des écarts de caisse

### Backend API
- ✅ Architecture Clean/Layered
- ✅ Authentification JWT
- ✅ Synchronisation robuste avec gestion des conflits
- ✅ Logging avec Serilog
- ✅ Documentation Swagger

## 👥 Utilisateurs par défaut

```
Admin:
  Username: admin
  Password: Admin@123
```

## 📄 Licence

MIT License - Voir [LICENSE](LICENSE) pour plus de détails.

---

Développé avec ❤️ pour la gestion moderne des hammams.
