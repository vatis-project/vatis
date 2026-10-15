# macOS app icon

`AppIcon.icon` is the Icon Composer source of the macOS icon. `Assets.car` and `AppIcon.icns` are compiled from it
and are copied into the app bundle by `scripts/build.sh`:

- macOS 26 and later use `Assets.car` (the `CFBundleIconName` key in `Info.plist`).
- Older macOS versions use `AppIcon.icns` (`CFBundleIconFile`).

`actool` only exists on macOS, so the two compiled files are committed. After changing the icon, rebuild them on a Mac
with Xcode 26 or later:

```
mkdir out
xcrun actool AppIcon.icon --compile out --platform macosx --minimum-deployment-target 14.0 \
  --app-icon AppIcon --output-partial-info-plist out/partial.plist
```

then copy `out/Assets.car` and `out/AppIcon.icns` here. `out/partial.plist` holds the two `Info.plist` keys mentioned
above, which `build.sh` already writes.

The Windows and Linux icons are `vATIS.Desktop/Assets/MainIcon.ico` and `MainIcon.png`.
