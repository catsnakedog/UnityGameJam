using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityGameJam.Characters;

namespace UnityGameJam.Shelter
{
    public sealed class CharacterEquipmentRow : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text itemName;
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left && data.clickCount == 2)
                GetComponentInParent<ShelterCharacterCard>()?.Unequip();
        }

        public void Display(EquippedItemView item)
        {
            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;
            itemName.text = item.Name;
        }
    }
}
