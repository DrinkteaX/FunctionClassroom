$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319"
$csc = Join-Path $framework "csc.exe"
if (!(Test-Path -LiteralPath $csc)) {
    throw "Windows C# compiler was not found at $csc"
}

$buildDir = Join-Path $root ".build"
$distDir = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
New-Item -ItemType Directory -Force -Path $distDir | Out-Null

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

Write-Host ""
Write-Host "Build complete: $distDir" -ForegroundColor Green
Write-Host "Run: $(Join-Path $distDir 'FunctionClassroom.exe')"
