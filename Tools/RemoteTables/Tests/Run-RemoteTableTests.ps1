param([string]$UnityEditorData)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Push-Location $project
try {
    $rspFile = Get-ChildItem 'Library/Bee/artifacts' -Recurse -Filter 'Hotfix.rsp' | Where-Object { (Get-Content $_.FullName) -contains '-define:UNITY_EDITOR' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (!$rspFile) { throw 'Open this project in Unity first to generate its compiler response file.' }
    $original = Get-Content $rspFile.FullName
    if (!$UnityEditorData) {
        $reference = $original | Where-Object { $_ -match '^-[rR]:"(.+)/UnityReferenceAssemblies/' } | Select-Object -First 1
        if ($reference -match '^-[rR]:"(.+)/UnityReferenceAssemblies/') { $UnityEditorData = $Matches[1] }
        else { throw 'Pass -UnityEditorData pointing to Unity Editor/Data.' }
    }
    $output = Join-Path $project 'Temp/RemoteTableVerification'
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $csc = Join-Path $UnityEditorData 'DotNetSdkRoslyn/csc.dll'
    $dotnet = Join-Path $UnityEditorData 'NetCoreRuntime/dotnet.exe'
    $mono = Join-Path $UnityEditorData 'MonoBleedingEdge/bin/mono.exe'
    $testRsp = @($original | Where-Object { $_ -match '^-r:.*UnityReferenceAssemblies' })
    $json = Get-ChildItem 'Library/PackageCache/com.unity.nuget.newtonsoft-json*/Runtime/Newtonsoft.Json.dll' | Select-Object -First 1
    $testRsp += '-r:"' + $json.FullName + '"'
    $testRsp += '-target:exe','-out:"Temp/RemoteTableVerification/RemoteTableTests.exe"','-langversion:9.0','-define:UNITY_ANDROID','-nologo'
    foreach ($name in @('RemoteTableManager','RemoteTableSyncService','RemoteTableLocalStore','RemoteTableManifest','RemoteTableTextUtility','JsonTableConverter')) {
        $testRsp += '"Assets/ArrowGame/Scripts/RemoteTable/' + $name + '.cs"'
    }
    $testRsp += '"Tools/RemoteTables/Tests/RemoteTableTests.cs"'
    [IO.File]::WriteAllLines((Join-Path $output 'tests.rsp'), $testRsp)
    Copy-Item $json.FullName $output -Force
    & $dotnet $csc ('@' + (Join-Path $output 'tests.rsp'))
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $mono (Join-Path $output 'RemoteTableTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Remote table regression tests failed.' }
    $compileRsp = @($original | Where-Object { $_ -notmatch '^-(out|refout):' })
    $compileRsp += '-out:"Temp/RemoteTableVerification/Hotfix.dll"'
    [IO.File]::WriteAllLines((Join-Path $output 'compile.rsp'), $compileRsp)
    & $dotnet $csc ('@' + (Join-Path $output 'compile.rsp'))
    if ($LASTEXITCODE -ne 0) { throw 'Hotfix compilation failed.' }
}
finally { Pop-Location }
