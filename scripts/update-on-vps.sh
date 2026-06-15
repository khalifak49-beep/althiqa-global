#!/bin/bash
# =====================================================================
#  Quick update script — pulls latest from GitHub + rebuilds + restarts
#  Run this on the VPS each time you push changes:
#    cd /var/www/althiqa && ./scripts/update-on-vps.sh
# =====================================================================
set -e

APP_DIR="/var/www/althiqa"
SERVICE_USER="althiqa"

cd $APP_DIR
echo "[1/4] Pulling latest changes..."
git pull

echo "[2/4] Publishing release build..."
dotnet publish HomeMaids.csproj -c Release -o $APP_DIR/publish --no-self-contained

echo "[3/4] Fixing permissions..."
chown -R $SERVICE_USER:$SERVICE_USER $APP_DIR/publish

echo "[4/4] Restarting service..."
systemctl restart althiqa
sleep 3
systemctl status althiqa --no-pager -l | head -10

echo ""
echo "✅ Update complete — site is live at \$DOMAIN"
