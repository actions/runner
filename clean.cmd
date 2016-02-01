@echo off
pushd src\Worker
rmdir bin /s /q
rmdir obj /s /q
del project.lock.json
popd

@echo off
pushd src\Agent
rmdir bin /s /q
rmdir obj /s /q
del project.lock.json
popd

pushd src\Test
rmdir bin /s /q
rmdir obj /s /q
del project.lock.json
popd

pushd src\Microsoft.VisualStudio.Services.Agent
rmdir bin /s /q
rmdir obj /s /q
del project.lock.json
popd
