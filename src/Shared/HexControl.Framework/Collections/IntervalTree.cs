﻿using System.Collections;
using JetBrains.Annotations;

namespace HexControl.Framework.Collections;

[PublicAPI]
internal interface IIntervalTree<TKey, TValue> : IEnumerable<Interval<TKey, TValue>>
{
    int Count { get; }

    IEnumerable<TValue> Values { get; }

    void Add(TKey from, TKey to, TValue value);

    IEnumerable<TValue> Query(TKey key);
}


internal readonly record struct Interval<TKey, TValue>(TKey From, TKey To, TValue Value);

[PublicAPI]
internal class IntervalTree<TKey, TValue> : IIntervalTree<TKey, TValue>
{
    private readonly IComparer<TKey> _comparer = Comparer<TKey>.Default;
    private readonly IComparer<Interval<TKey, TValue>> _comparerAscending =
        Comparer<Interval<TKey, TValue>>.Create(static (i, o) => Comparer<TKey>.Default.Compare(i.From, o.From));
    private readonly IComparer<IntervalHalf> _comparerHalfDescending =
        Comparer<IntervalHalf>.Create(static (i, o) => Comparer<TKey>.Default.Compare(o.Start, i.Start));
    private Interval<TKey, TValue>[] _intervals = new Interval<TKey, TValue>[16];
    private int _intervalCount = 0;
    private IntervalHalf[] _intervalsDescending = Array.Empty<IntervalHalf>();
    private List<Node> _nodes = new();
    private bool _isBuilt = false;

    public int Count => _intervalCount;

    public IEnumerable<TValue> Values =>
        _intervals.Take(_intervalCount).Select(i => i.Value);

    public IEnumerator<Interval<TKey, TValue>> GetEnumerator() =>
        _intervals.AsEnumerable().Take(_intervalCount).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public void Add(TKey from, TKey to, TValue value)
    {
        if (_intervalCount == _intervals.Length)
        {
            // need to grow the existing array
            var newArray = new Interval<TKey, TValue>[_intervals.Length * 2];
            Array.Copy(_intervals, newArray, _intervals.Length);
            _intervals = newArray;
        }

        _intervals[_intervalCount++] = new Interval<TKey, TValue>(from, to, value);
        _isBuilt = false;
    }

