Option Explicit
Dim shell, files, folder, script, powershell, command, result
Set shell = CreateObject("WScript.Shell")
Set files = CreateObject("Scripting.FileSystemObject")
folder = files.GetParentFolderName(WScript.ScriptFullName)
script = files.BuildPath(folder, "Process-Requests.ps1")
If Not files.FileExists(script) Then WScript.Quit 2
powershell = shell.ExpandEnvironmentStrings("%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe")
shell.CurrentDirectory = folder
command = Chr(34) & powershell & Chr(34) & " -NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File " & Chr(34) & script & Chr(34)
' wscript has no console; style 0 starts PowerShell hidden, rather than hiding
' a console after PowerShell has already started. Wait to preserve task exit status.
result = shell.Run(command, 0, True)
WScript.Quit result
