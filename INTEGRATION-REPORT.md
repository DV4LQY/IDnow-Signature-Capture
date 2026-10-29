# IDNow / Wacom STU-430 integration record

## Delivered interface

IDNowWacomBridge.exe provides a direct replacement for IDNow's existing F6 capture shortcut while the bridge is running:

1. Select the target signature panel in IDNow.
2. Press F6.
3. The bridge suppresses IDNow's legacy Topaz capture action and opens the STU-430 signing dialog over IDNow.
4. Sign and tap OK on the STU-430.
5. IDNow is returned to the foreground. Right-click the selected panel and choose Paste.

The capture utility places a 370 x 120, 1-bit BMP with a minimal 0.25 mm stroke on the clipboard and writes a dated copy to Documents\IDNow Signatures. IDNow is not minimized or restored as part of this return flow.

## Files added

All new material is isolated below:

C:\Program Files (x86)\IDNow\_Wacom430_Work\

- IDNowWacomBridge\ — source, build script, launcher, and compiled F6 bridge.
- stu-sdk-samples\extra samples\csharp\IDNowWacomCapture\ — the x86 Wacom STU-430 capture utility and its Wacom SDK runtime files.

## Files deliberately not changed

No production IDNow executable, template, database, licence, registry entry, COM registration, or MIS-4LQY.ini value was changed by this work. The current INI file was already last written before the bridge source folder was created.

## Verification completed

- The bridge builds as an x86 .NET executable.
- It was started successfully, verified alive, and then closed.
- Direct Wacom SDK probing found one USB STU device: product 0x00A4, model STU-430, screen 320 x 200.
- The final live step requires a real pen signature: run the bridge, focus IDNow, press F6, sign, and paste the clipboard image into the panel.

## Limitation and rationale

IDNow's compiled VB6 form expects Topaz's SigPlus.ocx COM/ActiveX contract. Wacom's STU SDK is a different interface, so substituting it inside the compiled form would require an unsupported ActiveX impersonation layer or a binary patch. The F6 bridge supplies the direct user workflow without modifying the production application.

## Rollback

Exit the bridge from its notification-area menu. It removes its temporary keyboard hook when it exits and creates no startup item or registry change. To remove the integration completely, delete only:

The selected signature directory is stored per user at `%LOCALAPPDATA%\IDNowWacomBridge\signature-folder.txt`; choose `Use default signature folder` from the tray menu to remove that setting.

The selected signature appearance is stored per user at `%LOCALAPPDATA%\IDNowWacomBridge\signature-appearance.txt`; use the `Signature appearance...` tray command to choose a 0.10 to 2.50 mm stroke and color, or `Use default signature appearance` to remove the setting. The generated 370 x 120 BMP and clipboard image retain both settings; a legacy IDNow template may still normalize color when it imports the image.

C:\Program Files (x86)\IDNow\_Wacom430_Work\

This does not affect IDNow or its data.
