@echo off
setlocal enabledelayedexpansion

REM ========================================================
REM  Bootstrap: Azure CLI + Firebase CLI + Terraform backend
REM ========================================================

REM --- Configuration ---
set "RG_NAME=feed-sieve"
set "STORAGE_ACCOUNT=feedsieve"
set "CONTAINER_NAME=terraform-state"
set "LOCATION=westeurope"

echo === Bootstrap: Azure + Firebase + Terraform ===
echo.

REM ========================================================
REM  1. Azure CLI
REM ========================================================
echo [1/7] Checking Azure CLI...
where az >nul 2>&1
if not errorlevel 1 goto :az_ok

echo   Not found. Installing via winget...
where winget >nul 2>&1
if errorlevel 1 (
    echo   winget is not available. Install Azure CLI manually: https://aka.ms/installazurecliwindows
    goto :end_fail
)
winget install -e --id Microsoft.AzureCLI --accept-source-agreements --accept-package-agreements
if errorlevel 1 goto :fail_az_install

REM winget does not refresh PATH for the current shell -- add Azure CLI manually.
set "PATH=%PATH%;%ProgramFiles%\Microsoft SDKs\Azure\CLI2\wbin"
where az >nul 2>&1
if errorlevel 1 (
    echo   Azure CLI installed, but not on PATH. Open a new terminal and re-run this script.
    goto :end_fail
)
:az_ok
echo   OK.
echo.

REM ========================================================
REM  2. Firebase CLI
REM ========================================================
echo [2/7] Checking Firebase CLI...
where firebase >nul 2>&1
if not errorlevel 1 goto :fb_ok

echo   Not found. Installing via npm...
where npm >nul 2>&1
if errorlevel 1 (
    echo   npm not found. Install Node.js from https://nodejs.org/ then re-run.
    goto :end_fail
)
call npm install -g firebase-tools
if errorlevel 1 goto :fail_fb_install
:fb_ok
echo   OK.
echo.

REM ========================================================
REM  3. Azure login (only if needed)
REM ========================================================
echo [3/7] Checking Azure auth...
call az account show >nul 2>&1
if not errorlevel 1 goto :az_auth_ok

echo   Not signed in. Starting device-code login...
call az login --use-device-code
if errorlevel 1 goto :fail_az_login
:az_auth_ok
echo   OK.
echo.

REM ========================================================
REM  4. Resource group / storage account / container
REM ========================================================
echo [4/7] Ensuring Azure storage backend...

call az group show --name %RG_NAME% >nul 2>&1
if not errorlevel 1 goto :rg_ok
echo   Creating resource group %RG_NAME%...
call az group create --name %RG_NAME% --location %LOCATION% >nul
if errorlevel 1 goto :fail_rg
:rg_ok

call az storage account show --name %STORAGE_ACCOUNT% --resource-group %RG_NAME% >nul 2>&1
if not errorlevel 1 goto :sa_ok
echo   Creating storage account %STORAGE_ACCOUNT%...
call az storage account create ^
    --name %STORAGE_ACCOUNT% ^
    --resource-group %RG_NAME% ^
    --location %LOCATION% ^
    --sku Standard_LRS >nul
if errorlevel 1 goto :fail_sa
:sa_ok

REM Fetch the storage key so container ops do not require data-plane RBAC.
set "STORAGE_KEY="
for /f "usebackq delims=" %%K in (`az storage account keys list --resource-group %RG_NAME% --account-name %STORAGE_ACCOUNT% --query "[0].value" -o tsv`) do set "STORAGE_KEY=%%K"
if not defined STORAGE_KEY goto :fail_key

call az storage container show --name %CONTAINER_NAME% --account-name %STORAGE_ACCOUNT% --account-key "!STORAGE_KEY!" >nul 2>&1
if not errorlevel 1 goto :container_ok
echo   Creating container %CONTAINER_NAME%...
call az storage container create ^
    --name %CONTAINER_NAME% ^
    --account-name %STORAGE_ACCOUNT% ^
    --account-key "!STORAGE_KEY!" >nul
if errorlevel 1 goto :fail_container
:container_ok
echo   OK.
echo.

REM ========================================================
REM  5. Firebase login (only if needed)
REM ========================================================
echo [5/7] Checking Firebase auth...
call firebase projects:list >nul 2>&1
if not errorlevel 1 goto :fb_auth_ok

echo   Not signed in. Starting login...
call firebase login
if errorlevel 1 goto :fail_fb_login
:fb_auth_ok
echo   OK.
echo.

REM ========================================================
REM  6. terraform init
REM ========================================================
echo [6/7] terraform init...
call terraform init
if errorlevel 1 goto :fail_tf_init
echo.

REM ========================================================
REM  7. terraform apply
REM ========================================================
echo [7/7] terraform apply...
call terraform apply
if errorlevel 1 goto :fail_tf_apply

echo.
echo === Done ===
endlocal & exit /b 0


:fail_az_install
echo ERROR: Failed to install Azure CLI via winget.
goto :end_fail
:fail_fb_install
echo ERROR: Failed to install Firebase CLI via npm.
goto :end_fail
:fail_az_login
echo ERROR: Azure login failed.
goto :end_fail
:fail_rg
echo ERROR: Failed to create resource group %RG_NAME%.
goto :end_fail
:fail_sa
echo ERROR: Failed to create storage account %STORAGE_ACCOUNT%.
goto :end_fail
:fail_key
echo ERROR: Could not fetch storage account key.
goto :end_fail
:fail_container
echo ERROR: Failed to create container %CONTAINER_NAME%.
goto :end_fail
:fail_fb_login
echo ERROR: Firebase login failed.
goto :end_fail
:fail_tf_init
echo ERROR: terraform init failed.
goto :end_fail
:fail_tf_apply
echo ERROR: terraform apply failed.
goto :end_fail

:end_fail
endlocal & exit /b 1