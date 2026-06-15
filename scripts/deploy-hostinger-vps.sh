#!/bin/bash
# =====================================================================
#  Al Thiqa Global — One-shot Hostinger VPS deployment (Ubuntu 22.04)
#
#  Usage:
#    1. SSH into your VPS:  ssh root@YOUR_VPS_IP
#    2. Download this:      wget https://raw.githubusercontent.com/khalifak49-beep/althiqa-global/main/scripts/deploy-hostinger-vps.sh
#    3. Edit DOMAIN below   nano deploy-hostinger-vps.sh
#    4. Run:                chmod +x deploy-hostinger-vps.sh && ./deploy-hostinger-vps.sh
#
#  This installs: .NET 9 + PostgreSQL 16 + Nginx + Certbot + UFW + the app
#  Total runtime: ~10-15 minutes
# =====================================================================

set -e  # Stop on any error

# ===== EDIT THESE BEFORE RUNNING =====
# Leave DOMAIN empty ("") to run on IP only (HTTP). Add the domain + re-run the SSL step later.
DOMAIN=""                          # Your domain (or leave empty for IP-only access)
EMAIL="admin@althiqa.local"        # For Let's Encrypt SSL certificate (only used when DOMAIN is set)
DB_PASSWORD="ChangeMe_Strong_2026!" # PostgreSQL password — pick a strong one!
ADMIN_EMAIL="admin@althiqa.local"   # Initial admin login
ADMIN_PASSWORD="ChangeMe_Strong_2026!" # Initial admin password
REPO_URL="https://github.com/khalifak49-beep/althiqa-global.git"
APP_DIR="/var/www/althiqa"
SERVICE_USER="althiqa"
# ======================================

# Auto-detect server IP (used when DOMAIN is empty)
SERVER_IP="$(curl -s ifconfig.me || curl -s ipinfo.io/ip || hostname -I | awk '{print $1}')"
SERVER_NAME="${DOMAIN:-$SERVER_IP}"

echo "================================================"
echo " Al Thiqa Global — Hostinger VPS Deployment"
echo "================================================"
if [ -z "$DOMAIN" ]; then
    echo " Mode:     IP-only (no domain, HTTP only)"
    echo " Access:   http://$SERVER_IP"
else
    echo " Domain:   $DOMAIN  (must point to $SERVER_IP)"
    echo " Access:   https://$DOMAIN"
fi
echo "App dir:  $APP_DIR"
echo ""

# === 1. System update ===
echo "[1/9] Updating system packages..."
apt-get update -qq
apt-get upgrade -y -qq

# === 2. Install .NET 9 SDK + Runtime ===
echo "[2/9] Installing .NET 9..."
wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
dpkg -i /tmp/packages-microsoft-prod.deb >/dev/null
apt-get update -qq
apt-get install -y -qq dotnet-sdk-9.0 aspnetcore-runtime-9.0
dotnet --version

# === 3. Install PostgreSQL 16 ===
echo "[3/9] Installing PostgreSQL 16..."
sh -c 'echo "deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | apt-key add - >/dev/null 2>&1
apt-get update -qq
apt-get install -y -qq postgresql-16 postgresql-contrib-16

systemctl enable postgresql
systemctl start postgresql

# Create DB + user
sudo -u postgres psql <<EOF
CREATE USER althiqa_app WITH PASSWORD '$DB_PASSWORD';
CREATE DATABASE althiqa OWNER althiqa_app;
GRANT ALL PRIVILEGES ON DATABASE althiqa TO althiqa_app;
EOF
echo "PostgreSQL DB 'althiqa' created with user 'althiqa_app'"

# === 4. Install Nginx + Certbot ===
echo "[4/9] Installing Nginx + Certbot..."
apt-get install -y -qq nginx certbot python3-certbot-nginx

# === 5. UFW firewall ===
echo "[5/9] Configuring firewall..."
ufw allow OpenSSH
ufw allow 'Nginx Full'
ufw --force enable

# === 6. Create service user + clone repo ===
echo "[6/9] Cloning + building the app..."
id -u $SERVICE_USER &>/dev/null || useradd -r -s /bin/false -d /var/lib/$SERVICE_USER $SERVICE_USER

