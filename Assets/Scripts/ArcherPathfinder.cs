using System.Collections.Generic;
using UnityEngine;


public static class ArcherPathfinder
{
    private const int MaximumGridCells = 4096;
    private static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

    private struct OpenNode
    {
        public int Index;
        public float Score;
        public OpenNode(int index, float score) { Index = index; Score = score; }
    }

    private sealed class MinHeap
    {
        private readonly List<OpenNode> _data = new List<OpenNode>(128);
        public int Count => _data.Count;

        public void Push(OpenNode value)
        {
            int i = _data.Count;
            _data.Add(value);
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_data[parent].Score <= value.Score)
                    break;
                _data[i] = _data[parent];
                i = parent;
            }
            _data[i] = value;
        }

        public OpenNode Pop()
        {
            OpenNode first = _data[0];
            int lastIndex = _data.Count - 1;
            OpenNode last = _data[lastIndex];
            _data.RemoveAt(lastIndex);
            if (_data.Count == 0)
                return first;

            int i = 0;
            while (true)
            {
                int child = i * 2 + 1;
                if (child >= _data.Count)
                    break;
                int right = child + 1;
                if (right < _data.Count && _data[right].Score < _data[child].Score)
                    child = right;
                if (_data[child].Score >= last.Score)
                    break;
                _data[i] = _data[child];
                i = child;
            }
            _data[i] = last;
            return first;
        }
    }

    public static bool TryBuildPath(
        ArcherArea area, Vector2 start, Vector2 target, float firingDistance,
        float spacing, float clearance, LayerMask blockers, List<Vector2> result)
    {
        result.Clear();
        if (area == null)
            return false;

        Bounds bounds = area.AreaBounds;
        if (bounds.size.x < 0.001f || bounds.size.y < 0.001f)
            return false;


        const float targetCellCount = 1900f;
        float requested = Mathf.Max(0.08f, spacing,
            Mathf.Sqrt((bounds.size.x * bounds.size.y) / targetCellCount));
        int width = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / requested));
        int height = Mathf.Max(1, Mathf.CeilToInt(bounds.size.y / requested));
        int count = width * height;
        while (count > MaximumGridCells)
        {
            requested *= 1.10f;
            width = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / requested));
            height = Mathf.Max(1, Mathf.CeilToInt(bounds.size.y / requested));
            count = width * height;
        }

        float stepX = bounds.size.x / width;
        float stepY = bounds.size.y / height;
        var positions = new Vector2[count];
        var openCells = new bool[count];
        var distance = new float[count];
        var parent = new int[count];
        var closed = new bool[count];

        int startIndex = -1;
        float closestStart = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            int x = i % width;
            int y = i / width;
            Vector2 p = new Vector2(bounds.min.x + (x + 0.5f) * stepX,
                                    bounds.min.y + (y + 0.5f) * stepY);
            positions[i] = p;
            openCells[i] = area.CanStand(p, clearance, blockers);
            distance[i] = float.PositiveInfinity;
            parent[i] = -1;

            if (!openCells[i])
                continue;

            float d = (start - p).sqrMagnitude;
            if (d < closestStart)
            {
                closestStart = d;
                startIndex = i;
            }
        }

        if (startIndex < 0)
            return false;

        
        if (!area.CanTravel(start, positions[startIndex], clearance, blockers))
        {
            startIndex = -1;
            closestStart = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (!openCells[i] || !area.CanTravel(start, positions[i], clearance, blockers))
                    continue;
                float d = (start - positions[i]).sqrMagnitude;
                if (d >= closestStart)
                    continue;
                closestStart = d;
                startIndex = i;
            }
            if (startIndex < 0)
                return false;
        }

        var heap = new MinHeap();
        distance[startIndex] = 0f;
        heap.Push(new OpenNode(startIndex, Estimate(positions[startIndex], target, firingDistance)));

        int best = startIndex;
        float bestDistance = (positions[best] - target).sqrMagnitude;
        const float epsilon = 0.0001f;

        while (heap.Count > 0)
        {
            OpenNode node = heap.Pop();
            int index = node.Index;
            if (closed[index])
                continue;
            closed[index] = true;

            float targetDistance = (positions[index] - target).sqrMagnitude;
            if (targetDistance < bestDistance - epsilon ||
                (Mathf.Abs(targetDistance - bestDistance) < epsilon && distance[index] < distance[best]))
            {
                best = index;
                bestDistance = targetDistance;
            }

            if (targetDistance <= firingDistance * firingDistance)
            {
                best = index;
                break;
            }

            int x = index % width;
            int y = index / width;
            for (int d = 0; d < Dx.Length; d++)
            {
                int nx = x + Dx[d];
                int ny = y + Dy[d];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                int neighbor = nx + ny * width;
                if (!openCells[neighbor] || closed[neighbor])
                    continue;

                if (Dx[d] != 0 && Dy[d] != 0)
                {
                    
                    if (!openCells[(x + Dx[d]) + y * width] ||
                        !openCells[x + (y + Dy[d]) * width])
                        continue;
                }

                if (!area.CanTravel(positions[index], positions[neighbor], clearance, blockers))
                    continue;

                float cost = Vector2.Distance(positions[index], positions[neighbor]);
                float tentative = distance[index] + cost;
                if (tentative >= distance[neighbor])
                    continue;

                distance[neighbor] = tentative;
                parent[neighbor] = index;
                float score = tentative + Estimate(positions[neighbor], target, firingDistance);
                heap.Push(new OpenNode(neighbor, score));
            }
        }

        
        int at = best;
        int guard = 0;
        while (at >= 0 && guard++ <= count)
        {
            result.Add(positions[at]);
            at = parent[at];
        }
        result.Reverse();
        return result.Count > 0;
    }

    private static float Estimate(Vector2 point, Vector2 enemy, float attackDistance)
    {
        return Mathf.Max(0f, Vector2.Distance(point, enemy) - attackDistance);
    }
}
