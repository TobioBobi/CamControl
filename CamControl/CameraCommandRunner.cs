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
                reportError("Kamera antwortet nicht rechtzeitig. IP-Adresse und Netzwerk prüfen.");
            }
            catch (HttpRequestException exception)
            {
                reportError(exception.StatusCode is { } statusCode
                    ? $"Befehl abgelehnt (HTTP {(int)statusCode}). IP-Adresse und Kamerakonfiguration prüfen."
                    : "Keine Verbindung zur Kamera. IP-Adresse, Netzwerk und Stromversorgung prüfen.");
            }

            return false;
        }
    }
}
