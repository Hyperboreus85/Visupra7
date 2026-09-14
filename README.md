# Visupra7

Applicazione WinForms x86 per Windows 7 SP1 e .NET Framework 4.8. Usa DirectShow nativo per webcam/anteprima, JPEG per gli screenshot e un processo FFmpeg isolato con pipe e coda limitata per MP4 H.264.

L'anteprima supporta la modalità a schermo intero tramite pulsante o doppio clic. In fullscreen rimane disponibile un overlay sempre visibile con REC, STOP, acquisizione immagine, durata registrazione e uscita tramite Esc/F11.

L'interfaccia è disponibile in inglese e italiano. Il primo avvio usa l'inglese (Language=en in App.config); la lingua può essere cambiata immediatamente dal selettore nell'header.

## Build e deploy

Eseguire `deploy-win7.bat`. La build non richiede Visual Studio o NuGet: usa il compilatore .NET Framework installato. Lo script prepara `dist` e la sincronizza in `\\192.168.10.123\Visupra7`.

La registrazione usa la build Win32 statica di FFmpeg inclusa in `Tools\ffmpeg.exe`; l'app non usa FFmpeg dal PATH. Versione, provenienza, licenza e checksum sono documentati in `Tools\README.md`. In assenza del binario, anteprima e screenshot restano disponibili e il tentativo di registrazione mostra un errore leggibile.

Le impostazioni sono in `App.config`: qualità JPEG, CRF/preset H.264, limite FPS, risoluzione preferita, directory e predisposizione della futura durata segmento (`SegmentMinutes`, attualmente 0/non attiva).

I file runtime vengono creati accanto all'eseguibile in `Screenshots`, `Recordings` e `Logs`.
