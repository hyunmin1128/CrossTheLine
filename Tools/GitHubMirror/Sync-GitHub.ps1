[CmdletBinding()]
param([switch]$Push, [switch]$Status)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'AuthorIdentity.ps1')
. (Join-Path $PSScriptRoot 'MirrorRemote.ps1')
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$stateRoot = Join-Path (Split-Path $projectRoot -Parent) 'CrossTheLine-GitHubMirror.private'
$sourceRoot = Join-Path $stateRoot 'source'
$gitRoot = Join-Path $stateRoot 'git'
$repoSpec = 'CrossTheLine/CrossTheLine@kilhyunmin1128mttrmqsz@unity'
$remoteUrl = 'https://github.com/hyunmin1128/CrossTheLine.git'
$baseline = 97
$env:GIT_TERMINAL_PROMPT = '0'
$env:GCM_INTERACTIVE = 'Never'
New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null
$lock = $null
try {
    try { $lock = [IO.File]::Open((Join-Path $stateRoot 'sync.lock'), 'OpenOrCreate', 'ReadWrite', 'None') }
    catch [IO.IOException] { Write-Output 'Another sync is running.'; exit 0 }
    function Run([string]$Exe, [string[]]$Arguments) {
        $result = & $Exe @Arguments 2>&1
        if ($LASTEXITCODE -ne 0) { throw "$Exe failed ($LASTEXITCODE): $($result -join [Environment]::NewLine)" }
        return $result
    }
    $lastFile = Join-Path $stateRoot 'last-pushed.txt'
    $last = if (Test-Path -LiteralPath $lastFile) { [int](Get-Content -LiteralPath $lastFile -Raw) } else { $baseline - 1 }
    if (!$Status) {
        $last = Connect-MirrorRemote -GitRoot $gitRoot -StateRoot $stateRoot -RemoteUrl $remoteUrl -LastPushed $last -Baseline $baseline
        [IO.File]::WriteAllText($lastFile,[string]$last)
    }
    $query = "where branch = '/main' and changesetid > $last order by changesetid asc"
    [xml]$changes = (Run cm @('find','changeset',$query,'on',"repository '$repoSpec'",'--xml')) -join "`n"
    $pending = @($changes.PLASTICQUERY.CHANGESET | Where-Object { $_ })
    if ($Status) { [pscustomobject]@{ Repository=$remoteUrl; LastPushed=$last; Pending=@($pending | ForEach-Object {$_.CHANGESETID}); State=$stateRoot }; return }
    if (!$pending.Count) { Write-Output "Up to date at UVCS cs:$last"; return }
    # Resolve every author before copying any new snapshot. Never attribute
    # an unknown collaborator to the account that happens to upload the mirror.
    $authors = Read-MirrorAuthors -Path (Join-Path $PSScriptRoot 'authors.json')
    $identities = @{}
    foreach ($change in $pending) {
        $identities[[int]$change.CHANGESETID] = Resolve-MirrorAuthor -Change $change -Authors $authors
    }
    if (!(Test-Path -LiteralPath (Join-Path $sourceRoot '.plastic'))) {
        New-Item -ItemType Directory -Path $sourceRoot -Force | Out-Null
        # Only the committed Unity project and authoring tools are mirrored.
        @('/Docs','/output','/ProfilerCaptures','/Builds','/Library','/Temp','/Logs','/UserSettings') | Set-Content -LiteralPath (Join-Path $sourceRoot 'cloaked.conf') -Encoding UTF8
        Run cm @('workspace','create','CrossTheLine_GitHubMirror_1128',$sourceRoot,$repoSpec) | Out-Null
    }
    if (!(Test-Path -LiteralPath (Join-Path $stateRoot 'mirror.marker'))) { throw 'Missing mirror safety marker.' }
    $actualRemote = (Run git @('-C',$gitRoot,'remote','get-url','origin')) -join ''
    if ($actualRemote -ne $remoteUrl) { throw 'Unexpected GitHub destination.' }
    foreach ($change in $pending) {
        $id = [int]$change.CHANGESETID
        $identity = $identities[$id]
        Run cm @('update',$sourceRoot,"--changeset=cs:$id@$repoSpec",'--dontmerge','--noinput','--silent') | Out-Null
        foreach ($folder in @('Assets','Packages','ProjectSettings','AgentScripts','Tools')) {
            $from = Join-Path $sourceRoot $folder
            $to = [IO.Path]::GetFullPath((Join-Path $gitRoot $folder))
            if (!$to.StartsWith($gitRoot + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe mirror destination.' }
            if (!(Test-Path -LiteralPath $from)) { throw "Missing committed folder: $folder" }
            # /MIR deletes stale files only inside this generated, marker-checked copy.
            & robocopy $from $to /MIR /XJ /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
            if ($LASTEXITCODE -ge 8) { throw "Copy failed for $folder" }
        }
        $large = @(Get-ChildItem -LiteralPath $gitRoot -File -Recurse | Where-Object { !$_.FullName.StartsWith((Join-Path $gitRoot '.git') + '\') -and $_.Length -ge 100000000 })
        if ($large.Count) { throw ('GitHub size limit: ' + (($large | ForEach-Object {$_.FullName}) -join ', ')) }
        @('/Library/','/Temp/','/Obj/','/Build/','/Builds/','/Logs/','/UserSettings/','/.plastic/','*.csproj','*.sln','*.slnx','*.private','*.private.meta','.env','.env.*','*.pfx','*.p12','*.pem') | Set-Content -LiteralPath (Join-Path $gitRoot '.gitignore') -Encoding UTF8
        @('# CrossTheLine','', 'One-way snapshot mirror of committed Unity Version Control /main changes.', 'Work and check in through Unity Version Control. GitHub changes are not imported.', '', "UVCS changeset: $id", '', 'Includes Assets, Packages, ProjectSettings, AgentScripts and Tools.', 'Local caches, builds, QA captures and backup archives are excluded.') | Set-Content -LiteralPath (Join-Path $gitRoot 'README.md') -Encoding UTF8
        Run git @('-C',$gitRoot,'add','--all') | Out-Null
        $message = "UVCS cs:$id" + $(if($change.COMMENT){" - $($change.COMMENT)"}else{''})
        $messageFile = Join-Path $stateRoot 'commit-message.txt'
        [IO.File]::WriteAllText($messageFile,$message,[Text.UTF8Encoding]::new($false))
        $staged = (Run git @('-C',$gitRoot,'diff','--cached','--name-only')) -join ''
        if ($staged) {
            Run git @('-C',$gitRoot,'commit','--author',"$($identity.Name) <$($identity.Email)>",'--date',$identity.Date,'--file',$messageFile) | Out-Null
        } else {
            # A previous no-Push run may already have created this commit using
            # the old fixed author. Do not silently push it or rewrite history.
            $headAuthor = (Run git @('-C',$gitRoot,'log','-1','--format=%an%n%ae'))
            if (@($headAuthor).Count -ne 2 -or $headAuthor[0] -cne $identity.Name -or $headAuthor[1] -ine $identity.Email) {
                throw "Prepared cs:$id has a different Git author. Inspect the local mirror before uploading."
            }
        }
        if (!$Push) { Write-Output "Prepared UVCS cs:$id locally; no upload. Re-run with -Push."; break }
        Run git @('-C',$gitRoot,'push','--porcelain','origin','HEAD:refs/heads/main') | Out-Null
        [IO.File]::WriteAllText($lastFile,[string]$id)
        $entry = "$(Get-Date -Format o) uploaded UVCS cs:$id"
        Add-Content -LiteralPath (Join-Path $stateRoot 'sync.log') -Value $entry
        Write-Output $entry
    }
} catch {
    Add-Content -LiteralPath (Join-Path $stateRoot 'sync.log') -Value "$(Get-Date -Format o) ERROR $($_.Exception.Message)"
    throw
} finally { if ($lock) { $lock.Dispose() } }
