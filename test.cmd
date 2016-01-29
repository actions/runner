@echo off
pushd src\tests
dotnet restore
dotnet build
dotnet publish
pushd bin\Debug\dnxcore50\win7-x64
corerun "xunit.console.netcore.exe" "tests.dll" -xml "testResults.xml" -notrait category=failing
popd
popd
