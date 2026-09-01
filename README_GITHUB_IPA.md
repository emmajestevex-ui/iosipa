# RemoControl — crear IPA online con GitHub Actions

Este repositorio está preparado para compilar **RemoControl para iPhone/iPad** usando un runner macOS de GitHub Actions.

## Importante

El flujo **no usa tu certificado de Apple ni tu provisioning profile**. La compilación usa una firma ad-hoc temporal únicamente para que las herramientas de Apple generen el `.app`; después el workflow elimina firmas y perfiles antes de empaquetar `RemoControl-UNSIGNED.ipa`.

Ese IPA está pensado para **volver a firmarlo tú** con ESign u otra herramienta de firma compatible.

## Cómo usarlo

1. Crea un repositorio nuevo en GitHub.
2. **Descomprime este ZIP** en tu PC. No subas el ZIP como un único archivo.
3. Sube a la raíz del repositorio todo el contenido, incluyendo la carpeta oculta `.github`.
4. En GitHub entra a **Actions**.
5. Abre **Crear IPA para firmar con ESign**.
6. Pulsa **Run workflow**.
7. Espera a que termine la compilación.
8. Abre la ejecución terminada y descarga el artefacto **RemoControl-IPA-SIN-FIRMA**.
9. Dentro estará `RemoControl-UNSIGNED.ipa`.
10. Firma ese IPA con ESign usando tu certificado/perfil.

## Solo iOS

Esta carpeta no incluye la app de PC. Es solamente para subir el proyecto móvil a GitHub y generar el IPA de iPhone/iPad.

## Archivos principales

- `RemoControlMobile.sln` — solución.
- `RemoControlMobile/` — proyecto MAUI Android + iOS.
- `.github/workflows/build-ios-unsigned.yml` — compilación automática en macOS.
