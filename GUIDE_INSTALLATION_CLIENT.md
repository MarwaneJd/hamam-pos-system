# 📋 Guide d'Installation — Application Hammam (Pour le Client)

> Ce guide est destiné à la personne qui installe l'application sur les PCs des hammams.

---

## Étape 1 : Installer le Runtime .NET 8 (une seule fois par PC)

1. Ouvrir ce lien dans un navigateur :  
   👉 **https://dotnet.microsoft.com/en-us/download/dotnet/8.0**

2. Dans la section **".NET Desktop Runtime"**, cliquer sur **"x64"** à côté de **"Windows"**  
   *(le fichier fait environ 55 MB)*

3. Double-cliquer sur le fichier téléchargé

4. Cliquer sur **"Installer"** → attendre → **"Fermer"**

✅ **C'est fait !** Pas besoin de redémarrer le PC.

---

## Étape 2 : Installer l'application Hammam

1. Copier le dossier `release\` reçu (par clé USB, WhatsApp, ou email) dans `C:\Hammam\`

2. Vérifier que le fichier `appsettings.json` contient la bonne adresse du serveur

3. Double-cliquer sur **HammamDesktop.App.exe** pour lancer l'application

4. *(Optionnel)* Créer un raccourci sur le bureau :
   - Clic droit sur `HammamDesktop.App.exe` → **Envoyer vers** → **Bureau (créer un raccourci)**

---

## Étape 3 : Mettre à jour l'application

Quand vous recevez une nouvelle version :

1. Fermer l'application Hammam si elle est ouverte
2. Remplacer l'ancien `HammamDesktop.App.exe` par le nouveau
3. Relancer l'application

> ⚠️ **Ne pas supprimer** le fichier `appsettings.json` — il contient la configuration.

---

## ❓ En cas de problème

| Problème | Solution |
|----------|----------|
| "L'application ne démarre pas" | Vérifier que le Runtime .NET 8 est bien installé (Étape 1) |
| "Erreur de connexion au serveur" | Vérifier la connexion Internet du PC |
| "L'imprimante n'imprime pas" | Vérifier que l'imprimante est branchée et allumée |
| "L'application est lente" | Redémarrer le PC, vérifier la connexion Internet |
