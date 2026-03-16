@echo off
echo Running C# data generator...
cd CSharpGenerator
dotnet run
cd ..
echo.
echo Done! Check the /data folder for your CSV files.
pause
