Privacy for CamControl
Updated: October 22, 2026
Publisher: TobioBobi

Local data
The camera IP address, button names, camera preset assignments and startup/exit positions are stored under %LOCALAPPDATA%\CamControl (settings.json, buttons.json, automatic-positions.json). With MSIX, Windows may redirect files to the app's private data area. There is no user account, telemetry or advertising, and no data is sent to the publisher.

Appearance and language preferences are also stored locally in that folder (appearance.json, language.json).

Camera control
HTTP-CGI commands for movement, zoom and presets are sent over unencrypted HTTP to the configured camera. CamControl does not receive or store camera images, video or audio. Preset positions are stored by the camera; its own data processing depends on the device.

Demo
No network commands. Demo names, positions and automatic positions are kept in memory only and discarded when the app exits. Your camera settings are preserved.

GitHub and support
The support link opens GitHub only when clicked. GitHub's privacy policy applies there. Public issues are visible to everyone; do not include confidential data.
Project & support: https://github.com/TobioBobi/CamControl
GitHub privacy: https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement

Deleting data
With the app closed, delete %LOCALAPPDATA%\CamControl. You can reset a private MSIX data area through Windows app settings; separately delete any existing files in the folder above. Presets stored on the camera are preserved.
