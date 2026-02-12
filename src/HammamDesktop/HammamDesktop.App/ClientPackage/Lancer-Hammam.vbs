Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")
strPath = fso.GetParentFolderName(Wscript.ScriptFullName) & "\HammamPOS.exe"
WshShell.Run chr(34) & strPath & chr(34), 0, False
Set WshShell = Nothing
