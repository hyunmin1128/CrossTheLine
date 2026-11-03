Option Explicit
On Error Resume Next
Dim shell, files, folder, project, workspace, kind, requests, request, note, taskCommand, branch, state, audit
Set shell = CreateObject("WScript.Shell")
Set files = CreateObject("Scripting.FileSystemObject")
If shell.Environment("PROCESS")("CTL_GITHUB_MIRROR_WORKER") = "1" Then WScript.Quit 0
If WScript.Arguments.Count <> 2 Then WScript.Quit 0
kind = WScript.Arguments(0)
If kind <> "checkin" And kind <> "update" Then WScript.Quit 0
folder = files.GetParentFolderName(WScript.ScriptFullName)
project = files.GetAbsolutePathName(files.BuildPath(folder, "..\.."))
state = files.BuildPath(files.GetParentFolderName(project), "CrossTheLine-GitHubMirror.private")
workspace = WScript.Arguments(1)
If kind = "update" And shell.Environment("PROCESS")("PLASTIC_UPDATE_PATH") <> "" Then workspace = shell.Environment("PROCESS")("PLASTIC_UPDATE_PATH")
workspace = files.GetAbsolutePathName(workspace)
Set audit = files.OpenTextFile(files.BuildPath(state, "hook.log"), 8, True)
audit.WriteLine CStr(Now) & " " & kind & " workspace=" & workspace
audit.Close
Err.Clear
If LCase(workspace) <> LCase(project) Then WScript.Quit 0
branch = shell.Environment("PROCESS")("PLASTIC_FULL_BRANCH_NAME")
If kind = "checkin" And branch <> "" And branch <> "/main" Then WScript.Quit 0
requests = files.BuildPath(files.GetParentFolderName(project), "CrossTheLine-GitHubMirror.private\requests")
If Not files.FolderExists(requests) Then WScript.Quit 0
request = files.BuildPath(requests, files.GetTempName())
Set note = files.CreateTextFile(request, False)
note.WriteLine CStr(Now) & " " & kind
note.Close
If Err.Number <> 0 Then WScript.Quit 0
taskCommand = Chr(34) & shell.ExpandEnvironmentStrings("%SystemRoot%\System32\schtasks.exe") & Chr(34) & " /Run /TN CrossTheLine-UnityToGitHub"
shell.Run taskCommand, 0, True
' An upload failure must never turn a successful checkin/update into an error.
WScript.Quit 0
