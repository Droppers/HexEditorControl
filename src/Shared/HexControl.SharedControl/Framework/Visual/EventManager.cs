using System.Runtime.CompilerServices;
using HexControl.Core.Helpers;

namespace HexControl.SharedControl.Framework.Visual;

public enum EventStrategy
{
    Bubble,
    Tunnel
}

internal class EventManager
{
    private static readonly ObjectPool<Queue<VisualElement>> QueuePool = new(20);

    private readonly Dictionary<VisualElement, Dictionary<string, List<EventHandler<HandledEventArgs>>>>
        _elementEventHandlers;

    private readonly VisualElementTree _tree;

    public EventManager(VisualElementTree tree)
    {
        _tree = tree;
        _elementEventHandlers =
            new Dictionary<VisualElement, Dictionary<string, List<EventHandler<HandledEventArgs>>>>();
    }


    public void AddHandler<T>(
        VisualElement element,
        EventHandler<T>? handler,
        [CallerMemberName] string? name = null)
        where T : HandledEventArgs
    {
        name = name ?? throw new ArgumentNullException(nameof(name));
        if (handler is null)
        {
            return;
        }

        if (!_elementEventHandlers.TryGetValue(element, out var events))
        {
            events = new Dictionary<string, List<EventHandler<HandledEventArgs>>>();
            _elementEventHandlers.Add(element, events);
        }

        if (!events.TryGetValue(name, out var eventHandlers))
        {
            eventHandlers = new List<EventHandler<HandledEventArgs>>();
            events.Add(name, eventHandlers);
        }

        // Wrap the delegate invocation in another delegate to allow the event args to be cast
        eventHandlers.Add((sender, e) => handler.Invoke(sender, (T)e));
    }

    public void RemoveHandler<T>(
        VisualElement element,
        EventHandler<T>? removeHandler,
        [CallerMemberName] string? name = null)
        where T : HandledEventArgs
    {
        name = name ?? throw new ArgumentNullException(nameof(name));
        if (removeHandler is null)
        {
            return;
        }

        if (!_elementEventHandlers.TryGetValue(element, out var dict) || !dict.TryGetValue(name, out var handlers)) { }

        // TODO: Implement removal of event handlers, not possible with current way of storing them. Not important for now.
        //handlers.Remove(removeHandler);
    }

    public bool ClearHandlers(VisualElement element) => _elementEventHandlers.Remove(element);

    public IReadOnlyList<EventHandler<HandledEventArgs>> GetHandlers(VisualElement element, string name)
    {
        if (!_elementEventHandlers.TryGetValue(element, out var dict) || !dict.TryGetValue(name, out var handlers))
        {
            return Array.Empty<EventHandler<HandledEventArgs>>();
        }

        return handlers;
    }

    public void Raise(
        VisualElement source,
        string name,
        HandledEventArgs args,
        EventStrategy strategy = EventStrategy.Bubble)
    {
        switch (strategy)
        {
            case EventStrategy.Bubble:
                Bubble(source, name, source, args);
                break;
            case EventStrategy.Tunnel:
                Tunnel(source, name, source, args);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
        }
    }

    public void Raise(
        VisualElement source,
        string name,
        HandledEventArgs args,
        Func<VisualElement, bool> predicate,
        EventStrategy strategy = EventStrategy.Bubble)
    {
        switch (strategy)
        {
            case EventStrategy.Bubble:
            {
                var target = GetTargetElement(_tree.Root, predicate);
                Bubble(target, name, source, args);
                break;
            }
            case EventStrategy.Tunnel:
                Tunnel(source, name, source, args);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
        }
    }

    private void Tunnel(VisualElement startElement, string name, object sender, HandledEventArgs args)
    {
        var queue = QueuePool.Rent();
        try
        {
            queue.Clear();
            queue.Enqueue(startElement);

            while (queue.Count > 0 && !args.Handled)
            {
                var currentElement = queue.Dequeue();
                InvokeHandlers(currentElement, name, sender, args);

                for (var i = 0; i < currentElement.Children.Count; i++)
                {
                    queue.Enqueue(currentElement.Children[i]);
                }
            }
        }
        finally
        {
            QueuePool.Return(queue);
        }
    }

    private void Bubble(VisualElement element, string name, object sender, HandledEventArgs args)
    {
        var currentElement = element;
        while (currentElement is not null && !args.Handled)
        {
            if (args.Handled)
            {
                return;
            }

            InvokeHandlers(currentElement, name, sender, args);
            currentElement = currentElement.Parent;
        }
    }

    private void InvokeHandlers(VisualElement element, string name, object sender, HandledEventArgs args)
    {
        var handlers = GetHandlers(element, name);
        foreach (var handler in handlers)
        {
            if (args.Handled)
            {
                return;
            }

            handler.Invoke(sender, args);
        }
    }

    private static VisualElement GetTargetElement(VisualElement startElement, Func<VisualElement, bool> predicate)
    {
        var queue = QueuePool.Rent();
        try
        {
            queue.Clear();
            queue.Enqueue(startElement);

            while (queue.Count > 0)
            {
                var element = queue.Dequeue();

                var isTargetElement = true;
                for (var i = 0; i < element.Children.Count; i++)
                {
                    var child = element.Children[i];
                    if (!predicate.Invoke(child))
                    {
                        continue;
                    }

                    isTargetElement = false;
                    queue.Enqueue(child);
                }

                if (isTargetElement)
                {
                    return element;
                }
            }

            return startElement;
        }
        finally
        {
            QueuePool.Return(queue);
        }
    }
}