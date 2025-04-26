REM Build the solution
dotnet build src\ArchiveCacheManager.sln

REM Create the Release folder
mkdir Release

REM Copy the Plugin files
copy /Y src\Plugin\bin\Debug\net9.0-windows Release

REM Copy the ArchiveCacheManager files
copy /Y src\ArchiveCacheManager\bin\Debug\net9.0-windows Release\

REM Copy the documentation files
copy /Y HISTORY.md Release\history.txt
copy /Y README.md Release\release.txt

REM Copy the 7-zip files
mkdir Release\7-Zip
copy /Y thirdparty\7-Zip\* Release\7-Zip
move /Y Release\7-Zip\7z.exe Release\7-Zip\7z.exe.original
move /Y Release\7-Zip\7z.dll Release\7-Zip\7z.dll.original

REM Copy the badges
mkdir Release\Badges
copy /Y src\Plugin\Resources\Badges Release\Badges

REM Create the output zip
del *.zip
thirdparty\7-Zip\7z.exe a ArchiveCacheManager.zip Release\*