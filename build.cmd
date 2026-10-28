@echo off
rem Builds the portable game: one self-contained DiceDrop.exe, no installer and no .NET install needed.
rem Usage: build.cmd [output folder]   (default: dist)
setlocal
set "ROOT=%~dp0"
set "OUTPUT=%~1"
if "%OUTPUT%"=="" set "OUTPUT=%ROOT%dist"

rem Version: DICEDROP_VERSION if set (e.g. set DICEDROP_VERSION=0.1.1), else the newest vX.Y.Z tag behind
rem this commit, the same version the release workflow would stamp. With neither, 0.0.0: no update checks.
set "VERSION=%DICEDROP_VERSION%"
if not defined VERSION (
    for /f "delims=" %%t in ('git -C "%ROOT%." describe --tags --abbrev^=0 --match "v[0-9]*.[0-9]*.[0-9]*" 2^>nul') do set "TAG=%%t"
)
if not defined VERSION if defined TAG set "VERSION=%TAG:~1%"
if not defined VERSION set "VERSION=0.0.0"
echo Building DiceDrop %VERSION%

dotnet test "%ROOT%DiceDrop.slnx" -c Release
if errorlevel 1 (
    echo Tests failed; not publishing. 1>&2
    exit /b 1
)

dotnet publish "%ROOT%src\Ludo.Presentation.Wpf\Ludo.Presentation.Wpf.csproj" -c Release -o "%OUTPUT%" -p:Version=%VERSION%
if errorlevel 1 (
    echo Publish failed. 1>&2
    exit /b 1
)

dir /b "%OUTPUT%"
