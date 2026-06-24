#!/bin/bash
# =====================================================================
#  Quick update script — pulls latest from GitHub + rebuilds + restarts
#  Run this on the VPS each time you push changes:
#    /var/www/althiqa/scripts/update-on-vps.sh
# =====================================================================
set -e

APP_DIR="/var/www/althiqa"
SERVICE_USER="althiqa"

cd $APP_DIR
echo "[1/5] Pulling latest changes..."
git pull

echo "[2/5] Stopping althiqa service (so publish can overwrite locked files)..."
systemctl stop althiqa || true

echo "[3/5] Publishing release build..."
dotnet publish HomeMaids.csproj -c Release -o $APP_DIR/publish --no-self-contained

echo "[4/5] Fixing permissions..."
chown -R $SERVICE_USER:$SERVICE_USER $APP_DIR/publish

echo "[5/5] Starting service..."
systemctl start althiqa
sleep 4
systemctl status althiqa --no-pager -l | head -10

echo ""
echo "✅ Update complete."
