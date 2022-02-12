using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.SharedControl.Framework.Visual;

internal class VisualElementTree
{
    public VisualElementTree(VisualElement root)
    {
        Root = root;
        State = new StateManager();
        Events = new EventManager(this);
        DirtyRectangles = new List<SharedRectangle>();
    }

    public VisualElement Root { get; }
    public StateManager State { get; }
    public EventManager Events { get; }

    public List<SharedRectangle> DirtyRectangles { get; }

    public SharedRectangle? DirtyRect
    {
        get
        {
            switch (DirtyRectangles.Count)
            {
                case 0:
                    return null;
                case 1:
                    return DirtyRectangles[0];
                case 2:
                    return SharedRectangle.Union(DirtyRectangles[0], DirtyRectangles[1]);
            }

            var dirtyRect = DirtyRectangles[0];
            for (var i = 1; i < DirtyRectangles.Count; i++)
            {
                dirtyRect = SharedRectangle.Union(dirtyRect, DirtyRectangles[i]);
            }

            return dirtyRect;
        }
    }

    public void AddDirtyRect(SharedRectangle rectangle)
    {
        DirtyRectangles.Add(rectangle);
    }

    public void ClearDirtyRect()
    {
        DirtyRectangles.Clear();
    }
}