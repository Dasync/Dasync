pushd

cd $(Join-Path $PSScriptRoot '..')

dotnet build
dotnet pack --no-build

$packagesDirectory = $(Resolve-Path $(Join-Path $PSScriptRoot 'packages')).ToString()
New-Item -Path $PSScriptRoot -Name 'packages' -ItemType 'directory' -Force | Out-Null

$allPackageFiles = Get-Childitem -Include '*.nupkg','*.snupkg' -File -Recurse -ErrorAction SilentlyContinue
$allPackageFiles | % {
  $sourceFilePath = $(Resolve-Path $_.FullName).ToString()
  if (-not $sourceFilePath.StartsWith($packagesDirectory))
  {
    Copy-Item -Path $_.FullName -Destination $packagesDirectory -Force
  }
}

popd