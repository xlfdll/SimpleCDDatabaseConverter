# SimpleCD Database Converter
A converter that converts SimpleCD Desktop database to the format used in [VeryCD Offline Web Service](https://github.com/xlfdll/VeryCDOfflineWebService).

## System Requirements
* .NET Framework 4.7.2+

[Runtime configuration](https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-configure-an-app-to-support-net-framework-4-or-4-5) is needed for running on other .NET Framework versions.

## Usage
**Before using the converter, make sure SimpleCD Desktop is patched to the latest version so that the database is updated.**

```
SimpleCDDatabaseConverter <source database file> <target database file>
```
* **\<source database file\>** - SimpleCD Desktop SQLite database (usually with the name **verycd.sqlite3.db**)
* **\<target database file\>** - VeryCD Offline Web Service SQLite database (should be named **main.db** by default)

Notice that this converter only handles main database for items. **Comment database is not supported by the converter, nor VeryCD Offline Web Service.**

**This repository does NOT provide either original or converted database files.**

## Development Prerequisites
* Visual Studio 2015+

Before the build, generate-build-number.sh needs to be executed in a Git / Bash shell to generate build information code file (BuildInfo.cs).
