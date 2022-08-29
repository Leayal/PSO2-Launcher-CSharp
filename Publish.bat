@echo off
cd /d %~dp0
SETLOCAL
SET "PublishRootDir=docs\publish\v6"
SET "MSBUILDDISABLENODEREUSE=1"
if not exist %PublishRootDir% (
 mkdir "%PublishRootDir%"
)

REM Microsoft was too weird in management SDKs that I have to put everything in the repo.
REM Resulting in enlarge the repo for no benefits or anything good at all. Simply evolving backward.
REM My recommendation is that you shouldn't use any Visual Studio products for .NET development.
REM Because its Dotnet SDK version management,isolation and selection were too beautiful for a mere mortal like me to understand.
SET "PATH=%~dp0Tools\sdk\6.0.100;%PATH%"
REM I need to targeting this SDK because there's no way to directly target the 6.0.0 runtime (so that all assembly files are compatible with all .NET6 versions).

dotnet build -c Release -o "Build\LauncherCore-natives" "LauncherCore\LauncherCore.csproj"
dotnet publish -c Publish --no-self-contained -p:PublishReadyToRun=true -r win-x64 -o "Build\LauncherCoreNew" "LauncherCoreNew\LauncherCoreNew.csproj"
del /Q /F "Build\LauncherCoreNew\e_sqlcipher.dll"
del /Q /F "Build\LauncherCoreNew\WebView2Loader.dll"
del /F /Q "Build\LauncherCoreNew\Microsoft.Web.WebView2.WPF.dll"
copy /B /L /Y "Build\LauncherCoreNew\*.dll" "%PublishRootDir%\files\"

if not exist %PublishRootDir%\files\native-x64 (
 mkdir "%PublishRootDir%\files\native-x64"
)
copy /B /L /Y "Build\LauncherCore-natives\runtimes\win-x64\native\*.dll" "%PublishRootDir%\files\native-x64\"

if not exist %PublishRootDir%\files\native-x86 (
 mkdir "%PublishRootDir%\files\native-x86"
)
copy /B /L /Y "Build\LauncherCore-natives\runtimes\win-x86\native\*.dll" "%PublishRootDir%\files\native-x86\"

dotnet publish -c Publish --no-self-contained -p:PublishReadyToRun=true -r win-x64 -o "Build\Updater" "Updater\Updater.csproj"
copy /B /L /Y "Build\Updater\*.dll" "%PublishRootDir%\files\"

if not exist %PublishRootDir%\files\plugins\rss (
 mkdir "%PublishRootDir%\files\plugins\rss"
)
dotnet publish -c Release --no-self-contained -p:PublishReadyToRun=true -r win-x64 -o "Build\WordpressRSSFeed" "WordpressRSSFeed\WordpressRSSFeed.csproj"
copy /B /L /Y "Build\WordpressRSSFeed\WordpressRSSFeed.dll" "%PublishRootDir%\files\plugins\rss\"

dotnet publish -c Release --no-self-contained -p:PublishReadyToRun=true -r win-x64 -o "Build\PSUBlogRSSFeed" "PSUBlogRSSFeed\PSUBlogRSSFeed.csproj"
copy /B /L /Y "Build\PSUBlogRSSFeed\PSUBlogRSSFeed.dll" "%PublishRootDir%\files\plugins\rss\"

REM Remove unncessary files
REM del /F /Q "%PublishRootDir%\files\Microsoft.Web.WebView2.WPF.dll"
REM del /F /Q "%PublishRootDir%\files\WebView2Loader.dll"
del /F /Q "%PublishRootDir%\files\PSO2LeaLauncher.dll"

Tools\SHA1Maker.exe "%PublishRootDir%" "%PublishRootDir%\update.json" "https://leayal.github.io/PSO2-Launcher-CSharp/publish/v6/"
ENDLOCAL