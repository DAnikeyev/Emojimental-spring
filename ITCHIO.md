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

## Recreate the upload ZIP from scratch
Create the archive from `publish\wwwroot` with normalized `/` entry paths:

```powershell
Set-Location "C:\Repos\GitHub\Emojimental-spring"

$src = "C:\Repos\GitHub\Emojimental-spring\Emojimental\bin\Release\net10.0\publish\wwwroot"
$zip = "C:\Repos\GitHub\Emojimental-spring\Emojimental\bin\Release\net10.0\Emojimental-itchio-release.zip"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (Test-Path $zip) { Remove-Item $zip -Force }

$archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem $src -Recurse -File | ForEach-Object {
        $relative = $_.FullName.Substring($src.Length).TrimStart('\\')
        $entryName = $relative.Replace('\\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive,
            $_.FullName,
            $entryName,
            [System.IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
    }
}
finally {
    if ($archive) { $archive.Dispose() }
}
```

## Verify ZIP internal paths
Check that nested files use `/` inside the archive:

```powershell
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = "C:\Repos\GitHub\Emojimental-spring\Emojimental\bin\Release\net10.0\Emojimental-itchio-release.zip"
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $archive.Entries |
        Where-Object {
            $_.FullName -in @(
                'index.html',
                'Emojimental.styles.css',
                'css/app.css',
                'js/field.js',
                '_framework/dotnet.skh7c8i5m6.js',
                '_framework/blazor.webassembly.66stpp682q.js'
            )
        } |
        Select-Object FullName,
                      Length,
                      @{Name='ContainsBackslash'; Expression = { $_.FullName.Contains([char]92) }},
                      @{Name='ContainsSlash'; Expression = { $_.FullName.Contains([char]47) }} |
        Format-Table -AutoSize
}
finally {
    $archive.Dispose()
}
```

Expected result:

- `index.html` and `Emojimental.styles.css` at the ZIP root
- nested paths like `css/app.css`, `js/field.js`, `_framework/dotnet.skh7c8i5m6.js`
- `ContainsBackslash = False` for all checked entries

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
2. Recreate `Emojimental-itchio-release.zip` with the command above.
3. Make sure `index.html` is at the root of the zip.
4. Verify nested entries use `/`, not `\`.
5. Upload the zip as an HTML5 game on itch.io.

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
- If itch.io returns `403` for files under `css`, `js`, `lib`, or `_framework`, inspect the uploaded ZIP's internal entry paths first.

