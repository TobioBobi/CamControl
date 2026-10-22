# Store description for CamControl

## Short description

Control PTZ cameras using compatible HTTP-CGI commands — with large buttons,
custom presets and a demo mode.

## Description

CamControl controls pan, tilt, zoom and presets on your PTZ camera. Large buttons
and keyboard shortcuts make it easy to use. Choose your own button names and
camera preset numbers, save camera positions and optionally select a startup or
exit position.

Compatibility depends on the supported commands, regardless of manufacturer or
model. The camera must support the HTTP-CGI command set at
`/cgi-bin/ptzctrl.cgi?ptzcmd&…` for movement, zoom, stops and presets. A reachable
IPv4 address and HTTP on port 80 without authentication are required.

Try the features without a camera using the built-in demo mode. No network
commands are sent in the demo. The preview is a simulation; CamControl does not
display live video.

Your settings stay local. No user account, advertising or telemetry.

The interface is available in German and English. The app follows the Windows
display language; you can choose a different language in settings.

## Submission notes

This is a draft for Partner Center and has not been entered there. Hardware
testing must confirm the command set used by the app. See the demo verification
instructions in [store-testing.md](store-testing.md).
