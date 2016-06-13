@echo off

set PROGRAMARGS=

set OPENCOVER=%ProgramFiles(x86)%\OpenCover\OpenCover.Console.exe
set REPORTGEN=%ProgramFiles(x86)%\ReportGenerator_2.1.8.0\bin\ReportGenerator.exe
mkdir results 2>nul
"%OPENCOVER%" "-excludebyattribute:(*.ExcludeFromCodeCoverageAttribute)" -safemode:off -threshold:9999 -register:user "-target:..\TreeLib\TreeLibTest\bin\Debug\TreeLibTest.exe" "-targetargs:%PROGRAMARGS%" -output:results\output.xml -returntargetcode -log:Fatal

mkdir report 2>nul
"%REPORTGEN%" -reports:results\output.xml -targetdir:report
start report\index.htm
