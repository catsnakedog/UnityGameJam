using System;
using GameJam.Audio;
using GameJam.Data;
using ShelterRun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.Shelter;

public sealed class TutorialBriefing : MonoBehaviour
{
    public const string SceneName = "Tutorial";
    public RectTransform viewport, corridor, entrance;
    public TutorialBroadcast dialogue;
    RectTransform focus;
    bool overview;
    string starter;

    async void Start()
    {
        GameAudio.Music(GameSound.Prologue);
        focus = corridor;
        try
        {
            await GameData.InitializeAsync();
            if (!this) return;
            var rules = await GameData.LoadSettingsAsync<FarmingRules>(FarmingDataKeys.Rules);
            if (!this) return;
            starter = rules.StartingPersonId;
            dialogue.Open("tutorial_", "클릭하여 파밍 시작", Focus, BeginFarming);
        }
        catch (Exception error)
        {
            if (!this) return;
            Debug.LogException(error, this);
            dialogue.ShowError("클릭하여 타이틀로", () => SceneManager.LoadSceneAsync(TitleMenu.SceneName));
        }
    }

    void BeginFarming()
    {
        FarmingRunTransfer.BeginRun(starter, BunkerDifficulty.Medium, tutorial: true);
        SceneManager.LoadSceneAsync("Farming");
    }

    void Focus(string situation)
    {
        overview = situation == "tutorial_overview";
        focus = situation == "tutorial_entrance" ? entrance : corridor;
    }

    void LateUpdate()
    {
        if (!focus || viewport.rect.height <= 0) return;
        float farmingZoom = viewport.rect.height / corridor.rect.height;
        float targetZoom = overview ? 1f : farmingZoom * (focus == entrance ? 1.18f : 1f);
        if (!GameOptions.BlocksInput)
            corridor.localScale = Vector3.one * Mathf.MoveTowards(corridor.localScale.x, targetZoom, Time.unscaledDeltaTime * 5f);
        dialogue.Highlight(focus, focus == corridor ? 0 : 14);
    }
}
