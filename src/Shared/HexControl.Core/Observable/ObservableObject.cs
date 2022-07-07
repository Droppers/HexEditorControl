using System.Runtime.CompilerServices;

namespace HexControl.Core.Observable;

public abstract class ObservableObject
{
    public event EventHandler<PropertyChangedEventArgs>? PropertyChanged;

    protected static TField Get<TField>(ref TField field) => field;

    protected void Set<TField>(ref TField field, TField newValue, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<TField>.Default.Equals(field, newValue))
        {
            return;
        }

        field = newValue;

        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }
}