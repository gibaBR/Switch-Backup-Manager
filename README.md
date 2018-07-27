# Switch-Backup-Manager
Complete Switch Backups management tool

## Main Features
* Manage your local and SD card Switch backups
* See game info (just image for now, but more is comming)
* Group and sort files list
* Keep track of scene releases using nswdb.com database
* Trim your files
* Auto rename your files to match game name (other options in near future)
* Copy and move files between your local collection and SD card (either way)

## Requirements
* .NET 4.6

## Screenshots

![main](https://i.imgur.com/SHlcuGp.png)

![main](https://i.imgur.com/dH5t3Vi.png)

![main](https://i.imgur.com/Ufhn64p.png)

![main](https://i.imgur.com/dHRvZ4J.png)

![main](https://i.imgur.com/3Qyat3W.png)

![main](https://i.imgur.com/vENGuJn.png)

![main](https://i.imgur.com/wABgTLS.png)

## Changelog

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
* [Release 1.0.8](https://github.com/gibaBR/Switch-Backup-Manager/releases/tag/v1.0.8)


