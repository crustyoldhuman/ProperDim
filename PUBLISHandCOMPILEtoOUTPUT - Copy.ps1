$projectPath = "ProperDim\ProperDim.csproj" 
$msixProject = "ProperDim.MSIXPackage\ProperDim.MSIXPackage.wapproj"
$fullOutputDir = "ProperDim\bin\Release\Full"
$liteOutputDir = "ProperDim\bin\Release\Lite"
$portableOutputDir = "ProperDim\bin\Release\Portable"

if (Test-Path $fullOutputDir) { Remove-Item -Path "$fullOutputDir\*" -Recurse -Force }
if (Test-Path $liteOutputDir) { Remove-Item -Path "$liteOutputDir\*" -Recurse -Force }
if (Test-Path $portableOutputDir) { Remove-Item -Path "$portableOutputDir\*" -Recurse -Force }
if (Test-Path "Output") { Remove-Item -Path "Output\*" -Recurse -Force } else { New-Item -ItemType Directory -Force -Path "Output" | Out-Null }

Write-Host "Publishing Full Build..."
dotnet publish $projectPath -c Release -o $fullOutputDir --self-contained true

Write-Host "Publishing Lite Build..."
dotnet publish $projectPath -c Release -o $liteOutputDir --self-contained false

Write-Host "Publishing Standalone Portable Build..."
dotnet publish $projectPath -c Release -r win-x64 -o $portableOutputDir --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

$sourceExe = "$portableOutputDir\ProperDim.exe"
$destExe = "Output\ProperDim_Portable.exe"

if (Test-Path $sourceExe) {
    Copy-Item -Path $sourceExe -Destination $destExe -Force
    Write-Host "Copied true standalone executable to Output\ProperDim_Portable.exe."
} else {
    Write-Host "Error: Could not find $sourceExe to copy."
}

Write-Host "Zipping PDB symbols for Microsoft Store submission..."
$symbolPath = "Output\ProperDim_Symbols.zip"
if (Test-Path $symbolPath) { Remove-Item $symbolPath -Force }
Get-ChildItem -Path $fullOutputDir -Filter "*.pdb" | Compress-Archive -DestinationPath $symbolPath

$innoCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$fullScript = "Inno_FullSetup.iss"
$liteScript = "inno_litesetup.iss"

if (Test-Path $innoCompiler) {
    Write-Host "Compiling Full Installer..."
    & $innoCompiler $fullScript

    Write-Host "Compiling Lite Installer..."
    & $innoCompiler $liteScript
} else {
    Write-Host "Error: Inno Setup compiler not found at $innoCompiler"
}

Write-Host ""
Write-Host "============================================="
Write-Host " PUBLISHING MICROSOFT STORE BUILD (.MSIX) "
Write-Host "============================================="
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

if (Test-Path $vswhere) {
    $msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    
    if ($msbuildPath) {
        Write-Host "Found MSBuild at: $msbuildPath"
        Write-Host "Compiling MSIX Store Upload Package..."
        
        & $msbuildPath $msixProject /p:Configuration=Release /p:Platform=x64 /p:UapAppxPackageBuildMode=StoreUpload /p:AppxBundle=Always /p:GenerateAppxPackageOnBuild=true /restore
        
        $appPackagesDir = "ProperDim.MSIXPackage\AppPackages"
        if (Test-Path $appPackagesDir) {
            Write-Host "Moving MSIX packages to Output folder..."
            Copy-Item -Path "$appPackagesDir\*" -Destination "Output" -Recurse -Force
        }
        
    } else {
        Write-Host "Error: MSBuild.exe could not be found. Please ensure the Visual Studio Build Tools are installed."
    }
} else {
    Write-Host "Error: vswhere.exe not found. Cannot automatically locate MSBuild to compile the MSIX package."
}

Write-Host ""
Write-Host "============================================="
Write-Host " ALL BUILDS COMPLETE "
Write-Host "============================================="
Read-Host -Prompt "Press Enter to exit"