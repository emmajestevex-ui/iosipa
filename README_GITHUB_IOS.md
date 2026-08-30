# RemoControl Mobile - listo para GitHub / iOS

Este paquete está preparado para subir el **código fuente**, no el APK, a GitHub y compilar iOS con GitHub Actions usando un runner macOS.

## Qué subir a GitHub

Sube **todo el contenido de esta carpeta** a la raíz del repositorio, incluyendo:

- `RemoControlMobile.slnx`
- carpeta `RemoControlMobile/`
- carpeta oculta `.github/workflows/`
- `.gitignore`

No subas `bin`, `obj`, `.vs`, certificados `.p12/.pfx` ni perfiles `.mobileprovision`.

## Comprobación sin firma

En **Actions** aparecerá `Validate RemoControl iOS`. Puedes ejecutarlo manualmente con **Run workflow**. Compila para simulador y sirve para detectar errores del proyecto sin necesitar certificados.

## Para generar un IPA instalable

Apple exige firma para una aplicación que vaya a ejecutarse en un iPhone real. El workflow `Build RemoControl iOS` genera el IPA firmado.

En GitHub abre:

`Settings > Secrets and variables > Actions > New repository secret`

y crea estos cuatro secrets:

1. `IOS_CERTIFICATE_P12_BASE64`
   - Tu certificado Apple en formato `.p12`, convertido a Base64.
2. `IOS_CERTIFICATE_PASSWORD`
   - La contraseña del `.p12`.
3. `IOS_PROVISION_PROFILE_BASE64`
   - Tu perfil `.mobileprovision`, convertido a Base64.
4. `IOS_CODESIGN_KEY`
   - El nombre exacto de la identidad de firma, por ejemplo `Apple Development: Nombre Apellido (TEAMID)` o `Apple Distribution: Nombre (TEAMID)` según el perfil utilizado.

Después entra en:

`Actions > Build RemoControl iOS > Run workflow`

Cuando termine en verde, abre el run y descarga el artifact:

`RemoControlMobile-iOS-IPA`

Dentro estará `RemoControlMobile.ipa`.

## Cómo convertir archivos a Base64

### En macOS

```bash
base64 -i certificado.p12 | pbcopy
base64 -i perfil.mobileprovision | pbcopy
```

### En PowerShell (Windows)

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\ruta\certificado.p12")) | Set-Clipboard
[Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\ruta\perfil.mobileprovision")) | Set-Clipboard
```

## Identificador de la app

El proyecto usa:

`com.remocontrol.mobile`

El App ID del portal de Apple y el provisioning profile deben corresponder con ese Bundle ID. Si utilizas otro App ID, cambia `<ApplicationId>` en `RemoControlMobile/RemoControlMobile.csproj` antes de compilar.

## Importante

GitHub puede hacer la compilación en macOS, pero no elimina las reglas de Apple: un IPA para iPhone necesita una firma válida y un provisioning profile apropiado para el método de instalación (Development, Ad Hoc, TestFlight/App Store, etc.).
