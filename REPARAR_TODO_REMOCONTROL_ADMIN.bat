@echo off
setlocal EnableExtensions
title Reparar MSI Center / RemoControl

set "PORT=5050"
set "REPORT=%USERPROFILE%\Desktop\MSI_CENTER_CONEXION.txt"

net session >nul 2>&1
if not "%errorlevel%"=="0" (
    echo Solicitando permisos de administrador...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo.
echo ============================================================
echo  MSI Center / RemoControl - reparacion completa
echo ============================================================
echo.

echo [1/6] Abriendo el puerto %PORT% en Windows Firewall...
netsh advfirewall firewall delete rule name="MSI Center RemoControl Tailscale 5050" >nul 2>&1
netsh advfirewall firewall delete rule name="MSI Center RemoControl Local 5050" >nul 2>&1
netsh advfirewall firewall delete rule name="MSI Center RemoControl 5050 Entrada" >nul 2>&1
netsh advfirewall firewall add rule name="MSI Center RemoControl Tailscale 5050" dir=in action=allow protocol=TCP localport=%PORT% remoteip=100.64.0.0/10 profile=any >nul
netsh advfirewall firewall add rule name="MSI Center RemoControl Local 5050" dir=in action=allow protocol=TCP localport=%PORT% profile=any >nul
netsh advfirewall firewall add rule name="MSI Center RemoControl 5050 Entrada" dir=in action=allow protocol=TCP localport=%PORT% profile=any >nul
netsh http show urlacl url=http://+:%PORT%/ >nul 2>&1
if not "%errorlevel%"=="0" (
    netsh http add urlacl url=http://+:%PORT%/ user=Everyone >nul 2>&1
    netsh http add urlacl url=http://+:%PORT%/ user=Todos >nul 2>&1
)

echo Poniendo redes activas como privadas cuando Windows lo permite...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-NetConnectionProfile | Where-Object {$_.NetworkCategory -ne 'Private'} | ForEach-Object { try { Set-NetConnectionProfile -InterfaceIndex $_.InterfaceIndex -NetworkCategory Private -ErrorAction Stop } catch {} }" >nul 2>&1

echo [2/6] Revisando Tailscale...
where tailscale.exe >nul 2>&1
if "%errorlevel%"=="0" (
    tailscale set --shields-up=false >nul 2>&1
    tailscale set --accept-dns=false >nul 2>&1
    tailscale set --unattended=true >nul 2>&1
    for /f "usebackq delims=" %%I in (`tailscale ip -4 2^>nul`) do (
        if not defined TAILSCALE_IP set "TAILSCALE_IP=%%I"
    )
) else (
    echo Tailscale no se encontro en PATH. Si esta instalado, abrelo manualmente.
)

echo [3/6] Activando audio/camara remotos en RemoControl cuando sea posible...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$p=Join-Path $env:APPDATA 'RemoControl\settings.json'; $d=Split-Path $p; New-Item -ItemType Directory -Force -Path $d | Out-Null; try { if(Test-Path $p){ $j=Get-Content -Raw -Path $p | ConvertFrom-Json } else { $j=[pscustomobject]@{} }; $j | Add-Member -NotePropertyName AudioRemoteEnabled -NotePropertyValue $true -Force; $j | Add-Member -NotePropertyName CameraRemoteEnabled -NotePropertyValue $true -Force; $j | ConvertTo-Json -Depth 10 | Set-Content -Path $p -Encoding UTF8 } catch { }" >nul 2>&1

echo Abriendo permisos de camara y microfono de Windows...
start "" "ms-settings:privacy-webcam" >nul 2>&1
start "" "ms-settings:privacy-microphone" >nul 2>&1

echo Reiniciando RemoControl PC si encuentra la version reparada...
set "REMO_EXE="
if exist "%USERPROFILE%\Downloads\RemoControl_PC_PROFESIONAL_V5\RemoControl\bin\Debug\RemoControl.exe" set "REMO_EXE=%USERPROFILE%\Downloads\RemoControl_PC_PROFESIONAL_V5\RemoControl\bin\Debug\RemoControl.exe"
if not defined REMO_EXE if exist "%USERPROFILE%\Downloads\RemoControl_PC_PROFESIONAL_V5\RemoControl\bin\Debug\app.publish\RemoControl.exe" set "REMO_EXE=%USERPROFILE%\Downloads\RemoControl_PC_PROFESIONAL_V5\RemoControl\bin\Debug\app.publish\RemoControl.exe"
if not defined REMO_EXE if exist "%USERPROFILE%\Downloads\RemoControlPC\RemoControl\RemoControl\bin\Debug\RemoControl.exe" set "REMO_EXE=%USERPROFILE%\Downloads\RemoControlPC\RemoControl\RemoControl\bin\Debug\RemoControl.exe"
if defined REMO_EXE (
    taskkill /IM RemoControl.exe /F >nul 2>&1
    start "" "%REMO_EXE%"
    timeout /t 4 /nobreak >nul
) else (
    echo No encontre RemoControl.exe reparado. Si tienes la app abierta, cierrala y abre la version nueva.
)

echo [4/6] Revisando si RemoControl escucha en el puerto %PORT%...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$l=Get-NetTCPConnection -LocalPort %PORT% -State Listen -ErrorAction SilentlyContinue; if($l){'OK: hay servidor escuchando en el puerto %PORT%.'}else{'ERROR: no hay ningun servidor escuchando en el puerto %PORT%. Abre RemoControl PC y dejalo abierto.'}"

powershell -NoProfile -ExecutionPolicy Bypass -Command "if(-not (Get-NetTCPConnection -LocalPort %PORT% -State Listen -ErrorAction SilentlyContinue)){ exit 1 }" >nul 2>&1
if not "%errorlevel%"=="0" (
    echo Intentando abrir RemoControl PC automaticamente...
    if exist "%USERPROFILE%\Downloads\RemoControl_PC_PROFESIONAL_V5\RemoControl\bin\Debug\app.publish\RemoControl.exe" start "" "%USERPROFILE%\Downloads\RemoControl_PC_PROFESIONAL_V5\RemoControl\bin\Debug\app.publish\RemoControl.exe"
    if exist "%USERPROFILE%\Downloads\RemoControl_PC_PROFESIONAL_V5\RemoControl\bin\Debug\RemoControl.exe" start "" "%USERPROFILE%\Downloads\RemoControl_PC_PROFESIONAL_V5\RemoControl\bin\Debug\RemoControl.exe"
    if exist "%USERPROFILE%\Downloads\RemoControlPC\RemoControl\RemoControl\bin\Debug\RemoControl.exe" start "" "%USERPROFILE%\Downloads\RemoControlPC\RemoControl\RemoControl\bin\Debug\RemoControl.exe"
    timeout /t 4 /nobreak >nul
    powershell -NoProfile -ExecutionPolicy Bypass -Command "$l=Get-NetTCPConnection -LocalPort %PORT% -State Listen -ErrorAction SilentlyContinue; if($l){'OK: RemoControl ya escucha en el puerto %PORT%.'}else{'AVISO: no pude iniciar RemoControl PC automaticamente. Abre la app de PC manualmente.'}"
)

echo [5/6] Probando desde esta misma laptop...
curl.exe -s -i -m 8 "http://127.0.0.1:%PORT%/status" 2>nul | findstr /i "HTTP ok autorizado unauthorized no autorizado error" 
if not "%errorlevel%"=="0" (
    echo No se pudo leer respuesta local. Si arriba dice que no hay servidor, abre RemoControl PC.
)

if defined TAILSCALE_IP (
    echo [6/6] Probando la IP Tailscale de esta laptop...
    curl.exe -s -i -m 8 "http://%TAILSCALE_IP%:%PORT%/status" 2>nul | findstr /i "HTTP ok autorizado unauthorized no autorizado error"
) else (
    echo [6/6] No se detecto IP de Tailscale en esta laptop.
)

echo.
echo ============================================================
echo  QUE PONER EN EL IPHONE
echo ============================================================
echo.
if defined TAILSCALE_IP (
    echo Servidor / IP en MSI Center:
    echo http://%TAILSCALE_IP%:%PORT%
    echo.
    echo Prueba de Safari:
    echo http://%TAILSCALE_IP%:%PORT%/status
) else (
    echo Abre Tailscale y copia la IP de THIS DEVICE / MSI.
    echo En MSI Center debe quedar asi:
    echo http://IP_TAILSCALE_DE_LA_LAPTOP:%PORT%
)
echo.
echo Si Safari muestra "No autorizado" o "Unauthorized", la red ya funciona.
echo Si la app falla pero Safari muestra eso, instala el IPA nuevo y revisa el token.
echo.

(
    echo MSI Center / RemoControl - datos de conexion
    echo Fecha: %date% %time%
    echo.
    if defined TAILSCALE_IP (
        echo Servidor / IP para MSI Center:
        echo http://%TAILSCALE_IP%:%PORT%
        echo.
        echo Prueba en Safari:
        echo http://%TAILSCALE_IP%:%PORT%/status
    ) else (
        echo No se detecto IP de Tailscale.
        echo Abre Tailscale y copia la IP de THIS DEVICE / MSI.
    )
    echo.
    echo Si Safari muestra No autorizado, la red funciona.
    echo En la app NO escribas /status.
    echo Copia el token completo desde RemoControl PC.
) > "%REPORT%"

echo Reporte guardado en:
echo %REPORT%
echo.
pause
