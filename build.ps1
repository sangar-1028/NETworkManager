$BuildPath = "$PSScriptRoot\Build"

Set-Location -Path $PSScriptRoot

if (Test-Path -Path $BuildPath) {
    Remove-Item -Path $BuildPath -Recurse -ErrorAction Stop
}

# Run a cleanup
& ".\cleanup.ps1"

# Set the version based on the current date (e.g. 2021.2.15.0)
$Date = Get-Date
$Patch = 0
$VersionString = "$($Date.Year).$($Date.Month).$($Date.Day).$Patch"

# Set assembly version
$PatternVersion = '\[assembly: AssemblyVersion\("(.*)"\)\]'
$PatternFileVersion = '\[assembly: AssemblyFileVersion\("(.*)"\)\]'

$AssemblyFile = "$PSScriptRoot\Source\GlobalAssemblyInfo.cs"

$AssemlbyContent = Get-Content -Path $AssemblyFile -Encoding utf8
$AssemlbyContent = $AssemlbyContent -replace $PatternVersion,"[assembly: AssemblyVersion(""$($VersionString)"")]"
$AssemlbyContent = $AssemlbyContent -replace $PatternFileVersion,"[assembly: AssemblyFileVersion(""$($VersionString)"")]"
$AssemlbyContent | Set-Content -Path $AssemblyFile -Encoding utf8

# Set inno setup version
$PatternSetupVersion = '#define MyAppVersion "(.*)"'

$InnoSetupFile = "$PSScriptRoot\InnoSetup.iss"

$SetupContent = Get-Content -Path $InnoSetupFile -Encoding utf8
$SetupContent = $SetupContent -replace $PatternSetupVersion,"#define MyAppVersion ""$($VersionString)"""
$SetupContent | Set-Content -Path $InnoSetupFile -Encoding utf8

### Warnings ###
# CS4014 - Call is not awaited
# NU1701 - Target framework is .NET Framework
# CS1591 - Missing XML comment

# Dotnet clean, restore, build and publish
##dotnet clean "$PSScriptRoot\Source\NETworkManager.sln"
##dotnet restore "$PSScriptRoot\Source\NETworkManager.sln"
# Issue: dotnet publish ignores zh-CN, zh-TW (https://github.com/dotnet/msbuild/issues/3897)
##dotnet publish --configuration Release --framework net6.0-windows10.0.17763.0 --runtime win10-x64 --self-contained false --output "$BuildPath\NETworkManager" "$PSScriptRoot\Source\NETworkManager\NETworkManager.csproj" 
##Read-Host "Copy the missing language files and press enter"

# Get msbuild path (docs: https://www.meziantou.net/locating-msbuild-on-a-machine.htm)
$VSwherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

if(-not(Test-Path -Path $VSwherePath -PathType Leaf))
{
    Write-Error "Could not find VSwhere. Is Visual Studio installed?" -ErrorAction Stop
}

$VSwhere = & $VSwherePath -version "[16.0,18.0)" -products * -requires Microsoft.Component.MSBuild -prerelease -latest -utf8 -format json | ConvertFrom-Json
$MSBuildPath = Join-Path $VSwhere[0].installationPath "MSBuild" "Current" "Bin" "MSBuild.exe"

# Test if we found msbuild
if(-not(Test-Path -Path $MSBuildPath -PathType Leaf))
{
    Write-Error "Could not find msbuild. Is Visual Studio installed?" -ErrorAction Stop
}

Start-Process -FilePath $MSBuildPath -ArgumentList "$PSScriptRoot\Source\NETworkManager.sln /restore /t:Clean,Build /p:Configuration=Release /p:TargetFramework=net6.0-windows10.0.17763.0 /p:RuntimeIdentifier=win-x64 /p:SelfContained=false /p:OutputPath=$BuildPath\NETworkManager" -Wait -NoNewWindow

# Test if release build is available
if(-not(Test-Path -Path "$BuildPath\NETworkManager\NETworkManager.exe" -PathType Leaf))
{
    Write-Error "Could not find release build. Is .NET SDK 6.0 or later installed?" -ErrorAction Stop
}

# Get NETworkManager file version
$Version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$BuildPath\NETworkManager\NETworkManager.exe").FileVersion

# Cleanup .pdb files
Get-ChildItem -Recurse | Where-Object {$_.Name.EndsWith(".pdb")} | Remove-Item

# Archiv Build
Compress-Archive -Path "$BuildPath\NETworkManager" -DestinationPath "$BuildPath\NETworkManager_$($Version)_Archive.zip"

# Portable Build
New-Item -Path "$BuildPath\NETworkManager" -Name "IsPortable.settings" -ItemType File
Compress-Archive -Path "$BuildPath\NETworkManager" -DestinationPath "$BuildPath\NETworkManager_$($Version)_Portable.zip"
Remove-Item -Path "$BuildPath\NETworkManager\IsPortable.settings"

# Installer Build
$InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6"

# Check if additional language files are available
$InnoSetupLanguageMissing = $false

foreach($File in @("ChineseSimplified.isl", "ChineseTraditional.isl", "Hungarian.isl", "Korean.isl")) {
    if(-not(Test-Path -Path "$InnoSetupPath\Languages\$File" -PathType Leaf))
    {
        Write-Host "$File not found in InnoSetup language folder." -ForegroundColor Yellow
        $InnoSetupLanguageMissing = $true
    }
}

if($InnoSetupLanguageMissing) {
    Write-Host "You can download the language files here: https://github.com/jrsoftware/issrc/blob/main/Files/Languages/" -ForegroundColor Yellow
}

# Check if InnoSetup is installed
$InnoSetupCompiler = "$InnoSetupPath\ISCC.exe"

if(-not(Test-Path -Path $InnoSetupCompiler -PathType Leaf) -or $InnoSetupLanguageMissing)
{
    Write-Host "InnoSetup is not installed correctly. Skip installer build..." -ForegroundColor Cyan
}
else
{
    Start-Process -FilePath $InnoSetupCompiler -ArgumentList """$PSScriptRoot\InnoSetup.iss""" -NoNewWindow -Wait
}

# SHA256 file hash
foreach($hash in Get-ChildItem -Path $BuildPath | Where-Object {$_.Name.EndsWith(".zip") -or $_.Name.EndsWith(".exe")} | Get-FileHash)
{
    "$($hash.Algorithm) | $($hash.Hash) | $([System.IO.Path]::GetFileName($hash.Path))" | Out-File -FilePath "$BuildPath\NETworkManager_$($Version)_Hash.txt" -Encoding utf8 -Append
}

# Build finished
Write-Host "`n`nBuild finished! The following files have been created under ""$BuildPath""`n" -ForegroundColor Green

Get-Content "$BuildPath\NETworkManager_$($Version)_Hash.txt"
