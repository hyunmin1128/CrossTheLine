$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'MirrorRemote.ps1')
$workspace=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$root=Join-Path $workspace ('output\GitHubMirrorTests\'+[guid]::NewGuid().ToString('N'))
$gitExe=(Get-Command git -CommandType Application -ErrorAction Stop).Source
function Invoke-TestGit([string[]]$Arguments){$result=& $gitExe @Arguments 2>&1;if($LASTEXITCODE -ne 0){throw ($result -join "`n")};return $result}
$checks=0
function Check($ok,[string]$why){if(!$ok){throw $why};$script:checks++}
New-Item -ItemType Directory -Path $root -Force | Out-Null
try {
    $remote=Join-Path $root 'remote.git';$publisher=Join-Path $root 'publisher';$state=Join-Path $root 'state';$mirror=Join-Path $state 'git'
    New-Item -ItemType Directory -Path $state | Out-Null
    Invoke-TestGit @('init','--quiet','--bare','--initial-branch=main',$remote) | Out-Null
    Invoke-TestGit @('init','--quiet','--initial-branch=main',$publisher) | Out-Null
    Invoke-TestGit @('-C',$publisher,'config','user.name','Fixture') | Out-Null
    Invoke-TestGit @('-C',$publisher,'config','user.email','fixture@example.invalid') | Out-Null
    Invoke-TestGit @('-C',$publisher,'remote','add','origin',$remote) | Out-Null
    $hooks=Join-Path $root 'hooks';New-Item -ItemType Directory -Path $hooks | Out-Null
    function Commit-Snapshot([string]$Repo,[int]$Id,[string]$Extra=''){
        [IO.File]::WriteAllText((Join-Path $Repo 'README.md'),"UVCS changeset: $Id`n$Extra")
        Invoke-TestGit @('-C',$Repo,'add','README.md') | Out-Null
        Invoke-TestGit @('-C',$Repo,'-c','commit.gpgsign=false','-c',"core.hooksPath=$hooks",'commit','--quiet','-m',"UVCS cs:$Id") | Out-Null
    }
    Commit-Snapshot $publisher 101
    Invoke-TestGit @('-C',$publisher,'push','--quiet','origin','main') | Out-Null
    $last=Connect-MirrorRemote $mirror $state $remote 96 97
    Check ($last -eq 101) 'New PC did not recover remote checkpoint.'
    Check (((Invoke-TestGit @('-C',$mirror,'rev-parse','HEAD')) -join '') -eq ((Invoke-TestGit @('-C',$publisher,'rev-parse','HEAD')) -join '')) 'Existing history was not preserved.'
    Commit-Snapshot $publisher 102
    Invoke-TestGit @('-C',$publisher,'push','--quiet','origin','main') | Out-Null
    Check ((Connect-MirrorRemote $mirror $state $remote 101 97) -eq 102) 'Existing PC did not fast-forward.'
    Commit-Snapshot $mirror 103 'prepared'
    $prepared=(Invoke-TestGit @('-C',$mirror,'rev-parse','HEAD')) -join ''
    Check ((Connect-MirrorRemote $mirror $state $remote 102 97) -eq 102) 'Prepared commit changed published checkpoint.'
    Check (((Invoke-TestGit @('-C',$mirror,'rev-parse','HEAD')) -join '') -eq $prepared) 'Prepared history was overwritten.'
    Commit-Snapshot $publisher 103 'other publisher'
    Invoke-TestGit @('-C',$publisher,'push','--quiet','origin','main') | Out-Null
    $blocked=$false;try{Connect-MirrorRemote $mirror $state $remote 102 97 | Out-Null}catch{$blocked=$true}
    Check $blocked 'Diverged histories were not blocked.'
    Check (((Invoke-TestGit @('-C',$mirror,'rev-parse','HEAD')) -join '') -eq $prepared) 'Divergence modified local history.'
    [IO.File]::WriteAllText((Join-Path $mirror 'unexpected.txt'),'uncommitted')
    $blocked=$false;try{Connect-MirrorRemote $mirror $state $remote 102 97 | Out-Null}catch{$blocked=$true}
    Check $blocked 'Dirty mirror was not blocked.'
    Write-Output "PASS $checks remote reconciliation checks; local-only bare fixture; no GitHub upload."
}finally{
    $target=[IO.Path]::GetFullPath($root);$allowed=[IO.Path]::GetFullPath((Join-Path $workspace 'output\GitHubMirrorTests'))+[IO.Path]::DirectorySeparatorChar
    if(!$target.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe fixture cleanup path'}
    Remove-Item -LiteralPath $target -Recurse -Force
}
