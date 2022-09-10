@echo off
setlocal
cd /d %~dp0
powershell.exe -file "DownloadDotnetSDK.ps1"
endlocal