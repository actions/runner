@echo off
dotnet restore src/corelib 
dotnet restore src/vstsworker 
dotnet build src/vstsworker