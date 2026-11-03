$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$stateRoot=Join-Path (Split-Path $projectRoot -Parent) 'CrossTheLine-GitHubMirror.private'
New-Item -ItemType Directory -Path (Join-Path $stateRoot 'requests') -Force | Out-Null
$configPath=(& cm getconfig location | Out-String).Trim()
if($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $configPath)){throw 'Cannot locate UVCS client configuration.'}
$backup=Join-Path $stateRoot 'client-before-event-hooks.conf.private'
if(!(Test-Path -LiteralPath $backup)){Copy-Item -LiteralPath $configPath -Destination $backup}
$config=[xml]::new();$config.PreserveWhitespace=$true;$config.Load($configPath)
$vars=$config.DocumentElement.SelectSingleNode('TriggerVariables')
if(!$vars){$vars=$config.CreateElement('TriggerVariables');[void]$config.DocumentElement.AppendChild($vars)}
foreach($entry in @(@('CTL_GITHUB_WSCRIPT',"$env:SystemRoot\System32\wscript.exe"),@('CTL_GITHUB_HOOK',(Join-Path $PSScriptRoot 'After-Uvcs.vbs')))){
    $node=$vars.SelectSingleNode("TriggerVariable[@name='$($entry[0])']")
    if(!$node){$node=$config.CreateElement('TriggerVariable');$node.SetAttribute('name',$entry[0]);[void]$vars.AppendChild($node)}
    $node.SetAttribute('value',$entry[1])
}
$config.Save($configPath)
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Install-AutoSync.ps1')
if($LASTEXITCODE -ne 0){throw 'Cannot configure on-demand task.'}
$server='kilhyunmin1128mttrmqsz@unity'
foreach($event in @(@('after-clientcheckin','CTL-GitHub-Checkin','checkin'),@('after-update','CTL-GitHub-Update','update'))){
    $type=$event[0];$name=$event[1]
    # UVCS normalizes slash-prefixed arguments as paths; wscript needs only the
    # script path here. The hook handles errors itself and always returns zero.
    $command='"@CTL_GITHUB_WSCRIPT" "@CTL_GITHUB_HOOK" '+$event[2]+' "@WKSPACE_PATH"'
    $rows=@(& cm trigger list $type "--server=$server" '--format={1}|{2}')
    if($LASTEXITCODE -ne 0){throw "Cannot read $type triggers."}
    $existing=@($rows | Where-Object {$_ -like "$name|*"})
    if($existing.Count){
        if($existing[0].Replace('"','') -ne "$name|$command".Replace('"','')){throw "Unexpected existing trigger $name"}
    }else{
        & cm trigger create $type $name $command '--filter=rep:CrossTheLine/CrossTheLine' "--server=$server"
        if($LASTEXITCODE -ne 0){throw "Cannot create $type trigger."}
    }
}
Write-Output 'Event hooks installed; local variables opt in only this PC. No periodic triggers.'
