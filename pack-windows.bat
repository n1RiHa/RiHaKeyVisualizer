@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

set /p version=<VERSION.txt

mkdir tmp
cd tmp
mkdir RIHaKeyVisualizer

copy /y "..\Info.json" "RIHaKeyVisualizer\"
copy /y "..\riha_on.png" "RIHaKeyVisualizer\"
copy /y "..\riha_left.png" "RIHaKeyVisualizer\"
copy /y "..\riha_right.png" "RIHaKeyVisualizer\"
copy /y "..\riha_off.png" "RIHaKeyVisualizer\"
copy /y "..\bin\Release\netstandard2.1\RIHaKeyVisualizer.dll" "RIHaKeyVisualizer\"

cd RIHaKeyVisualizer

for /f "delims=" %%a in (Info.json) do (
    SET s=%%a
    SET s=!s:$VERSION=%version%!
    echo !s! >> "..\InfoChanged.json"
)

del /f /q Info.json
move /y "..\InfoChanged.json" "Info.json"

cd ..

tar -c -f RIHaKeyVisualizer-%version%-netstandard2.1.zip RIHaKeyVisualizer

move /y RIHaKeyVisualizer-%version%-netstandard2.1.zip ..
cd ..

pause