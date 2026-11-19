using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ShelterRun
{
    public sealed class FarmingInventory
    {
        readonly List<FarmingPickup> carried = new List<FarmingPickup>();
        public int Capacity { get; }
        public int Used { get; private set; }
        public ReadOnlyCollection<FarmingPickup> Carried => carried.AsReadOnly();
        public FarmingInventory(int capacity) { Capacity = capacity; }
        public bool CanAdd(FarmingPickup item) => item != null && item.State == PickupState.Available && Used + item.Record.Weight <= Capacity;
        public bool TryAdd(FarmingPickup item)
        {
            if (!CanAdd(item) || carried.Contains(item)) return false;
            carried.Add(item);
            Used += item.Record.Weight;
            return true;
        }
        public FarmingPickup RemoveLast()
        {
            if (carried.Count == 0) return null;
            var item = carried[carried.Count - 1];
            carried.RemoveAt(carried.Count - 1);
            Used -= item.Record.Weight;
            return item;
        }
    }
}
