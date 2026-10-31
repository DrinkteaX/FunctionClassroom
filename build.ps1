$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319"
$csc = Join-Path $framework "csc.exe"
if (!(Test-Path -LiteralPath $csc)) {
    throw "Windows C# compiler was not found at $csc"
}

$buildDir = Join-Path $root ".build"
$distDir = Join-Path $root "dist"
$releaseDir = Join-Path $root "release"
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
New-Item -ItemType Directory -Force -Path $distDir | Out-Null
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

$wpfReferences = @(
    "/reference:$(Join-Path $framework 'WPF\PresentationFramework.dll')",
    "/reference:$(Join-Path $framework 'WPF\PresentationCore.dll')",
    "/reference:$(Join-Path $framework 'WPF\WindowsBase.dll')",
    "/reference:$(Join-Path $framework 'System.Xaml.dll')"
)

$mathSources = @(
    (Join-Path $root "src\FunctionClassroom\ExpressionParser.cs"),
    (Join-Path $root "src\FunctionClassroom\FunctionExpression.cs"),
    (Join-Path $root "src\FunctionClassroom\PlotSampler.cs"),
    (Join-Path $root "src\FunctionClassroom\IntersectionFinder.cs"),
    (Join-Path $root "src\FunctionClassroom\ProjectStore.cs")
)

$testExe = Join-Path $buildDir "MathCoreTests.exe"
& $csc /nologo /codepage:65001 /warn:4 /warnaserror+ /target:exe "/out:$testExe" `
    (Join-Path $root "tests\MathCoreTests.cs") $mathSources
if ($LASTEXITCODE -ne 0) { throw "Math tests did not compile." }
& $testExe
if ($LASTEXITCODE -ne 0) { throw "Math tests failed." }

$touchTestExe = Join-Path $buildDir "TouchViewportTests.exe"
& $csc /nologo /codepage:65001 /warn:4 /warnaserror+ /target:exe "/out:$touchTestExe" `
    $wpfReferences (Join-Path $root "tests\TouchViewportTests.cs") `
    (Join-Path $root "src\FunctionClassroom\ExpressionParser.cs") `
    (Join-Path $root "src\FunctionClassroom\FunctionExpression.cs") `
    (Join-Path $root "src\FunctionClassroom\PlotSampler.cs") `
    (Join-Path $root "src\FunctionClassroom\PlotModel.cs")
if ($LASTEXITCODE -ne 0) { throw "Viewport tests did not compile." }
& $touchTestExe
if ($LASTEXITCODE -ne 0) { throw "Viewport tests failed." }

$renderTestExe = Join-Path $buildDir "RenderingTests.exe"
$renderSources = @(
    (Join-Path $root "tests\RenderingTests.cs"),
    (Join-Path $root "src\FunctionClassroom\ExpressionParser.cs"),
    (Join-Path $root "src\FunctionClassroom\FunctionExpression.cs"),
    (Join-Path $root "src\FunctionClassroom\PlotSampler.cs"),
    (Join-Path $root "src\FunctionClassroom\PlotModel.cs"),
    (Join-Path $root "src\FunctionClassroom\GraphView.cs"),
    (Join-Path $root "src\FunctionClassroom\MathDisplay.cs"),
    (Join-Path $root "src\FunctionClassroom\ImageExporter.cs")
)
& $csc /nologo /codepage:65001 /warn:4 /warnaserror+ /target:exe "/out:$renderTestExe" `
    $wpfReferences $renderSources
if ($LASTEXITCODE -ne 0) { throw "Rendering tests did not compile." }
& $renderTestExe
if ($LASTEXITCODE -ne 0) { throw "Rendering tests failed." }

$appSources = @(Get-ChildItem (Join-Path $root "src\FunctionClassroom\*.cs") |
    Where-Object { $_.Name -ne "Program.cs" } |
    ForEach-Object { $_.FullName })
$appSources += Join-Path $root "src\FunctionClassroom\Program.cs"
$appExe = Join-Path $buildDir "FunctionClassroom.exe"
& $csc /nologo /codepage:65001 /warn:4 /warnaserror+ /target:winexe /platform:x64 `
    /main:FunctionClassroom.Program `
    "/out:$appExe" `
    "/win32manifest:$(Join-Path $root 'app.manifest')" `
    $wpfReferences $appSources
if ($LASTEXITCODE -ne 0) { throw "Application compilation failed." }

Copy-Item -LiteralPath $appExe -Destination (Join-Path $distDir "FunctionClassroom.exe") -Force
Copy-Item -LiteralPath (Join-Path $root "app.config") `
    -Destination (Join-Path $distDir "FunctionClassroom.exe.config") -Force
Copy-Item -LiteralPath (Join-Path $root "README.md") -Destination $distDir -Force
Copy-Item -LiteralPath (Join-Path $root "INSTALL.md") -Destination $distDir -Force

$portableZip = Join-Path $releaseDir "FunctionClassroom_v0.1.0_portable.zip"
if (Test-Path -LiteralPath $portableZip) {
    Remove-Item -LiteralPath $portableZip -Force
}
Compress-Archive -Path (Join-Path $distDir "*") -DestinationPath $portableZip -CompressionLevel Optimal

$setupExe = Join-Path $buildDir "FunctionClassroomSetup.exe"
$setupReferences = $wpfReferences + @(
    "/reference:$(Join-Path $framework 'System.Windows.Forms.dll')",
    "/reference:$(Join-Path $framework 'System.Drawing.dll')"
)
$setupSources = @(
    (Join-Path $root "installer\Setup.cs"),
    (Join-Path $root "installer\AssemblyInfo.cs")
)
& $csc /nologo /codepage:65001 /warn:4 /warnaserror+ /target:winexe /platform:x64 `
    "/out:$setupExe" `
    "/win32manifest:$(Join-Path $root 'installer\installer.manifest')" `
    "/resource:$(Join-Path $distDir 'FunctionClassroom.exe'),FunctionClassroom.exe" `
    "/resource:$(Join-Path $distDir 'FunctionClassroom.exe.config'),FunctionClassroom.exe.config" `
    "/resource:$(Join-Path $distDir 'README.md'),README.md" `
    $setupReferences $setupSources
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed." }

$smokeDir = Join-Path $buildDir "installer-smoke"
if (Test-Path -LiteralPath $smokeDir) {
    [System.IO.Directory]::Delete($smokeDir, $true)
}
$smoke = Start-Process -FilePath $setupExe `
    -ArgumentList @("/verify", "/target:$smokeDir") `
    -WindowStyle Hidden -Wait -PassThru
if ($smoke.ExitCode -ne 0) { throw "Installer payload verification failed." }
if (!(Test-Path -LiteralPath (Join-Path $smokeDir "FunctionClassroom.exe"))) {
    throw "Installer did not extract the application."
}
[System.IO.Directory]::Delete($smokeDir, $true)

$releaseSetup = Join-Path $releaseDir "FunctionClassroom_Setup_v0.1.0.exe"
Copy-Item -LiteralPath $setupExe -Destination $releaseSetup -Force
Copy-Item -LiteralPath (Join-Path $root "README.md") -Destination $releaseDir -Force
Copy-Item -LiteralPath (Join-Path $root "INSTALL.md") -Destination $releaseDir -Force

Write-Host ""
Write-Host "Build complete." -ForegroundColor Green
Write-Host "Portable: $portableZip"
Write-Host "Installer: $releaseSetup"
