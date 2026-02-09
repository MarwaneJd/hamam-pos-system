#!/bin/bash
# ============================================
#  Hammam System - VPS Deployment Script
#  Run: chmod +x deploy.sh && ./deploy.sh
# ============================================

set -e

echo "========================================"
echo "  DEPLOIEMENT DU SYSTEME HAMMAM"
echo "========================================"
echo ""

# ============================================
# 1. Install Docker if not present
# ============================================
if ! command -v docker &> /dev/null; then
    echo "[1/5] Installation de Docker..."
    curl -fsSL https://get.docker.com | sh
    sudo usermod -aG docker $USER
    echo "Docker installé. Reconnectez-vous pour appliquer les permissions."
else
    echo "[1/5] Docker déjà installé ✅"
fi

# ============================================
# 2. Install Docker Compose plugin if not present
# ============================================
if ! docker compose version &> /dev/null; then
    echo "[2/5] Installation de Docker Compose..."
    sudo apt-get update && sudo apt-get install -y docker-compose-plugin
else
    echo "[2/5] Docker Compose déjà installé ✅"
fi

# ============================================
# 3. Create .env file from template
# ============================================
if [ ! -f .env ]; then
    echo "[3/5] Création du fichier .env depuis .env.example..."
    cp .env.example .env
    echo ""
    echo "⚠️  IMPORTANT: Modifiez le fichier .env avec vos vrais secrets!"
    echo "   nano .env"
    echo ""
    echo "   - POSTGRES_PASSWORD : mot de passe PostgreSQL"
    echo "   - JWT_SECRET        : clé secrète JWT (min 32 caractères)"
    echo "   - CORS_ORIGINS      : domaines autorisés (votre domaine)"
    echo ""
    read -p "Appuyez sur Entrée après avoir configuré .env..." 
else
    echo "[3/5] Fichier .env existe déjà ✅"
fi

# ============================================
# 4. Build and start containers
# ============================================
echo "[4/5] Construction et démarrage des conteneurs..."
docker compose up -d --build

# ============================================
# 5. Wait and verify
# ============================================
echo "[5/5] Vérification du déploiement..."
echo ""
echo "Attente du démarrage des services (30s)..."
sleep 30

# Check health
if curl -sf http://localhost/health > /dev/null 2>&1; then
    echo "✅ API Health Check: OK"
else
    echo "⚠️  API pas encore prête, vérifiez les logs:"
    echo "   docker compose logs api"
fi

if curl -sf http://localhost > /dev/null 2>&1; then
    echo "✅ Dashboard: OK"
else
    echo "⚠️  Dashboard pas encore prêt, vérifiez les logs:"
    echo "   docker compose logs dashboard"
fi

# Get server IP
SERVER_IP=$(curl -sf ifconfig.me 2>/dev/null || hostname -I | awk '{print $1}')

echo ""
echo "========================================"
echo "  DEPLOIEMENT TERMINÉ! 🚀"
echo "========================================"
echo ""
echo "  Dashboard:  http://$SERVER_IP"
echo "  API Health:  http://$SERVER_IP/health"
echo ""
echo "  Commandes utiles:"
echo "    docker compose logs -f        # Voir les logs"
echo "    docker compose ps             # État des services"
echo "    docker compose restart api    # Redémarrer l'API"
echo "    docker compose down           # Arrêter tout"
echo "    docker compose up -d --build  # Reconstruire"
echo ""
echo "  ⚠️  Pour HTTPS, configurez un reverse proxy"
echo "     avec Caddy ou Certbot (Let's Encrypt)"
echo "========================================"
