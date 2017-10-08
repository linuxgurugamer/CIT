
set H=R:\KSP_1.3.1_dev
echo %H%

copy bin\Debug\BAM.dll ..\..\GameData\CIT\BAM\Plugins

cd ..\..\GameData
xcopy /y /s /i CIT %H%\GameData\CIT