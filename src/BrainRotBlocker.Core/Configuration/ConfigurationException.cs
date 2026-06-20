namespace BrainRotBlocker.Core.Configuration;

/// <summary>
/// Thrown when a configuration is structurally invalid: malformed patterns,
/// duplicate identifiers, or rules that reference a budget group that does not
/// exist.
/// </summary>
public sealed class ConfigurationException : Exception
{
    public ConfigurationException(string message)
        : base(message)
    {
    }

    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
