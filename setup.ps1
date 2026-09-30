#!/usr/bin/env pwsh

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Expense Tracker Application Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Step 1: Checking .NET installation..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ ERROR: .NET is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install .NET 9.0 SDK from https://dotnet.microsoft.com/download" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Step 2: Installing Entity Framework Core Tools..." -ForegroundColor Yellow
try {
    dotnet tool install --global dotnet-ef --version 9.0.0 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Updating Entity Framework Core Tools..." -ForegroundColor Yellow
        dotnet tool update --global dotnet-ef --version 9.0.0
    }
    Write-Host "✅ EF Core Tools installed/updated" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to install EF Core Tools" -ForegroundColor Red
}

Write-Host ""
Write-Host "Step 3: Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ NuGet packages restored successfully" -ForegroundColor Green
} else {
    Write-Host "❌ ERROR: Failed to restore packages" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Step 4: Building the project..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Project built successfully" -ForegroundColor Green
} else {
    Write-Host "❌ ERROR: Build failed" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Step 5: Creating database migration..." -ForegroundColor Yellow
dotnet ef migrations add InitialCreate --force
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Migration created successfully" -ForegroundColor Green
} else {
    Write-Host "❌ ERROR: Failed to create migration" -ForegroundColor Red
    Write-Host "Please check your connection string in appsettings.json" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Step 6: Updating database..." -ForegroundColor Yellow
dotnet ef database update
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Database updated successfully" -ForegroundColor Green
} else {
    Write-Host "❌ ERROR: Failed to update database" -ForegroundColor Red
    Write-Host "Please ensure MySQL is running and connection string is correct" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Default Admin Account:" -ForegroundColor Yellow
Write-Host "Email: admin@expensetracker.com" -ForegroundColor White
Write-Host "Password: Admin123!" -ForegroundColor White
Write-Host ""
Write-Host "To start the application, run: " -NoNewline -ForegroundColor Yellow
Write-Host "dotnet run" -ForegroundColor Cyan
Write-Host "Then open: " -NoNewline -ForegroundColor Yellow
Write-Host "https://localhost:5001" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to exit"
