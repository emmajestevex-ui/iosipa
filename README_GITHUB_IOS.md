# MSI Center - listo para GitHub / iOS

Este paquete está preparado para reemplazar el contenido del repositorio de RemoControlMobile V5 y compilar iOS desde GitHub Actions.

## Qué subir a GitHub

Sube todo el contenido de esta carpeta a la raíz del repositorio:

- `RemoControlMobile.slnx`
- carpeta `RemoControlMobile/`
- carpeta oculta `.github/workflows/`
- `.gitignore`

No subas `bin`, `obj`, `.vs`, certificados `.p12/.pfx` ni perfiles `.mobileprovision`.

## Validar sin firma

En Actions aparecerá `Validate MSI Center iOS`.

Ejecuta:

`Actions > Validate MSI Center iOS > Run workflow`

Ese workflow compila para simulador y sirve para detectar errores del proyecto sin certificados.

## Generar IPA para ESign

En Actions aparecerá `Build MSI Center iOS ESign`.

Ejecuta:

`Actions > Build MSI Center iOS ESign > Run workflow`

Cuando termine en verde, descarga el artifact:

`MSICenter-iOS-ESign-IPA`

Dentro estará:

`MSICenter_iOS_ESign.ipa`

Ese IPA está empaquetado para que lo firmes después con ESign.

## Conexión con Tailscale

Para usar la app desde otra red, en el iPhone escribe la dirección Tailscale de la PC:

`http://100.84.101.10:5050`

No uses `100.93.173.59`: esa es la IP Tailscale del iPhone.

Antes de probar dentro de MSI Center, abre Safari en el iPhone y entra a:

`http://100.84.101.10:5050/status`

Si Safari muestra `401` o `Unauthorized`, Tailscale sí está llegando a la PC. En ese caso revisa el token de seguridad en la app o instala el IPA nuevo.

Si Safari no abre la página, ejecuta en Windows como administrador:

`REPARAR_TAILSCALE_5050_ADMIN.bat`

## Identificador de la app

El proyecto conserva el Bundle ID:

`com.remocontrol.mobile`

No cambies ese identificador si ya lo estabas usando con tus workflows o instalaciones actuales.
