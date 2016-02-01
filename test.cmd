@echo off
pushd src\Test
dotnet build
dotnet publish
pushd bin\Debug\dnxcore50\win7-x64
corerun "xunit.console.netcore.exe" "Test.dll" -trait Level=L0 -xml "testResults.xml"
popd
popd
