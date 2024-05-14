@echo off

pushd %~dp0
set script_dir=%CD%

for /f "tokens=1,2 delims=:," %%a in ('findstr "version" "%cd%\package.json"') do ( set version=%%b&goto :break1)
:break1
for /f "tokens=1,2 delims=:," %%a in ('findstr "name" "%cd%\package.json"') do ( set name=%%b&goto :break2)
:break2

if not defined name (
goto :endBreak
)
if not defined version (
goto :endBreak
)
set name=%name:"=%
set name=%name: =%
set version=%version:"=%
set version=%version: =%
set branchTag=%name%-%version%

set gitRootPath=%cd:\Assets\=\,%
FOR /f "tokens=1,2 delims=," %%a IN ("%gitRootPath%") do ( set relativePath=Assets\%%b&set gitRootPath=%%a&goto :break3)
:break3

@echo on
cd "%gitRootPath%"
git subtree split --prefix="%relativePath:\=/%" --branch %name%
git tag -d %branchTag%
git rm --cached ".\%relativePath%\upm-push.cmd"
git rm --cached ".\%relativePath%\upm-push.cmd.meta"
git push origin :refs/tags/%branchTag%
git tag "%branchTag%" %name%
git push origin %name% --tags

:endBreak
@echo off
SET /p exit=Press any key to exit