
# Subscription_tracker


## .NET / MAUI

- .NET 10 SDK  
  - Tipo: LTS  
  - Lanzamiento: 11 nov 2025  
  - Fin de soporte: 14 nov 2028  
  - Target actual del proyecto: `net10.0-android`

## Android SDK

- Android SDK root:  
  - Ruta configurada: `/opt/android-sdk` (o la que definas en `ANDROID_HOME`).

- Plataformas instaladas relevantes:
  - `platforms;android-36`  
    - API level: 36  
    - Versión de Android: Android 16 (preview/reciente)  
    - Archivo clave usado por MAUI: `/platforms/android-36/android.jar`
- Herramientas:
  - `cmdline-tools` (para `sdkmanager`, `avdmanager`) 
  - `platform-tools` (para `adb`) 

## JDK

- JDK activo para las tools Android:
  - `java-17-openjdk` (requerido por cmdline-tools y MAUI/Android)
