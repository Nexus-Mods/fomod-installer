param([string]$type, [string]$Configuration = "Release")

$ErrorActionPreferenceOld = $ErrorActionPreference;
$ErrorActionPreference = "Stop";

function Copy-Item2([System.String] $Path, [System.String] $Destination) {
    $directory = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($Destination));
    New-Item -Type Directory -Path $directory -Force;
    Copy-Item -Path $Path -Destination $Destination -Recurse -Force;
}

try {
    # Clean
    if ($type -eq "build" -or $type -eq "test" -or $type -eq "clear") {
        Remove-Item *.tgz, *.h, *.dll, *.lib, build, dist, coverage, .nyc_output -Recurse -Force -ErrorAction Ignore;
    }
    # Build C#
    if ($type -eq "build" -or $type -eq "test" -or $type -eq "build-native") {
        Write-Host "Building ModInstaller.Native ($Configuration)";

        Invoke-Command -ScriptBlock {
            dotnet publish -r win-x64 --self-contained -c $Configuration ../ModInstaller.Native;
        }
        
        Copy-Item2 -Path "../ModInstaller.Native/bin/$Configuration/net9.0/win-x64/native/ModInstaller.Native.dll" -Destination $PWD | Out-Null;
        Copy-Item2 -Path "../ModInstaller.Native/bin/$Configuration/net9.0/win-x64/native/ModInstaller.Native.lib" -Destination $PWD | Out-Null;
        Copy-Item2 -Path "../ModInstaller.Native/bin/$Configuration/net9.0/win-x64/ModInstaller.Native.h" -Destination $PWD | Out-Null;
    }
    # Build NAPI
    if ($type -eq "build" -or $type -eq "test" -or $type -eq "build-napi") {
        Write-Host "Building NAPI ($Configuration)";

        $tag = "";
        $tag = If ($Configuration -eq "Release") { "--release" } Else { $tag }
        $tag = If ($Configuration -eq "Debug") { "--debug" } Else { $tag }
        Invoke-Command -ScriptBlock {
            npx node-gyp rebuild --arch=x64 $tag;
            #npx cmake-js compile --arch=x64 $tag;
        }
    }
    # Build JS
    if ($type -eq "build" -or $type -eq "test" -or $type -eq "test-build" -or $type -eq "build-ts") {
        Write-Host "Building @nexusmods/modinstaller";

        Invoke-Command -ScriptBlock {
            npx tsc -p tsconfig.json;
            npx tsc -p tsconfig.module.json;
        }
    }
    if ($type -eq "build" -or $type -eq "test" -or $type -eq "test-build" -or $type -eq "build-content") {
        Write-Host "Copying content";

        Copy-Item2 -Path "ModInstaller.Native.dll" -Destination "dist" | Out-Null;
        Copy-Item2 -Path "build/$Configuration/modinstaller.node" -Destination "dist" | Out-Null;
    }
}
finally {
    $ErrorActionPreference = $ErrorActionPreferenceOld;
}

