using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Minimal KD-tree for 3D points
/// </summary>
public class KDTree3
{ // copilot generated
    private class Node
    {
        public Vector3 point;
        public Node left, right;
        public int axis;
    }

    private readonly Node root;

    public KDTree3(IEnumerable<Vector3> points)
    {
        var pts = points.ToArray();
        root = Build(pts, 0, pts.Length, 0);
    }

    private Node Build(Vector3[] pts, int start, int end, int axis)
    {
        if (start >= end) return null;
        int mid = (start + end) / 2;
        Array.Sort(pts, start, end - start, Comparer<Vector3>.Create((a, b) => a[axis].CompareTo(b[axis])));
        var node = new Node { point = pts[mid], axis = axis };
        node.left = Build(pts, start, mid, (axis + 1) % 3);
        node.right = Build(pts, mid + 1, end, (axis + 1) % 3);
        return node;
    }

    public float NearestDistance(Vector3 target)
    {
        return Mathf.Sqrt(Nearest(root, target, float.MaxValue));
    }

    private float Nearest(Node node, Vector3 target, float best)
    {
        if (node == null) return best;
        float dist = (node.point - target).sqrMagnitude;
        if (dist < best) best = dist;
        float delta = target[node.axis] - node.point[node.axis];
        Node first = delta < 0 ? node.left : node.right;
        Node second = delta < 0 ? node.right : node.left;
        best = Nearest(first, target, best);
        if (delta * delta < best)
            best = Nearest(second, target, best);
        return best;
    }
}
