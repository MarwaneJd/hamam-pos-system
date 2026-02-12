Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
strFolder = fso.GetParentFolderName(Wscript.ScriptFullName)
WshShell.CurrentDirectory = strFolder
WshShell.Run "dotnet run", 0, False
Set WshShell = Nothing
