param([Parameter(Mandatory=$true)][string]$Compiler,[string]$OutputName='PuppetDemand644v6')
$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot)
$refs='mscorlib','System','System.Core','netstandard','Game','Newtonsoft.Json','Unity.Entities','Unity.Collections','Unity.Mathematics','UnityEngine','UnityEngine.CoreModule','Colossal.Core','Colossal.IO.AssetDatabase','Colossal.Collections'
$paths=@($refs | ForEach-Object {Join-Path $env:CSII_MANAGEDPATH ($_+'.dll')}) + (Join-Path $env:CSII_LOCALMODSPATH 'ChicagoBuildBridge/ChicagoBuildBridge.dll'),(Join-Path $root 'build/candidate/ChicagoPopulationHold20261007.dll')
Push-Location -LiteralPath $root
try {
 $arguments=@($paths | ForEach-Object {'/r:'+ [IO.Path]::GetRelativePath($root,$_ )})
 & $Compiler /nologo /nostdlib+ /target:library /langversion:latest /optimize+ ('/out:tests/bin/'+$OutputName+'.dll') @arguments 'tests/live/Demand644.cs'
 if($LASTEXITCODE){throw 'Fixture compilation failed'}
}finally{Pop-Location}
