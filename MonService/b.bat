rmdir Publish /s/q
pause
dotnet clean
dotnet build
dotnet publish -c Release -o Publish