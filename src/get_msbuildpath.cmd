@echo OFF
REM ===============================================
REM Query the windows registry to get the location of MsBuild.exe. 
REM Note that this returns a cygwin style path with slashes, to workaround an oddity with ash+sed in configure.win
REM ===============================================
REM set MSBuildToolsPath="c:/program files (x86)/Microsoft Visual Studio/2017/Enterprise/MsBuild/15.0/MSBuild.exe"
set MSBuildToolsPath=%VS150COMNTOOLS%../../MSBuild/15.0/Bin/
REM for /F "tokens=1,2*" %%i in ('reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" /v "MSBuildToolsPath"') DO (
REM if "%%i"=="MSBuildToolsPath" (
REM   @SET "MSBuildToolsPath=%%k"
REM  )
REM )
REM Remove a trailing '\' that causes grief in configure.win
REM @if not "%MSBuildToolsPath%"=="" set MSBuildToolsPath=%MSBuildToolsPath:~0,-1%

REM If the path is returned with backslash, then ash seems to receive it with an oddity:
REM C:\Windows\Microsoft.NET\Framework4.0.30319\MSBuild.exe
REM It proved easier to substitute in DOS than with ash+sed. Sigh.
set MSBuildToolsPath='%MSBuildToolsPath%MSBuild.exe'
set MSBuildToolsPath=%MSBuildToolsPath:\=/%
@echo %MSBuildToolsPath%
