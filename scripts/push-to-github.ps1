# =====================================================================
#  Al Thiqa - One-click GitHub publish (English-only to avoid encoding)
# =====================================================================
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName Microsoft.VisualBasic
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoName = "althiqa-global"
$repoDesc = "Al Thiqa Global Cleaning Services - hourly maid booking platform"
$repoRoot = "c:\Users\RAO\Desktop\HomeMaids"

Set-Location $repoRoot

function Show-Box($title, $msg, $icon = "Information") {
    [System.Windows.Forms.MessageBox]::Show($msg, "Al Thiqa - $title", "OK", $icon) | Out-Null
}

function Pause-And-Exit($code = 0) {
    Write-Host ""
    Write-Host "Press any key to close..." -ForegroundColor Yellow
    $null = [System.Console]::ReadKey($true)
    exit $code
}

try {
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host " Al Thiqa - Push to GitHub" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""

    # === Step 1: open PAT creation page ===
    Write-Host "[1/4] Opening GitHub Personal Access Token page..." -ForegroundColor Cyan
    Start-Process "https://github.com/settings/tokens/new?description=althiqa-render-push&scopes=repo,workflow"
    Start-Sleep 2

    Show-Box "Step 1" "In the GitHub page that just opened:`n`n1. Scroll down`n2. Click the green 'Generate token' button`n3. Copy the token (starts with ghp_...)`n4. Paste it in the next dialog`n`nClick OK when ready."

    # === Step 2: ask user for the PAT ===
    $pat = [Microsoft.VisualBasic.Interaction]::InputBox(
        "Paste your Personal Access Token here (starts with ghp_):",
        "Al Thiqa - GitHub Token",
        ""
    )
    if ([string]::IsNullOrWhiteSpace($pat)) {
        Show-Box "Cancelled" "No token entered. Cancelled." "Warning"
        Write-Host "CANCELLED: no token provided" -ForegroundColor Red
        Pause-And-Exit 1
    }
    $pat = $pat.Trim()
    Write-Host "  Token received (length: $($pat.Length) chars)" -ForegroundColor Green

    # === Step 3: validate token via API ===
    Write-Host "[2/4] Verifying token..." -ForegroundColor Cyan
    $headers = @{
        Authorization = "token $pat"
        Accept        = "application/vnd.github+json"
        "User-Agent"  = "althiqa-push-script"
    }
    try {
        $me = Invoke-RestMethod -Uri "https://api.github.com/user" -Headers $headers -ErrorAction Stop
        $username = $me.login
        Write-Host "  Authenticated as: $username" -ForegroundColor Green
    } catch {
        $emsg = $_.Exception.Message
        Show-Box "Auth Failed" "Token invalid or expired:`n$emsg" "Error"
        Write-Host "FAIL: $emsg" -ForegroundColor Red
        Pause-And-Exit 1
    }

    # === Step 4: create repository (or use existing) ===
    Write-Host "[3/4] Creating repository $repoName..." -ForegroundColor Cyan
    $createBody = @{
        name        = $repoName
        description = $repoDesc
        private     = $true
        auto_init   = $false
    } | ConvertTo-Json

    $htmlUrl = $null
    $remoteUrl = $null
    try {
        $repo = Invoke-RestMethod -Method Post -Uri "https://api.github.com/user/repos" -Headers $headers -Body $createBody -ContentType "application/json" -ErrorAction Stop
        $remoteUrl = $repo.clone_url
        $htmlUrl   = $repo.html_url
        Write-Host "  Created: $htmlUrl" -ForegroundColor Green
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response) { $statusCode = [int]$_.Exception.Response.StatusCode }
        if ($statusCode -eq 422) {
            Write-Host "  Repo already exists, using it..." -ForegroundColor Yellow
            try {
                $existing = Invoke-RestMethod -Uri "https://api.github.com/repos/$username/$repoName" -Headers $headers -ErrorAction Stop
                $remoteUrl = $existing.clone_url
                $htmlUrl   = $existing.html_url
                Write-Host "  Using existing: $htmlUrl" -ForegroundColor Green
            } catch {
                Show-Box "Repo Read Failed" $_.Exception.Message "Error"
                Pause-And-Exit 1
            }
        } else {
            Show-Box "Create Failed" "HTTP $statusCode`n$($_.Exception.Message)" "Error"
            Write-Host "FAIL ($statusCode): $($_.Exception.Message)" -ForegroundColor Red
            Pause-And-Exit 1
        }
    }

    # === Step 5: push ===
    Write-Host "[4/4] Pushing files to GitHub (may take a minute)..." -ForegroundColor Cyan
    $authUrl = $remoteUrl -replace "https://", "https://x-access-token:$pat@"

    & git remote remove origin 2>$null | Out-Null
    & git remote add origin $authUrl
    & git config user.email "$username@users.noreply.github.com"
    & git config user.name $username

    $currentBranch = (& git rev-parse --abbrev-ref HEAD 2>&1).Trim()
    if ($currentBranch -ne "main") { & git branch -M main 2>&1 | Out-Null }

    $pushOutput = & git push -u origin main 2>&1
    if ($LASTEXITCODE -ne 0) {
        $msg = ($pushOutput | Out-String).Trim()
        Show-Box "Push Failed" $msg "Error"
        Write-Host "FAIL: $msg" -ForegroundColor Red
        Pause-And-Exit 1
    }
    Write-Host "  Push successful." -ForegroundColor Green

    & git remote set-url origin $remoteUrl

    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host " SUCCESS - Project is on GitHub!" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host " Repo: $htmlUrl" -ForegroundColor White
    Write-Host ""

    Show-Box "SUCCESS" "Project pushed successfully!`n`nRepo: $htmlUrl`n`nClick OK to open Render + GitHub in your browser."

    Start-Process "https://dashboard.render.com/select-repo?type=blueprint"
    Start-Process $htmlUrl

    Pause-And-Exit 0
}
catch {
    $emsg = "Unexpected error:`n`n$($_.Exception.Message)`n`nLine: $($_.InvocationInfo.ScriptLineNumber)"
    Show-Box "Error" $emsg "Error"
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    Pause-And-Exit 1
}
