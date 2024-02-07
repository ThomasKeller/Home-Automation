namespace HA.Service.Settings;

public class ApplicationSettingsException : Exception
{
    public string? ErrorSummary { get; protected set; }

    public ApplicationSettingsException(string errorSummary)
    {
        ErrorSummary = errorSummary;
    }

    public ApplicationSettingsException(string message, string errorSummary)
        : base(message)
    {
        ErrorSummary = errorSummary;
    }

    public ApplicationSettingsException(string message, Exception innerException, string errorSummary)
        : base(message, innerException)
    {
        ErrorSummary = errorSummary;
    }
}