@echo off
set _defaultToolDir=c:\windows\Microsoft.NET\Framework\v1.0.3705;C:\Program Files\Microsoft Visual Studio .NET\FrameworkSDK\bin
echo .

if not "%DotNetToolDir%"=="" goto :setPath
echo Warning: Could not find DotNetToolDir environment variable
echo .
echo set DotNetToolDir=%_defaultToolDir%
set DotNetToolDir=%_defaultToolDir%

:setPath
set _backupPath=%path%
set path=%path%;%DotNetToolDir%

set _command=lib\nant\nant %*
echo %_command%
echo .
call %_command%

set _command=
set path=%_backupPath%
set _backupPath=
set _defaultToolDir=