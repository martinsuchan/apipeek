
$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
if (!(Test-Path $msbuild)) {
    $msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
    if (!(Test-Path $msbuild)) {
        throw "Cannot find MSBuild for VS2017. Tried Community and Professional edition path.";
    }
}

set-alias msbuild $msbuild
$path = $PSScriptRoot
$config = "Release"
$sln  = "$path\Source\ApiPeek.sln"

if (!(Test-Path $sln)) {
    throw "Solution not found! $sln";
}

$appxOutFolder = "$path\Source\ApiPeek.App.UWP\AppPackages"
if (Test-Path $appxOutFolder) {
    Remove-Item $appxOutFolder -Force -Recurse
}

Write-Host "Restoring NuGet packages for $sln`n" -foregroundcolor Green
.\NuGet\nuget restore $sln

Write-Host "Building solution $sln, $config`n" -foregroundcolor Green
msbuild $sln /p:Configuration=$config /p:AppxBundlePlatforms="x86|x64|ARM" /p:AppxBundle=Always /p:UapAppxPackageBuildMode=StoreUpload /v:m /nologo /maxcpucount:4 

Exit $LASTEXITCODE