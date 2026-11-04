$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'AuthorIdentity.ps1')
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$testRoot = Join-Path $workspace ('output\GitHubMirrorTests\' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$checks = 0
$gitExecutable = (Get-Command git -CommandType Application -ErrorAction Stop).Source
function Check($Condition, [string]$Message) {
    if (!$Condition) { throw $Message }
    $script:checks++
}
function Git([string[]]$Arguments) {
    $result = & $gitExecutable @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Git test failed: $($result -join [Environment]::NewLine)" }
    return $result
}
try {
    $mappingPath = Join-Path $testRoot 'authors.json'
    @{
        version=1
        authors=@(
            @{uvcsOwner='one@example.invalid'; name='First Author'; email='first@example.invalid'},
            @{uvcsOwner='two@example.invalid'; name='Second Author'; email='second@example.invalid'}
        )
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $mappingPath -Encoding UTF8
    $authors = Read-MirrorAuthors -Path $mappingPath
    $first = Resolve-MirrorAuthor -Change ([pscustomobject]@{CHANGESETID=102;OWNER='one@example.invalid';DATE='2026-11-03T14:00:00+09:00'}) -Authors $authors
    $second = Resolve-MirrorAuthor -Change ([pscustomobject]@{CHANGESETID=103;OWNER='TWO@EXAMPLE.INVALID';DATE='2026-11-04T14:00:00+09:00'}) -Authors $authors
    Check ($first.Name -eq 'First Author') 'First author not resolved.'
    Check ($second.Email -eq 'second@example.invalid') 'Case-insensitive Unity identity lookup failed.'
    $blocked = $false
    try { Resolve-MirrorAuthor -Change ([pscustomobject]@{CHANGESETID=104;OWNER='unknown@example.invalid';DATE='2026-11-04T14:00:00+09:00'}) -Authors $authors | Out-Null } catch { $blocked = $true }
    Check $blocked 'Unknown author was not blocked.'
    $blocked = $false
    try { Resolve-MirrorAuthor -Change ([pscustomobject]@{CHANGESETID=104;OWNER='one@example.invalid';DATE='invalid'}) -Authors $authors | Out-Null } catch { $blocked = $true }
    Check $blocked 'Invalid checkin date was accepted.'
    $config = Get-Content -LiteralPath $mappingPath -Raw | ConvertFrom-Json
    $config.authors += $config.authors[0]
    $config | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $mappingPath -Encoding UTF8
    $blocked = $false
    try { Read-MirrorAuthors -Path $mappingPath | Out-Null } catch { $blocked = $true }
    Check $blocked 'Duplicate identity was accepted.'
    $config.authors = @($config.authors[0]); $config.authors[0].name = "Invalid`nAuthor"
    $config | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $mappingPath -Encoding UTF8
    $blocked = $false
    try { Read-MirrorAuthors -Path $mappingPath | Out-Null } catch { $blocked = $true }
    Check $blocked 'Multiline identity was accepted.'

    # Real local commits, with no remotes, no global config changes and no push.
    Git @('init','--quiet',$testRoot) | Out-Null
    Git @('-C',$testRoot,'config','user.name','Mirror Uploader') | Out-Null
    Git @('-C',$testRoot,'config','user.email','uploader@example.invalid') | Out-Null
    $emptyHooks = Join-Path $testRoot 'empty-hooks'; New-Item -ItemType Directory -Path $emptyHooks | Out-Null
    foreach ($identity in @($first,$second)) {
        [IO.File]::WriteAllText((Join-Path $testRoot 'asset.txt'),$identity.Name)
        Git @('-C',$testRoot,'add','asset.txt') | Out-Null
        Git @('-C',$testRoot,'-c','commit.gpgsign=false','-c',"core.hooksPath=$emptyHooks",'commit','--quiet','--author',"$($identity.Name) <$($identity.Email)>",'--date',$identity.Date,'-m','Attribution test') | Out-Null
        $actual = @(Git @('-C',$testRoot,'log','-1','--format=%an%n%ae%n%cn%n%cI%n%aI'))
        Check ($actual[0] -eq $identity.Name -and $actual[1] -eq $identity.Email) 'Commit author was not preserved.'
        Check ($actual[2] -eq 'Mirror Uploader') 'Author and uploader were not separated.'
        Check (([DateTimeOffset]$actual[4]).ToUnixTimeSeconds() -eq ([DateTimeOffset]$identity.Date).ToUnixTimeSeconds()) 'Original checkin date was not preserved.'
    }
    Check ((Git @('-C',$testRoot,'rev-list','--count','HEAD')) -eq '2') 'Two authors must produce two commits.'
    Write-Output "PASS $checks attribution checks; two real local Git commits; no network/push/global configuration changes."
} finally {
    $resolved = [IO.Path]::GetFullPath($testRoot)
    $allowed = [IO.Path]::GetFullPath((Join-Path $workspace 'output\GitHubMirrorTests')) + [IO.Path]::DirectorySeparatorChar
    if (!$resolved.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe test cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
