build: TischeInstaller.exe

TischeInstaller.exe: tische.nsi
	set Configuration=$(CONFIGURATION)
	makensis tische.nsi

clean:
	-del TischeInstaller.exe

rebuild: clean build
