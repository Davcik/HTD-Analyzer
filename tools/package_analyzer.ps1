<#
PowerShell helper to package a Python analyzer script into a single Windows executable using PyInstaller
Usage:
  .\package_analyzer.ps1 -ScriptPath "D:\path\to\HTD_analyzer.py" -Python "C:\Python39\python.exe" -OutputDir "..\HTD Analyzer\bin\Release"
Parameters:
  -ScriptPath  (required) Path to the Python script to package
  -Python      (optional) Path to python executable or command (default: python)
  -OutputDir   (optional) Destination directory to copy the produced exe (default: .\"HTD Analyzer\bin\Release")
#>
param(
    [Parameter(Mandatory = $true)] [string]$ScriptPath,
    [string]$Python = "python",
    [string]$OutputDir = ".\HTD Analyzer\bin\Release"
)

try {
    $fullScript = Resolve-Path -Path $ScriptPath -ErrorAction Stop
} catch {
    Write-Error "Script not found: $ScriptPath"
    exit 1
}
$fullScript = $fullScript.Path
$scriptDir = Split-Path -Path $fullScript -Parent
$scriptFile = Split-Path -Path $fullScript -Leaf

Write-Host "Packaging $fullScript using Python command: $Python"

# Ensure PyInstaller is installed for the selected Python
$pyinstallerCheck = & $Python -m PyInstaller --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "PyInstaller not found for $Python. Installing via pip (user)..."
    & $Python -m pip install --user pyinstaller
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install PyInstaller for $Python. Install manually and retry."
        exit 1
    }
}

# Run PyInstaller in the script directory and put output in a local dist folder
Push-Location $scriptDir
Write-Host "Running PyInstaller... (this may take a minute)"
$distDir = Join-Path $scriptDir "dist"
# Remove old build artifacts if present to avoid stale results
if (Test-Path (Join-Path $scriptDir "build")) { Remove-Item -Recurse -Force (Join-Path $scriptDir "build") }
if (Test-Path $distDir) { Remove-Item -Recurse -Force $distDir }

& $Python -m PyInstaller --onefile --name document_analyzer --distpath "$distDir" "$scriptFile"
$rc = $LASTEXITCODE
Pop-Location

if ($rc -ne 0) {
    Write-Error "PyInstaller failed with exit code $rc"
    exit $rc
}

$builtExe = Join-Path $scriptDir "dist\document_analyzer.exe"
if (-not (Test-Path $builtExe)) {
    Write-Error "Built executable not found: $builtExe"
    exit 1
}

# Ensure destination exists
$resolvedOut = Resolve-Path -LiteralPath $OutputDir -ErrorAction SilentlyContinue
if (-not $resolvedOut) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    $resolvedOut = Resolve-Path -LiteralPath $OutputDir
}
$destPath = Join-Path $resolvedOut.Path "document_analyzer.exe"
Copy-Item -Path $builtExe -Destination $destPath -Force

Write-Host "Packaged executable copied to: $destPath"
Write-Host "Done."