@echo off
goto START

-------------------------------------------------------
 MakeReleaseZips.bat

 Makes the zips for the release content.

 Thu, 19 Jun 2008  22:17


-------------------------------------------------------


:START

setlocal

set zipit=c:\dinoch\bin\zipit.exe
set stamp=%DATE% %TIME%
set stamp=%stamp:/=-%
set stamp=%stamp: =-%
set stamp=%stamp::=%

@set tfile1=%TEMP%\makereleasezip-%RANDOM%-%stamp%.tmp

@REM get the version: 
type Library\Properties\AssemblyInfo.cs | c:\cygwin\bin\grep AssemblyVersion | c:\cygwin\bin\sed -e 's/^.*"\(.*\)".*/\1 /' > %tfile1%

call c:\dinoch\bin\setz.bat type %tfile1%

set version=%setz:~0,3%
echo version is %version%


c:\.net3.5\msbuild.exe

mkdir ..\releases\v%version%-%stamp%

call :MakeHelpFile

call :MakeDevelopersRedist

call :MakeRuntimeRedist

call :MakeZipUtils

call :MakeMsi


c:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe .\clean.ps1

call :MakeSrcZip


goto :END


--------------------------------------------
@REM MakeHelpFile subroutine
@REM example output zipfile name:  DotNetZipLib-v1.5.chm

:MakeHelpFile

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Invoking Sandcastle HFB to make the Compiled Help File
  echo.


  "C:\Program Files\EWSoftware\Sandcastle Help File Builder\SandcastleBuilderConsole.exe" DotNetZip.shfb
  move Help\DotNetZipLib-v*.chm ..\releases\v%version%-%stamp%

goto :EOF
@REM end subroutine
--------------------------------------------




--------------------------------------------
@REM MakeDevelopersRedist subroutine
@REM example output zipfile name:  DotNetZipLib-DevKit-v1.5.zip

:MakeDevelopersRedist

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Developer's redistributable zip...
  echo.

  set zipfile=DotNetZipLib-DevKit-v%version%.zip

  echo zipfile is %zipfile%
  cd Library\bin\Debug
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%  -c "Zip Library v%version% packed on %stamp%" Ionic.Utils.Zip.dll Ionic.Utils.Zip.XML Ionic.Utils.Zip.pdb 
  cd ..\..\..
  %zipit% ..\releases\v%version%-%stamp%\%zipfile%  License.txt
  cd ..\releases\v%version%-%stamp%
  for %%V in ("*.chm") do   %zipit% %zipfile%  %%V
  cd ..\..\DotNetZip

goto :EOF
@REM end subroutine
--------------------------------------------



--------------------------------------------
@REM MakeRuntimeRedist subroutine
@REM example output zipfile name:  DotNetZipLib-Runtime-v1.5.zip

:MakeRuntimeRedist

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the user's redistributable zip...
  echo.


  set zipfile=DotNetZipLib-Runtime-v%version%.zip

  echo zipfile is %zipfile%
  cd Library\bin\Debug
  %zipit% ..\..\..\..\releases\v%version%-%stamp%\%zipfile%  -c "Zip Library v%version% packed on %stamp%" Ionic.Utils.Zip.dll 
  cd ..\..\..
  %zipit% ..\releases\v%version%-%stamp%\%zipfile%  License.txt

goto :EOF
@REM end subroutine
--------------------------------------------




--------------------------------------------
@REM MakeZipUtils subroutine

:MakeZipUtils

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Zip Utils zip...
  echo.


    set zipfile=DotNetZipUtils-v%version%.zip
    del ..\%zipfile%

    cd Examples
    cd ZipIt\bin\debug
    %zipit% ..\..\..\..\..\releases\v%version%-%stamp%\%zipfile% -c "Zip utilities v%version% packed on %stamp%"  Zipit.exe Ionic.Utils.Zip.dll 
    cd ..\..\..\Unzip\bin\debug
    %zipit%  ..\..\..\..\..\releases\v%version%-%stamp%\%zipfile%  Unzip.exe
    cd ..\..\..\SelfExtracting\bin\debug
    %zipit%  ..\..\..\..\..\releases\v%version%-%stamp%\%zipfile%  CreateSelfExtractor.exe
    cd ..\..\..\..
    %zipit% ..\releases\v%version%-%stamp%\%zipfile%  License.txt

goto :EOF
@REM end subroutine
--------------------------------------------



--------------------------------------------
@REM MakeMsi subroutine
@REM example output zipfile name:  ZipLib-v1.5.msi

:MakeMsi

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the MSI...
  echo.

  c:\vs2008\Common7\ide\devenv.exe DotNetZip.sln /build debug /project "Zip Utilities MSI"
  c:\dinoch\dev\dotnet\AwaitFile Setup\Debug\DotNetZipUtils.msi
  move Setup\Debug\DotNetZipUtils.msi ..\releases\v%version%-%stamp%\DotNetZipUtils-v%version%.msi

goto :EOF
@REM end subroutine
--------------------------------------------




--------------------------------------------
@REM MakeSrcZip subroutine
:MakeSrcZip

  echo.
  echo +++++++++++++++++++++++++++++++++++++++++++++++++++++++
  echo.
  echo Making the Source Zip...
  echo.

    set zipfile=DotNetZip-src-v%version%.zip

    cd..
    c:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe  DotNetZip\ZipSrc.ps1

    move DotNetZip-src-v*.zip  releases\v%version%-%stamp%
    cd DotNetZip

@REM    del /q Library\Resources\*.*
@REM
@REM    c:\cygwin\bin\find.exe .  -type f | grep -v  _tfs | grep -v notused | grep -v -i setup.exe | grep -v \~ | grep -v \#  | grep -v Documentation | grep -v CodePlex-Readme.txt | grep -v semantic.cache | grep -v sln.cache | grep -v TestResults | grep -v .suo > %tfile1%
@REM
@REM
@REM    @for /f "usebackq" %%W in (%tfile1%) do call :ZIPONE %%W

goto :EOF
@REM end subroutine
--------------------------------------------


@REM 
@REM --------------------------------------------
@REM @REM ZIPONE subroutine
@REM 
@REM :ZIPONE
@REM %zipit% ..\%zipfile%  %1
@REM goto :EOF
@REM @REM end subroutine
@REM --------------------------------------------
@REM 



:END
if exist %tfile1% @del %tfile1%

echo release zips are in releases\v%version%-%stamp%

endlocal



