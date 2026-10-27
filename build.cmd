@echo off
rem Builds the portable game: one self-contained DiceDrop.exe, no installer and no .NET install needed.
rem Usage: build.cmd [output folder]   (default: dist)
setlocal
set "ROOT=%~dp0"
set "OUTPUT=%~1"
if "%OUTPUT%"=="" set "OUTPUT=%ROOT%dist"

dotnet test "%ROOT%DiceDrop.slnx" -c Release
if errorlevel 1 (
    echo Tests failed; not publishing. 1>&2
    exit /b 1
)

dotnet publish "%ROOT%src\Ludo.Presentation.Wpf\Ludo.Presentation.Wpf.csproj" -c Release -o "%OUTPUT%"
if errorlevel 1 (
    echo Publish failed. 1>&2
    exit /b 1
)

dir /b "%OUTPUT%"
