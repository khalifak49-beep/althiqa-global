# ====================================================================
#  Al Thiqa - 24/7 watchdog
#  Keeps the ASP.NET server + Cloudflare tunnel alive forever.
#  Restarts each on crash. Writes current public URL to current-url.txt.
# ====================================================================
$ErrorActionPreference = 'Continue'
$root      = "c:\Users\RAO\Desktop\HomeMaids"
$dll       = Join-Path $root "bin\Debug\net9.0\HomeMaids.dll"
$cf        = Join-Path $env:TEMP "cloudflared.exe"
$logDir    = Join-Path $root "Logs\watchdog"
$urlFile   = Join-Path $root "current-url.txt"
$port      = 5050

New-Item -ItemType Directory -Path $logDir -Force | Out-Null

function Log($msg) {
    $stamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line  = "[$stamp] $msg"
    $line | Out-File -FilePath (Join-Path $logDir ("watchdog-" + (Get-Date -Format "yyyy-MM-dd") + ".log")) -Append -Encoding utf8
}

# Wait for SQL Server to be available (system might be in early boot)
function Wait-Sql {
    for ($i=0; $i -lt 30; $i++) {
        try {
            $r = sqlcmd -S ".\SQLEXPRESS" -d HomeMaidsDb -E -Q "SELECT 1" -h -1 -W 2>$null
            if ($LASTEXITCODE -eq 0) { Log "SQL Server is up"; return $true }
        } catch {}
        Start-Sleep 10
    }
    Log "WARN: SQL Server not responsive after 5 min, continuing anyway"
    return $false
}

# Start (or restart) the ASP.NET server, return its Process object
function Start-Server {
    Log "Starting ASP.NET server on :$port ..."
    # Free port first
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | ForEach-Object {
        Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep 2
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "http://0.0.0.0:$port"
    $serverLog = Join-Path $logDir "server.log"
    $p = Start-Process -FilePath "dotnet" -ArgumentList "`"$dll`"" `
            -RedirectStandardOutput $serverLog -RedirectStandardError (Join-Path $logDir "server.err.log") `
            -WindowStyle Hidden -PassThru
    return $p
}

# Start (or restart) the Cloudflare tunnel
function Start-Tunnel {
    Log "Starting Cloudflare tunnel ..."
    Get-Process cloudflared -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep 2
    $tunnelLog = Join-Path $logDir "tunnel.log"
    $tunnelErr = Join-Path $logDir "tunnel.err.log"
    if (Test-Path $tunnelLog) { Remove-Item $tunnelLog -Force }
    if (Test-Path $tunnelErr) { Remove-Item $tunnelErr -Force }
    $p = Start-Process -FilePath $cf `
            -ArgumentList "tunnel","--no-autoupdate","--url","http://127.0.0.1:$port" `
            -RedirectStandardOutput $tunnelLog -RedirectStandardError $tunnelErr `
            -WindowStyle Hidden -PassThru
    return @{ Process = $p; OutPath = $tunnelLog; ErrPath = $tunnelErr }
}

# Read the public URL — cloudflared on Windows writes everything to stderr
function Read-TunnelUrl($tunnel) {
    foreach ($path in @($tunnel.OutPath, $tunnel.ErrPath)) {
        if (-not (Test-Path $path)) { continue }
        $content = Get-Content $path -Raw -ErrorAction SilentlyContinue
        if (-not $content) { continue }
        $m = [regex]::Match($content, "https://[a-z0-9-]+\.trycloudflare\.com")
        if ($m.Success -and $m.Value -ne "https://api.trycloudflare.com") { return $m.Value }
    }
    return $null
}

# Check if server responds locally
function Test-Server {
    try {
        $r = Invoke-WebRequest -Uri "http://127.0.0.1:$port/" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        return $r.StatusCode -lt 500
    } catch { return $false }
}

# === MAIN LOOP ===
Log "Watchdog started (PID $PID)"
Wait-Sql | Out-Null

$server = Start-Server
$tunnel = Start-Tunnel
$lastUrl = $null
$lastUrlWrittenAt = Get-Date

while ($true) {
    Start-Sleep 30

    # 1) Health-check server
    if (-not (Test-Server)) {
        Log "Server unresponsive — restarting..."
        try { Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue } catch {}
        Start-Sleep 3
        $server = Start-Server
        Start-Sleep 8
    }

    # 2) Health-check tunnel
    if ($tunnel.Process.HasExited) {
        Log "Tunnel process exited — restarting..."
        $tunnel = Start-Tunnel
        Start-Sleep 6
    }

    # 3) Update current URL file when changed
    $url = Read-TunnelUrl $tunnel
    if ($url -and $url -ne $lastUrl) {
        $lastUrl = $url
        $lastUrlWrittenAt = Get-Date
        @"
=========================================
 الثقة العالمية - الرابط العام الحالي
=========================================
$url

آخر تحديث: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
=========================================
"@ | Out-File -FilePath $urlFile -Encoding utf8
        Log "Public URL: $url"
    }
}
