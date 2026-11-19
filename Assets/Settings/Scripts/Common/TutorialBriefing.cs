using System;
using System.Linq;
using GameJam.Audio;
using GameJam.Data;
using ShelterRun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGameJam.Shelter;

public sealed class TutorialBriefing : MonoBehaviour, IPointerClickHandler
{
    public const string SceneName = "Tutorial";
    public RectTransform viewport, corridor, entrance, bag, clockRegion;
    public GameObject bagPanel;
    public TMP_Text bagCount;
    public Sprite[] bagExamples;
    public RectTransform leftShade, rightShade, bottomShade, topShade;
    public Image speaker;
    public SpriteRenderer sampler;
    public AnimationClip arrival, broadcast;
    public RectTransform bubble;
    public TMP_Text caption, hint;

    DataRow[] lines;
    RectTransform focus;
    string starter;
    int index = -1;
    float elapsed;
    bool overview, entered, leaving, failed;

    async void Start()
    {
        GameAudio.Music(GameSound.Prologue);
        bubble.gameObject.SetActive(false);
        hint.gameObject.SetActive(false);
        focus = corridor;
        try
        {
            await GameData.InitializeAsync();
            if (!this) return;
            BindBagPreview();
            lines = GameData.GetTable("ShelterDialogue").Rows.Where(row => row.Get<string>("role") == "주최측")
                .OrderBy(row => row.Id, StringComparer.Ordinal).ToArray();
            if (lines.Length == 0) throw new InvalidOperationException("튜토리얼 방송 대사가 없습니다.");
            var rules = await GameData.LoadSettingsAsync<FarmingRules>(FarmingDataKeys.Rules);
            if (this) starter = rules.StartingPersonId;
        }
        catch (Exception error)
        {
            if (!this) return;
            Debug.LogException(error, this);
            failed = true;
            caption.text = "안내 방송을 불러올 수 없습니다.";
            hint.text = "클릭하여 타이틀로";
            bubble.gameObject.SetActive(true);
            hint.gameObject.SetActive(true);
        }
    }

    void BindBagPreview()
    {
        var slots = bagPanel.GetComponentsInChildren<ShelterInventorySlot>(true);
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            Sprite icon = i < bagExamples.Length ? bagExamples[i] : null;
            if (icon && GameData.TryGet(ShelterItemCatalog.TableName, icon.name, out var item))
            {
                slot.Bind(null, item.Get<string>("displayName"), string.Empty, icon);
                count++;
            }
            else slot.Bind(null, string.Empty, string.Empty, null);
        }
        bagCount.text = $"보유 {count}개";
    }

    void Update()
    {
        if (GameOptions.BlocksInput || failed || leaving) return;
        elapsed += Time.unscaledDeltaTime;
        if (!entered && elapsed >= arrival.length && starter != null)
        {
            entered = true;
            ShowLine(0);
        }
        var clip = entered ? broadcast : arrival;
        float time = entered ? elapsed % clip.length : Mathf.Min(elapsed, clip.length - .001f);
        clip.SampleAnimation(sampler.gameObject, time);
        var frame = sampler.sprite;
        if (speaker.sprite != frame && frame)
        {
            speaker.sprite = frame;
            speaker.rectTransform.pivot = frame.pivot / frame.rect.size;
            speaker.rectTransform.sizeDelta = frame.rect.size * 3;
        }
    }

    public void OnPointerClick(PointerEventData pointer)
    {
        if (pointer.button != PointerEventData.InputButton.Left || GameOptions.BlocksInput || leaving) return;
        if (failed) { leaving = true; SceneManager.LoadSceneAsync(TitleMenu.SceneName); return; }
        if (!entered) return;
        if (index + 1 < lines.Length) ShowLine(index + 1);
        else
        {
            leaving = true;
            FarmingRunTransfer.BeginRun(starter);
            SceneManager.LoadSceneAsync("Farming");
        }
    }

    void ShowLine(int next)
    {
        index = next;
        elapsed = 0;
        var line = lines[next];
        string situation = line.Get<string>("situation");
        overview = situation == "tutorial_overview";
        switch (situation)
        {
            case "tutorial_entrance": focus = entrance; break;
            case "tutorial_bag": focus = bag; break;
            case "tutorial_clock": focus = clockRegion; break;
            default: focus = corridor; break;
        }
        bagPanel.SetActive(focus == bag);
        caption.text = line.Get<string>("line");
        float width = Mathf.Clamp(caption.GetPreferredValues(caption.text).x + 68, 350, 1180);
        float height = caption.GetPreferredValues(caption.text, width - 68, Mathf.Infinity).y;
        bubble.sizeDelta = new Vector2(width, Mathf.Ceil(height) + 55);
        // The speaker's ceiling strip crosses the clock. Bring the clock above it for its cue.
        if (focus == clockRegion) clockRegion.SetAsLastSibling();
        else clockRegion.SetSiblingIndex(bag.GetSiblingIndex());
        hint.text = next == lines.Length - 1 ? "클릭하여 파밍 시작" : "클릭하여 다음";
        bubble.gameObject.SetActive(true);
        hint.gameObject.SetActive(true);
    }

    void LateUpdate()
    {
        if (!focus || viewport.rect.width <= 0) return;
        // Farming frames the full height of the corridor with its left edge at the screen edge.
        float farmingZoom = viewport.rect.height / corridor.rect.height;
        float targetZoom = overview ? 1f : farmingZoom * (focus == entrance ? 1.18f : 1f);
        if (!GameOptions.BlocksInput)
            corridor.localScale = Vector3.one * Mathf.MoveTowards(corridor.localScale.x, targetZoom, Time.unscaledDeltaTime * 5f);
        var local = viewport.rect;
        Vector2 min = viewport.InverseTransformPoint(focus.TransformPoint(focus.rect.min));
        Vector2 max = viewport.InverseTransformPoint(focus.TransformPoint(focus.rect.max));
        float pad = focus == corridor ? 0 : 14;
        Vector2 lo = Vector2.Max(Vector2.zero, (min - local.min - Vector2.one * pad) / local.size);
        Vector2 hi = Vector2.Min(Vector2.one, (max - local.min + Vector2.one * pad) / local.size);
        Shade(leftShade, Vector2.zero, new Vector2(lo.x, 1));
        Shade(rightShade, new Vector2(hi.x, 0), Vector2.one);
        Shade(bottomShade, new Vector2(lo.x, 0), new Vector2(hi.x, lo.y));
        Shade(topShade, new Vector2(lo.x, hi.y), new Vector2(hi.x, 1));
    }

    static void Shade(RectTransform panel, Vector2 min, Vector2 max)
    {
        panel.anchorMin = min;
        panel.anchorMax = max;
    }
}
