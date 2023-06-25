# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/GWReloaded/*" -Force -Recurse
dotnet publish "./GWReloaded.csproj" -c Release -o "$env:RELOADEDIIMODS/GWReloaded" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location