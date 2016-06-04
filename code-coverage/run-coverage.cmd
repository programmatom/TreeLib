mkdir results
"%ProgramFiles(x86)%\OpenCover\OpenCover.Console.exe" -register:user -target:..\TreeLib\TreeLibTest\bin\Debug\TreeLibTest.exe -output:results\output.xml -returntargetcode -log:Off
mkdir report
"%ProgramFiles(x86)%\ReportGenerator_2.1.8.0\bin\ReportGenerator.exe" -reports:results\output.xml -targetdir:report
start report\index.htm
