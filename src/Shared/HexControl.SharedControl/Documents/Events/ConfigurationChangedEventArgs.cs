using JetBrains.Annotations;

namespace HexControl.SharedControl.Documents.Events;

[PublicAPI]
public class ConfigurationChangedEventArgs : EventArgs
{
    public ConfigurationChangedEventArgs(DocumentConfiguration oldConfiguration, DocumentConfiguration newConfiguration,
        string[] changes)
    {
        OldConfiguration = oldConfiguration;
        NewConfiguration = newConfiguration;
        Changes = changes;
    }

    public DocumentConfiguration OldConfiguration { get; }
    public DocumentConfiguration NewConfiguration { get; }
    public string[] Changes { get; }
}