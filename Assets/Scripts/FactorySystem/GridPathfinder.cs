using System.Collections.Generic;
using UnityEngine;

public static class GridPathfinder
{
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int target, Dictionary<Vector2Int, Entity> entities, int maxIterations = 100)
    {
        var openSet = new PriorityQueue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        var gScore = new Dictionary<Vector2Int, int>();
        var fScore = new Dictionary<Vector2Int, int>();

        openSet.Enqueue(start, 0);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, target);

        var iterations = 0;
        
        while (openSet.Count > 0)
        {
            if (++iterations > maxIterations)
                return null;
            
            var current = openSet.Dequeue();
            
            if (current == target)
                return ReconstructPath(cameFrom, current);
            
            foreach (var neighbor in GetNeighbors(current))
            {
                if (entities.ContainsKey(neighbor) && neighbor != target)
                    continue;
                
                int tentativeGScore = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Heuristic(neighbor, target);

                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return null;
    }

    private static IEnumerable<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        yield return pos + Vector2Int.up;
        yield return pos + Vector2Int.down;
        yield return pos + Vector2Int.left;
        yield return pos + Vector2Int.right;
    }

    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private class PriorityQueue<T>
    {
        private readonly List<(T item, int priority)> _elements = new();

        public int Count => _elements.Count;

        public void Enqueue(T item, int priority)
        {
            _elements.Add((item, priority));
        }

        public T Dequeue()
        {
            int bestIndex = 0;

            for (int i = 1; i < _elements.Count; i++)
            {
                if (_elements[i].priority < _elements[bestIndex].priority)
                    bestIndex = i;
            }

            var bestItem = _elements[bestIndex].item;
            _elements.RemoveAt(bestIndex);

            return bestItem;
        }
    }
}