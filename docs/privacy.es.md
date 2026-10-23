Privacidad de CamControl
Actualizado: 22 de octubre de 2026
Editor: TobioBobi

Datos locales
La dirección IP de la cámara, los nombres de los botones, las asignaciones de preajustes y las posiciones de inicio y salida se guardan en %LOCALAPPDATA%\CamControl (settings.json, buttons.json, automatic-positions.json). Con MSIX, Windows puede redirigir los archivos al área de datos privada de la aplicación. No hay cuentas de usuario, telemetría ni publicidad, y no se envían datos al editor.

Las preferencias de apariencia e idioma también se guardan localmente en esa carpeta (appearance.json, language.json).

Control de la cámara
Los comandos HTTP-CGI de movimiento, zoom y preajustes se envían mediante HTTP sin cifrar a la cámara configurada. CamControl no recibe ni guarda imágenes, vídeo o audio de la cámara. La cámara guarda las posiciones de los preajustes; su propio tratamiento de datos depende del dispositivo.

Demostración
No se envían comandos de red. Los nombres, las posiciones y las posiciones automáticas de la demostración se mantienen únicamente en memoria y se descartan al cerrar la aplicación. La configuración de tu cámara se conserva.

GitHub y soporte
El enlace de soporte solo abre GitHub al pulsarlo. Allí se aplica la política de privacidad de GitHub. Las incidencias públicas son visibles para todo el mundo; no incluyas datos confidenciales.
Proyecto y soporte: https://github.com/TobioBobi/CamControl
Privacidad de GitHub: https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement

Eliminar datos
Con la aplicación cerrada, elimina %LOCALAPPDATA%\CamControl. Puedes restablecer el área de datos privada de MSIX desde la configuración de aplicaciones de Windows; elimina por separado los archivos que ya existan en la carpeta indicada. Los preajustes guardados en la cámara se conservan.
