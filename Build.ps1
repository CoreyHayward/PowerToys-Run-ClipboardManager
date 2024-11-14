$ErrorActionPreference = "Stop"

$projectDirectory = "$PSScriptRoot\Community.PowerToys.Run.Plugin.ClipboardManager"
[xml]$xml = Get-Content -Path "$projectDirectory\Community.PowerToys.Run.Plugin.ClipboardManager.csproj"
$version = $xml.Project.PropertyGroup.Version
$version = "$version".Trim()

$pasterReleaseDirectory = "$PSScriptRoot\Paster\bin\Release\net8.0-windows"

foreach ($platform in "ARM64", "x64")
{
    if (Test-Path -Path "$projectDirectory\bin")
    {
        Remove-Item -Path "$projectDirectory\bin\*" -Recurse
    }

    if (Test-Path -Path "$projectDirectory\obj")
    {
        Remove-Item -Path "$projectDirectory\obj\*" -Recurse
    }

    dotnet build $projectDirectory.sln -c Release /p:Platform=$platform

    $releaseDirectory = "$projectDirectory\bin\$platform\Release"
    Remove-Item -Path "$projectDirectory\bin\*" -Recurse -Include *.xml, *.pdb, PowerToys.*, Wox.*, WinRT*, Microsoft*
    New-Item -ItemType Directory -Force -Path $releaseDirectory\Paster
    Copy-Item -Path $pasterReleaseDirectory\* -Destination $releaseDirectory\Paster -Recurse
    Rename-Item -Path $releaseDirectory -NewName "ClipboardManager"

    Compress-Archive -Path "$projectDirectory\bin\$platform\ClipboardManager" -DestinationPath "$PSScriptRoot\ClipboardManager-$version-$platform.zip"
}
