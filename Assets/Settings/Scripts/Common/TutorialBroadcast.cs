using System;
using System.Linq;
using GameJam.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class TutorialBroadcast : MonoBehaviour, IPointerClickHandler
{
    public RectTransform leftShade, rightShade, bottomShade, topShade, bubble;
    public TMP_Text caption, hint;
    public Image speaker, soundWaves;
    public SpriteRenderer sampler;
    public AnimationClip arrival, broadcast;

    DataRow[] lines;
    Action<string> focus;
    Action finish;
    string lastHint;
    int index;
    float elapsed;

    public void Open(string situationPrefix, string finalPrompt, Action<string> showTarget, Action onFinish)
    {
        lines = GameData.GetTable("ShelterDialogue").Rows
            .Where(r => r.Get<string>("role") == "주최측" &&
                r.Get<string>("situation").StartsWith(situationPrefix, StringComparison.Ordinal))
            .OrderBy(r => r.Id, StringComparer.Ordinal).ToArray();
        if (lines.Length == 0) throw new InvalidOperationException("방송 대사가 없습니다: " + situationPrefix);
        focus = showTarget;
        finish = onFinish;
        lastHint = finalPrompt;
        index = -1;
        elapsed = 0;
        gameObject.SetActive(true);
        bubble.gameObject.SetActive(false);
        hint.gameObject.SetActive(false);
        soundWaves.enabled = false;
        Sample(arrival, 0);
    }

    void Update()
    {
        if (lines == null || GameOptions.BlocksInput) return;
        elapsed += Time.unscaledDeltaTime;
        if (index < 0 && elapsed < arrival.length)
        {
            Sample(arrival, elapsed);
            return;
        }
        if (index < 0)
        {
            elapsed -= arrival.length;
            ShowLine(0);
        }
        float time = elapsed % broadcast.length;
        Sample(broadcast, time);
        // The imported speaker cone moves by a pixel. Echo its beat beside the cabinet.
        float beat = Mathf.Repeat(time / broadcast.length * 2f, 1f);
        soundWaves.enabled = true;
        soundWaves.canvasRenderer.SetAlpha(1f - beat);
        soundWaves.rectTransform.localScale = Vector3.one * Mathf.Lerp(.78f, 1.12f, beat);
    }

    void Sample(AnimationClip clip, float time)
    {
        clip.SampleAnimation(sampler.gameObject, time);
        var frame = sampler.sprite;
        speaker.sprite = frame;
        speaker.rectTransform.pivot = frame.pivot / frame.rect.size;
        speaker.rectTransform.sizeDelta = frame.rect.size * 3;
    }

    public void OnPointerClick(PointerEventData pointer)
    {
        if (pointer.button != PointerEventData.InputButton.Left || GameOptions.BlocksInput || finish == null || index < 0) return;
        pointer.Use();
        if (lines != null && index + 1 < lines.Length) ShowLine(index + 1);
        else
        {
            var onFinish = finish;
            finish = null;
            lines = null;
            onFinish();
        }
    }

    void ShowLine(int next)
    {
        index = next;
        var row = lines[index];
        focus(row.Get<string>("situation"));
        ShowText(row.Get<string>("line"), index == lines.Length - 1 ? lastHint : "클릭하여 다음");
    }

    public void ShowError(string prompt, Action onFinish)
    {
        lines = null;
        index = 0;
        finish = onFinish;
        gameObject.SetActive(true);
        soundWaves.enabled = false;
        ShowText("안내 방송을 불러올 수 없습니다.", prompt);
    }

    void ShowText(string text, string prompt)
    {
        caption.text = text;
        float width = Mathf.Clamp(caption.GetPreferredValues(text).x + 68, 350, 1180);
        float height = caption.GetPreferredValues(text, width - 68, Mathf.Infinity).y;
        bubble.sizeDelta = new Vector2(width, Mathf.Ceil(height) + 55);
        hint.text = prompt;
        bubble.gameObject.SetActive(true);
        hint.gameObject.SetActive(true);
    }

    public void Highlight(RectTransform target, float padding, bool includeChildren = false)
    {
        var view = (RectTransform)transform;
        var rect = view.rect;
        if (rect.width <= 0 || rect.height <= 0) return;
        Vector2 min = view.InverseTransformPoint(target.TransformPoint(target.rect.min));
        Vector2 max = view.InverseTransformPoint(target.TransformPoint(target.rect.max));
        if (includeChildren)
        {
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(view, target);
            min = bounds.min;
            max = bounds.max;
        }
        Highlight((min - rect.min - Vector2.one * padding) / rect.size,
            (max - rect.min + Vector2.one * padding) / rect.size);
    }

    public void Highlight(Vector2 min, Vector2 max)
    {
        min = Vector2.Min(Vector2.one, Vector2.Max(Vector2.zero, min));
        max = Vector2.Max(min, Vector2.Min(Vector2.one, max));
        Shade(leftShade, Vector2.zero, new Vector2(min.x, 1));
        Shade(rightShade, new Vector2(max.x, 0), Vector2.one);
        Shade(bottomShade, new Vector2(min.x, 0), new Vector2(max.x, min.y));
        Shade(topShade, new Vector2(min.x, max.y), new Vector2(max.x, 1));
    }

    static void Shade(RectTransform panel, Vector2 min, Vector2 max)
    {
        panel.anchorMin = min;
        panel.anchorMax = max;
    }
}
