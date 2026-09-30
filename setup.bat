@echo off
echo ========================================
echo Expense Tracker Application Setup
echo ========================================
echo.

echo Step 1: Checking .NET installation...
dotnet --version
if %errorlevel% neq 0 (
    echo ERROR: .NET is not installed or not in PATH
    echo Please install .NET 9.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo.
echo Step 2: Installing Entity Framework Core Tools...
dotnet tool install --global dotnet-ef --version 9.0.0
if %errorlevel% neq 0 (
    echo Updating Entity Framework Core Tools...
    dotnet tool update --global dotnet-ef --version 9.0.0
)

echo.
echo Step 3: Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

echo.
echo Step 4: Building the project...
dotnet build
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Step 5: Creating database migration...
dotnet ef migrations add InitialCreate --force
if %errorlevel% neq 0 (
    echo ERROR: Failed to create migration
    echo Please check your connection string in appsettings.json
    pause
    exit /b 1
)

echo.
echo Step 6: Updating database...
dotnet ef database update
if %errorlevel% neq 0 (
    echo ERROR: Failed to update database
    echo Please ensure MySQL is running and connection string is correct
    pause
    exit /b 1
)

echo.
echo ========================================
echo Setup completed successfully!
echo ========================================
echo.
echo Default Admin Account:
echo Email: admin@expensetracker.com
echo Password: Admin123!
echo.
echo To start the application, run: dotnet run
echo Then open: https://localhost:5001
echo.
pause
