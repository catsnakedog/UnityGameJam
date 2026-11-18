using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ShelterItems = GameJam.Data.Generated.ShelterItems.Table;
using Random = UnityEngine.Random;

namespace ShelterRun
{
    public static class FarmingSpawnLayout
    {
        public readonly struct Placement
        {
            public readonly FarmingRecord Record;
            public readonly FarmingRoom2D Room;
            public readonly Transform Tabletop;
            public readonly Vector3 Ground;

            internal Placement(FarmingRecord record, FarmingRoom2D room, float x, Transform tabletop = null)
            {
                Record = record;
                Room = room;
                Tabletop = tabletop;
                Ground = new Vector3(x, room.GroundY, 0);
            }
        }

        sealed class RoomSlots
        {
            public readonly FarmingRoom2D Room;
            public readonly FarmingRules.RoomSpawnRule Rule;
            public readonly List<Transform> Tabletop = new List<Transform>();
            public readonly List<float> Aisles = new List<float>();

            public RoomSlots(FarmingRoom2D room, FarmingRules.RoomSpawnRule rule)
            {
                Room = room;
                Rule = rule;
                var tables = room.GetComponentsInChildren<FarmingSpawnTable>().OrderBy(t => t.transform.position.x).ToArray();
                for (int i = 0; i < tables.Length; i++)
                {
                    var slots = tables[i].TabletopSlots;
                    if (slots == null || slots.Count == 0 || slots.Any(s => !s))
                        throw new InvalidOperationException($"Missing tabletop slots on {room.name}/{tables[i].name}.");
                    Tabletop.AddRange(slots);
                    if (i > 0)
                        Aisles.Add((tables[i - 1].transform.position.x + tables[i].transform.position.x) * .5f);
                }
                if (Aisles.Count < rule.People || Tabletop.Count == 0 || Tabletop.Distinct().Count() != Tabletop.Count)
                    throw new InvalidOperationException($"Invalid table or aisle capacity in {room.name}.");
            }

            public Placement PlaceItem(FarmingRecord record)
            {
                var slot = TakeRandom(Tabletop);
                return new Placement(record, Room, slot.position.x, slot);
            }
        }

        public static List<Placement> Create(FarmingMap2D map, FarmingBunker bunker, FarmingRules rules, FarmingDatabase database)
        {
            var rooms = map.Doors.Where(d => d.Area == map.Corridor)
                .OrderBy(d => Mathf.Abs(d.transform.position.x - bunker.LandingPosition.x))
                .Select(d => d.Target.Area).ToArray();
            if (rooms.Length != rules.RoomSpawns.Count || rooms.Length != map.Rooms.Length ||
                !new HashSet<FarmingRoom2D>(rooms).SetEquals(map.Rooms) || rooms.Distinct().Count() != rooms.Length)
                throw new InvalidOperationException("Each farming room needs one corridor entrance and one spawn rule.");

            var slots = rooms.Select((room, index) => new RoomSlots(room, rules.RoomSpawns[index])).ToArray();
            var people = database.Records.Where(r => r.Type == FarmingObjectType.Person).ToList();
            var starter = database.Get(rules.StartingPersonId);
            if (!people.Remove(starter) || starter.Weight > rules.Capacity)
                throw new InvalidOperationException($"Starting person {starter.Id} must be a Person within carry capacity.");
            var can = database.Get(FarmingDataKeys.SpaghettiCan);
            var items = database.Records.Where(r => r.Type == FarmingObjectType.Item && r != can).ToList();
            if (people.Count != slots.Sum(s => s.Rule.People) || can.Type != FarmingObjectType.Item ||
                items.Count + rooms.Length > slots.Sum(s => s.Tabletop.Count))
                throw new InvalidOperationException("Farming roster does not fit the configured room quotas or tabletop slots.");

            var result = new List<Placement>();
            foreach (var room in slots)
            {
                result.Add(room.PlaceItem(can));
                for (int i = 0; i < room.Rule.People; i++)
                    result.Add(new Placement(TakeRandom(people), room.Room, TakeRandom(room.Aisles)));
            }
            // Reserve legendary placements before ordinary items can fill a room.
            var byRank = items.ToLookup(item => ShelterItems.Get(item.Id).rank == "legend");
            foreach (var item in byRank[true])
                result.Add(DrawRoom(slots, true).PlaceItem(item));
            foreach (var item in byRank[false])
                result.Add(DrawRoom(slots, false).PlaceItem(item));
            return result;
        }

        static RoomSlots DrawRoom(RoomSlots[] rooms, bool legend)
        {
            // For ordinary items each free tabletop position has the same probability.
            int Weight(RoomSlots room) => room.Tabletop.Count == 0 ? 0 : legend ? room.Rule.LegendWeight : room.Tabletop.Count;
            int roll = Random.Range(0, rooms.Sum(Weight));
            foreach (var room in rooms)
            {
                roll -= Weight(room);
                if (roll < 0) return room;
            }
            throw new InvalidOperationException("No farming room has a free tabletop slot.");
        }

        static T TakeRandom<T>(List<T> available)
        {
            int index = Random.Range(0, available.Count);
            T value = available[index];
            available.RemoveAt(index);
            return value;
        }
    }
}
