@echo off
cd /d "%~dp0"
if exist "_publish\AuroraMachine2SG_Portable\AuroraMachine2SG.exe" (
    start "" "_publish\AuroraMachine2SG_Portable\AuroraMachine2SG.exe"
) else (
    dotnet run --project src\AuroraMachine2SG.App
)
