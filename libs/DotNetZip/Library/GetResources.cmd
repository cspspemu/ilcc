REM @echo off
goto START

-------------------------------------------------------
 GetResources.bat

 Copy resources from the CreateSelfExtractor Example project to be used in the Zip library.
 This script assumes it will be run by Visual Studio, starting with the 
 current directory of C:\dinoch\dev\dotnet\zip\DotNetZip\Library\bin\Debug

 Sat, 07 Jun 2008  10:39

-------------------------------------------------------


:START
setlocal

set ResourceDirectory=Resources
cd ..\..\
mkdir %ResourceDirectory%
copy /y ..\Examples\SelfExtracting\CommandLineSelfExtractorStub.cs        %ResourceDirectory%
copy /y ..\Examples\SelfExtracting\WinFormsSelfExtractorStub.cs           %ResourceDirectory%
copy /y ..\Examples\SelfExtracting\PasswordDialog.cs                      %ResourceDirectory%
copy /y ..\Examples\SelfExtracting\WinFormsSelfExtractorStub.Designer.cs  %ResourceDirectory%
copy /y ..\Examples\SelfExtracting\PasswordDialog.Designer.cs             %ResourceDirectory%


copy /y ..\Examples\SelfExtracting\WinFormsSelfExtractorStub.resx     %ResourceDirectory%
copy /y ..\Examples\SelfExtracting\PasswordDialog.resx     %ResourceDirectory%


@REM c:\netsdk3.0\bin\resgen.exe ..\Examples\SelfExtracting\WinFormsSelfExtractorStub.resx     %ResourceDirectory%\WinFormsSelfExtractorStub.resources
@REM c:\netsdk3.0\bin\resgen.exe ..\Examples\SelfExtracting\PasswordDialog.resx                %ResourceDirectory%\PasswordDialog.resources
@REM 
@REM del %ResourceDirectory%.zip
@REM c:\dinoch\bin\zipit.exe %ResourceDirectory%.zip %ResourceDirectory%
@REM rd /s /q %ResourceDirectory%


endlocal
:END



