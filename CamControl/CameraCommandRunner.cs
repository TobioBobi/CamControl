namespace CamControl
{
    internal static class CameraCommandRunner
    {
        public static async Task<bool> RunAsync(Func<Task> command, Action<string> reportError,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await command();
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Deliberate cancellation is not a network timeout.
                return false;
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
