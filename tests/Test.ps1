param([string]$Compiler = $env:CSC_PATH)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
if (!$Compiler) { $Compiler = Join-Path $projectRoot '../../../tools/roslyn/tasks/net472/csc.exe' }
$bin = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force -Path $bin | Out-Null
$refs = 'mscorlib','netstandard','System','System.Core','Game','Unity.Entities','Unity.Collections','Unity.Mathematics','UnityEngine.CoreModule','Colossal.Collections','Colossal.Core','Colossal.IO.AssetDatabase','Colossal.Logging','Colossal.Localization'
$paths = @($refs | ForEach-Object { Join-Path $env:CSII_MANAGEDPATH ($_+'.dll') }) + (Join-Path $projectRoot 'build/candidate/ChicagoPopulationHold20261007.dll'),(Join-Path $projectRoot 'dependencies/0Harmony.dll')
Push-Location -LiteralPath $PSScriptRoot
try {
    $referenceArgs = @($paths | ForEach-Object { '/r:'+[IO.Path]::GetRelativePath($PSScriptRoot,$_) })
    foreach ($suite in 'Offline633','MaintenanceRegression','PolicyRegression') {
        $source = @($suite+'.cs')
        if ($suite -eq 'PolicyRegression') { $source += '../src/Policies.cs'; $suiteRefs = @($referenceArgs | Where-Object { $_ -notmatch 'ChicagoPopulationHold20261007.dll' }) } else { $suiteRefs = $referenceArgs }
        & $Compiler /nologo /nostdlib+ /unsafe /target:exe /langversion:latest ('/out:bin/'+$suite+'.exe') @suiteRefs @source
        if ($LASTEXITCODE) { throw "Compile failed: $suite" }
        '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}' | Set-Content -LiteralPath (Join-Path $bin ($suite+'.runtimeconfig.json'))
        $target = if ($suite -eq 'MaintenanceRegression') { '../build/candidate/ChicagoPopulationHold20261007.dll' } else { '../build/candidate' }
        & dotnet (Join-Path $bin ($suite+'.exe')) $target
        if ($LASTEXITCODE) { throw "Offline test failed: $suite" }
    }
} finally { Pop-Location }
