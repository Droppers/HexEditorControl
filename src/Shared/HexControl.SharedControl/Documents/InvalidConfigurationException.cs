using JetBrains.Annotations;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public class InvalidConfigurationException : Exception
{
    public InvalidConfigurationException(string property, string message) : base(message)
    {
        Property = property;
    }

    public string Property { get; }
}
