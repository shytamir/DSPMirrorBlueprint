@echo off
setlocal

set "GAME_ROOT=%~1"
if not defined GAME_ROOT set "GAME_ROOT=C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program"

set "DOTNET_EXE=%ProgramFiles%\dotnet\dotnet.exe"
if not exist "%DOTNET_EXE%" set "DOTNET_EXE=dotnet"

"%DOTNET_EXE%" build "%~dp0src\DSPMirrorBlueprint\DSPMirrorBlueprint.csproj" --configuration Release -p:GameRoot="%GAME_ROOT%"
exit /b %ERRORLEVEL%
