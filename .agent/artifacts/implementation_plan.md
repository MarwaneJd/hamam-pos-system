# 🏢 Système de Gestion Hammam - Plan d'Implémentation

## Vue d'ensemble

Ce projet comprend 3 composants principaux :
1. **Desktop App** (C#/WPF/.NET 8) - Point de vente pour les employés
2. **Backend API** (ASP.NET Core 8) - Serveur central avec PostgreSQL
3. **Dashboard Web** (React 18/Vite/Tailwind) - Administration

---

## 📁 Structure du Projet

```
systemHammam/
├── src/
│   ├── HammamDesktop/           # Application Desktop WPF
│   │   ├── HammamDesktop.App/   # Projet principal WPF
│   │   ├── HammamDesktop.Core/  # Logique métier partagée
│   │   └── HammamDesktop.Data/  # Accès données SQLite
│   │
│   ├── HammamAPI/               # Backend API
│   │   ├── HammamAPI.Domain/    # Entités domaine
│   │   ├── HammamAPI.Application/ # Services métier
│   │   ├── HammamAPI.Infrastructure/ # EF Core, PostgreSQL
│   │   └── HammamAPI.WebAPI/    # Controllers, JWT
│   │
│   └── hammam-dashboard/        # Dashboard React
│       ├── src/
│       │   ├── components/
│       │   ├── pages/
│       │   ├── services/
│       │   └── hooks/
│       └── package.json
│
├── database/
│   ├── migrations/
│   └── seeds/
│
├── docs/
│   └── API.md
│
└── README.md
```

---

## 🎯 Phase 1 : Infrastructure de Base (Semaine 1)

### 1.1 Backend API - Domain Layer
- [ ] Créer solution ASP.NET Core 8
- [ ] Entités : `Hammam`, `Employe`, `TypeTicket`, `Ticket`
- [ ] Value Objects : `HammamId`, `TicketId` (UUID)
- [ ] Enums : `SyncStatus`, `EmployeRole`

### 1.2 Backend API - Infrastructure Layer
- [ ] Configuration PostgreSQL avec EF Core 8
- [ ] Migrations initiales
- [ ] Seeds de données (6 hammams, 3 types tickets)

### 1.3 Backend API - WebAPI Layer
- [ ] Configuration JWT Authentication
- [ ] Endpoints de base CRUD
- [ ] Swagger/OpenAPI documentation
- [ ] CORS configuration

---

## 🖥️ Phase 2 : Application Desktop (Semaine 2)

### 2.1 Structure MVVM
- [ ] Configuration DI avec Microsoft.Extensions.DependencyInjection
- [ ] Base ViewModels avec CommunityToolkit.Mvvm
- [ ] Navigation entre vues

### 2.2 Base de données locale
- [ ] Configuration SQLite avec EF Core
- [ ] Schéma miroir du serveur
- [ ] Flag `SyncStatus` sur les tickets

### 2.3 Écran de connexion
- [ ] Vue Login avec Material Design
- [ ] Authentification JWT
- [ ] Stockage sécurisé du token
- [ ] Expiration 8h automatique

### 2.4 Écran principal de vente
- [ ] 3 gros boutons : HOMME (15 DH), FEMME (15 DH), ENFANT (10 DH)
- [ ] Compteur tickets du jour
- [ ] Son de confirmation (bip)
- [ ] Animation visuelle de confirmation

### 2.5 Synchronisation
- [ ] Service de sync toutes les 5 minutes
- [ ] Détection connexion internet
- [ ] Envoi par batch de 100 tickets
- [ ] Gestion des erreurs et retry

---

## 🌐 Phase 3 : Dashboard Web (Semaine 3)

### 3.1 Setup React
- [ ] Création projet Vite + React 18
- [ ] Configuration Tailwind CSS 3
- [ ] Structure des dossiers
- [ ] React Router v6

### 3.2 Authentification Admin
- [ ] Page Login
- [ ] Context Auth avec JWT
- [ ] Protected Routes
- [ ] Logout automatique

### 3.3 Dashboard principal
- [ ] 3 KPIs en haut (tickets, revenus, hammams actifs)
- [ ] Tableau des 6 hammams
- [ ] Tableau des 12 employés (classement)
- [ ] Filtres de période (Aujourd'hui, 7j, 30j, Custom)

### 3.4 Gestion des employés
- [ ] Liste avec pagination
- [ ] Modal création employé
- [ ] Modal modification
- [ ] Reset mot de passe (génération aléatoire)
- [ ] Activation/Désactivation

### 3.5 Rapports
- [ ] Sélection type rapport
- [ ] Filtres hammams/employés
- [ ] Prévisualisation tableau
- [ ] Export Excel (.xlsx)
- [ ] Export PDF
- [ ] Export CSV

---

## 🔧 Phase 4 : Fonctionnalités Avancées (Semaine 4)

### 4.1 Détection des écarts
- [ ] Calcul automatique écarts de caisse
- [ ] Alertes visuelles (rouge si > 5%)
- [ ] Historique des écarts

### 4.2 Tests et optimisations
- [ ] Tests de charge (6 PC simultanés)
- [ ] Tests offline 3 jours
- [ ] Optimisation SQL indexes
- [ ] Benchmark génération rapports

### 4.3 Packaging et déploiement
- [ ] Installeur Windows (Inno Setup)
- [ ] Script de déploiement serveur
- [ ] Documentation utilisateur

---

## 🗄️ Schéma Base de Données

### Table `hammam`
| Colonne | Type | Description |
|---------|------|-------------|
| id | UUID | Clé primaire |
| code | VARCHAR(10) | Code unique (ex: HAM001) |
| nom | VARCHAR(100) | Nom du hammam |
| adresse | VARCHAR(255) | Adresse complète |
| actif | BOOLEAN | Est-ce actif ? |
| created_at | TIMESTAMP | Date création |

### Table `employe`
| Colonne | Type | Description |
|---------|------|-------------|
| id | UUID | Clé primaire |
| username | VARCHAR(50) | Login unique |
| password_hash | VARCHAR(255) | BCrypt hash |
| nom | VARCHAR(100) | Nom complet |
| prenom | VARCHAR(100) | Prénom |
| hammam_id | UUID | FK vers hammam |
| langue | VARCHAR(2) | FR, AR |
| role | VARCHAR(20) | EMPLOYE, ADMIN |
| actif | BOOLEAN | Peut se connecter ? |
| created_at | TIMESTAMP | Date création |

### Table `type_ticket`
| Colonne | Type | Description |
|---------|------|-------------|
| id | UUID | Clé primaire |
| nom | VARCHAR(50) | HOMME, FEMME, ENFANT |
| prix | DECIMAL(10,2) | Prix en DH |

### Table `ticket`
| Colonne | Type | Description |
|---------|------|-------------|
| id | UUID | Clé primaire (générée localement) |
| type_ticket_id | UUID | FK vers type_ticket |
| employe_id | UUID | FK vers employe |
| hammam_id | UUID | FK vers hammam |
| prix | DECIMAL(10,2) | Prix au moment de la vente |
| created_at | TIMESTAMP | Date/heure vente |
| synced_at | TIMESTAMP | Date synchronisation (null si non sync) |
| sync_status | VARCHAR(20) | PENDING, SYNCED, ERROR |

---

## 🔐 Configuration Sécurité

### JWT Settings
```json
{
  "JwtSettings": {
    "Secret": "[256-bit secret key]",
    "Issuer": "HammamAPI",
    "Audience": "HammamClients",
    "ExpirationHours": 8,
    "RefreshTokenExpirationDays": 30
  }
}
```

### BCrypt
- Cost Factor: 12
- Salt: Auto-generated

---

## ✅ Critères de Validation

| Test | Critère |
|------|---------|
| Offline 3 jours | Ventes continuent sans problème |
| Sync après coupure | < 2 minutes pour tout synchroniser |
| Rapport 1000 tickets | Génération < 5 secondes |
| Nouvel employé | Connexion possible en < 30 secondes |
| Écart caisse | Détecté et affiché en rouge si > 5% |
| 6 PC simultanés | Aucun ralentissement |
| Délai dashboard | Stats à jour < 10 secondes après vente |

---

## 🚀 Prochaines Étapes

1. **Créer la structure des projets**
2. **Implémenter le Backend API (base)**
3. **Créer l'Application Desktop**
4. **Développer le Dashboard React**
5. **Tests et optimisations**
