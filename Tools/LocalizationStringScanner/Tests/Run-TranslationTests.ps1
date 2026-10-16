param([string]$UnityEditorData)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
Push-Location $project
try {
    $rspFile = Get-ChildItem 'Library/Bee/artifacts' -Recurse -Filter 'Assembly-CSharp-Editor.rsp' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (!$rspFile) { throw 'Open this project in Unity first to generate its compiler response file.' }
    $original = Get-Content $rspFile.FullName
    if (!$UnityEditorData) {
        $reference = $original | Where-Object { $_ -match '^-[rR]:"(.+)/UnityReferenceAssemblies/' } | Select-Object -First 1
        if ($reference -match '^-[rR]:"(.+)/UnityReferenceAssemblies/') { $UnityEditorData = $Matches[1] }
        else { throw 'Pass -UnityEditorData pointing to the Unity Editor/Data directory.' }
    }
    $output = Join-Path $project 'Temp/LocalizationVerification'
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $csc = Join-Path $UnityEditorData 'DotNetSdkRoslyn/csc.dll'
    $dotnet = Join-Path $UnityEditorData 'NetCoreRuntime/dotnet.exe'
    $mono = Join-Path $UnityEditorData 'MonoBleedingEdge/bin/mono.exe'
    $testRsp = @($original | Where-Object { $_ -match '^-r:.*(UnityReferenceAssemblies|EPPlus|Recyclable)' })
    $testRsp += '-target:exe','-out:"Temp/LocalizationVerification/TranslationWorkbookTests.exe"','-langversion:9.0','-nologo'
    $testRsp += '"Assets/Plugins/UnityGameFramework/GameFramework/Localization/Language.cs"'
    $testRsp += '"Assets/ArrowGame/ScriptsBuiltin/Editor/EditorTools/LocalizationScanner/TranslationWorkbook.cs"'
    $testRsp += '"Tools/LocalizationStringScanner/Tests/TranslationWorkbookTests.cs"'
    [IO.File]::WriteAllLines((Join-Path $output 'tests.rsp'), $testRsp)
    Copy-Item 'Assets/Plugins/ForEditor/EPPlus.6.2/*.dll' $output
    & $dotnet $csc ('@' + (Join-Path $output 'tests.rsp'))
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $mono (Join-Path $output 'TranslationWorkbookTests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Workbook regression tests failed.' }
    $editorRsp = @($original | Where-Object { $_ -notmatch '^-(out|refout):' -and $_ -notmatch 'TranslationWorkbook(Service)?\.cs' })
    $editorRsp += '-out:"Temp/LocalizationVerification/Assembly-CSharp-Editor.dll"'
    $editorRsp += '"Assets/ArrowGame/ScriptsBuiltin/Editor/EditorTools/LocalizationScanner/TranslationWorkbook.cs"'
    $editorRsp += '"Assets/ArrowGame/ScriptsBuiltin/Editor/EditorTools/LocalizationScanner/TranslationWorkbookService.cs"'
    [IO.File]::WriteAllLines((Join-Path $output 'compile.rsp'), $editorRsp)
    & $dotnet $csc ('@' + (Join-Path $output 'compile.rsp'))
    if ($LASTEXITCODE -ne 0) { throw 'Editor compilation failed.' }
}
finally { Pop-Location }