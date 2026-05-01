# itch.io HTML5 publish

## Why `file://` fails
Blazor WebAssembly is not meant to be launched directly from a local file path. The browser blocks or mis-handles loading the `_framework` files and WebAssembly runtime from `file:///...`, so you can see the generic:

`An unhandled error has occurred. Reload`

Test the published build over HTTP instead.

## Build
```powershell
Set-Location "C:\Repos\GitHub\Emojimental-spring\Emojimental"
dotnet publish -c Release
```

## Local test
Serve the published `wwwroot` folder:

```powershell
Set-Location "C:\Repos\GitHub\Emojimental-spring\Emojimental\bin\Release\net10.0\publish\wwwroot"
py -3 -m http.server 8123
```

Then open:

- `http://127.0.0.1:8123/`

## Upload to itch.io
1. Go to `Emojimental\bin\Release\net10.0\publish\wwwroot`.
2. Zip the **contents** of that folder, not the folder itself.
3. Make sure `index.html` is at the root of the zip.
4. Upload the zip as an HTML5 game on itch.io.

The zip should contain entries like:

- `index.html`
- `_framework/...`
- `css/...`
- `js/...`
- `lib/...`

## Important
- Do **not** upload the outer `publish` folder.
- Do **not** test with `file:///.../index.html`.
- This project now uses a relative base path, which is safer for itch.io's nested HTML5 hosting path.

