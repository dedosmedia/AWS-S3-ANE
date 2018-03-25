REM Get the path to the script and trim to get the directory.
@echo off
SET SZIP="C:\Program Files\7-Zip\7z.exe"
SET AIR_PATH="E:\DedosMedia\AIRSDK\WINDOWS\AIRSDK_29.0.0.112\bin\"
echo Setting path to current directory to:
SET pathtome=%~dp0
echo %pathtome%

SET projectName=AWSS3ANE

REM Setup the directory.
echo Making directories.

IF NOT EXIST %pathtome%platforms mkdir %pathtome%platforms
IF NOT EXIST %pathtome%platforms\win  %pathtome%platforms\win
IF NOT EXIST %pathtome%platforms\win\x86  %pathtome%platforms\win\x86
IF NOT EXIST %pathtome%platforms\win\x86\debug mkdir %pathtome%platforms\win\x86\debug

REM Copy SWC into place.
echo Copying SWC into place.
echo %pathtome%..\bin\%projectName%.swc
copy %pathtome%..\bin\%projectName%.swc %pathtome%

REM contents of SWC.
echo Extracting files form SWC.
echo %pathtome%%projectName%.swc
copy %pathtome%%projectName%.swc %pathtome%%projectName%Extract.swc
ren %pathtome%%projectName%Extract.swc %projectName%Extract.zip

call %SZIP% e %pathtome%%projectName%Extract.zip -o%pathtome%

del %pathtome%%projectName%Extract.zip

REM Copy library.swf to folders.
echo Copying library.swf into place.
copy %pathtome%library.swf %pathtome%platforms\win\x86\debug


REM Copy native libraries into place.
echo Copying native libraries into place.

copy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\%projectName%.dll %pathtome%platforms\win\x86\debug
copy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\%projectName%Lib.dll %AIR_PATH%%projectName%Lib.dll
copy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\%projectName%Lib.pdb %AIR_PATH%%projectName%Lib.pdb
copy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\%projectName%Lib.dll %pathtome%..\..\c_sharp_libs_x86\%projectName%Lib.dll
copy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\FreSharpCore.dll %AIR_PATH%FreSharpCore.dll
copy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\FreSharpCore.pdb %AIR_PATH%FreSharpCore.pdb
copy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\FreSharp.dll %AIR_PATH%FreSharp.dll
copy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\FreSharp.pdb %AIR_PATH%FreSharp.pdb
copy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\FreSharpCore.dll %pathtome%..\..\c_sharp_libs_x86\FreSharpCore.dll
copy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\FreSharp.dll %pathtome%..\..\c_sharp_libs_x86\FreSharp.dll


echo Saving a copy of required dlls 
xcopy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\*.dll %pathtome%..\..\c_sharp_libs_x86 /Y

echo Copying required dlls and pdb for DEBUG
xcopy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\*.dll %AIR_PATH%/Y
xcopy %pathtome%..\..\native_library\win\%projectName%\x86\Debug\*.pdb %AIR_PATH%/Y

REM Run the build command.
echo Building Debug.
call %AIR_PATH%adt.bat -package -target ane %pathtome%%projectName%.ane %pathtome%extension_win.xml -swc %pathtome%%projectName%.swc ^
-platform Windows-x86 -C %pathtome%platforms\win\x86\debug %projectName%.dll library.swf

call DEL /F /Q /A %pathtome%platforms\win\x86\debug\%projectName%.dll
call DEL /F /Q /A %pathtome%platforms\win\x86\debug\library.swf
call DEL /F /Q /A %pathtome%%projectName%.swc
call DEL /F /Q /A %pathtome%library.swf
call DEL /F /Q /A %pathtome%catalog.xml

echo FIN
