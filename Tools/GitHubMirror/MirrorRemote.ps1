# Fast-forward-only reconciliation for a generated mirror, including a new PC.
function Connect-MirrorRemote {
    param([string]$GitRoot, [string]$StateRoot, [string]$RemoteUrl, [int]$LastPushed, [int]$Baseline)
    $gitExe = (Get-Command git -CommandType Application -ErrorAction Stop).Source
    function Invoke-MirrorGit([string[]]$Arguments) {
        $output = & $gitExe @Arguments 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Git mirror setup failed: $($output -join [Environment]::NewLine)" }
        return $output
    }
    $marker = Join-Path $StateRoot 'mirror.marker'
    if (!(Test-Path -LiteralPath (Join-Path $GitRoot '.git'))) {
        if ((Test-Path -LiteralPath $GitRoot) -and @(Get-ChildItem -LiteralPath $GitRoot -Force).Count) { throw 'Refusing to initialize a nonempty mirror folder.' }
        New-Item -ItemType Directory -Path $GitRoot -Force | Out-Null
        Invoke-MirrorGit @('init','--initial-branch=main',$GitRoot) | Out-Null
        Invoke-MirrorGit @('-C',$GitRoot,'remote','add','origin',$RemoteUrl) | Out-Null
        Invoke-MirrorGit @('-C',$GitRoot,'config','user.name','hyunmin1128') | Out-Null
        Invoke-MirrorGit @('-C',$GitRoot,'config','user.email','hyunmin1128@users.noreply.github.com') | Out-Null
        Invoke-MirrorGit @('-C',$GitRoot,'config','core.autocrlf','false') | Out-Null
        'CrossTheLine generated mirror only' | Set-Content -LiteralPath $marker
    }
    if (!(Test-Path -LiteralPath $marker)) { throw 'Missing mirror safety marker.' }
    $actualRemote = (Invoke-MirrorGit @('-C',$GitRoot,'remote','get-url','origin')) -join ''
    if ($actualRemote -ne $RemoteUrl) { throw 'Unexpected GitHub destination.' }
    if ((Invoke-MirrorGit @('-C',$GitRoot,'status','--porcelain')) -join '') { throw 'Generated mirror has uncommitted changes; inspect it before syncing.' }
    $remoteHead = @(Invoke-MirrorGit @('-C',$GitRoot,'ls-remote','origin','refs/heads/main'))
    if (!$remoteHead.Count) {
        if ($LastPushed -ge $Baseline) { throw 'GitHub main disappeared; refusing to recreate published history.' }
        return $LastPushed
    }
    Invoke-MirrorGit @('-C',$GitRoot,'fetch','--quiet','origin','main') | Out-Null
    $readme = (Invoke-MirrorGit @('-C',$GitRoot,'show','origin/main:README.md')) -join "`n"
    if ($readme -notmatch '(?m)^UVCS changeset: (\d+)\s*$') { throw 'GitHub main is not a recognized UVCS mirror snapshot.' }
    $remoteChangeset = [int]$Matches[1]
    $subject = (Invoke-MirrorGit @('-C',$GitRoot,'log','-1','--format=%s','origin/main')) -join ''
    if ($subject -notmatch ('^UVCS cs:' + $remoteChangeset + '(?: -|$)')) { throw 'GitHub commit and snapshot metadata disagree.' }
    if ($LastPushed -gt $remoteChangeset) { throw 'Local upload checkpoint is ahead of GitHub; refusing to rewrite history.' }
    & $gitExe -C $GitRoot rev-parse --verify HEAD *> $null
    if ($LASTEXITCODE -ne 0) {
        Invoke-MirrorGit @('-C',$GitRoot,'checkout','--quiet','-B','main','origin/main') | Out-Null
    } else {
        & $gitExe -C $GitRoot merge-base --is-ancestor HEAD origin/main *> $null
        $localIsAncestor = $LASTEXITCODE
        if ($localIsAncestor -eq 0) {
            Invoke-MirrorGit @('-C',$GitRoot,'merge','--quiet','--ff-only','origin/main') | Out-Null
        } else {
            if ($localIsAncestor -ne 1) { throw 'Cannot compare Git mirror histories.' }
            & $gitExe -C $GitRoot merge-base --is-ancestor origin/main HEAD *> $null
            if ($LASTEXITCODE -ne 0) { throw 'Local and GitHub histories diverged; no force-push will be attempted.' }
            $ahead = @(Invoke-MirrorGit @('-C',$GitRoot,'rev-list','origin/main..HEAD'))
            if ($ahead.Count -ne 1) { throw 'More than one unpublished mirror commit; manual inspection required.' }
            $localSubject = (Invoke-MirrorGit @('-C',$GitRoot,'log','-1','--format=%s')) -join ''
            if ($localSubject -notmatch '^UVCS cs:(\d+)(?: -|$)' -or [int]$Matches[1] -le $remoteChangeset) { throw 'Unpublished commit is not a later UVCS snapshot.' }
        }
    }
    return $remoteChangeset
}
