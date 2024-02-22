rem @echo off
set local
echo %1

set arg1=%1
set arg1=%arg1:"=%

set __temppath=%TEMP:\=\\%
echo PATH:  %__temppath%

echo y | gen *.sql /O "sqlcmd %arg1% -W -s , -i $F" /x
