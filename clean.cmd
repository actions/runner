@echo off
pushd src\vstsworker
rmdir bin /s /q
rmdir obj /s /q
del project.lock.json
popd
 
pushd src\tests
rmdir bin /s /q
rmdir obj /s /q
del project.lock.json
popd

pushd src\corelib
rmdir bin /s /q
rmdir obj /s /q
del project.lock.json
popd
