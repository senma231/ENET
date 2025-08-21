@echo off
echo Starting build process...

dotnet publish -c Release -o ".\publish" --self-contained true -r win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

if %ERRORLEVEL% EQU 0 (
    echo Build successful! Files are in .\publish
) else (
    echo Build failed!
)

pause