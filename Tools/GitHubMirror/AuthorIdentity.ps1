# Pure attribution helpers. Dot-sourcing this file never starts a sync or changes Git config.
function Read-MirrorAuthors {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Missing author mapping: $Path" }
    $config = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($config.version -ne 1) { throw 'Unsupported author mapping version.' }
    $result = @{}
    foreach ($author in @($config.authors)) {
        $owner = ([string]$author.uvcsOwner).Trim()
        $name = ([string]$author.name).Trim()
        $email = ([string]$author.email).Trim()
        if (!$owner -or !$name -or $owner -match '[\x00-\x1f\x7f]' -or $name -match '[<>\x00-\x1f\x7f]' -or $email -notmatch '^[^\s<>@]+@[^\s<>@]+$') {
            throw 'Each author requires a valid uvcsOwner, name and GitHub-linked commit email.'
        }
        if ($result.ContainsKey($owner)) { throw "Duplicate Unity author mapping: $owner" }
        $result[$owner] = [pscustomobject]@{ Name=$name; Email=$email }
    }
    if (!$result.Count) { throw 'Author mapping is empty.' }
    return $result
}

function Resolve-MirrorAuthor {
    param([Parameter(Mandatory=$true)]$Change, [Parameter(Mandatory=$true)][hashtable]$Authors)
    $owner = ([string]$Change.OWNER).Trim()
    if (!$owner -or !$Authors.ContainsKey($owner)) {
        throw "No GitHub author mapping for UVCS cs:$($Change.CHANGESETID), owner '$owner'. Add the confirmed identity to authors.json before uploading."
    }
    $date = [DateTimeOffset]::MinValue
    if (![DateTimeOffset]::TryParse([string]$Change.DATE, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$date)) {
        throw "Invalid Unity checkin date for cs:$($Change.CHANGESETID)."
    }
    return [pscustomobject]@{
        Name = $Authors[$owner].Name
        Email = $Authors[$owner].Email
        Date = $date.ToString('o', [Globalization.CultureInfo]::InvariantCulture)
    }
}
