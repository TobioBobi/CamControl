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
                reportError(Texts.Error_Timeout);
            }
            catch (HttpRequestException exception)
            {
                reportError(exception.StatusCode is { } statusCode
                    ? Texts.Format(Texts.Error_Http, (int)statusCode)
                    : Texts.Error_Connection);
            }

            return false;
        }
    }
}
