
:try
SET /A tries=3

:loop
IF %tries% LEQ 0 GOTO return

01FClientSync sync
IF %ERRORLEVEL% EQU 0 GOTO return

SET /A tries-=1
GOTO loop

:return
EXIT /B