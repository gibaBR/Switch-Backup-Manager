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

![main](https://i.imgur.com/7D7MXPK.png)

![main](https://i.imgur.com/5RipVQI.png)

![main](https://i.imgur.com/eHKzI2R.png)

## Changelog

v 1.0.5
* You can now configure the autorename pattern using tags
* Preparing for next release, when more info will be retrieved from "scene" database
* **IMPORTANT: Its a good idea to redo your local database as on next release there will be required information 
  **that starts to be stored on this version (1.0.5). This way the transition will be smoother.**
* Some more code refactoring

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
* [Release 1.0](https://github.com/gibaBR/Switch-Backup-Manager/files/2188652/Switch.Backup.Manager.v1.0.zip)


