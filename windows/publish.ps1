param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "$PSScriptRoot\artifacts\Dousha.Windows"
)

$ErrorActionPreference = "Stop"

dotnet publish "$PSScriptRoot\src\Dousha.Windows.App\Dousha.Windows.App.csproj" `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained false `
    --output $Output

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Portable app published to $Output"
