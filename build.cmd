@echo off
if exist deploy rd /s /q deploy
dotnet publish -o deploy -c Release
