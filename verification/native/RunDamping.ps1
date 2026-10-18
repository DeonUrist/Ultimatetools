param([string]$GameDir='E:\SteamLibrary\steamapps\common\Apocalypter')
$ErrorActionPreference='Stop'
$taskRepo=(Resolve-Path -LiteralPath "$PSScriptRoot\..\..").Path
$taskGame=(Resolve-Path -LiteralPath $GameDir).Path
$taskRuntime=Join-Path $taskRepo 'research\damping-runtime'
$taskReport=Join-Path $taskRuntime 'report.txt'
dotnet build "$taskRepo\UltimateTools.csproj" -c Release "-p:GameDir=$taskGame" --nologo
if($LASTEXITCODE -ne 0){throw 'Production build failed'}
dotnet build "$PSScriptRoot\NativeVerifier.csproj" -c Release "-p:GameDir=$taskGame" --nologo
if($LASTEXITCODE -ne 0){throw 'Verifier build failed'}
New-Item -ItemType Directory -Path "$taskRuntime\BepInEx\plugins", "$taskRuntime\BepInEx\config" -Force | Out-Null
foreach($file in @('Apocalypter.exe','UnityPlayer.dll','winhttp.dll','doorstop_config.ini','.doorstop_version')){
    Copy-Item -LiteralPath "$taskGame\$file" -Destination $taskRuntime -Force
}
Copy-Item -LiteralPath "$taskGame\BepInEx\core" -Destination "$taskRuntime\BepInEx" -Recurse -Force
foreach($dir in @('Apocalypter_Data','MonoBleedingEdge')){
    if(!(Test-Path -LiteralPath "$taskRuntime\$dir")){
        New-Item -ItemType Junction -Path "$taskRuntime\$dir" -Target "$taskGame\$dir" | Out-Null
    }
}
Copy-Item -LiteralPath "$taskRepo\bin\Release\UltimateTools.dll", "$PSScriptRoot\bin\Release\UltimateToolsNativeVerifier.dll" -Destination "$taskRuntime\BepInEx\plugins" -Force
@'
[Logging.Console]
Enabled = false
[Logging.Disk]
Enabled = true
LogLevels = All
'@ | Set-Content -LiteralPath "$taskRuntime\BepInEx\config\BepInEx.cfg"
if(Test-Path -LiteralPath $taskReport){Remove-Item -LiteralPath $taskReport}
$taskArgs=@('-batchmode','-nographics','-logFile',('"'+$taskRuntime+'\unity.log"'),('-ultimate-tools-report="'+$taskReport+'"'),'-ultimate-tools-damping')
$taskProcess=Start-Process -FilePath "$taskRuntime\Apocalypter.exe" -WorkingDirectory $taskRuntime -WindowStyle Hidden -ArgumentList $taskArgs -PassThru
try{
    if(!$taskProcess.WaitForExit(45000)){throw 'Damping verifier exceeded 45 seconds'}
    if(!(Test-Path -LiteralPath $taskReport)){throw "Verifier produced no report; inspect $taskRuntime\unity.log"}
    $taskResult=Get-Content -LiteralPath $taskReport
    $taskResult
    if(!($taskResult -match '^PASS:') -or ($taskResult -match '^ERROR:')){throw 'Damping regression checks failed'}
}finally{
    if(!$taskProcess.HasExited){Stop-Process -Id $taskProcess.Id}
}
