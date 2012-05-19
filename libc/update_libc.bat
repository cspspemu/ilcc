@echo off
CD "%~dp0"
..\utils\7z a include.zip -mx0 include > NUL
copy /Y include.zip ..\ilcc\ilcc.Include\Resources\include.zip 2> NUL > NUL