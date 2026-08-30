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

## Reparar conexión de Windows/Tailscale

Antes de probar el iPhone, en Windows ejecuta como administrador:

`REPARAR_TODO_REMOCONTROL_ADMIN.bat`

Ese archivo abre el puerto `5050`, revisa Tailscale, revisa si RemoControl PC está escuchando y muestra la dirección exacta para el iPhone.

Para usar la app desde otra red, en el iPhone escribe la dirección Tailscale de la PC. No copies la IP del iPhone.

Ejecuta `REPARAR_TODO_REMOCONTROL_ADMIN.bat` en la laptop y el mismo reparador mostrará la dirección exacta actual.

Antes de probar dentro de MSI Center, abre Safari en el iPhone y entra a:

`http://IP_TAILSCALE_DE_LA_LAPTOP:5050/status`

Si Safari muestra `401` o `Unauthorized`, Tailscale sí está llegando a la PC. En ese caso revisa el token de seguridad en la app o instala el IPA nuevo.

Si Safari no abre la página, RemoControl PC no está abierto, Windows todavía bloquea el puerto o Tailscale no está conectado.

## Identificador de la app

El proyecto conserva el Bundle ID:

`com.remocontrol.mobile`

No cambies ese identificador si ya lo estabas usando con tus workflows o instalaciones actuales.
