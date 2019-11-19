pushd

cd Join-Path $PSScriptRoot '..'

dotnet build
dotnet pack

$packagesDirectory = Join-Path $PSScriptRoot 'packages'
New-Item -Path $PSScriptRoot -Name 'packages' -ItemType 'directory' -Force

$allPackageFiles = Get-Childitem -Include '*.nupkg','*.snupkg' -File -Recurse -ErrorAction SilentlyContinue
$allPackageFiles | % { Copy-Item -Path $_.FullName -Destination $packagesDirectory -Force }

popd