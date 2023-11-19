[string]$myrootpath = Split-Path $MyInvocation.MyCommand.Path -Parent
[string]$SDKDir = [System.IO.Path]::Combine($myrootpath, "sdk\8.0.100")

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
    Print-OneLine (".NET8 SDK doesn't exist locally at: ",$SDKDir)
    Write-Host "Begin to download the SDK."
    if ([Environment]::Is64BitOperatingSystem) {
        [string]$OutFile = [System.IO.Path]::Combine($OutDir, "dotnet-sdk-8.0.100-win-x64.zip")
        Print-OneLine ("Downloading .NET8 SDK archive (x64) to: ",$OutFile)
        Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/2b2d6133-c4f9-46dd-9ab6-86443a7f5783/340054e2ac7de2bff9eea73ec9d4995a/dotnet-sdk-8.0.100-win-x64.zip

" -OutFile $OutFile -UseBasicParsing
        return "dotnet-sdk-8.0.100-win-x64.zip"
    } else {
        [string]$OutFile = [System.IO.Path]::Combine($OutDir, "dotnet-sdk-8.0.100-win-x86.zip")
        Print-OneLine ("Downloading .NET8 SDK archive (x86) to: ",$OutFile)
        Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/210579cb-610d-4040-9052-e024a42bcd9c/e260700ae965a0f7d3bf38e8d8f0778c/dotnet-sdk-8.0.100-win-x86.zip

" -OutFile $OutFile -UseBasicParsing
        return "dotnet-sdk-8.0.100-win-x86.zip"
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
        Add-Type -assembly “System.IO.Compression.FileSystem E
        [System.IO.Compression.ZipFile]::ExtractToDirectory($localpath, $SDKDir)
    }
    Remove-Item $localpath -Force -Confirm:$false
    
    Print-OneLine (".NET8 SDK has been downloaded to local directory at: ",$SDKDir)
    Write-Output $SDKDir
} else {
    Print-OneLine (".NET8 SDK found locally at: ",$SDKDir)
    Write-Output $SDKDir
}
