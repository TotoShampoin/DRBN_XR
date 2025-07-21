using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// Minimal generic KD-tree for 3D points, where position is extracted from T
/// </summary>
public class KDTree3<T>
{ // copilot generated
    private class Node
    {
        public T item;
        public Node left, right;
        public int axis;
    }

    private readonly Node root;
    private readonly Func<T, Vector3> positionSelector;

    static readonly ProfilerMarker buildMarker = new("Membrane.KDTree3.Build");

    public KDTree3(IEnumerable<T> items, Func<T, Vector3> positionSelector)
    {
        this.positionSelector = positionSelector
            ?? throw new ArgumentNullException(nameof(positionSelector));
        var arr = items.ToArray();
        buildMarker.Begin();
        root = Build(arr, 0, arr.Length, 0);
        buildMarker.End();
    }

    private Node Build(T[] arr, int start, int end, int axis)
    {
        if (start >= end) return null;
        int mid = (start + end) / 2;
        Array.Sort(
            arr, start, end - start,
            Comparer<T>.Create((a, b) =>
                positionSelector(a)[axis].CompareTo(positionSelector(b)[axis])));
        var node = new Node
        {
            item = arr[mid],
            axis = axis,
            left = Build(arr, start, mid, (axis + 1) % 3),
            right = Build(arr, mid + 1, end, (axis + 1) % 3)
        };
        return node;
    }

    public float NearestDistance(Vector3 target)
    {
        return Mathf.Sqrt(Nearest(root, target, float.MaxValue));
    }

    private float Nearest(Node node, Vector3 target, float best)
    {
        if (node == null) return best;
        float dist = (positionSelector(node.item) - target).sqrMagnitude;
        if (dist < best) best = dist;
        float delta = target[node.axis] - positionSelector(node.item)[node.axis];
        Node first = delta < 0 ? node.left : node.right;
        Node second = delta < 0 ? node.right : node.left;
        best = Nearest(first, target, best);
        if (delta * delta < best)
            best = Nearest(second, target, best);
        return best;
    }
}
