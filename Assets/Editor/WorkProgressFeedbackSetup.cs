using System;
using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityGameJam.Characters;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

public static class WorkProgressFeedbackSetup
{
    const string Prefab = "Assets/Prefabs/Shelter/Survivor.prefab";
    const string Planning = "Assets/Art/UI/WorkPlanning.png";
    const string Graphics = "Assets/Art/UI/WorkGraphics.png";
    const string Programming = "Assets/Art/UI/Inventory/laptop.png";

    public static void Apply()
    {
        ImportIcon(Planning);
        ImportIcon(Graphics);
        var actor = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            if (!actor.GetComponent<WorkProgressFeedback>())
            {
                var feedback = actor.AddComponent<WorkProgressFeedback>();
                var rect = Child("Work feedback", actor.transform, 0, 180, 40);
                var canvas = rect.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = actor.transform.Find("Mental gauge").GetComponent<Canvas>().sortingOrder + 1;
                rect.localScale = Vector3.one * 0.01f;
                rect.localPosition = new Vector3(0f, 2.86f, 0f);
                var bubble = rect.gameObject.AddComponent<CanvasGroup>();
                bubble.interactable = bubble.blocksRaycasts = false;
                var picture = Child("Field icon", rect, -50, 40, 40).gameObject.AddComponent<Image>();
                picture.sprite = Load<Sprite>(Planning);
                picture.preserveAspect = true;
                picture.raycastTarget = false;
                var label = Child("Increase", rect, 25, 103, 40).gameObject.AddComponent<TextMeshProUGUI>();
                label.rectTransform.sizeDelta = new Vector2(103, 40);
                label.font = Load<TMP_FontAsset>("Assets/Fonts/FarmingKorean.asset");
                label.fontSize = 30;
                label.fontStyle = FontStyles.Bold;
                label.color = new Color32(255, 237, 188, 255);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.richText = false;
                label.raycastTarget = false;
                label.text = "+0.6%";
                var serialized = new SerializedObject(feedback);
                serialized.FindProperty("bubble").objectReferenceValue = bubble;
                serialized.FindProperty("icon").objectReferenceValue = picture;
                serialized.FindProperty("amountLabel").objectReferenceValue = label;
                serialized.FindProperty("planningIcon").objectReferenceValue = picture.sprite;
                serialized.FindProperty("graphicsIcon").objectReferenceValue = Load<Sprite>(Graphics);
                serialized.FindProperty("programmingIcon").objectReferenceValue = Load<Sprite>(Programming);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (!PrefabUtility.SaveAsPrefabAsset(actor, Prefab))
                    throw new InvalidOperationException("Could not save survivor work feedback.");
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(actor); }
        Validate();
    }

    public static void Validate()
    {
        var actor = Load<GameObject>(Prefab);
        var feedback = actor.GetComponent<WorkProgressFeedback>();
        if (!feedback) throw new InvalidOperationException("Survivor work feedback is missing.");
        var serialized = new SerializedObject(feedback);
        var icon = serialized.FindProperty("icon").objectReferenceValue as Image;
        var label = serialized.FindProperty("amountLabel").objectReferenceValue as TMP_Text;
        var bubble = serialized.FindProperty("bubble").objectReferenceValue as CanvasGroup;
        if (!icon || !label || !label.font || !bubble || bubble.blocksRaycasts || icon.raycastTarget || label.raycastTarget ||
            !icon.transform.IsChildOf(bubble.transform) || !label.transform.IsChildOf(bubble.transform) ||
            label.rectTransform.anchoredPosition.x <= icon.rectTransform.anchoredPosition.x ||
            serialized.FindProperty("planningIcon").objectReferenceValue != Load<Sprite>(Planning) ||
            serialized.FindProperty("graphicsIcon").objectReferenceValue != Load<Sprite>(Graphics) ||
            serialized.FindProperty("programmingIcon").objectReferenceValue != Load<Sprite>(Programming))
            throw new InvalidOperationException("Survivor work feedback layout or references are invalid.");
    }

    public static bool Matches(CharacterIdentity actor, GameDevField field, float amount)
    {
        var bubble = actor.transform.Find("Work feedback");
        if (!bubble || !bubble.gameObject.activeInHierarchy) return false;
        string path = field == GameDevField.Planning ? Planning : field == GameDevField.Graphics ? Graphics : Programming;
        var icon = bubble.GetComponentInChildren<Image>();
        var label = bubble.GetComponentInChildren<TMP_Text>();
        if (!icon || icon.sprite != Load<Sprite>(path) || !label) return false;
        float left = icon.rectTransform.anchoredPosition.x + icon.rectTransform.rect.xMin;
        float right = label.rectTransform.anchoredPosition.x + label.textBounds.max.x;
        if (Mathf.Abs(bubble.localPosition.x) > 0.001f || Mathf.Abs(left + right) > 0.1f) return false;
        string text = label.text;
        return text.StartsWith("+", StringComparison.Ordinal) && text.EndsWith("%", StringComparison.Ordinal) &&
            float.TryParse(text.Substring(1, text.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) &&
            Mathf.Abs(number - amount) <= 0.00051f;
    }

    static RectTransform Child(string name, Transform parent, float x, float width, float height)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, 0);
        return rect;
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (!asset) throw new InvalidOperationException("Missing work feedback asset at " + path);
        return asset;
    }

    static void ImportIcon(string path)
    {
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
            importer.filterMode == FilterMode.Point && !importer.mipmapEnabled &&
            importer.textureCompression == TextureImporterCompression.Uncompressed) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
