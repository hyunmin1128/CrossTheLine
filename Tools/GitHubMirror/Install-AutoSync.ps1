[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$scriptPath=Join-Path $PSScriptRoot 'Sync-GitHub.ps1'
$taskName='CrossTheLine-UnityToGitHub'
$account=[Security.Principal.WindowsIdentity]::GetCurrent().Name
$existing=Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if($existing -and !($existing.Actions.Arguments -like '*Tools\GitHubMirror\Sync-GitHub.ps1*')) { throw 'A different task already uses this name.' }
$action=New-ScheduledTaskAction -Execute "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -Argument "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$scriptPath`" -Push" -WorkingDirectory (Split-Path $scriptPath -Parent)
$timer=New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 1)
$login=New-ScheduledTaskTrigger -AtLogOn -User $account
$settings=New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 1) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
$principal=New-ScheduledTaskPrincipal -UserId $account -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger @($timer,$login) -Settings $settings -Principal $principal -Description 'One-way mirror of committed CrossTheLine UVCS /main changes to private GitHub. Does not check in or edit the working project.' -Force | Select-Object TaskName,State
