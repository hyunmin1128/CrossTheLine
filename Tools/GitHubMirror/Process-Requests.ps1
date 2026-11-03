$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$stateRoot=Join-Path (Split-Path $projectRoot -Parent) 'CrossTheLine-GitHubMirror.private'
$requests=Join-Path $stateRoot 'requests'
$env:CTL_GITHUB_MIRROR_WORKER='1'
while(Test-Path -LiteralPath $requests){
    $batch=@(Get-ChildItem -LiteralPath $requests -File)
    if(!$batch.Count){break}
    & (Join-Path $PSScriptRoot 'Sync-GitHub.ps1') -Push
    if(!$?){throw 'GitHub sync failed; requests retained for the next event.'}
    foreach($item in $batch){
        $resolved=[IO.Path]::GetFullPath($item.FullName)
        if(!$resolved.StartsWith($requests+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected request path.'}
        Remove-Item -LiteralPath $resolved
    }
    Add-Content -LiteralPath (Join-Path $stateRoot 'events.log') -Value "$(Get-Date -Format o) processed $($batch.Count) event request(s)"
}
