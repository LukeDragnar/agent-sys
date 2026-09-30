@echo off
REM ============================================================
REM Deploy Agent System (BE + FE) to Devonline
REM
REM Usage:
REM   deploy-all.bat            - deploy both BE + FE
REM   deploy-all.bat be         - deploy only backend
REM   deploy-all.bat fe         - deploy only frontend
REM   deploy-all.bat no-redeploy - skip Portainer webhook trigger
REM ============================================================

setlocal enabledelayedexpansion

set ROOT=%~dp0..
set MODE=%1
set NO_REDEPLOY=

if /i "%2"=="no-redeploy" set NO_REDEPLOY=1
if /i "%1"=="no-redeploy" (
    set NO_REDEPLOY=1
    set MODE=
)

set DO_BE=1
set DO_FE=1
if /i "%MODE%"=="be" set DO_FE=0
if /i "%MODE%"=="fe" set DO_BE=0

set PORTAINER_WEBHOOK=http://10.0.161.22:9000/api/webhooks/2d40a348-655a-4ffd-895b-0633c986a620

echo.
echo ============================================================
echo  Agent System Deploy
echo  BE: %DO_BE%   FE: %DO_FE%   Skip redeploy: %NO_REDEPLOY%
echo ============================================================
echo.

REM ─── Backend ─────────────────────────────────────────────────
if "%DO_BE%"=="1" (
    echo [BE] Building + pushing image...
    pushd "%ROOT%\backend\AgentService"
    call docker_build_and_push-Devonline.bat
    if errorlevel 1 (
        echo [BE] FAILED
        popd
        exit /b 1
    )
    popd
    echo [BE] OK
    echo.
)

REM ─── Frontend ────────────────────────────────────────────────
if "%DO_FE%"=="1" (
    echo [FE] Building + pushing image...
    pushd "%ROOT%\frontend"
    call npm run deploy-staging
    if errorlevel 1 (
        echo [FE] FAILED
        popd
        exit /b 1
    )
    popd
    echo [FE] OK
    echo.
)

REM ─── Trigger redeploy via Portainer webhook ──────────────────
if not defined NO_REDEPLOY (
    echo [Portainer] Triggering redeploy webhook...
    curl -s -X POST "%PORTAINER_WEBHOOK%"
    if errorlevel 1 (
        echo [Portainer] webhook FAILED ^(image vẫn đã push, cần redeploy thủ công^)
    ) else (
        echo.
        echo [Portainer] OK
    )
    echo.
)

echo ============================================================
echo  Deploy DONE
echo ============================================================
endlocal
