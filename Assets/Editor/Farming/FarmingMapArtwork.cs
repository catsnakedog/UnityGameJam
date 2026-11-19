using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ShelterRun.Editor
{
    public static class FarmingMapArtwork
    {
        const float PixelsPerUnit = 22;
        const int FloorDepth = 16;
        // Source pixel centres measured from the left edge of the two backgrounds.
        static readonly int[] HallDoors = { 215, 455, 650 };
        static readonly int[] DeskPositions = { 95, 125, 174, 238, 270, 322 };

        static Sprite Background(string name)
        {
            string path = "Assets/Art/Farming/SideView/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.spritePixelsPerUnit != PixelsPerUnit || importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single || importer.filterMode != FilterMode.Point ||
                importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void Apply(FarmingMap2D map, FarmingCamera camera)
        {
            var corridor = Background("Corridor");
            var room = Background("Room");
            Layout(map.Corridor, corridor, true);
            foreach (var area in map.Rooms) Layout(area, room, false);
            var entrances = map.Doors.Where(d => d.Area == map.Corridor).OrderBy(d => d.transform.position.x).ToArray();
            for (int i = 0; i < entrances.Length; i++)
            {
                PlaceDoor(entrances[i], HallDoors[i]);
                PlaceDoor(entrances[i].Target, 24);
            }
            var hall = map.Corridor;
            var bunker = hall.GetComponentInChildren<FarmingBunker>(true);
            Position(bunker.transform, hall.Left + 47 / PixelsPerUnit, hall.GroundY);
            HideDrawing(bunker);
            var cameraData = new SerializedObject(camera);
            cameraData.FindProperty("referenceSize").floatValue = corridor.bounds.size.y / 2;
            cameraData.ApplyModifiedPropertiesWithoutUndo();
            map.Corridor.Apply(1);
            foreach (var area in map.Rooms) area.Apply(0);
        }

        static void Layout(FarmingRoom2D area, Sprite sprite, bool hall)
        {
            var visuals = area.transform.Find("Visuals");
            float x = area.Left, y = area.GroundY;
            float width = sprite.bounds.size.x, height = sprite.bounds.size.y;
            float bottom = y - FloorDepth / PixelsPerUnit;
            foreach (Transform child in visuals.Cast<Transform>().ToArray())
                if (child.name == "Wall" || child.name == "Floor" || child.name == "Window" || child.name == "Chair" ||
                    (child.name.StartsWith("Label ") && child.name != "Label BUNKER"))
                    Object.DestroyImmediate(child.gameObject);

            var background = visuals.Find("Map artwork");
            if (!background)
            {
                background = new GameObject("Map artwork", typeof(SpriteRenderer)).transform;
                background.SetParent(visuals, false);
            }
            var image = background.GetComponent<SpriteRenderer>();
            image.sprite = sprite;
            image.sortingOrder = -100;
            Position(background, x - sprite.bounds.min.x, bottom - sprite.bounds.min.y);
            Position(area.transform.Find("Walk right"), x + width, y);
            Position(area.transform.Find("Camera height"), x + width / 2, bottom + height / 2);
            var data = new SerializedObject(area);
            data.FindProperty("footprint").rectValue = new Rect(x, bottom, width, height);
            data.ApplyModifiedPropertiesWithoutUndo();
            if (hall) return;
            var tables = visuals.GetComponentsInChildren<FarmingSpawnTable>().OrderBy(t => t.transform.position.x).ToArray();
            for (int i = 0; i < tables.Length; i++)
            {
                var table = tables[i];
                float deskX = x + DeskPositions[i] / PixelsPerUnit;
                Position(table.transform, deskX, y);
                HideDrawing(table);
                for (int j = 0; j < table.TabletopSlots.Count; j++)
                    Position(table.TabletopSlots[j], deskX + (j == 0 ? -8 : 8) / PixelsPerUnit, y + 17 / PixelsPerUnit);
            }
        }

        static void PlaceDoor(FarmingDoor2D door, int pixel)
        {
            var area = door.Area;
            Position(door.transform, area.Left + pixel / PixelsPerUnit, area.GroundY);
            HideDrawing(door);
            var focus = door.transform.Find("Focus");
            var size = focus.GetComponent<SpriteRenderer>().sprite.bounds.size;
            focus.localScale = new Vector3(31 / (PixelsPerUnit * size.x), 52 / (PixelsPerUnit * size.y), 1);
            PrefabUtility.RecordPrefabInstancePropertyModifications(focus);
            var label = door.GetComponentInChildren<TMP_Text>(true);
            if (label) Position(label.transform, door.transform.position.x, area.GroundY + 59 / PixelsPerUnit);
        }

        static void HideDrawing(Component owner)
        {
            var renderer = owner.GetComponent<SpriteRenderer>();
            renderer.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }

        static void Position(Transform target, float x, float y)
        {
            target.position = new Vector3(x, y, target.position.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
