set /p versionNumber=

REM clean up
del builds\FsEye-%versionNumber%.zip
del builds\FsEye.%versionNumber%.nupkg
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
copy "FsEye.DataGridView.Plugin\bin\Release\FsEye.DataGridView.Plugin.dll" staging\plugins
copy "FsEye.LinqPad.Plugin\bin\Release\FsEye.LinqPad.Plugin.dll" staging\plugins
copy "FsEye.LinqPad.Plugin\bin\Release\LINQPad.exe" staging\plugins

REM zip staging files
cd staging
"..\tools\7z\7za.exe" a -tzip "..\builds\FsEye-%versionNumber%.zip" *
cd ..

REM extract build
"tools\7z\7za.exe" x "builds\FsEye-%versionNumber%.zip" -o"builds\FsEye-%versionNumber%"
"tools\7z\7za.exe" x "builds\FsEye-%versionNumber%.zip" -o"builds\FsEye"

REM preparing nuget dirs

mkdir nuget
mkdir nuget\content
mkdir nuget\lib
mkdir nuget\lib\net40
mkdir nuget\lib\net40\plugins
copy FsEye.nuspec nuget

REM copy staging builds to nuget package...

copy staging\FsEye.dll nuget\lib\net40\FsEye.dll
copy staging\plugins\* nuget\lib\net40\plugins\
copy FsEye\bin\Release\FsEye.NuGet.fsx nuget\content\FsEye.fsx

REM create nuget package...

".nuget\nuget.exe" pack nuget\FsEye.nuspec -Version %versionNumber%
copy FsEye.%versionNumber%.nupkg builds
del FsEye.%versionNumber%.nupkg

REM cleanup...

rd /q /s staging
rd /q /s nuget

pause
