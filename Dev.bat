@echo off
cd /d %~dp0
call Tools\SetupEnv.bat
SETLOCAL
REM Microsoft was too weird in management SDKs that I have to put everything in the repo.
REM Resulting in enlarge the repo for no benefits or anything good at all. Simply evolving backward.
REM My recommendation is that you shouldn't use any Visual Studio products for .NET development.
REM Because its Dotnet SDK version management,isolation and selection were too beautiful for a mere mortal like me to understand.
SET "PATH=%~dp0Tools\sdk\6.0.100;%PATH%"
REM I need to targeting this SDK because there's no way to directly target the 6.0.0 runtime (so that all assembly files are compatible with all .NET6 versions).
start "" "PSO2-Launcher-CSharp.sln"
ENDLOCAL
exit