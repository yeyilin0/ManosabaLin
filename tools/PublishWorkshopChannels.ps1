param(
    [ValidateSet("Both", "Official", "Beta")]
    [string] $Channel = "Both",

    [ValidateSet("None", "Official", "Beta")]
    [string] $Activate = "None",

    [string] $Configuration = "Release",

    [string] $UploaderRoot = "C:\Users\Lenovo\Downloads\ModUploader-win-x64\ManosabaLin",

    [string] $UploaderExe = "",

    [string] $WorkshopId = "",

    [switch] $Upload,

    [ValidateSet("BetaThenOfficial", "OfficialThenBeta")]
    [string] $UploadOrder = "BetaThenOfficial",

    [string] $OfficialChangeNote = "",

    [string] $BetaChangeNote = "",

    [switch] $SkipPckExport
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $RepoRoot "ManosabaLin.csproj"
$ModName = "ManosabaLin"
$WorkshopFiles = @("$ModName.dll", "$ModName.json", "$ModName.pck")

if ([string]::IsNullOrWhiteSpace($UploaderExe)) {
    $UploaderExe = Join-Path (Split-Path -Parent $UploaderRoot) "ModUploader.exe"
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FilePath,

        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE`: $FilePath $($Arguments -join ' ')"
    }
}

function Get-MsBuildProperty {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PropertyName,

        [Parameter(Mandatory = $true)]
        [string] $Sts2Channel
    )

    $output = & dotnet msbuild $ProjectPath `
        -nologo `
        "-p:Configuration=$Configuration" `
        "-p:Sts2Channel=$Sts2Channel" `
        "-getProperty:$PropertyName"

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read MSBuild property $PropertyName for channel $Sts2Channel."
    }

    $value = $output | Where-Object { $_.Trim().Length -gt 0 } | Select-Object -Last 1
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "MSBuild property $PropertyName for channel $Sts2Channel is empty."
    }

    return $value.Trim()
}

function Copy-WorkshopFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SourceDir,

        [Parameter(Mandatory = $true)]
        [string] $TargetDir,

        [Parameter(Mandatory = $true)]
        [string] $ManifestSourcePath,

        [Parameter(Mandatory = $true)]
        [string] $DllSourcePath
    )

    New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null

    foreach ($fileName in $WorkshopFiles) {
        $sourcePath = switch ($fileName) {
            "$ModName.json" { $ManifestSourcePath; break }
            "$ModName.dll" { $DllSourcePath; break }
            default { Join-Path $SourceDir $fileName }
        }

        if (-not (Test-Path -LiteralPath $sourcePath)) {
            throw "Expected workshop file was not found: $sourcePath"
        }

        Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $TargetDir $fileName) -Force
    }
}

function Publish-Channel {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Official", "Beta")]
        [string] $Sts2Channel
    )

    Write-Host "Publishing $Sts2Channel package..."

    $oldSkip = $env:STS2_SKIP_PCK_EXPORT
    try {
        if ($SkipPckExport) {
            $env:STS2_SKIP_PCK_EXPORT = "1"
        } else {
            Remove-Item Env:\STS2_SKIP_PCK_EXPORT -ErrorAction SilentlyContinue
        }

        Invoke-CheckedCommand "dotnet" @(
            "restore",
            $ProjectPath,
            "-p:Sts2Channel=$Sts2Channel",
            "--force"
        )

        Invoke-CheckedCommand "dotnet" @(
            "publish",
            $ProjectPath,
            "-c",
            $Configuration,
            "--no-restore",
            "-p:Sts2Channel=$Sts2Channel"
        )
    } finally {
        if ($null -eq $oldSkip) {
            Remove-Item Env:\STS2_SKIP_PCK_EXPORT -ErrorAction SilentlyContinue
        } else {
            $env:STS2_SKIP_PCK_EXPORT = $oldSkip
        }
    }

    $modOutputDir = Get-MsBuildProperty "ModOutputDir" $Sts2Channel
    $manifestOutputFile = Get-MsBuildProperty "ManifestOutputFile" $Sts2Channel
    $targetPath = Get-MsBuildProperty "TargetPath" $Sts2Channel
    $stageDir = Join-Path $UploaderRoot ("content-" + $Sts2Channel.ToLowerInvariant())

    Copy-WorkshopFiles $modOutputDir $stageDir $manifestOutputFile $targetPath
    Write-Host "Staged $Sts2Channel package at $stageDir"

    return $stageDir
}

function Activate-Channel {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Official", "Beta")]
        [string] $Sts2Channel
    )

    $stageDir = Join-Path $UploaderRoot ("content-" + $Sts2Channel.ToLowerInvariant())
    $contentDir = Join-Path $UploaderRoot "content"

    if (-not (Test-Path -LiteralPath $stageDir)) {
        throw "Cannot activate $Sts2Channel because the staged directory does not exist: $stageDir"
    }

    New-Item -ItemType Directory -Force -Path $contentDir | Out-Null

    foreach ($fileName in $WorkshopFiles) {
        $targetPath = Join-Path $contentDir $fileName
        if (Test-Path -LiteralPath $targetPath) {
            Remove-Item -LiteralPath $targetPath -Force
        }

        Copy-Item -LiteralPath (Join-Path $stageDir $fileName) -Destination $targetPath -Force
    }

    Write-Host "Activated $Sts2Channel package at $contentDir"
}

function Get-WorkshopId {
    if (-not [string]::IsNullOrWhiteSpace($WorkshopId)) {
        return $WorkshopId
    }

    $modIdPath = Join-Path $UploaderRoot "mod_id.txt"
    if (-not (Test-Path -LiteralPath $modIdPath)) {
        throw "Workshop id was not provided and mod_id.txt was not found: $modIdPath"
    }

    $id = (Get-Content -LiteralPath $modIdPath -Raw).Trim()
    if ([string]::IsNullOrWhiteSpace($id)) {
        throw "Workshop id was not provided and mod_id.txt is empty: $modIdPath"
    }

    return $id
}

function Set-WorkshopChangeNote {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ChangeNote
    )

    $workshopJsonPath = Join-Path $UploaderRoot "workshop.json"
    if (-not (Test-Path -LiteralPath $workshopJsonPath)) {
        throw "workshop.json was not found: $workshopJsonPath"
    }

    $config = Get-Content -LiteralPath $workshopJsonPath -Raw | ConvertFrom-Json
    if ($config.PSObject.Properties.Name -contains "changeNote") {
        $config.changeNote = $ChangeNote
    } else {
        $config | Add-Member -NotePropertyName "changeNote" -NotePropertyValue $ChangeNote
    }

    $config | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $workshopJsonPath -Encoding UTF8
    Write-Host "Updated workshop change note: $ChangeNote"
}

function Get-DefaultChangeNote {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Official", "Beta")]
        [string] $Sts2Channel
    )

    $version = Get-MsBuildProperty "Version" $Sts2Channel
    if ($Sts2Channel -eq "Official") {
        if (-not [string]::IsNullOrWhiteSpace($OfficialChangeNote)) {
            return $OfficialChangeNote
        }

        return "[Official] ManosabaLin v$version - STS2 official branch"
    }

    if (-not [string]::IsNullOrWhiteSpace($BetaChangeNote)) {
        return $BetaChangeNote
    }

    return "[Beta] ManosabaLin v$version - STS2 public-beta branch"
}

function Upload-Channel {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Official", "Beta")]
        [string] $Sts2Channel
    )

    if (-not (Test-Path -LiteralPath $UploaderExe)) {
        throw "ModUploader.exe was not found: $UploaderExe"
    }

    Activate-Channel $Sts2Channel
    Set-WorkshopChangeNote (Get-DefaultChangeNote $Sts2Channel)

    $id = Get-WorkshopId
    Write-Host "Uploading $Sts2Channel package to workshop item $id..."
    Push-Location (Split-Path -Parent $UploaderExe)
    try {
        Invoke-CheckedCommand $UploaderExe @(
            "upload",
            "-w",
            $UploaderRoot,
            "-i",
            $id
        )
    } finally {
        Pop-Location
    }
}

$channels = if ($Channel -eq "Both") { @("Official", "Beta") } else { @($Channel) }

foreach ($sts2Channel in $channels) {
    Publish-Channel $sts2Channel | Out-Null
}

if ($Upload) {
    $uploadChannels = if ($Channel -eq "Both") {
        if ($UploadOrder -eq "BetaThenOfficial") {
            @("Beta", "Official")
        } else {
            @("Official", "Beta")
        }
    } else {
        @($Channel)
    }

    foreach ($sts2Channel in $uploadChannels) {
        Upload-Channel $sts2Channel
    }
} elseif ($Activate -ne "None") {
    Activate-Channel $Activate
}
