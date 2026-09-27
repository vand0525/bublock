# RollingFileWriterTests

Checks `RollingFileWriter` against a temp folder with a fake clock:

- Daily file name `<base>-YYYYMMDD.log`.
- New UTC day opens a new file.
- Size roll to `<base>-YYYYMMDD.1.log` once the current file is full.
- After a restart, appends to the newest non-full file for the day.
