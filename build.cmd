@echo off
if exist deploy rd /s /q deploy
vs-clean .
dotnet build -c Release
dotnet publish -o deploy -c Release
