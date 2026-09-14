using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace Visupra7
{
    internal static class Localization
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"AppSubtitle", "CAMERA STUDIO  /  WINDOWS 7"}, {"Language", "Language"}, {"Offline", "OFFLINE"},
            {"SourceVideo", "VIDEO SOURCE"}, {"Webcam", "Webcam"}, {"DetectWebcam", "DETECT WEBCAMS"}, {"VideoFormat", "Video format"},
            {"Controls", "CONTROLS"}, {"StartPreview", "▶   START PREVIEW"}, {"Stop", "■   STOP"}, {"TakePhoto", "TAKE PHOTO"},
            {"StartRecording", "●   START RECORDING"}, {"StopAndSave", "■   STOP AND SAVE MP4"},
            {"StabilityHint", "The preview stays active even if\nthe encoder encounters an error."},
            {"Preview", "PREVIEW"}, {"NoSignal", "NO SIGNAL"}, {"NoVideoSource", "NO VIDEO SOURCE"},
            {"NoVideoHint", "Select a webcam and start the preview"}, {"Fullscreen", "FULL SCREEN"},
            {"DiagnosticLog", "DIAGNOSTIC LOG"}, {"Hide", "HIDE"}, {"Show", "SHOW"}, {"Ready", "READY"},
            {"Searching", "SEARCHING"}, {"NoCamera", "NO CAMERA"}, {"Connecting", "CONNECTING"}, {"Live", "LIVE"},
            {"Error", "ERROR"}, {"SignalLost", "SIGNAL LOST"}, {"DefaultFormat", "Camera default"},
            {"Detecting", "Detecting webcams..."}, {"NoCameraDetected", "No webcam detected"}, {"CamerasDetected", "{0} webcam(s) detected"},
            {"StartingCamera", "Starting webcam..."}, {"PreviewActive", "Preview active · {0}"}, {"CameraStartError", "Unable to start webcam"},
            {"CameraStartBody", "Unable to start the webcam.\r\n\r\n{0}"}, {"StoppingCamera", "Stopping webcam..."}, {"CameraStopped", "Webcam stopped"},
            {"WaitFirstFrame", "The first webcam frame is not available yet."}, {"ScreenshotSaved", "Screenshot saved · {0}"},
            {"ScreenshotError", "Screenshot error"}, {"RecordingStarted", "Recording · {0}"}, {"RecordingNotStarted", "Recording not started"},
            {"FinalizingMp4", "Finalizing MP4..."}, {"RecordingSaved", "Recording saved · {0}"}, {"RecordingFailed", "Recording failed"},
            {"RecordingError", "Recording error"}, {"FullscreenState", "Full screen · press Esc or F11 to exit"},
            {"PreviewRestored", "Preview restored"}, {"FullscreenError", "Full screen error"}, {"EncoderError", "Encoder error · preview is still active"},
            {"DeviceLost", "Webcam disconnected or stream interrupted"}, {"OverlayRecord", "●  REC"}, {"OverlayStop", "■  STOP"},
            {"OverlayCapture", "CAPTURE IMAGE"}, {"OverlayExit", "EXIT  ESC"}, {"OverlayReady", "READY"},
            {"DetectTip", "Refresh the DirectShow device list"}, {"ScreenshotTip", "Save the current frame as a JPEG image"}, {"RecordTip", "Record H.264 MP4 through FFmpeg"}
        };

        private static readonly Dictionary<string, string> Italian = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"AppSubtitle", "STUDIO CAMERA  /  WINDOWS 7"}, {"Language", "Lingua"}, {"Offline", "OFFLINE"},
            {"SourceVideo", "SORGENTE VIDEO"}, {"Webcam", "Webcam"}, {"DetectWebcam", "RILEVA WEBCAM"}, {"VideoFormat", "Formato video"},
            {"Controls", "CONTROLLI"}, {"StartPreview", "▶   AVVIA ANTEPRIMA"}, {"Stop", "■   FERMA"}, {"TakePhoto", "SCATTA FOTO"},
            {"StartRecording", "●   AVVIA REGISTRAZIONE"}, {"StopAndSave", "■   TERMINA E SALVA MP4"},
            {"StabilityHint", "L'anteprima resta attiva anche se\nl'encoder incontra un errore."},
            {"Preview", "ANTEPRIMA"}, {"NoSignal", "NESSUN SEGNALE"}, {"NoVideoSource", "NESSUNA SORGENTE VIDEO"},
            {"NoVideoHint", "Seleziona una webcam e avvia l'anteprima"}, {"Fullscreen", "SCHERMO INTERO"},
            {"DiagnosticLog", "LOG DIAGNOSTICO"}, {"Hide", "NASCONDI"}, {"Show", "MOSTRA"}, {"Ready", "PRONTO"},
            {"Searching", "RICERCA"}, {"NoCamera", "NESSUNA CAMERA"}, {"Connecting", "CONNESSIONE"}, {"Live", "LIVE"},
            {"Error", "ERRORE"}, {"SignalLost", "SEGNALE PERSO"}, {"DefaultFormat", "Predefinito webcam"},
            {"Detecting", "Rilevamento webcam in corso..."}, {"NoCameraDetected", "Nessuna webcam rilevata"}, {"CamerasDetected", "{0} webcam rilevate"},
            {"StartingCamera", "Avvio webcam..."}, {"PreviewActive", "Anteprima attiva · {0}"}, {"CameraStartError", "Errore avvio webcam"},
            {"CameraStartBody", "Impossibile avviare la webcam.\r\n\r\n{0}"}, {"StoppingCamera", "Arresto webcam..."}, {"CameraStopped", "Webcam ferma"},
            {"WaitFirstFrame", "Non è ancora disponibile un frame."}, {"ScreenshotSaved", "Screenshot salvato · {0}"},
            {"ScreenshotError", "Errore screenshot"}, {"RecordingStarted", "Registrazione in corso · {0}"}, {"RecordingNotStarted", "Registrazione non avviata"},
            {"FinalizingMp4", "Finalizzazione MP4..."}, {"RecordingSaved", "Registrazione salvata · {0}"}, {"RecordingFailed", "Registrazione fallita"},
            {"RecordingError", "Errore registrazione"}, {"FullscreenState", "Modalità schermo intero · Esc o F11 per uscire"},
            {"PreviewRestored", "Anteprima ripristinata"}, {"FullscreenError", "Errore schermo intero"}, {"EncoderError", "Encoder in errore · anteprima ancora attiva"},
            {"DeviceLost", "Webcam scollegata o flusso interrotto"}, {"OverlayRecord", "●  REC"}, {"OverlayStop", "■  STOP"},
            {"OverlayCapture", "ACQUISISCI IMMAGINE"}, {"OverlayExit", "ESCI  ESC"}, {"OverlayReady", "PRONTO"},
            {"DetectTip", "Aggiorna l'elenco dei dispositivi DirectShow"}, {"ScreenshotTip", "Salva il frame corrente in formato JPEG"}, {"RecordTip", "Registra MP4 H.264 tramite FFmpeg"}
        };

        private static string currentCode = "en";
        public static string CurrentCode { get { return currentCode; } }
        public static void SetLanguage(string code) { currentCode = string.Equals(code, "it", StringComparison.OrdinalIgnoreCase) ? "it" : "en"; }
        public static string T(string key, params object[] args)
        {
            string value; Dictionary<string, string> source = CurrentCode == "it" ? Italian : English;
            if (!source.TryGetValue(key, out value)) value = key;
            return args == null || args.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, args);
        }
        public static void Bind(Control control, string key) { control.Tag = "loc:" + key; control.Text = T(key); }
        public static void Apply(Control root)
        {
            string tag = root.Tag as string; if (tag != null && tag.StartsWith("loc:", StringComparison.Ordinal)) root.Text = T(tag.Substring(4));
            foreach (Control child in root.Controls) Apply(child);
        }
    }
}
