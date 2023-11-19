SET "DOTNET_CLI_TELEMETRY_OPTOUT=1"

@echo off
cd /d %~dp0

REM This file is now served as a fall-back whenever MS messed up the SDK again.
REM This batch script will force download the SDK v8.0.100.
call Tools\SetupEnv.bat

SETLOCAL
REM Use the specific SDK.
SET "PATH=%~dp0Tools\sdk\8.0.100;%PATH%"
start "" "PSO2-Launcher-CSharp.sln"
ENDLOCAL
