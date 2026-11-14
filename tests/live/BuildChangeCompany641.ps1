param(
    [Parameter(Mandatory=$true)][string]$Compiler,
    [Parameter(Mandatory=$true)][string]$ChangeCompanyPath,
    [string]$BridgePath = (Join-Path $env:CSII_LOCALMODSPATH 'ChicagoBuildBridge/ChicagoBuildBridge.dll')
)
$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot)
$refs='mscorlib','System','System.Core','netstandard','Game','Newtonsoft.Json','Unity.Entities','Unity.Collections','Unity.Mathematics','UnityEngine','UnityEngine.CoreModule','Colossal.Core','Colossal.IO.AssetDatabase'
$paths=@($refs | ForEach-Object {Join-Path $env:CSII_MANAGEDPATH ($_+'.dll')}) + $BridgePath,$ChangeCompanyPath,(Join-Path $root 'build/candidate/ChicagoPopulationHold20261007.dll')
New-Item -ItemType Directory -Force -Path (Join-Path $root 'tests/bin') | Out-Null
Push-Location -LiteralPath $root
try {
    $arguments=@($paths | ForEach-Object {'/r:'+ [IO.Path]::GetRelativePath($root,$_ )})
    & $Compiler /nologo /nostdlib+ /target:library /langversion:latest /optimize+ /out:tests/bin/PuppetChangeCompany641.dll @arguments tests/live/ChangeCompany641.cs
    if($LASTEXITCODE){throw 'Change Company fixture compilation failed'}
    Write-Output 'Compiled QA fixture only. Nothing installed or executed in CS2.'
} finally {Pop-Location}
