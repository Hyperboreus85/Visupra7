@echo off
setlocal
cd /d "%~dp0"
call build-release.bat
if errorlevel 1 exit /b 1

if exist "dist" rmdir /s /q "dist"
mkdir "dist"
copy /y "bin\Release\Visupra7.exe" "dist\Visupra7.exe" >nul || goto :copyerror
copy /y "bin\Release\Visupra7.exe.config" "dist\Visupra7.exe.config" >nul || goto :copyerror
if exist "Tools\ffmpeg.exe" (
  mkdir "dist\Tools"
  copy /y "Tools\ffmpeg.exe" "dist\Tools\ffmpeg.exe" >nul || goto :copyerror
) else (
  echo AVVISO: Tools\ffmpeg.exe non presente. Anteprima e screenshot funzioneranno, registrazione disabilitata.
)

if not exist "dist\Visupra7.exe" goto :verifyerror
if not exist "dist\Visupra7.exe.config" goto :verifyerror
if not exist "\\192.168.10.123\Visupra7" mkdir "\\192.168.10.123\Visupra7"
robocopy "dist" "\\192.168.10.123\Visupra7" /E /R:2 /W:2 /NFL /NDL /NJH /NJS
if errorlevel 8 (
  echo ERRORE: deploy Robocopy fallito con codice %ERRORLEVEL%.
  exit /b 1
)
if not exist "\\192.168.10.123\Visupra7\Visupra7.exe" (
  echo ERRORE: Visupra7.exe non presente a destinazione dopo Robocopy.
  exit /b 1
)
if not exist "\\192.168.10.123\Visupra7\Visupra7.exe.config" (
  echo ERRORE: configurazione non presente a destinazione dopo Robocopy.
  exit /b 1
)
fc /b "dist\Visupra7.exe" "\\192.168.10.123\Visupra7\Visupra7.exe" >nul
if errorlevel 1 (
  echo ERRORE: verifica binaria dell'eseguibile distribuito fallita.
  exit /b 1
)
echo Deploy completato in \\192.168.10.123\Visupra7
exit /b 0

:copyerror
echo ERRORE: copia dei file in dist fallita.
exit /b 1
:verifyerror
echo ERRORE: verifica dei file dist fallita.
exit /b 1
