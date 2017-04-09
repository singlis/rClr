
set VSCOMNTOOLS=%VS150COMNTOOLS%
@if "%VSCOMNTOOLS%"=="" VSCOMNTOOLS=%VS140COMNTOOLS%
@if "%VSCOMNTOOLS%"=="" goto error_no_VS140COMNTOOLSDIR
REM for instance C:\bin\VS2012\Common7\Tools\

set VSDEVENV="%VSCOMNTOOLS%..\..\VC\Auxiliary\Build\vcvarsall.bat"
@if not exist %VSDEVENV% goto error_no_vcvarsall
@call %VSDEVENV% x86_amd64
@goto end

:error_no_VS140COMNTOOLSDIR
@echo ERROR: setup_vcpp cannot determine the location of the VS Common Tools folder.
@goto end

:error_no_vcvarsall
@echo ERROR: Cannot find file %VSDEVENV%.
@goto end

:end

