using JetBrains.Annotations;

namespace HexControl.SharedControl.Framework.Visual;

[PublicAPI]
internal class StateManager
{
    public enum ElementState
    {
        Captured,
        Focused
    }

    private readonly Dictionary<ElementState, VisualElement?> _states = new();

    public event EventHandler<StateOwnerChangedEventArgs>? StateOwnerChanged;

    public VisualElement? CapturedElement => GetElement(ElementState.Captured);
    public VisualElement? FocusedElement => GetElement(ElementState.Focused);

    private bool IsOwner(ElementState state, VisualElement element) =>
        _states.TryGetValue(state, out var capturedElement) && ReferenceEquals(element, capturedElement);

    private void BecomeOwner(ElementState state, VisualElement? element)
    {
        if (_states.TryGetValue(state, out var currentOwner) && ReferenceEquals(currentOwner, element))
        {
            return;
        }

        _states[state] = element;
        StateOwnerChanged?.Invoke(this, new StateOwnerChangedEventArgs(state, currentOwner, element));
    }

    private VisualElement? GetElement(ElementState state) =>
        _states.TryGetValue(state, out var element) ? element : null;

    public bool IsCaptured(VisualElement element) => IsOwner(ElementState.Captured, element);

    public void Capture(VisualElement element)
    {
        BecomeOwner(ElementState.Captured, element);
    }

    public void ReleaseCapture()
    {
        BecomeOwner(ElementState.Captured, null);
    }

    public bool IsFocused(VisualElement element) => IsOwner(ElementState.Focused, element);

    public void Focus(VisualElement element)
    {
        BecomeOwner(ElementState.Focused, element);
    }

    public void ReleaseFocus()
    {
        BecomeOwner(ElementState.Focused, null);
    }

    public void ClearState(VisualElement element)
    {
        if (ReferenceEquals(CapturedElement, element))
        {
            ReleaseCapture();
        }

        if (ReferenceEquals(FocusedElement, element))
        {
            ReleaseFocus();
        }
    }

    public class StateOwnerChangedEventArgs : EventArgs
    {
        public StateOwnerChangedEventArgs(ElementState state, VisualElement? oldOwner, VisualElement? newOwner)
        {
            State = state;
            OldOwner = oldOwner;
            NewOwner = newOwner;
        }

        public ElementState State { get; }
        public VisualElement? OldOwner { get; }
        public VisualElement? NewOwner { get; }
    }
}