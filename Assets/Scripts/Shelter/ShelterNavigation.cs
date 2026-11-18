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
        public ShelterElevator[] Elevators { get; private set; } = Array.Empty<ShelterElevator>();
        // Legacy editor checks can still compile; ladders no longer contribute traversal edges.
        public ShelterLadder[] Ladders => Array.Empty<ShelterLadder>();
        public int Version { get; private set; }
        private int signature;
        private float nextRefresh;

        public struct Waypoint
        {
            public Vector2 position;
            public ShelterElevator elevator;
            public float fromFloor;
            public int side;
            public ShelterLadder ladder => null;
            public Waypoint(Vector2 point, ShelterElevator lift = null, float from = 0, int boardingSide = 0)
            { position = point; elevator = lift; fromFloor = from; side = boardingSide; }
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
            public ShelterElevator elevator;
            public float fromFloor;
            public int side;
            public Edge(int to, ShelterElevator lift = null, float from = 0, int boardingSide = 0)
            { this.to = to; elevator = lift; fromFloor = from; side = boardingSide; }
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
            var lifts = FindObjectsByType<ShelterElevator>(FindObjectsSortMode.InstanceID).Where(l => l.gameObject.scene == gameObject.scene).ToArray();
            foreach (var lift in lifts) lift.RefreshStops(Areas);
            Elevators = lifts.Where(l => l.IsUsable).ToArray();
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
                foreach (var l in Elevators)
                {
                    hash = hash * 31 + l.GetInstanceID();
                    hash = hash * 31 + l.Bottom.GetHashCode();
                    hash = hash * 31 + l.Top.GetHashCode();
                    foreach (float stop in l.Stops) hash = hash * 31 + stop.GetHashCode();
                }
                if (hash != signature || Version == 0) { signature = hash; Version++; }
            }
        }

        public ShelterArea AreaAt(Vector2 feet, float heightTolerance = 0.08f) => Areas.FirstOrDefault(a =>
            Mathf.Abs(feet.y - a.FloorY) <= heightTolerance && feet.x >= a.Left - connectionTolerance && feet.x <= a.Right + connectionTolerance);

        public void GetConnectedFloorBounds(Vector2 feet, out float left, out float right)
        {
            left = right = feet.x;
            float previousLeft, previousRight;
            do
            {
                previousLeft = left;
                previousRight = right;
                foreach (var area in Areas)
                {
                    if (Mathf.Abs(feet.y - area.FloorY) > connectionTolerance ||
                        area.Left > right + connectionTolerance || area.Right < left - connectionTolerance) continue;
                    left = Mathf.Min(left, area.Left);
                    right = Mathf.Max(right, area.Right);
                }
            } while (left < previousLeft || right > previousRight);
        }

        public bool TryFindPath(Vector2 start, ShelterLadder currentLadder, ShelterArea destination,
            float arrivalOffset, float walkSpeed, float climbSpeed, out List<Waypoint> path)
            => TryFindElevatorPath(start, destination, arrivalOffset, walkSpeed, out path);

        public bool TryFindElevatorPath(Vector2 start, ShelterArea destination,
            float arrivalOffset, float walkSpeed, out List<Waypoint> path)
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
            foreach (var lift in Elevators)
            {
                foreach (float from in lift.Stops) foreach (int side in new[] { -1, 1 })
                {
                    Vector2 call = lift.CallPoint(from, side);
                    var entrances = AtEndpoint(call);
                    if (entrances.Length == 0) continue;
                    int boarding = Add(nodes, call); AttachEndpoint(nodes, boarding, entrances);
                    foreach (float to in lift.Stops)
                    {
                        if (Mathf.Abs(to - from) < connectionTolerance) continue;
                        Vector2 exit = lift.ExitPoint(to, side); var exits = AtEndpoint(exit);
                        if (exits.Length == 0) continue;
                        int arrival = Add(nodes, exit); AttachEndpoint(nodes, arrival, exits);
                        nodes[boarding].edges.Add(new Edge(arrival, lift, from, side));
                    }
                }
            }
            for (int i = 1; i < nodes.Count; i++)
                for (int j = i + 1; j < nodes.Count; j++)
                    if (nodes[i].area != null && nodes[i].area == nodes[j].area) Link(nodes, i, j);

            var distance = Enumerable.Repeat(float.PositiveInfinity, nodes.Count).ToArray();
            var previous = Enumerable.Repeat(-1, nodes.Count).ToArray();
            var viaElevator = new Edge[nodes.Count];
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
                    float cost = distance[best] + (edge.elevator
                        ? Mathf.Abs(nodes[edge.to].point.y - edge.fromFloor) / edge.elevator.Rules.TravelSpeed +
                          (edge.elevator.Rules.BoardingOffset + edge.elevator.Rules.ExitOffset) / Mathf.Max(0.1f, walkSpeed)
                        : Vector2.Distance(nodes[best].point, nodes[edge.to].point) / Mathf.Max(0.1f, walkSpeed));
                    if (cost >= distance[edge.to]) continue;
                    distance[edge.to] = cost;
                    previous[edge.to] = best;
                    viaElevator[edge.to] = edge;
                }
            }
            if (previous[1] < 0) return false;
            path = new List<Waypoint>();
            for (int at = 1; at != 0; at = previous[at]) path.Add(new Waypoint(nodes[at].point, viaElevator[at].elevator, viaElevator[at].fromFloor, viaElevator[at].side));
            path.Reverse();
            return true;
        }

        private ShelterArea[] AtEndpoint(Vector2 point) => Areas.Where(a =>
            Mathf.Abs(point.y - a.FloorY) <= connectionTolerance && point.x >= a.Left - connectionTolerance && point.x <= a.Right + connectionTolerance).ToArray();
        private static int Add(List<Node> nodes, Vector2 point, ShelterArea area = null)
        { nodes.Add(new Node(point, area)); return nodes.Count - 1; }
        private static void Link(List<Node> nodes, int a, int b)
        { nodes[a].edges.Add(new Edge(b)); nodes[b].edges.Add(new Edge(a)); }
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
