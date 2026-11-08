param(
    [string]$Managed = $env:CSII_MANAGEDPATH,
    [string]$Compiler = $env:CSC_PATH,
    [string]$HarmonyPath = (Join-Path $PSScriptRoot 'dependencies/0Harmony.dll')
)
$ErrorActionPreference = 'Stop'
if (!$Compiler) { $Compiler = Join-Path $PSScriptRoot '../../../tools/roslyn/tasks/net472/csc.exe' }
if (!(Test-Path -LiteralPath $Compiler)) { throw 'Pass -Compiler with a Roslyn csc.exe path or set CSC_PATH.' }
if ((Get-FileHash -LiteralPath $HarmonyPath).Hash -ne 'D04EC5B2590DD1BBB57F0F11DC417B1A5BEBBB36B1F97006D35F2D87C3A2A69F') { throw 'Expected the official Harmony 2.2.2 .NET Framework 4.7.2 binary.' }
$raw = Join-Path $PSScriptRoot 'build\raw'
$package = Join-Path $PSScriptRoot 'build\candidate'
New-Item -ItemType Directory -Force -Path $raw,$package | Out-Null
# Keep the original load identity and bridge entry point for existing Chicago GPT tooling.
$name = 'ChicagoPopulationHold20261007.dll'
$dll = Join-Path $raw $name
$refs = @('mscorlib','netstandard','System','System.Core','System.Memory','Game','UnityEngine','UnityEngine.CoreModule','Unity.Collections','Unity.Entities','Unity.Mathematics','Unity.Burst','Colossal.Core','Colossal.PSI.Common','Colossal.Collections','Colossal.Mathematics','Colossal.Logging','Colossal.UI.Binding','Colossal.UI','Colossal.Localization','Colossal.IO.AssetDatabase','Newtonsoft.Json') | ForEach-Object { Join-Path $Managed ($_ + '.dll') }
$refs = @($refs) + @($HarmonyPath)
foreach($ref in $refs) { if (!(Test-Path -LiteralPath $ref)) { throw "Missing reference: $ref" } }
$argsCsc = @('/nologo','/nostdlib+','/optimize+','/langversion:latest','/target:library','/deterministic+','/debug:portable',('/out:'+$dll)) + @($refs | ForEach-Object { '/reference:' + [IO.Path]::GetRelativePath($PSScriptRoot, $_) }) + @(Get-ChildItem (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | Sort-Object Name | ForEach-Object FullName)
Push-Location -LiteralPath $PSScriptRoot
try {
    & $Compiler @argsCsc
    if ($LASTEXITCODE) { throw 'Compilation failed' }
} finally { Pop-Location }
Copy-Item -LiteralPath $dll,(Join-Path $raw ([IO.Path]::ChangeExtension($name,'pdb'))) -Destination $package
$target = Join-Path $package $name
$post = @('PostProcess',$target,'-u',$env:CSII_UNITYMODPROJECTPATH) + @($refs | ForEach-Object {'-r'; $_}) + @('-p','Windows','-d','-v')
& $env:CSII_MODPOSTPROCESSORPATH @post *> (Join-Path $raw 'postprocess.log')
if ($LASTEXITCODE) { throw 'Official postprocessor failed. See build/raw/postprocess.log' }
Copy-Item -LiteralPath $HarmonyPath,(Join-Path $PSScriptRoot 'dependencies/Harmony-LICENSE.txt') -Destination $package -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'thumbnail.png') -Destination $package -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ui\GPTStuntYourGrowth.mjs'),(Join-Path $PSScriptRoot 'ui\GPTStuntGrowthSkyline.svg') -Destination $package -Force
[ordered]@{
    title = "GPT's Stunt My Growth by X_Bean_X"
    author = 'X_Bean_X'
    version = '1.75.629'
    runtimeTest = 'pending'
    gameAssemblySha256 = (Get-FileHash -LiteralPath (Join-Path $Managed 'Game.dll')).Hash
    referencedHarmony = [Reflection.AssemblyName]::GetAssemblyName($HarmonyPath).FullName
    files = @(Get-ChildItem -LiteralPath $package -File | ForEach-Object { [ordered]@{name=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash} })
} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $PSScriptRoot 'build\manifest.json')
Write-Output "Compiled and postprocessed offline candidate: $target"
