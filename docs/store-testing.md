# Test CamControl without camera hardware

The Release app includes a local demo that requires no camera, server, account,
network connection, administrator rights or separate simulator installation.
It demonstrates the normal control interface with simulated camera responses.
It does not verify the protocol or compatibility of a physical camera.

## Entry

On first launch without a saved camera IP, choose **Ohne Kamera ausprobieren**.
With an existing configuration, open **Einstellungen > Demo-Modus > Demo starten**.
Alternatively launch `CamControl.exe --demo`. Existing camera settings are kept;
demo selection is not saved across restarts. The window clearly labels demo mode.

## Check functionality

1. Click each position button or press 1–6 or 9. The blue frame on the stage and the
   cropped camera preview should change to that preset.
2. Hold a direction arrow or an arrow key. The frame should move. Release it;
   movement should stop. Zoom in first if more room to move is needed.
3. Hold Zoom In/Out or number-pad plus/minus. The frame and zoom value change;
   releasing the control stops the zoom.
4. During movement/zoom, switch to another window. Both channels should stop.
5. Call preset 9. It behaves like all other presets: no timed focus sequence.
6. Review the camera configuration and the help page. The IP is editable;
   **Speichern** activates only for a changed, valid IP. Cancel a changed IP and verify it is not saved.
7. Open **Einstellungen > Buttons & Presets > Bearbeiten …** for one button.
   Save a different name with **Nur Namen speichern**. Hold direction/zoom
   buttons to choose a new frame, then save the position and confirm replacement.
   Move elsewhere and use **Position anfahren** to verify recall.
   Close settings: the renamed main button recalls the new position too.
   Restart demo to restore defaults; real camera configuration is unchanged.
8. Scroll down in **Buttons & Presets** to **Automatische Positionen**.
   Choose startup/exit positions (or **Keine Position**); save enables on change.
   Switch out of demo and back to verify the startup choice in the same session.
   Close demo: it stops all channels, calls the selected exit preset if any,
   and exits without a focus sequence. Demo selections do not change real settings.

To leave demo mode, open **Einstellungen > Demo-Modus > Zur Kamera wechseln**,
review/edit the IP, then save. This deliberately activates camera
control; physical hardware is required to verify the real commands.

## Current limitations

- Preset numbers 1–6 and 9 remain fixed. All seven names and positions are editable;
  saving to a physical camera uses `posset` and still requires hardware validation.
- Default button names are **Preset 1–6** and **Preset 9**. The app never creates
  camera presets automatically; demo positions are simulated only in memory.
- Compatibility is based on the supported HTTP-CGI command set, regardless of
  brand or model. The camera must accept `/cgi-bin/ptzctrl.cgi?ptzcmd&…` over
  HTTP on port 80 with an IPv4 address and no authentication. See the command
  list in the README; a different HTTP-CGI API is not sufficient.
- Real hardware tests are still required before submission. Include the confirmed
  command support and these demo instructions in certification notes.
