
set H=R:\KSP_1.3.0_dev
echo %H%

copy bin\Debug\BAM.dll ..\..\GameData\CIT\BAM\Plugins

cd ..\..\GameData
xcopy /y /s CIT %H%\GameData\CIT