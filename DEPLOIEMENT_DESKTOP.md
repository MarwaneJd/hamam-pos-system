# Guide de Déploiement — Application Desktop Hammam

## 1. Prérequis — Installer .NET 8 Desktop Runtime

⚠️ **À faire une seule fois par PC**, avant la première utilisation.

1. Télécharger le **Runtime .NET 8 Desktop** :  
   👉 [https://dotnet.microsoft.com/en-us/download/dotnet/8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
   → Chercher **".NET Desktop Runtime 8.x.x"** → **Windows x64** → Télécharger l'installateur
2. Double-cliquer sur le `.exe` téléchargé → **Installer** → C'est fait ✅
3. Aucune configuration supplémentaire nécessaire

> 💡 Sans ce runtime, l'application ne démarre pas. Avec le runtime installé, l'application passe de ~178 MB à **~15-25 MB** et consomme beaucoup moins de RAM.

---

## 2. Publier l'application (framework-dependent + trimming)

```powershell
cd src\HammamDesktop\HammamDesktop.App
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\publish\release
```

Ça produit un dossier `publish\release\` avec un `HammamDesktop.App.exe` léger (~15-25 MB) qui tourne sur n'importe quel Windows 10/11 avec le runtime .NET 8 installé.

> **Ancienne commande (self-contained, ~178 MB)** — ne plus utiliser :
> ```powershell
> dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish\release
> ```

---

## 3. Installation sur chaque PC (6 hammams)

Sur chaque PC :

1. **Installer .NET 8 Desktop Runtime** (voir section 1 — une seule fois)
2. Copier le dossier `release\` sur une **clé USB** (ou téléchargement)
3. Coller dans `C:\Hammam\`
4. Configurer `appsettings.json` avec l'IP du VPS :

```json
{
  "ApiBaseUrl": "https://votre-vps.com/api",
  "PrinterName": ""
}
```

5. Créer un **raccourci bureau** vers `HammamDesktop.App.exe`
6. L'employé se connecte avec son username/password → l'app détecte automatiquement son hammam

### Structure sur le PC client :

```
📁 C:\Hammam\
├── HammamDesktop.App.exe      ← L'application (~15-25 MB)
├── appsettings.json            ← Config (URL API du VPS)
└── Lancer-Hammam.bat           ← Double-clic pour lancer
```

---

## 4. Mises à jour automatiques (photos, prix, types tickets...)

**Il n'y a RIEN à faire côté desktop.** L'architecture sync automatiquement :

| Action admin (dashboard web)       | Côté desktop                                                      |
|------------------------------------|-------------------------------------------------------------------|
| Changer photo d'un type ticket     | Au prochain login/sync, le desktop télécharge la nouvelle image   |
| Changer un prix                    | Le desktop récupère les types tickets à chaque login depuis l'API |
| Ajouter/supprimer un type ticket   | Idem, sync automatique au login                                   |
| Changer nom du hammam              | Récupéré au login                                                  |
| Ajouter un nouvel employé          | Il se connecte directement avec son compte                         |

---

## 5. Mise à jour de l'application (nouveau code)

Si vous modifiez le code desktop (bugs, nouvelles fonctionnalités) :

### Option A — Simple (recommandé pour 6 PCs)

1. Republier avec `dotnet publish` (même commande que section 2)
2. Envoyer le nouveau exe par **WhatsApp / email / clé USB**
3. Le client remplace l'ancien fichier `HammamDesktop.App.exe`

### Option B — Auto-update (si le projet grandit)

- Mettre le exe sur le VPS dans un dossier accessible
- Ajouter un mécanisme de vérification de version au démarrage
- Si nouvelle version disponible → télécharger et remplacer automatiquement

> Pour l'instant, **Option A** suffit largement pour 6 PCs.

---

## 6. Configuration réseau requise

Chaque PC a besoin de :

- ✅ **Internet** — pour communiquer avec le VPS (API backend)
- ✅ **Imprimante thermique 58mm** — connectée en USB avec driver Windows installé
- ✅ **Mode hors-ligne** — les tickets sont stockés en SQLite local et synchronisés automatiquement quand la connexion revient

---

## 7. Résumé du processus

```
┌─────────────────────────────────────────────────────┐
│                   VPS (Serveur)                      │
│                                                      │
│  ┌──────────┐    ┌──────────────┐    ┌───────────┐  │
│  │ API .NET │    │  PostgreSQL  │    │ Dashboard  │  │
│  │ port 5000│    │  Base de     │    │ React      │  │
│  │          │◄──►│  données     │◄──►│ (admin)    │  │
│  └────┬─────┘    └──────────────┘    └───────────┘  │
│       │                                              │
└───────┼──────────────────────────────────────────────┘
        │ HTTPS
        │
   ┌────┴──────────────────────────────────────┐
   │          Internet                          │
   └────┬────────┬────────┬──────────┬─────────┘
        │        │        │          │
   ┌────┴──┐ ┌──┴───┐ ┌──┴───┐  ┌──┴───┐
   │ PC 1  │ │ PC 2 │ │ PC 3 │  │ PC 6 │
   │Hammam1│ │Hammam2│ │Hammam3│ │Hammam6│
   │Desktop│ │Desktop│ │Desktop│ │Desktop│
   │+SQLite│ │+SQLite│ │+SQLite│ │+SQLite│
   └───────┘ └──────┘ └──────┘  └──────┘
```

Chaque PC desktop :
- Se connecte au VPS pour sync les données
- Stocke les tickets localement (SQLite) en cas de coupure internet
- Imprime les tickets sur l'imprimante thermique locale
- L'admin gère **tout** depuis le dashboard web (photos, prix, employés, comptabilité)
