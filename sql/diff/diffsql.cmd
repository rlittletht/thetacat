rem @echo off
set local
echo %1

set arg1=%1
set arg1=%arg1:"=%
set arg2=%2
set arg2=%arg2:"=%

rd /q /s %TEMP%\__sqldiff
mkdir %TEMP%\__sqldiff\left
mkdir %TEMP%\__sqldiff\right

set __temppath=%TEMP:\=\\%
echo PATH:  %__temppath%

echo y | gen *.sql /O "sqlcmd %arg1% -W -s , -i $F > %__temppath%\\__sqldiff\\left\\$F.out" /x
echo y | gen *.sql /O "sqlcmd %arg2% -W -s , -i $F > %__temppath%\\__sqldiff\\right\\$F.out" /x
rem sqlcmd %arg1% -W -s , -i diff.sql > %TEMP%\left.txt
rem sqlcmd %arg2% -W -s , -i diff.sql > %TEMP%\right.txt

windiff %TEMP%\__sqldiff\left %TEMP%\__sqldiff\right