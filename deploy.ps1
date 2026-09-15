# Deployment Automation Script for ASP.NET Core Clothing Ecommerce App

param (
    [Parameter(Mandatory=$false)]
    [ValidateSet("Docker", "Publish", "BuildOnly")]
    [string]$Target = "Docker"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Starting Deployment Task: $Target" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$ProjectPath = $PSScriptRoot

# 1. Clean & Build Verification
Write-Host "`n[1/3] Building solution..." -ForegroundColor Yellow
dotnet build "$ProjectPath\WebApplication_ClothingEcommerce.csproj" -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Deployment aborted." -ForegroundColor Red
    exit 1
}

# 2. Execute Selected Target
if ($Target -eq "Docker") {
    Write-Host "`n[2/3] Building & Launching Docker Compose Containers..." -ForegroundColor Yellow
    docker-compose -f "$ProjectPath\docker-compose.yml" up --build -d
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n[3/3] Docker container launched successfully on http://localhost:8080" -ForegroundColor Green
    } else {
        Write-Host "`nDocker deployment encountered an error. Ensure Docker Desktop is running." -ForegroundColor Red
    }
}
elseif ($Target -eq "Publish") {
    $PublishDir = "$ProjectPath\bin\Release\net9.0\publish"
    Write-Host "`n[2/3] Publishing release build to $PublishDir..." -ForegroundColor Yellow
    dotnet publish "$ProjectPath\WebApplication_ClothingEcommerce.csproj" -c Release -o $PublishDir /p:UseAppHost=false
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n[3/3] Publish completed successfully! Copy contents of '$PublishDir' to your IIS server." -ForegroundColor Green
    }
}
else {
    Write-Host "`n[2/3] Build completed successfully." -ForegroundColor Green
}

Write-Host "`nDeployment task finished." -ForegroundColor Cyan
