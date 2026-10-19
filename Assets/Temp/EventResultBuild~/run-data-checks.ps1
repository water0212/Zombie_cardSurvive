$ErrorActionPreference = 'Stop'
$dir = Join-Path $PSScriptRoot 'bin/Debug/netstandard2.1'
foreach ($name in @('nunit.framework.dll','UnityEngine.CoreModule.dll','UnityEngine.dll','EventResultBuild.dll')) {
    [Reflection.Assembly]::LoadFrom((Join-Path $dir $name)) | Out-Null
}
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $dir 'EventResultBuild.dll'))
$type = $assembly.GetType('ZombieCardSurvive.Tests.Editor.EventResultPresentationTests')
if ($null -eq $type) { throw ('Missing test type: ' + ($assembly.GetTypes().FullName -join ', ')) }
$test = [Activator]::CreateInstance([Type]$type)
foreach ($method in $type.GetMethods() | Where-Object { $_.DeclaringType -eq $type }) {
    $method.Invoke($test, @()) | Out-Null
    Write-Output ($method.Name + ': PASS')
}
