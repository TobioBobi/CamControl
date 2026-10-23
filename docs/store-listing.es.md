# Descripción de CamControl para la tienda

## Descripción breve

Controla cámaras PTZ mediante comandos HTTP-CGI compatibles, con botones grandes,
preajustes personalizados y un modo de demostración.

## Descripción

CamControl controla el giro, la inclinación, el zoom y los preajustes de tu cámara
PTZ. Los botones grandes y los atajos de teclado facilitan su uso. Elige tus propios
nombres de botón y números de preajuste, guarda posiciones de cámara y selecciona,
si lo deseas, una posición de inicio o salida.

La compatibilidad depende de los comandos admitidos, independientemente del
fabricante o modelo. La cámara debe admitir el conjunto de comandos HTTP-CGI en
`/cgi-bin/ptzctrl.cgi?ptzcmd&…` para movimiento, zoom, paradas y preajustes. Se
requieren una dirección IPv4 accesible y HTTP en el puerto 80 sin autenticación.

Prueba las funciones sin una cámara con el modo de demostración integrado. En la
demostración no se envían comandos de red. La vista previa es una simulación;
CamControl no muestra vídeo en directo.

Tus ajustes se guardan localmente. Sin cuentas de usuario, publicidad ni telemetría.

La interfaz está disponible en alemán, inglés y español. La aplicación utiliza el
idioma de Windows; puedes elegir otro idioma en los ajustes.

## Notas para la publicación

Este texto es un borrador para Partner Center y aún no se ha introducido allí.
Las pruebas con hardware deben confirmar el conjunto de comandos utilizado. Consulta
las instrucciones de verificación de la demostración en [store-testing.md](store-testing.md).
