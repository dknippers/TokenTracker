[CmdletBinding()]
param(
    [string]$QtBin = "C:\Qt\6.11.1\msvc2022_64\bin"
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$buildDir = Join-Path $root "build\windows-release"
$stageDir = Join-Path $root "build\windows-publish"
$zipPath = Join-Path $root "OpenCodeCostMeter.zip"

function Import-VisualStudioEnvironment {
    $vsWhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    $vsInstall = $null

    if (Test-Path -LiteralPath $vsWhere) {
        $vsInstall = (& $vsWhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath).Trim()
    }

    if ([string]::IsNullOrWhiteSpace($vsInstall)) {
        $vsRoot = Join-Path $env:ProgramFiles "Microsoft Visual Studio"
        $vsDevCmd = Get-ChildItem $vsRoot -Filter "VsDevCmd.bat" -File -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1
    }
    else {
        $vsDevCmd = Get-Item (Join-Path $vsInstall "Common7\Tools\VsDevCmd.bat") -ErrorAction SilentlyContinue
    }

    if ($null -eq $vsDevCmd) {
        throw "Visual Studio with the C++ desktop workload could not be found."
    }

    $environment = & cmd.exe /d /c "call `"$($vsDevCmd.FullName)`" -arch=x64 >nul && set"
    foreach ($line in $environment) {
        if ($line -match "^(?<name>[^=]+)=(?<value>.*)$") {
            Set-Item "Env:$($Matches.name)" $Matches.value
        }
    }
}

if (-not (Get-Command cl.exe -ErrorAction SilentlyContinue)) {
    Import-VisualStudioEnvironment
}

$cmakeCommand = Get-Command cmake.exe -ErrorAction SilentlyContinue
if ($null -eq $cmakeCommand) {
    $cmakeCandidates = Get-ChildItem "$env:ProgramFiles\Microsoft Visual Studio" -Filter "cmake.exe" -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like "*CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe" } |
        Select-Object -First 1
    $cmake = if ($null -ne $cmakeCandidates) { $cmakeCandidates.FullName } else { $null }
}
else {
    $cmake = $cmakeCommand.Source
}

if ([string]::IsNullOrWhiteSpace($cmake)) {
    throw "CMake was not found. Install CMake with Visual Studio or add it to PATH."
}

$ctestCommand = Get-Command ctest.exe -ErrorAction SilentlyContinue
if ($null -ne $ctestCommand) {
    $ctest = $ctestCommand.Source
}
else {
    $ctest = Join-Path (Split-Path -Parent $cmake) "ctest.exe"
}

if (-not (Test-Path -LiteralPath $ctest)) {
    throw "ctest.exe was not found alongside CMake."
}

$windeployqt = Join-Path $QtBin "windeployqt.exe"
if (-not (Test-Path -LiteralPath $windeployqt) -and -not $PSBoundParameters.ContainsKey("QtBin")) {
    $qtCandidate = Get-ChildItem "C:\Qt" -Filter "windeployqt.exe" -File -Recurse -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if ($null -ne $qtCandidate) {
        $windeployqt = $qtCandidate.FullName
        $QtBin = $qtCandidate.DirectoryName
    }
}

if (-not (Test-Path -LiteralPath $windeployqt)) {
    throw "windeployqt.exe was not found at '$windeployqt'. Pass the Qt bin directory with -QtBin."
}

$qtPrefix = Split-Path -Parent $QtBin

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $false)][string[]]$Arguments = @()
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE`: $FilePath $($Arguments -join ' ')"
    }
}

Write-Host "Configuring Release build..."
Invoke-Native $cmake @(
    "-S", (Join-Path $root "qt"),
    "-B", $buildDir,
    "-G", "NMake Makefiles",
    "-DCMAKE_BUILD_TYPE=Release",
    "-DCMAKE_PREFIX_PATH=$qtPrefix"
)

Write-Host "Building and testing..."
Invoke-Native $cmake @("--build", $buildDir)
Invoke-Native "ctest.exe" @("--test-dir", $buildDir, "--output-on-failure")

$executable = Join-Path $buildDir "OpenCodeCostMeter.exe"
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Release executable was not found at '$executable'."
}

Write-Host "Staging self-contained Windows distribution..."
Remove-Item -LiteralPath $stageDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -Path $stageDir -ItemType Directory -Force | Out-Null

Invoke-Native $windeployqt @(
    "--release",
    "--compiler-runtime",
    "--no-translations",
    "--no-network",
    "--skip-plugin-types", "generic,networkinformation,tls",
    "--exclude-plugins", "qgif,qico,qjpeg,qsqlibase,qsqlmimer,qsqloci,qsqlodbc,qsqlpsql",
    "--dir", $stageDir,
    $executable
)

Copy-Item $executable $stageDir
Copy-Item (Join-Path $buildDir "model-display-names.txt") $stageDir
Copy-Item (Join-Path $root "LICENSE"), (Join-Path $root "THIRD-PARTY-NOTICES.md") $stageDir

$redistRoot = $env:VCToolsRedistDir
$runtimeSource = $null
if (-not [string]::IsNullOrWhiteSpace($redistRoot)) {
    $runtimeSource = Get-ChildItem (Join-Path $redistRoot "x64") -Directory -Filter "Microsoft.VC*.CRT" -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

if ($null -ne $runtimeSource) {
    Copy-Item (Join-Path $runtimeSource.FullName "*.dll") $stageDir
    Remove-Item (Join-Path $stageDir "vc_redist.x64.exe") -Force -ErrorAction SilentlyContinue
}
else {
    Write-Warning "Could not locate the MSVC runtime DLLs. The staged vc_redist.x64.exe installer was retained."
}

Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -Force

Write-Host "Created $zipPath"
