using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameJam.Characters
{
    public readonly struct EquippedItemView
    {
        public string Name { get; }
        public Sprite Icon { get; }

        public EquippedItemView(string name, Sprite icon)
        {
            Name = name;
            Icon = icon;
        }
    }

    // The equipment owner supplies its current list, in display order.
    public interface ICharacterEquipmentView
    {
        IReadOnlyList<EquippedItemView> EquippedItems { get; }
        event Action EquipmentChanged;
    }
}