    public IEnumerable<TValue> Query(TKey target)
    {
        if (_isBuilt is false)
        {
            Build();
        }

        var result = new List<TValue>();
        var nodeIndex = 1;

        while (true)
        {
            if (nodeIndex >= _nodes.Count)
            {
                throw new IndexOutOfRangeException("nodeIndex outside of range of _nodes"); // this should not happen...
            }

            var node = _nodes[nodeIndex];

            if (node.IntervalCount is 0)
            {
                return result;
            }

            var centerComparison = _comparer.Compare(target, node.Center);

            switch (centerComparison)
            {
                case < 0:

                    // look left
                    // test node intervals for overlap
                    for (var i = node.IntervalIndex; i < node.IntervalIndex + node.IntervalCount; i++)
                    {
                        var intv = _intervals[i];
                        if (_comparer.Compare(target, intv.From) >= 0)
                        {
                            result.Add(intv.Value);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (node.Next is not 0)
                    {
                        nodeIndex = node.Next;
                        continue;
                    }

                    break;

                case > 0:

                    // look right
                    // test node intervals for overlap
                    for (var i = node.IntervalIndex; i < node.IntervalIndex + node.IntervalCount; i++)
                    {
                        var half = _intervalsDescending[i];
                        if (_comparer.Compare(target, half.Start) <= 0)
                        {
                            result.Add(half.Value);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (node.Next is not 0)
                    {
                        nodeIndex = node.Next + 1;
                        continue;
                    }

                    break;

                default:
                    // target == center
                    // add all node intervals
                    for (var i = node.IntervalIndex; i < node.IntervalIndex + node.IntervalCount; i++)
                    {
                        var intv = _intervals[i];
                        result.Add(intv.Value);
                    }

                    break;
            }

            break;
        }

        return result;
    }

    private void Build()
    {
        _nodes = new List<Node> { new(), new() };
        _intervalsDescending = new IntervalHalf[_intervalCount];

        Array.Sort(_intervals, 0, _intervalCount, _comparerAscending);

        BuildRec(0, _intervalCount - 1, 1);

        _isBuilt = true;

        void BuildRec(int min, int max, int nodeIndex)
        {
            var sliceWidth = max - min + 1;

            if (sliceWidth <= 0)
            {
                return;
            }

            var centerIndex = min + sliceWidth / 2;

            // Pick Center value
            var centerValue = _intervals[centerIndex].From;

            // Move index if multiple intervals share same 'From' value
            while (centerIndex < max
                && _comparer.Compare(centerValue, _intervals[centerIndex + 1].From) == 0)
            {
                centerIndex++;
            }

            // Iterate through intervals and pick the ones that overlap
            var i = min;
            var nodeIntervalCount = 0;
            for (; i <= max; i++)
            {
                var interval = _intervals[i];

                if (_comparer.Compare(interval.From, centerValue) > 0)
                {
                    // no more overlapping intervals, the rest fall to right side
                    break;
                }
                else if (_comparer.Compare(interval.To, centerValue) >= 0)
                {
                    // overlapping interval, add the descending half
                    //_intervalsDescending[intervalIndex] = new IntervalHalf(interval.To, interval.Value);
                    nodeIntervalCount++;
                }
                else
                {
                    if (nodeIntervalCount > 0)
                    {
                        // swap current interval with first 'center' interval
                        // this partitions the array so that all 'left' and 'center' intervals are grouped
                        // 'left' interval ordering is maintained (ascending)
                        // no data is lost, so we can work directly on the interval array and re-build in future
                        var tmp = _intervals[i - nodeIntervalCount];
                        _intervals[i - nodeIntervalCount] = interval;
                        _intervals[i] = tmp;
                    }
                }
            }

            var nodeIntervalIndex = i - nodeIntervalCount;

            // re-sort 'center' intervals
            Array.Sort(
                _intervals,
                nodeIntervalIndex,
                nodeIntervalCount,
                _comparerAscending);

            // add descending interval halves

            for (var j = nodeIntervalIndex; j < nodeIntervalIndex + nodeIntervalCount; j++)
            {
                var interval = _intervals[j];
                _intervalsDescending[j] = new IntervalHalf(interval.To, interval.Value);
            }

            // sort descending interval halves
            Array.Sort(
                _intervalsDescending,
                nodeIntervalIndex,
                nodeIntervalCount,
                _comparerHalfDescending);

            if (nodeIntervalCount == sliceWidth)
            {
                // all intervals stored, no need to recurse further
                _nodes[nodeIndex] = new Node(
                    centerValue,
                    next: 0,
                    nodeIntervalIndex,
                    nodeIntervalCount);
                return;
            }

            var nextIndex = _nodes.Count;

            // add node
            _nodes[nodeIndex] = new Node(
                centerValue,
                nextIndex,
                nodeIntervalIndex,
                nodeIntervalCount);

            // add two placeholder nodes to fixate the child indexes
            _nodes.Add(new Node());
            _nodes.Add(new Node());
            
            // left
            BuildRec(min, i - nodeIntervalCount - 1, nextIndex);

            // right
            BuildRec(i, max, nextIndex + 1);
        }
    }

    private readonly record struct Node
    {
        public Node(TKey center, int next, int intervalIndex, int intervalCount)
        {
            Center = center;
            Next = next;
            IntervalIndex = intervalIndex;
            IntervalCount = intervalCount;
        }

        public readonly TKey Center;
        public readonly int Next;
        public readonly int IntervalIndex;
        public readonly int IntervalCount;
    }

    private readonly record struct IntervalHalf
    {
        public IntervalHalf(TKey start, TValue value)
        {
            Start = start;
            Value = value;
        }

        public readonly TKey Start;
        public readonly TValue Value;
    }
}