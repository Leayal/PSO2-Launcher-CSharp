REM This batch script is a one-click build-everything (configuration of Release) into dist-prod folder.
REM After everything is done. User can launch the PSO2Lealauncher.exe in dist-prod folder and use it.
REM This is a Release build so it is not good for debug purpose. But for production use, this is what user will need.

SET "DOTNET_CLI_TELEMETRY_OPTOUT=1"

@echo off
cd /d %~dp0

REM Make all other .dll files
call Publish.bat

SETLOCAL
SET "PublishRootDir=docs\publish\v6"
SET "MSBUILDDISABLENODEREUSE=1"
if not exist %PublishRootDir% (
 mkdir "%PublishRootDir%"
)

IF EXIST "%~dp0Tools\sdk\6.0.100\dotnet.exe" (
 REM Use the specific SDK.
 SET "PATH=%PATH%;%~dp0Tools\sdk\6.0.100"
 REM I need to targeting this SDK because there's no way to directly target the specific 6.0.0 runtime (so that all assembly files are compatible with all .NET6 versions).
)

IF EXIST "dist-prod" (
 DEL /F /S /Q "dist-prod"
)
mkdir "dist-prod"

dotnet publish -c Release --no-self-contained -p:PublishReadyToRun=true -p:PublishSingleFile=true -r win-x64 -o "dist-prod" "PSO2Launcher\PSO2Launcher.csproj"
xcopy "%PublishRootDir%\files" "dist-prod\bin" /S /R /Y /I

ENDLOCAL