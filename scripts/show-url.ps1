# عرض الرابط العام الحالي للموقع
$f = "c:\Users\RAO\Desktop\HomeMaids\current-url.txt"
if (Test-Path $f) {
    Get-Content $f -Encoding utf8
} else {
    Write-Host "الرابط لم يُسجل بعد. تأكد من أن النظام شغّال ثم انتظر دقيقة." -ForegroundColor Yellow
}
Write-Host ""
Write-Host "اضغط أي زر للخروج..."
$null = [System.Console]::ReadKey($true)
