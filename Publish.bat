@echo off
cd /d %~dp0
SETLOCAL
SET "MSBUILDDISABLENODEREUSE=1"
if not exist docs\publish\files (
 mkdir "docs\publish\files"
)

dotnet build -c Release -o "Build\LauncherCore-natives" "LauncherCore\LauncherCore.csproj"
dotnet publish -c Release --no-self-contained -p:PublishReadyToRun=true -r win-x64 -o "Build\LauncherCore" "LauncherCore\LauncherCore.csproj"
del /Q /F "Build\LauncherCore\e_sqlcipher.dll"
copy /B /L /Y "Build\LauncherCore\*.dll" "docs\publish\files\"
if not exist docs\publish\files\x64 (
 mkdir "docs\publish\files\x64"
)
copy /B /L /Y "Build\LauncherCore-natives\runtimes\win-x64\native\*.dll" "docs\publish\files\x64"
if not exist docs\publish\files\x86 (
 mkdir "docs\publish\files\x86"
)
copy /B /L /Y "Build\LauncherCore-natives\runtimes\win-x86\native\*.dll" "docs\publish\files\x86"

dotnet publish -c Release --no-self-contained -p:PublishReadyToRun=true -r win-x64 -o "Build\Updater" "Updater\Updater.csproj"
copy /B /L /Y "Build\Updater\*.dll" "docs\publish\files\"

dotnet publish -c Release --no-self-contained -p:PublishReadyToRun=true -r win-x64 -o "Build\WordpressRSSFeed" "WordpressRSSFeed\WordpressRSSFeed.csproj"
copy /B /L /Y "Build\WordpressRSSFeed\WordpressRSSFeed.dll" "docs\publish\files\plugins\rss\"

dotnet publish -c Release --no-self-contained -p:PublishReadyToRun=true -r win-x64 -o "Build\PSUBlogRSSFeed" "PSUBlogRSSFeed\PSUBlogRSSFeed.csproj"
copy /B /L /Y "Build\PSUBlogRSSFeed\PSUBlogRSSFeed.dll" "docs\publish\files\plugins\rss\"

copy /B /L /Y "Dependencies\Build\net5.0\*.dll" "docs\publish\files\"

Tools\SHA1Maker.exe "docs\publish" "docs\publish\update.json" "https://leayal.github.io/PSO2-Launcher-CSharp/publish/"
ENDLOCAL