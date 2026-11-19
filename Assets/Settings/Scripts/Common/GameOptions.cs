using GameJam.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public sealed class GameOptions : MonoBehaviour
{
    public Button launcher, close, mute;
    public TMP_Text muteLabel;
    public Slider master, music, effects, ui, ambience;
    public GameObject modal, fallbackEvents;
    static GameOptions instance;
    GameObject previousFocus;
    float previousScale;
    int closedFrame = -1;
    public static bool BlocksInput => instance && (instance.modal.activeSelf || instance.closedFrame == Time.frameCount);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (!instance) Instantiate(Resources.Load<GameObject>("GameOptions"));
    }

    void Awake()
    {
        instance = this;
        BindButtonSounds(gameObject);
        DontDestroyOnLoad(gameObject);
        var sound = SoundManager.Instance;
        master.onValueChanged.AddListener(sound.SetMasterVolume);
        music.onValueChanged.AddListener(sound.SetMusicVolume);
        effects.onValueChanged.AddListener(sound.SetSfxVolume);
        ui.onValueChanged.AddListener(sound.SetUIVolume);
        ambience.onValueChanged.AddListener(sound.SetAmbienceVolume);
        mute.onClick.AddListener(() => { sound.SetMuted(!sound.Muted); Sync(); });
        close.onClick.AddListener(Close);
        launcher.onClick.AddListener(Open);
        SceneManager.sceneLoaded += SceneLoaded;
        SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects()) BindButtonSounds(root);
        if (modal.activeSelf) Close();
        fallbackEvents.SetActive(false);
        fallbackEvents.SetActive(!FindFirstObjectByType<EventSystem>());
        launcher.gameObject.SetActive(scene.name != TitleMenu.SceneName);
    }

    static void BindButtonSounds(GameObject root)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            button.onClick.RemoveListener(GameAudio.Click);
            button.onClick.AddListener(GameAudio.Click);
        }
    }

    public static void Show() { if (instance) instance.Open(); }
    public static void ShowLauncher(bool show) { if (instance) instance.launcher.gameObject.SetActive(show); }

    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (modal.activeSelf) Close(); else Open();
    }

    void Sync()
    {
        var sound = SoundManager.Instance;
        master.SetValueWithoutNotify(sound.MasterVolume);
        music.SetValueWithoutNotify(sound.MusicVolume);
        effects.SetValueWithoutNotify(sound.SfxVolume);
        ui.SetValueWithoutNotify(sound.UIVolume);
        ambience.SetValueWithoutNotify(sound.AmbienceVolume);
        muteLabel.text = sound.Muted ? "음소거 켜짐" : "음소거 꺼짐";
    }

    void Open()
    {
        if (modal.activeSelf) return;
        previousFocus = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        previousScale = Time.timeScale;
        Time.timeScale = 0;
        modal.SetActive(true);
        Sync();
        close.Select();
    }

    void Close()
    {
        modal.SetActive(false);
        Time.timeScale = previousScale;
        closedFrame = Time.frameCount;
        SoundManager.Instance.SaveSettings();
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(previousFocus && previousFocus.activeInHierarchy ? previousFocus : null);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (modal && modal.activeSelf) Time.timeScale = previousScale;
        if (instance == this) instance = null;
    }
}
