using GameJam.Audio;
using System;
using System.Linq;
using GameJam.Data;
using ShelterRun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGameJam.Characters;
using UnityGameJam.Shelter;

public sealed class TitleMenu : MonoBehaviour
{
    public const string SceneName = "Title";
    public GameObject home, selection;
    public Button start, tutorial, options, quit, back, confirm;
    public Button easy, medium, hard;
    public Button[] candidates;
    public Image preview;
    public SpriteRenderer sampler;
    public SurvivorAnimation artwork;
    public ShelterCharacterCard card;
    public TMP_Text selectedName, description;
    readonly SurvivorAnimation.Playback playback = new SurvivorAnimation.Playback();
    CharacterDefinition[] people;
    BunkerRules rules;
    SurvivorAnimation.Artwork currentArt;
    int current;
    BunkerDifficulty difficulty = BunkerDifficulty.Hard;
    bool loading;

    async void Start()
    {
        GameAudio.Music(GameSound.Title);
        start.interactable = false;
        tutorial.onClick.AddListener(OpenTutorial);
        options.onClick.AddListener(GameOptions.Show);
        quit.onClick.AddListener(Quit);
        back.onClick.AddListener(() => SetSelection(false));
        start.onClick.AddListener(() => SetSelection(true));
        confirm.onClick.AddListener(Launch);
        easy.onClick.AddListener(() => ChooseDifficulty(BunkerDifficulty.Easy));
        medium.onClick.AddListener(() => ChooseDifficulty(BunkerDifficulty.Medium));
        hard.onClick.AddListener(() => ChooseDifficulty(BunkerDifficulty.Hard));
        ChooseDifficulty(difficulty);
        try
        {
            await GameData.InitializeAsync();
            if (!this) return;
            rules = await GameData.LoadSettingsAsync<BunkerRules>(BunkerRules.Address);
            if (!this) return;
            people = GameData.GetTable(CharacterKeys.Table).Rows.Select(row => new CharacterDefinition(row)).ToArray();
            if (people.Length != candidates.Length) throw new InvalidOperationException("Character selection slots do not match Characters.");
            for (int i = 0; i < candidates.Length; i++)
            {
                int slot = i;
                candidates[i].GetComponentInChildren<TMP_Text>().text = people[i].DisplayName;
                candidates[i].onClick.AddListener(() => Choose(slot));
            }
            start.interactable = true;
            if (!GameOptions.BlocksInput) start.Select();
        }
        catch (Exception e) { if (this) start.GetComponentInChildren<TMP_Text>().text = "인물 로드 실패"; Debug.LogException(e); }
    }

    void SetSelection(bool show)
    {
        GameAudio.Music(show ? GameSound.Prologue : GameSound.Title);
        home.SetActive(!show);
        selection.SetActive(show);
        GameOptions.ShowLauncher(show);
        if (show) { Choose(current); confirm.Select(); } else start.Select();
    }

    void ChooseDifficulty(BunkerDifficulty value)
    {
        difficulty = value;
        easy.interactable = value != BunkerDifficulty.Easy;
        medium.interactable = value != BunkerDifficulty.Medium;
        hard.interactable = value != BunkerDifficulty.Hard;
    }

    void Choose(int slot)
    {
        current = slot;
        var person = people[slot];
        currentArt = artwork.FindArtwork(person.Id);
        selectedName.text = person.DisplayName;
        description.text = person.Description;
        for (int i = 0; i < candidates.Length; i++) candidates[i].interactable = i != slot;
        card.Preview(person, rules);
        playback.Restart();
        Animate(0);
    }

    void Update()
    {
        if (selection.activeSelf && currentArt != null && !GameOptions.BlocksInput) Animate(Time.deltaTime);
    }

    void Animate(float dt)
    {
        playback.Sample(currentArt, SurvivorAnimation.Pose.Idle, SurvivorAnimation.Pose.Idle, sampler, dt);
        var sprite = sampler.sprite;
        preview.sprite = sprite;
        preview.rectTransform.pivot = sprite.pivot / sprite.rect.size;
        preview.rectTransform.sizeDelta = sprite.rect.size * 4;
    }

    void OpenTutorial()
    {
        if (loading || GameOptions.BlocksInput) return;
        loading = true;
        SceneManager.LoadSceneAsync(TutorialBriefing.SceneName);
    }

    void Launch()
    {
        if (loading || GameOptions.BlocksInput) return;
        loading = true;
        FarmingRunTransfer.BeginRun(people[current].Id, difficulty);
        SceneManager.LoadSceneAsync("Farming");
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
