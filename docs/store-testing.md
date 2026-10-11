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

1. Click each position button or press 1–6. The blue frame on the stage and the
   cropped camera preview should change to that preset.
2. Hold a direction arrow or an arrow key. The frame should move. Release it;
   movement should stop. Zoom in first if more room to move is needed.
3. Hold Zoom In/Out or number-pad plus/minus. The frame and zoom value change;
   releasing the control stops the zoom.
4. During movement/zoom, switch to another window. Both channels should stop.
5. Start Standby (9). The view changes and a timed focus sequence is simulated.
   Buttons are blocked during this sequence. Open settings to cancel it.
6. Review the read-only camera configuration and the help page. Editing requires
   **Konfiguration bearbeiten**. Cancel a changed IP and verify it is not saved.
7. Open **Einstellungen > Buttons & Presets > Bearbeiten …** for one button.
   Save a different name with **Nur Namen speichern**. Hold direction/zoom
   buttons to choose a new frame, then save the position and confirm replacement.
   Move elsewhere and use **Gespeicherte Position anfahren** to verify recall.
   Close settings: the renamed main button recalls the new position too.
   Restart demo to restore defaults; real camera configuration is unchanged.
8. Close the demo. It stops simulated channels and exits without a standby delay.

To leave demo mode, open **Einstellungen > Demo-Modus > Kamera einrichten und
Demo beenden**, review/edit the IP, then save. This deliberately activates camera
control; physical hardware is required to verify the real commands.

## Current limitations

- Preset numbers 1–6 and 9 remain fixed. Names and positions for 1–6 are editable;
  saving to a physical camera uses `posset` and still requires hardware validation.
- “Demo stehend” is the name of preset 6, separate from the hardware-free mode.
- The model list remains to be confirmed. The app currently targets the HTTP
  `/cgi-bin/ptzctrl.cgi` command endpoint on port 80 with an IPv4 camera address.
- Real hardware tests are still required before submission. Include the confirmed
  model/protocol requirements and these demo instructions in certification notes.
