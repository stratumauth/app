# Stratum Desktop - Log Viewer
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Stratum Desktop - Log Viewer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$logDir = "$env:APPDATA\Stratum\logs"

if (Test-Path $logDir) {
    Write-Host "[OK] Log directory exists: $logDir" -ForegroundColor Green
    Write-Host ""

    $logFiles = Get-ChildItem "$logDir\*.log" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending

    if ($logFiles.Count -eq 0) {
        Write-Host "[!] No log files found" -ForegroundColor Yellow
        Write-Host "This means the application hasn't been run yet, or logging is not working." -ForegroundColor Yellow
    } else {
        Write-Host "Found $($logFiles.Count) log file(s)" -ForegroundColor Green
        Write-Host ""

        $latestLog = $logFiles[0]
        Write-Host "Latest log file: $($latestLog.Name)" -ForegroundColor Cyan
        Write-Host "Last modified: $($latestLog.LastWriteTime)" -ForegroundColor Cyan
        Write-Host "Size: $([math]::Round($latestLog.Length / 1KB, 2)) KB" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "Last 100 lines:" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""

        Get-Content $latestLog.FullName -Tail 100
    }
} else {
    Write-Host "[X] Log directory does not exist: $logDir" -ForegroundColor Red
    Write-Host "The application has not been run yet." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Press any key to exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
