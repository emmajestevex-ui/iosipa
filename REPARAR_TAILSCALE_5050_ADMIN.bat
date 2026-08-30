@echo off
setlocal

if exist "%~dp0REPARAR_TODO_REMOCONTROL_ADMIN.bat" (
    call "%~dp0REPARAR_TODO_REMOCONTROL_ADMIN.bat"
    exit /b
)

net session >nul 2>&1
if not "%errorlevel%"=="0" (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo Reparando acceso de MSI Center / RemoControl...
netsh advfirewall firewall delete rule name="MSI Center RemoControl Tailscale 5050" >nul 2>&1
netsh advfirewall firewall delete rule name="MSI Center RemoControl Local 5050" >nul 2>&1
netsh advfirewall firewall delete rule name="MSI Center RemoControl 5050 Entrada" >nul 2>&1

netsh advfirewall firewall add rule name="MSI Center RemoControl Tailscale 5050" dir=in action=allow protocol=TCP localport=5050 remoteip=100.64.0.0/10 profile=any
netsh advfirewall firewall add rule name="MSI Center RemoControl Local 5050" dir=in action=allow protocol=TCP localport=5050 profile=any
netsh advfirewall firewall add rule name="MSI Center RemoControl 5050 Entrada" dir=in action=allow protocol=TCP localport=5050 profile=any

for /f "usebackq delims=" %%I in (`tailscale ip -4 2^>nul`) do set TAILSCALE_IP=%%I

echo.
echo Listo. Ahora prueba en Safari del iPhone:
if defined TAILSCALE_IP (
    echo http://%TAILSCALE_IP%:5050/status
) else (
    echo http://IP_TAILSCALE_DE_LA_PC:5050/status
)
echo.
echo Si Safari muestra 401 o Unauthorized, Tailscale ya funciona.
echo Si Safari no abre la pagina, Tailscale o Windows aun bloquea la entrada.
echo.
pause
