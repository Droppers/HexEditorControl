namespace HexControl.SharedControl.Framework.Visual;

internal class VisualElementTree
{
    public VisualElementTree(VisualElement root)
    {
        Root = root;
        State = new StateManager();
        Events = new EventManager(this);
    }

    public VisualElement Root { get; }
    public StateManager State { get; }
    public EventManager Events { get; }
}