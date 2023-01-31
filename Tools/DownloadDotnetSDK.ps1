[string]$myrootpath = Split-Path $MyInvocation.MyCommand.Path -Parent
[string]$SDKDir = [System.IO.Path]::Combine($myrootpath, "sdk\6.0.100")

#Write-Host itself does the same but...I'm not entirely sure.
#Hence, the existence of this func.
function Print-OneLine {
    param ([string[]]$Strs)
    [int32]$lastIndex = $Strs.Length - 1
    For ($index=0; $index -le $lastIndex; $index++) {
        if ($index -eq $lastIndex) {
            Write-Host $Strs[$index]
        } else {
            Write-Host $Strs[$index] -NoNewline
        }
    }
}

function Download-SDK-Zip {
    [OutputType([string])]
    param ([string]$OutDir)
    Print-OneLine (".NET6 SDK doesn't exist locally at: ",$SDKDir)
    Write-Host "Begin to download the SDK."
    if ([Environment]::Is64BitOperatingSystem) {
        [string]$OutFile = [System.IO.Path]::Combine($OutDir, "dotnet-sdk-6.0.100-win-x64.zip")
        Print-OneLine ("Downloading .NET6 SDK archive (x64) to: ",$OutFile)
        Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/ca65b248-9750-4c2d-89e6-ef27073d5e95/05c682ca5498bfabc95985a4c72ac635/dotnet-sdk-6.0.100-win-x64.zip
" -OutFile $OutFile -UseBasicParsing
        return "dotnet-sdk-6.0.100-win-x64.zip"
    } else {
        [string]$OutFile = [System.IO.Path]::Combine($OutDir, "dotnet-sdk-6.0.100-win-x86.zip")
        Print-OneLine ("Downloading .NET6 SDK archive (x86) to: ",$OutFile)
        Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/c91c6641-580a-4b7d-a89a-5ed0e15bc318/f35ebceebbb06374734d1c238036e504/dotnet-sdk-6.0.100-win-x86.zip
" -OutFile $OutFile -UseBasicParsing
        return "dotnet-sdk-6.0.100-win-x86.zip"
    }
    
}

if(![System.IO.File]::Exists([System.IO.Path]::Combine($SDKDir, "dotnet.exe"))) {
    [string]$zipfilename = Download-SDK-Zip $myrootpath
    [string]$localpath = [System.IO.Path]::Combine($myrootpath, $zipfilename)

    Print-OneLine ("Extracting archive to : ",$SDKDir)
    # Expand-Archive cmdlets only exists on PowerShell v5 or higher.
    if ((Get-Host).Version.Major -ige 5) {
        Expand-Archive -Path $localpath -DestinationPath $SDKDir
    } else {
        # Polyfill it with .NET's function
        Add-Type -assembly “System.IO.Compression.FileSystem”
        [System.IO.Compression.ZipFile]::ExtractToDirectory($localpath, $SDKDir)
    }
    Remove-Item $localpath -Force -Confirm:$false
    
    Print-OneLine (".NET6 SDK has been downloaded to local directory at: ",$SDKDir)
    Write-Output $SDKDir
} else {
    Print-OneLine (".NET6 SDK found locally at: ",$SDKDir)
    Write-Output $SDKDir
}
