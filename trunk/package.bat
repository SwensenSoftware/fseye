set /p versionNumber=

rd /q /s builds\FsEye-%versionNumber%

mkdir builds\FsEye-%versionNumber%
mkdir builds\FsEye-%versionNumber%\plugins

copy LICENSE builds\FsEye-%versionNumber%
copy NOTICE builds\FsEye-%versionNumber%

copy FsEye\bin\Release\FsEye.fsx builds\FsEye-%versionNumber%
copy FsEye\bin\Release\FsEye.dll builds\FsEye-%versionNumber%
copy FsEye\bin\Release\FsEye.xml builds\FsEye-%versionNumber%

copy "FsEye.PropertyGrid.Plugin\bin\Release\FsEye.PropertyGrid.Plugin.dll" builds\FsEye-%versionNumber%\plugins
copy "FsEye.TreeView.Plugin\bin\Release\FsEye.TreeView.Plugin.dll" builds\FsEye-%versionNumber%\plugins

pause