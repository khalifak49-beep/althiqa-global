# إيقاف النظام كاملاً (السيرفر + النفق + المراقب)
Write-Host "إيقاف Watchdog..."
Get-Process powershell, pwsh -ErrorAction SilentlyContinue | Where-Object {
    $_.CommandLine -match 'run-forever\.ps1'
} | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "إيقاف السيرفر..."
Get-NetTCPConnection -LocalPort 5050 -State Listen -ErrorAction SilentlyContinue | ForEach-Object {
    Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
}
Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object { $_.MainModule.FileName -like "*\dotnet.exe" } | ForEach-Object {
    # Only stop our HomeMaids process, not all dotnet
    if (((Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)").CommandLine -like "*HomeMaids.dll*")) {
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "إيقاف نفق Cloudflare..."
Get-Process cloudflared -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "تم إيقاف كل العمليات." -ForegroundColor Green
Write-Host "اضغط أي زر للخروج..."
$null = [System.Console]::ReadKey($true)
