@echo off

set PROGRAMARGS=%1 %2 %3 %4 %5 %6 %7 %8 %9

set OPENCOVER=%ProgramFiles(x86)%\OpenCover\OpenCover.Console.exe
set REPORTGEN=%ProgramFiles(x86)%\ReportGenerator_2.1.8.0\bin\ReportGenerator.exe

mkdir results 2>nul
mkdir report 2>nul
call clean-coverage.cmd

"%OPENCOVER%" "-excludebyattribute:(*.ExcludeFromCodeCoverageAttribute)" -safemode:off -threshold:9999 -register:user "-target:..\TreeLib\TreeLibTest\bin\Debug\TreeLibTest.exe" "-targetargs:%PROGRAMARGS%" -output:results\output.xml -returntargetcode -log:Fatal

"%REPORTGEN%" -reports:results\output.xml -targetdir:report
start report\index.htm
