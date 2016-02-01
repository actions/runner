@echo off
dotnet restore src/Microsoft.VisualStudio.Services.Agent
dotnet restore src/Worker
dotnet restore src/Agent
dotnet restore src/Test