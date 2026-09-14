@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo ERRORE: compilatore .NET Framework non trovato: %CSC%
  exit /b 1
)
if not exist "bin\Release" mkdir "bin\Release"
"%CSC%" /nologo /target:winexe /platform:x86 /optimize+ /debug:pdbonly /out:"bin\Release\Visupra7.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Configuration.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll Properties\AssemblyInfo.cs AppSettings.cs Localization.cs Logger.cs DirectShowInterop.cs MediaEventInterop.cs WebcamCapture.cs FfmpegRecorder.cs FullscreenPreviewForm.cs MainForm.cs Program.cs
if errorlevel 1 (
  echo ERRORE: build Release x86 fallita.
  exit /b 1
)
copy /y App.config "bin\Release\Visupra7.exe.config" >nul
echo Build Release x86 completata.
exit /b 0
