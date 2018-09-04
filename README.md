# Switch-Backup-Manager
Complete Switch Backups management tool


[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=WR5ZZ7RH55NTG)

## Main Features
* Manage your local (XCI & NSP) and SD card Switch backups
* See game info retrieved from web or edit them manually
* Group and sort files list
* Keep track of scene releases using nswdb.com database
* Trim your files
* Auto rename your files using a user define pattern
* Copy and move files between your local collection and SD card (either way)

## Requirements
* .NET 4.6
* Visual C++ Redistributable for Visual Studio 2015

## Screenshots

![main](https://i.imgur.com/Nwbj0oj.png)

![main](https://i.imgur.com/KZzojbS.png)

![main](https://i.imgur.com/1MDpIr9.png)

![main](https://i.imgur.com/M4tmrN0.png)

![main](https://i.imgur.com/AtkY36y.png)


## Changelog

* 1.1.6
  - Fix: Unable to process 'Scene' NSP releases #45. Thanks to garoxas;
  - Filename on the lists now shows only filename by default, user can however choose to show complete path in options;
  - Highlights on the scene list the files you already have (NSP, XCI or both) with custom colors;
  - Now you can donate using paypal - if you want to. Link is on github page :-)

* 1.1.5
  - Fix #50: New autorenaming pattern is not used at runtime;
  - Fix #52: NSWDB site changed something that prevents the program of downloading their xml file. 

* 1.1.4
  - Fix #47: Some NSP update files were showing errors when adding to database (010065e003fd8800, 0100830004fb6800, 0100760002048800, 01005ee0036ec800, ..)
  - fix XCI files skipped when version number is non standard, fix #42 #44

* 1.1.3
  - get correct XCI Game Revision, fix #37 by @garoxas / @Garou;
  - Fixed filter for content type (dlc, base game, update) not working. Also, this filter is now saved on program preferences (will persist between sessions);
  - Separate renaming paterns for XCI and NSP files;
  - User can now limit filename size for NSP files.

* 1.1.2
  - Correct text format for game description by @garoxas / @Garou
  - Fixed #38 (Issue witch manual scrape.)
  - Autoremove missing files at startup is now optional (config)
  - Fixed wrong sumary when using filters
  - Add DLC, Update and Base game Filter on E-shop list
  - Fix minor error on auto scan folder, where scan was stopped when a single file failed
  - Fix for program check to the minimum DB version.

* 1.1.1
  - Now you can add NSP titles with the same TitleID. Thanks to @garoxas
  - add support for multiple XCI revisions Thanks to @garoxas
  - Game information like description, release date, nº o players, publisher and categories can now be scraped from web (optional, can be set on config screen)
  - User can manually edit information of the games.
  - Fixed #32 (Auto renaming problem)
  - Add CDNSP Renaming pattern (Detects if its a base game, DLC our Update)

* 1.1.0
  - nstoolmod.exe is no more! Thanks @iriez!
  - Lists position, sort order, size, Window Size and position, etc, are now saved when you exit the program.
  - many bugs removed.

* 1.0.9
  - Now NSP files works fast! Using nstoolmod.exe (provided with this release). Thanks to StudentBlake!!
  - Removed Fixed [DLC] [UPD] and [Version] from NSP Files (Issue #22). Added those options to renaming patterns in config screen. 
  - Changed Scene ID format to NNNN. Change affects auto renaming feature (Issue #21).

* 1.0.8
  - **Now you can configure folders to autoscan at startup** (FinnYoung's suggestion)
  - Adds new info to Database: Scene ID.
  - Adds more options to autorename patterns (Scene ID and Languages) (MR_TeCKnO's suggestion).
  - **NSP Games now have [DLC] and [UPD] on their names in accord to the content. Version is there too.**
  - Better control over database version matching application version (sorry, you will need to redo your database with this update).
  - Log is now shown inside the application.

* 1.0.7
  - Fixed  an error where title has Taiwanese as its only language (TitleID 0100D7700AF88000)
  - Game titles name are now retrieved from scene list as its more  user friendly (no more chinese names that causes problems when loading on switch)

* 1.0.6
  - Support for NSP (e-shop) files. Very experimental as it is very slow to scrap big files.
  - More configurations on File->Options menu
  - Auto update scene list on startup (optional)
  - Now you can copy any game information to clipboard (mouse over cell)
  - Some more code refactoring (if you had some problem adding files, please try again now)

* 1.0.5
  - You can now configure the autorename pattern using tags
  - Preparing for next release, when more info will be retrieved from "scene" database
  - **IMPORTANT: Its a good idea to redo your local database as on next release there will be required information**
    **that starts to be stored on this version (1.0.5). This way the transition will be smoother.**
  - Some more code refactoring

* 1.0.4
  - Adds filter to the games lists
  - Autorename now removes others special chars that may cause problems with SX OS like " ™ " and " ® "
  - ~~Bug when trying to rename splited files was resolved~~

* 1.0.3
  - Solves issue #2 where program doesnt add any files when you try to add two files with the same TitleID;
  - Adds some log information to help track errors;
  - Known bug: backups of Title 01009AA000FAA000 seems not to work for now. This backup will not be added to the list.

* 1.0.1 - Corrects a bug with invalid file names

* 1.0 - Initial release


## Source & Binaries
* [GitHub](https://github.com/gibaBR/Switch-Backup-Manager/archive/master.zip)
* [Release 1.1.5](https://github.com/gibaBR/Switch-Backup-Manager/releases/tag/v1.1.5)


