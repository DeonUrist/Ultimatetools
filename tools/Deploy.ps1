param([string]$GameDir='E:\SteamLibrary\steamapps\common\Apocalypter', [switch]$RunNativeChecks)
$ErrorActionPreference='Stop'
$GameDir=(Resolve-Path -LiteralPath $GameDir).Path
$taskRepo=(Resolve-Path -LiteralPath "$PSScriptRoot\..").Path
if(Get-Process Apocalypter -ErrorAction SilentlyContinue){throw 'Apocalypter is running; close it before installing this DLL.'}
dotnet build "$taskRepo\UltimateTools.csproj" -c Release "-p:GameDir=$GameDir"
if($LASTEXITCODE-ne 0){throw 'Production build failed.'}
if($RunNativeChecks){
dotnet build "$taskRepo\verification\native\NativeVerifier.csproj" -c Release "-p:GameDir=$GameDir"
if($LASTEXITCODE-ne 0){throw 'Native verifier build failed.'}
}
$taskStamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$taskBackup=Join-Path $GameDir "BepInEx\cache\UltimateTools\backups\$taskStamp"
$taskInstalled=Join-Path $GameDir 'BepInEx\plugins\UltimateTools\UltimateTools.dll'
$taskOld=Join-Path $GameDir 'BepInEx\plugins\PartAdjustment\PartAdjustment.dll'
$taskVerifier=Join-Path $GameDir 'BepInEx\plugins\UltimateToolsNativeVerifier.dll'
$taskLog=Join-Path $GameDir 'BepInEx\LogOutput.log'
$taskReport=Join-Path $taskRepo 'research\native-regression.txt'
$taskUnityLog=Join-Path $taskRepo 'research\native-regression.unity.log'
if($RunNativeChecks-and(Test-Path -LiteralPath $taskVerifier)){throw "A verifier is already installed: $taskVerifier"}
New-Item -ItemType Directory -Path $taskBackup,(Split-Path $taskInstalled),(Split-Path $taskReport) -Force | Out-Null
$taskOldMoved=$false
$taskInstalledMoved=$false
$taskPassed=$false
$taskStarted=$null
try{
    if(Test-Path -LiteralPath $taskOld){Move-Item -LiteralPath $taskOld -Destination (Join-Path $taskBackup 'PartAdjustment.dll');$taskOldMoved=$true}
    if(Test-Path -LiteralPath $taskInstalled){Move-Item -LiteralPath $taskInstalled -Destination (Join-Path $taskBackup 'UltimateTools.dll');$taskInstalledMoved=$true}
    if(Test-Path -LiteralPath $taskLog){Copy-Item -LiteralPath $taskLog -Destination (Join-Path $taskBackup 'LogOutput-before.log')}
    Copy-Item -LiteralPath "$taskRepo\bin\Release\UltimateTools.dll" -Destination $taskInstalled
    if($RunNativeChecks){
    Copy-Item -LiteralPath "$taskRepo\verification\native\bin\Release\UltimateToolsNativeVerifier.dll" -Destination $taskVerifier
    if(Test-Path -LiteralPath $taskReport){Move-Item -LiteralPath $taskReport -Destination (Join-Path $taskBackup 'previous-regression.txt')}
    $taskArgs=@('-batchmode','-nographics','-logFile',('"'+$taskUnityLog+'"'),('-ultimate-tools-report="'+$taskReport+'"'))
    $taskStarted=Start-Process -FilePath (Join-Path $GameDir 'Apocalypter.exe') -WorkingDirectory $GameDir -WindowStyle Hidden -ArgumentList $taskArgs -PassThru
    $taskWatch=[Diagnostics.Stopwatch]::StartNew()
    while(-not $taskStarted.WaitForExit(10000)){
        Write-Output ('Native Unity validation running: '+[int]$taskWatch.Elapsed.TotalSeconds+' seconds')
        if($taskWatch.Elapsed.TotalSeconds-gt 60){throw 'Native verifier exceeded 60 seconds.'}
    }
    if(-not(Test-Path -LiteralPath $taskReport)){throw "Native verifier produced no report. Inspect $taskUnityLog"}
    $taskResult=Get-Content -LiteralPath $taskReport
    $taskResult
    if(-not($taskResult-match '^PASS:')-or($taskResult-match '^ERROR:')){throw 'Native regression checks failed; restoring previous DLL.'}
    }
    if((Get-FileHash -LiteralPath $taskInstalled).Hash-ne(Get-FileHash -LiteralPath "$taskRepo\bin\Release\UltimateTools.dll").Hash){throw 'Deployed DLL differs from build.'}
    Copy-Item -LiteralPath "$taskRepo\icon.png" -Destination (Join-Path (Split-Path $taskInstalled) 'icon.png')
    $taskPassed=$true
    Write-Output "DEPLOYED: $taskInstalled"
    Write-Output "BACKUP: $taskBackup"
}
finally{
    if($taskStarted-and-not $taskStarted.HasExited){Stop-Process -Id $taskStarted.Id}
    if($RunNativeChecks-and(Test-Path -LiteralPath $taskVerifier)){Remove-Item -LiteralPath $taskVerifier}
    if(Test-Path -LiteralPath $taskLog){Copy-Item -LiteralPath $taskLog -Destination "$taskRepo\research\native-regression.bepinex.log"}
    if(Test-Path -LiteralPath (Join-Path $taskBackup 'LogOutput-before.log')){Copy-Item -LiteralPath (Join-Path $taskBackup 'LogOutput-before.log') -Destination $taskLog}
    if(-not $taskPassed){
        if(Test-Path -LiteralPath $taskInstalled){Remove-Item -LiteralPath $taskInstalled}
        if($taskOldMoved){Move-Item -LiteralPath (Join-Path $taskBackup 'PartAdjustment.dll') -Destination $taskOld}
        if($taskInstalledMoved){Move-Item -LiteralPath (Join-Path $taskBackup 'UltimateTools.dll') -Destination $taskInstalled}
    }
}
