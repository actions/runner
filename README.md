
install .NET core by following instructions on https://dotnet.github.io/getting-started/


"build.cmd" will sync all packages and build everything but the tests

"run.cmd" will run the vstsworker

"test.cmd" will sync test packages, build and run the xunit tests. Run "build" before "test".
the failed tests are printed, the full results are in src\tests\bin\Debug\dnxcore50\win7-x64\testResults.xml

"clean.cmd" will delete all temporary files -> should be run before commiting code
