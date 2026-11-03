[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$scriptPath=Join-Path $PSScriptRoot 'Sync-GitHub.ps1'
$launcherPath=Join-Path $PSScriptRoot 'Run-Hidden.vbs'
if(!(Test-Path -LiteralPath $scriptPath) -or !(Test-Path -LiteralPath $launcherPath) -or !(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'Process-Requests.ps1'))){throw 'Missing sync script or hidden launcher.'}
$taskName='CrossTheLine-UnityToGitHub'
$account=[Security.Principal.WindowsIdentity]::GetCurrent().Name
$existing=Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if($existing -and !($existing.Actions.Arguments -like '*Tools\GitHubMirror\Sync-GitHub.ps1*' -or $existing.Actions.Arguments -like '*Tools\GitHubMirror\Run-Hidden.vbs*')) { throw 'A different task already uses this name.' }
$action=New-ScheduledTaskAction -Execute "$env:SystemRoot\System32\wscript.exe" -Argument "//B //NoLogo `"$launcherPath`"" -WorkingDirectory $PSScriptRoot
$settings=New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances Queue -ExecutionTimeLimit (New-TimeSpan -Hours 1) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
$principal=New-ScheduledTaskPrincipal -UserId $account -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $taskName -Action $action -Settings $settings -Principal $principal -Description 'On-demand only: invoked after successful UVCS checkin/update in the original workspace. No polling or login timer.' -Force | Select-Object TaskName,State
