set /p versionNumber=

REM clean up
del builds\FsEye-%versionNumber%.zip
rd /q /s builds\FsEye-%versionNumber%
rd /q /s builds\FsEye

REM preparing staging dir
mkdir staging
mkdir staging\plugins

copy LICENSE staging
copy NOTICE staging

copy FsEye\bin\Release\FsEye.fsx staging
copy FsEye\bin\Release\FsEye.dll staging
copy FsEye\bin\Release\FsEye.xml staging

copy "FsEye.PropertyGrid.Plugin\bin\Release\FsEye.PropertyGrid.Plugin.dll" staging\plugins
copy "FsEye.TreeView.Plugin\bin\Release\FsEye.TreeView.Plugin.dll" staging\plugins

REM zip staging files
cd staging
"..\tools\7z\7za.exe" a -tzip "..\builds\FsEye-%versionNumber%.zip" *
cd ..

REM extract build
"tools\7z\7za.exe" x "builds\FsEye-%versionNumber%.zip" -o"builds\FsEye-%versionNumber%"
"tools\7z\7za.exe" x "builds\FsEye-%versionNumber%.zip" -o"builds\FsEye"

REM clean up
rd /q /s staging

pause