mkdir -p $APP_DIR
cd $APP_DIR
if [ -d ".git" ]; then
    git pull
else
    git clone $REPO_URL .
fi

# Publish
dotnet publish HomeMaids.csproj -c Release -o $APP_DIR/publish --no-self-contained

# Create folders the app writes to
mkdir -p $APP_DIR/publish/Logs $APP_DIR/publish/Backups $APP_DIR/publish/wwwroot/images/workers/uploads
chown -R $SERVICE_USER:$SERVICE_USER $APP_DIR/publish

# === 7. Configure systemd ===
echo "[7/9] Setting up systemd service..."
cat > /etc/systemd/system/althiqa.service <<EOF
[Unit]
Description=Al Thiqa Global Cleaning Services
After=network.target postgresql.service
Requires=postgresql.service

[Service]
WorkingDirectory=$APP_DIR/publish
ExecStart=/usr/bin/dotnet $APP_DIR/publish/HomeMaids.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=althiqa
User=$SERVICE_USER
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=DATABASE_URL=postgres://althiqa_app:$DB_PASSWORD@localhost:5432/althiqa
Environment=AdminSeed__Email=$ADMIN_EMAIL
Environment=AdminSeed__Password=$ADMIN_PASSWORD
Environment=AdminSeed__FullName=System Administrator
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable althiqa
systemctl start althiqa
sleep 5

# === 8. Configure Nginx as reverse proxy ===
echo "[8/9] Configuring Nginx reverse proxy..."

if [ -z "$DOMAIN" ]; then
    NGINX_SERVER_NAME="_"
else
    NGINX_SERVER_NAME="$DOMAIN www.$DOMAIN"
fi

cat > /etc/nginx/sites-available/althiqa <<EOF
server {
    listen 80 default_server;
    server_name $NGINX_SERVER_NAME;

    client_max_body_size 20M;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_read_timeout 300;
    }
}
EOF

ln -sf /etc/nginx/sites-available/althiqa /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl restart nginx

# === 9. SSL with Let's Encrypt (only when DOMAIN is set) ===
if [ -z "$DOMAIN" ]; then
    echo "[9/9] Skipping SSL — no domain configured."
    echo "      Site is reachable at: http://$SERVER_IP"
    echo "      To add SSL later: edit /tmp/deploy.sh, set DOMAIN, then run: certbot --nginx -d <domain> -m <email>"
else
    echo "[9/9] Obtaining SSL certificate from Let's Encrypt..."
    certbot --nginx -d $DOMAIN -d www.$DOMAIN --non-interactive --agree-tos -m $EMAIL --redirect \
        || echo "⚠️  SSL step failed — DNS may not have propagated yet. Re-run later: certbot --nginx -d $DOMAIN -d www.$DOMAIN -m $EMAIL --agree-tos"
    systemctl enable certbot.timer 2>/dev/null || true
fi

# === DONE ===
echo ""
echo "================================================"
echo " ✅ DEPLOYMENT COMPLETE"
echo "================================================"
if [ -z "$DOMAIN" ]; then
    echo " 🌐 Site:    http://$SERVER_IP"
else
    echo " 🌐 Site:    https://$DOMAIN"
fi
echo " 🔑 Admin:   $ADMIN_EMAIL / $ADMIN_PASSWORD"
echo " 📊 Status:  systemctl status althiqa"
echo " 📋 Logs:    journalctl -u althiqa -f"
echo " 🔄 Restart: systemctl restart althiqa"
echo " 🔃 Update:  cd $APP_DIR && git pull && dotnet publish HomeMaids.csproj -c Release -o publish && systemctl restart althiqa"
echo "================================================"
echo ""
echo "⚠️  IMMEDIATE ACTIONS:"
echo " 1. Login + change the admin password"
echo " 2. Configure Thawani keys: /Admin/PaymentGateways"
echo " 3. Configure WhatsApp/Email OTP: /Admin/WhatsApp + /Admin/EmailSettings"
echo " 4. Test the booking + payment flow"
