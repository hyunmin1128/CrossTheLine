using System.Collections.Generic;
using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaNavigation : MonoBehaviour
    {
        public Rect area = new Rect(-9.5f, -5.5f, 19, 10);
        public Vector2 footSize = new Vector2(0.5f, 0.125f);
        public Vector2 footOffset = new Vector2(0, 0.125f);
        public Collider2D[] obstacles;
        public bool showMap;
        private const float Step = 1f / 16;
        private int width, height;
        private bool[] walkable;
        private readonly Dictionary<long, Vector3[]> routes = new Dictionary<long, Vector3[]>();

        public void Rebuild()
        {
            routes.Clear();
            width = Mathf.RoundToInt(area.width / Step) + 1;
            height = Mathf.RoundToInt(area.height / Step) + 1;
            walkable = new bool[width * height];
            for (int i = 0; i < walkable.Length; i++)
            {
                Vector2 center = Point(i) + footOffset;
                var footprint = new Bounds(center, footSize);
                bool free = true;
                foreach (var obstacle in obstacles)
                {
                    if (!obstacle || !obstacle.enabled || obstacle.isTrigger) continue;
                    // Scene furniture uses axis-aligned BoxCollider2D footprints.
                    if (footprint.Intersects(obstacle.bounds)) { free = false; break; }
                }
                walkable[i] = free;
            }
        }

        private Vector2 Point(int index) => new Vector2(area.xMin + index % width * Step, area.yMin + index / width * Step);
        private int Index(Vector2 point)
        {
            int x = Mathf.RoundToInt((point.x - area.xMin) / Step);
            int y = Mathf.RoundToInt((point.y - area.yMin) / Step);
            if (x < 0 || x >= width || y < 0 || y >= height) return -1;
            return y * width + x;
        }

        public bool TryRoute(Vector2 start, Vector2 destination, out Vector3[] route)
        {
            if (walkable == null) Rebuild();
            route = System.Array.Empty<Vector3>();
            int from = Index(start), to = Index(destination);
            if (from < 0 || to < 0 || !walkable[from] || !walkable[to]) return false;
            long key = (long)from * walkable.Length + to;
            if (routes.TryGetValue(key, out route)) return true;
            var previous = new int[walkable.Length];
            System.Array.Fill(previous, -1);
            var queue = new Queue<int>();
            previous[from] = from;
            queue.Enqueue(from);
            while (queue.Count > 0 && previous[to] < 0)
            {
                int current = queue.Dequeue();
                int x = current % width;
                if (x > 0) Visit(current - 1, current, previous, queue);
                if (x + 1 < width) Visit(current + 1, current, previous, queue);
                if (current >= width) Visit(current - width, current, previous, queue);
                if (current + width < walkable.Length) Visit(current + width, current, previous, queue);
            }
            if (previous[to] < 0) return false;
            var cells = new List<Vector3>();
            for (int at = to; at != from; at = previous[at]) cells.Add(Point(at));
            cells.Add(Point(from));
            cells.Reverse();
            var corners = new List<Vector3> { cells[0] };
            for (int i = 1; i + 1 < cells.Count; i++)
                if (cells[i] - cells[i - 1] != cells[i + 1] - cells[i]) corners.Add(cells[i]);
            if (cells.Count > 1) corners.Add(cells[cells.Count - 1]);
            route = corners.ToArray();
            routes[key] = route;
            return true;
        }

        private void Visit(int cell, int parent, int[] previous, Queue<int> queue)
        {
            if (!walkable[cell] || previous[cell] >= 0) return;
            previous[cell] = parent;
            queue.Enqueue(cell);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showMap || walkable == null) return;
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            for (int i = 0; i < walkable.Length; i += 4)
                if (walkable[i]) Gizmos.DrawCube(Point(i), Vector3.one * Step);
        }
    }
}
