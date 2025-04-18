using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

public class SpatialHash<T>
{
    private readonly ConcurrentDictionary<Vector3Int, ConcurrentBag<T>> hashmap;
    public float cellSize;

    public SpatialHash(float cellSize)
    {
        hashmap = new ConcurrentDictionary<Vector3Int, ConcurrentBag<T>>();
        this.cellSize = cellSize;
    }

    public ConcurrentBag<T> this[Vector3Int key] => GetWithEmplace(key);

    public void Add(Vector3Int key, T value) => GetWithEmplace(key).Add(value);

    public void Remove(Vector3Int key, T value)
    {
        var cell = GetWithEmplace(key);
        var updatedCell = new ConcurrentBag<T>();

        // Copy all items except the one to remove
        foreach (var item in cell.Where(x => !EqualityComparer<T>.Default.Equals(x, value)))
        {
            updatedCell.Add(item);
        }

        // Replace the cell with updated contents
        hashmap.TryUpdate(key, updatedCell, cell);
    }

    public ConcurrentBag<T> Get(Vector3Int key)
    {
        return hashmap.TryGetValue(key, out var cell) ? cell : new ConcurrentBag<T>();
    }

    public ConcurrentBag<T> GetWithEmplace(Vector3Int key)
    {
        return hashmap.GetOrAdd(key, _ => new ConcurrentBag<T>());
    }

    public Vector3Int GetCell(Vector3 at) => Vector3Int.FloorToInt(at / cellSize);

    public void AddAt(Vector3 at, T value) => Add(GetCell(at), value);

    public bool ContainsAt(Vector3Int key, T value)
    {
        if (!hashmap.TryGetValue(key, out var cell)) return false;
        return cell.Contains(value);
    }

    public List<T> GetSurrounding(Vector3 at, float radius)
    {
        var result = new List<T>();
        var center = GetCell(at);
        var cellRadius = Mathf.CeilToInt(radius / cellSize);

        // Calculate bounds once
        var minX = center.x - cellRadius;
        var maxX = center.x + cellRadius;
        var minY = center.y - cellRadius;
        var maxY = center.y + cellRadius;
        var minZ = center.z - cellRadius;
        var maxZ = center.z + cellRadius;

        // Use a standard loop instead of Parallel.For - avoids overhead for small operations
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    var key = new Vector3Int(x, y, z);
                    // Only do lookup once per cell
                    if (hashmap.TryGetValue(key, out var cell))
                    {
                        result.AddRange(cell);
                    }
                }
            }
        }

        return result;
    }

    public void Move(Vector3 from, Vector3 to, T item)
    {
        var fromCell = GetCell(from);
        var toCell = GetCell(to);
        if (fromCell == toCell)
        {
            if (!ContainsAt(toCell, item))
                Add(toCell, item);
            return;
        }
        Remove(fromCell, item);
        AddAt(toCell, item);
    }

    public List<T> GetAll()
    {
        var result = new ConcurrentBag<T>();

        foreach (var cell in hashmap.Values)
        {
            foreach (var item in cell)
            {
                result.Add(item);
            }
        }

        return result.ToList();
    }

    public void Clear()
    {
        hashmap.Clear();
    }
}
