using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityGameJam.Shelter
{
    public sealed class ShelterRoomHeader : MonoBehaviour
    {
        [SerializeField] TMP_Text title, count;
        [SerializeField] Image icon;
        [SerializeField] Sprite person, zombie;
        [SerializeField, Min(0), Tooltip("Pixels from the displayed artwork’s left edge to the upper wall.")] int wallInsetPixels = 14;
        [SerializeField, Min(0), Tooltip("Pixels from the top of the room sprite to its roofline.")] int roofInsetPixels = 14;
        const int RoofClearancePixels = 2;
        ShelterArea room;
        ShelterWalker[] survivors;
        int lastPeople = -1;
        bool wasOccupied;

        public void Place(ShelterArea area)
        {
            room = area;
            var art = area.roomArtwork ? area.roomArtwork.sprite : null;
            float inset = art ? wallInsetPixels / art.pixelsPerUnit : 0f;
            float roofY = area.LocalBottom + area.height;
            if (art) roofY += (RoofClearancePixels - roofInsetPixels) / art.pixelsPerUnit;
            var box = (RectTransform)transform;
            float pivotAboveBottom = -box.rect.yMin * box.localScale.y;
            box.localPosition = new Vector3(-area.width * .5f + inset, roofY + pivotAboveBottom, 0);
            if (title && title.text != area.displayName) title.text = area.displayName;
            if (survivors != null) RefreshCount();
        }

        internal void Bind(ShelterWalker[] walkers)
        {
            survivors = walkers;
            Place(GetComponentInParent<ShelterArea>());
        }

        void LateUpdate()
        {
            if (room && survivors != null) RefreshCount();
        }

        void RefreshCount()
        {
            int people = 0;
            bool occupied = room.IsOccupied;
            if (!occupied)
                foreach (var walker in survivors)
                    if (walker && walker.isActiveAndEnabled && walker.CurrentRoom == room) people++;

            if (occupied == wasOccupied && people == lastPeople) return;
            lastPeople = people;
            wasOccupied = occupied;
            icon.sprite = occupied ? zombie : person;
            count.text = occupied ? "x/x" : $"{people}명";
            count.color = occupied ? new Color32(247, 158, 128, 255) : Color.white;
        }
    }
}
