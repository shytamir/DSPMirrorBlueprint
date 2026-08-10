@echo off
setlocal

set "GAME_ROOT=%~1"
if not defined GAME_ROOT set "GAME_ROOT=C:\Program Files (x86)\Steam\steamapps\common\Dyson Sphere Program"

set "DOTNET_EXE=%ProgramFiles%\dotnet\dotnet.exe"
if not exist "%DOTNET_EXE%" set "DOTNET_EXE=dotnet"

"%DOTNET_EXE%" build "%~dp0tests\DSPMirrorBlueprint.Tests\DSPMirrorBlueprint.Tests.csproj" --configuration Release -p:GameRoot="%GAME_ROOT%"
if errorlevel 1 exit /b %ERRORLEVEL%

"%~dp0tests\DSPMirrorBlueprint.Tests\bin\Release\DSPMirrorBlueprint.Tests.exe"
exit /b %ERRORLEVEL%
