using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityGameJam.Shelter
{
    [DefaultExecutionOrder(-200)]
    public sealed class ShelterNavigation : MonoBehaviour
    {
        [Tooltip("Maximum gap/height mismatch accepted between touching pieces, in world units.")]
        [Range(0.001f, 0.2f)] public float connectionTolerance = 0.06f;
        public ShelterArea[] Areas { get; private set; } = Array.Empty<ShelterArea>();
        public ShelterLadder[] Ladders { get; private set; } = Array.Empty<ShelterLadder>();
        public int Version { get; private set; }
        private int signature;
        private float nextRefresh;

        public struct Waypoint
        {
            public Vector2 position;
            public ShelterLadder ladder;
            public Waypoint(Vector2 point, ShelterLadder climb = null) { position = point; ladder = climb; }
        }
        private sealed class Node
        {
            public Vector2 point;
            public ShelterArea area;
            public readonly List<Edge> edges = new List<Edge>();
            public Node(Vector2 point, ShelterArea area = null) { this.point = point; this.area = area; }
        }
        private struct Edge
        {
            public int to;
            public ShelterLadder ladder;
            public Edge(int to, ShelterLadder ladder) { this.to = to; this.ladder = ladder; }
        }

        private void Awake() => Refresh();
        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }

        public void Refresh()
        {
            Areas = FindObjectsByType<ShelterArea>(FindObjectsSortMode.InstanceID)
                .Where(a => a.gameObject.scene == gameObject.scene && a.IsWalkable).ToArray();
            Ladders = FindObjectsByType<ShelterLadder>(FindObjectsSortMode.InstanceID)
                .Where(l => l.gameObject.scene == gameObject.scene && l.IsUsable).ToArray();
            unchecked
            {
                int hash = connectionTolerance.GetHashCode();
                foreach (var a in Areas)
                {
                    hash = hash * 31 + a.GetInstanceID();
                    hash = hash * 31 + a.Left.GetHashCode();
                    hash = hash * 31 + a.Right.GetHashCode();
                    hash = hash * 31 + a.FloorY.GetHashCode();
                }
                foreach (var l in Ladders)
                {
                    hash = hash * 31 + l.GetInstanceID();
                    hash = hash * 31 + l.Bottom.GetHashCode();
                    hash = hash * 31 + l.Top.GetHashCode();
                }
                if (hash != signature || Version == 0) { signature = hash; Version++; }
            }
        }

        public ShelterArea AreaAt(Vector2 feet, float heightTolerance = 0.08f) => Areas.FirstOrDefault(a =>
            Mathf.Abs(feet.y - a.FloorY) <= heightTolerance && feet.x >= a.Left - connectionTolerance && feet.x <= a.Right + connectionTolerance);

        public bool TryFindPath(Vector2 start, ShelterLadder currentLadder, ShelterArea destination,
            float arrivalOffset, float walkSpeed, float climbSpeed, out List<Waypoint> path)
        {
            Refresh();
            path = null;
            if (destination == null || !Areas.Contains(destination)) return false;
            var nodes = new List<Node> { new Node(start), new Node(destination.ArrivalPoint(arrivalOffset), destination) };
            for (int i = 0; i < Areas.Length; i++)
            {
                var a = Areas[i];
                if (Mathf.Abs(start.y - a.FloorY) <= connectionTolerance && start.x >= a.Left - connectionTolerance && start.x <= a.Right + connectionTolerance)
                    Link(nodes, 0, Add(nodes, new Vector2(Mathf.Clamp(start.x, a.Left, a.Right), a.FloorY), a));
                for (int j = i + 1; j < Areas.Length; j++)
                {
                    var b = Areas[j];
                    if (Mathf.Abs(a.FloorY - b.FloorY) > connectionTolerance) continue;
                    float left = Mathf.Max(a.Left, b.Left);
                    float right = Mathf.Min(a.Right, b.Right);
                    if (left - right > connectionTolerance) continue;
                    float join = (left + right) * 0.5f;
                    int n1 = Add(nodes, new Vector2(Mathf.Clamp(join, a.Left, a.Right), a.FloorY), a);
                    int n2 = Add(nodes, new Vector2(Mathf.Clamp(join, b.Left, b.Right), b.FloorY), b);
                    Link(nodes, n1, n2);
                }
            }
            foreach (var ladder in Ladders)
            {
                var bottomAreas = AtEndpoint(ladder.Bottom);
                var topAreas = AtEndpoint(ladder.Top);
                if (bottomAreas.Length == 0 || topAreas.Length == 0) continue;
                int lower = Add(nodes, ladder.Bottom);
                int upper = Add(nodes, ladder.Top);
                Link(nodes, lower, upper, ladder);
                AttachEndpoint(nodes, lower, bottomAreas);
                AttachEndpoint(nodes, upper, topAreas);
                if (currentLadder == ladder && Mathf.Abs(start.x - ladder.Bottom.x) <= connectionTolerance &&
                    start.y >= ladder.Bottom.y && start.y <= ladder.Top.y)
                {
                    Link(nodes, 0, lower, ladder);
                    Link(nodes, 0, upper, ladder);
                }
            }
            for (int i = 1; i < nodes.Count; i++)
                for (int j = i + 1; j < nodes.Count; j++)
                    if (nodes[i].area != null && nodes[i].area == nodes[j].area) Link(nodes, i, j);

            var distance = Enumerable.Repeat(float.PositiveInfinity, nodes.Count).ToArray();
            var previous = Enumerable.Repeat(-1, nodes.Count).ToArray();
            var viaLadder = new ShelterLadder[nodes.Count];
            var visited = new bool[nodes.Count];
            distance[0] = 0f;
            for (int iteration = 0; iteration < nodes.Count; iteration++)
            {
                int best = -1;
                for (int i = 0; i < nodes.Count; i++)
                    if (!visited[i] && (best < 0 || distance[i] < distance[best])) best = i;
                if (best < 0 || float.IsPositiveInfinity(distance[best]) || best == 1) break;
                visited[best] = true;
                foreach (var edge in nodes[best].edges)
                {
                    float speed = Mathf.Max(0.1f, edge.ladder != null ? climbSpeed : walkSpeed);
                    float cost = distance[best] + Vector2.Distance(nodes[best].point, nodes[edge.to].point) / speed;
                    if (cost >= distance[edge.to]) continue;
                    distance[edge.to] = cost;
                    previous[edge.to] = best;
                    viaLadder[edge.to] = edge.ladder;
                }
            }
            if (previous[1] < 0) return false;
            path = new List<Waypoint>();
            for (int at = 1; at != 0; at = previous[at]) path.Add(new Waypoint(nodes[at].point, viaLadder[at]));
            path.Reverse();
            return true;
        }

        private ShelterArea[] AtEndpoint(Vector2 point) => Areas.Where(a =>
            Mathf.Abs(point.y - a.FloorY) <= connectionTolerance && point.x >= a.Left - connectionTolerance && point.x <= a.Right + connectionTolerance).ToArray();
        private static int Add(List<Node> nodes, Vector2 point, ShelterArea area = null)
        { nodes.Add(new Node(point, area)); return nodes.Count - 1; }
        private static void Link(List<Node> nodes, int a, int b, ShelterLadder ladder = null)
        { nodes[a].edges.Add(new Edge(b, ladder)); nodes[b].edges.Add(new Edge(a, ladder)); }
        private static void AttachEndpoint(List<Node> nodes, int endpoint, ShelterArea[] areas)
        {
            foreach (var area in areas)
            {
                var p = nodes[endpoint].point;
                Link(nodes, endpoint, Add(nodes, new Vector2(Mathf.Clamp(p.x, area.Left, area.Right), area.FloorY), area));
            }
        }
    }
}
