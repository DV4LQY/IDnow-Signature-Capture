# IDNow Wacom STU-430 Direct F6 Bridge

This companion process connects IDNow's existing `F6` / Capture Signature
workflow to the Wacom STU-430 capture utility without altering `IDNow.exe`,
its menus, its INI file, its database, or any COM registrations.

## How it works

While the bridge is running, it observes only the `F6` key and only when the
foreground application is `IDNow.exe`. It suppresses that key before IDNow's
legacy Topaz `SigPlus.ocx` workflow can receive it, launches the STU-430
capture dialog centered over IDNow, and returns focus without changing
IDNow's minimized/restored state when capture ends.

The Wacom utility copies the normalized 370 x 120, 1-bit BMP to the clipboard.
After capture, right-click IDNow's selected signature panel and choose `Paste`.
The associated BMP is also stored in `Documents\IDNow Signatures`.

## Run

1. Start IDNow and select the destination signature panel.
2. Run `Run-IDNowWacomBridge.cmd`.
3. Wait for the `The STU-430 bridge is active` confirmation, then leave the
   tray icon running.
4. With IDNow active, press `F6`. The STU-430 signing dialog opens directly
   over IDNow. The bridge must run at the same Windows
   privilege level as IDNow (normally, neither should be started as
   Administrator).
5. Sign on the STU-430 and tap `OK` on the pad.
6. In IDNow, right-click the selected signature panel and choose `Paste`.

You can also right-click the tray icon and choose `Capture signature now`.

## Configure where signatures are saved

Right-click the bridge tray icon and choose `Set signature folder...`, then
select the destination directory. The choice is saved for the current Windows
user and every later F6 capture writes its BMP there. `Open signature folder`
opens the active location. Choose `Use default signature folder` to return to
`Documents\IDNow Signatures`.

## Design limitations

IDNow's in-form Topaz control is a compiled VB6 reference to `SigPlus.ocx`;
the Wacom STU SDK exposes a different COM contract. This bridge deliberately
does not overwrite or impersonate the Topaz control. It creates a direct F6
integration while keeping the production executable and registry untouched.

## Rollback

Exit the bridge from its tray menu or close the process. No permanent hook,
registry value, startup entry, or IDNow file is changed. Removing this
`IDNowWacomBridge` folder removes the bridge completely.

Only one bridge instance can run at once, so reopening the launcher cannot
produce duplicate captures.
