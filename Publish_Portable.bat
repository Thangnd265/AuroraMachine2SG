@echo off
cd /d "%~dp0"
echo Publishing self-contained portable Windows x64 package...
dotnet publish src\AuroraMachine2SG.App\AuroraMachine2SG.App.csproj -c Release -r win-x64 --self-contained true -o _publish\AuroraMachine2SG_Portable
echo Done! Copy _publish\AuroraMachine2SG_Portable to any Windows PC without Aurora or .NET installed.
pause
