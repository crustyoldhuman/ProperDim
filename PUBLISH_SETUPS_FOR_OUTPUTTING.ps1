$dotnet = "C:\Program Files\dotnet\dotnet.exe"
$project = "ProperDim\ProperDim.csproj"
$repoRoot = $PSScriptRoot

Write-Host "--- PUBLISHING LITE VERSION (Framework Dependent) ---" -ForegroundColor Cyan
& $dotnet publish $project -c Release -r win-x64 --self-contained false -o "$repoRoot\publish_lite"

Write-Host "`n--- PUBLISHING FULL VERSION (Self-Contained Single File) ---" -ForegroundColor Magenta
& $dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$repoRoot\publish_full"

Write-Host "`nDONE! Check your publish_lite and publish_full folders." -ForegroundColor Green
Pause