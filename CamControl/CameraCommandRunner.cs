namespace CamControl
{
    internal static class CameraCommandRunner
    {
        public static async Task<bool> RunAsync(Func<Task> command, Action<string> reportError)
        {
            try
            {
                await command();
                return true;
            }
            catch (OperationCanceledException)
            {
                reportError("Die Kamera hat nicht rechtzeitig geantwortet. Bitte Netzwerk und Kamera-Adresse prüfen.");
            }
            catch (HttpRequestException exception)
            {
                reportError(exception.StatusCode is { } statusCode
                    ? $"Die Kamera hat den Befehl mit HTTP {(int)statusCode} abgelehnt. Bitte Kamera-Adresse und Kamera-Konfiguration prüfen."
                    : "Die Verbindung zur Kamera ist fehlgeschlagen. Bitte Netzwerk und Kamera-Adresse prüfen.");
            }

            return false;
        }
    }
}
