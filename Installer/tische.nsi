!include "MUI2.nsh"

Name "Tisch"
OutFile "TischeInstall-$%Configuration%.exe"
InstallDir "$PROGRAMFILES\Tische"

!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
  
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "Tische command-line utility"
	SetOutPath "$INSTDIR"
	File ..\Tische\bin\$%Configuration%\*.config
	File ..\Tische\bin\$%Configuration%\*.exe
	File ..\Tische\bin\$%Configuration%\*.dll
	File ..\Tische\config-sample.xml
	File ..\Tische\config.xsd
	WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Uninstall"
	Delete "$INSTDIR\*.exe"
	Delete "$INSTDIR\*.dll"
	RMDir "$INSTDIR"
SectionEnd