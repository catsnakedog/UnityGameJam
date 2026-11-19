using UnityEngine;
using UnityEngine.UI;

namespace ShelterRun
{
    public sealed class FarmingCarrySlot : MonoBehaviour
    {
        [SerializeField] Image image;
        [SerializeField] Sprite empty;
        [SerializeField] Sprite person;
        [SerializeField] Sprite item;
        public void Show(FarmingObjectType? type)
        { image.sprite = type == null ? empty : type == FarmingObjectType.Person ? person : item; }
    }
}
