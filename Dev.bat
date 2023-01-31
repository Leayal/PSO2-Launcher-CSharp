@echo off
cd /d %~dp0

REM This file is now served as a fall-back whenever MS messed up the SDK again.
REM This batch script will force download the SDK v6.0.100.
call Tools\SetupEnv.bat

SETLOCAL
REM Use the specific SDK.
SET "PATH=%~dp0Tools\sdk\6.0.100;%PATH%"
REM I need to targeting this SDK because there's no way to directly target the specific 6.0.0 runtime (so that all assembly files are compatible with all .NET6 versions).
start "" "PSO2-Launcher-CSharp.sln"
ENDLOCAL
