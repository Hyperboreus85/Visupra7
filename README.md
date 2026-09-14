# Visupra7

Applicazione WinForms x86 per Windows 7 SP1 e .NET Framework 4.8. Usa DirectShow nativo per webcam/anteprima, JPEG per gli screenshot e un processo FFmpeg isolato con pipe e coda limitata per MP4 H.264.

## Build e deploy

Eseguire `deploy-win7.bat`. La build non richiede Visual Studio o NuGet: usa il compilatore .NET Framework installato. Lo script prepara `dist` e la sincronizza in `\\win7\Dev\Visupra7`.

La registrazione richiede una build **x86 compatibile con Windows 7** di FFmpeg, collocata in `Tools\ffmpeg.exe`. L'app non usa FFmpeg dal PATH. In assenza di FFmpeg, anteprima e screenshot restano disponibili e il tentativo di registrazione mostra un errore leggibile.

Le impostazioni sono in `App.config`: qualità JPEG, CRF/preset H.264, limite FPS, risoluzione preferita, directory e predisposizione della futura durata segmento (`SegmentMinutes`, attualmente 0/non attiva).

I file runtime vengono creati accanto all'eseguibile in `Screenshots`, `Recordings` e `Logs`.
