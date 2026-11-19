using System;
using System.Linq;
using GameJam.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

[InitializeOnLoad]
public sealed class EndingPreviewWindow : EditorWindow
{
    const string PendingKey = "UnityGameJam.EndingPreview.Pending";
    static bool loading;

    static EndingPreviewWindow()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/GameDev/엔딩 테스트")]
    static void Open()
    {
        var window = GetWindow<EndingPreviewWindow>("엔딩 테스트");
        window.minSize = new Vector2(300, 95);
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("엔딩을 선택하면 테스트용 벙커에서 컷씬과 결과창까지 재생합니다.", MessageType.None);
        bool changingMode = EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying;
        bool inOtherScene = EditorApplication.isPlaying && SceneManager.GetActiveScene().name != EndingCutscene.SceneName;
        using (new EditorGUI.DisabledScope(loading || changingMode || inOtherScene))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("배드", GUILayout.Height(32))) Request("ending_unfinished");
            if (GUILayout.Button("노멀", GUILayout.Height(32))) Request("ending_released");
            if (GUILayout.Button("굿", GUILayout.Height(32))) Request("ending_breakthrough");
        }
        if (loading) GUILayout.Label("엔딩을 불러오는 중…");
        else if (inOtherScene) GUILayout.Label("플레이 모드를 종료하면 테스트를 시작할 수 있습니다.");
    }

    static void Request(string endingId)
    {
        if (loading) return;
        if (EditorApplication.isPlaying) Launch(endingId);
        else
        {
            SessionState.SetString(PendingKey, endingId);
            EditorApplication.EnterPlaymode();
        }
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.EraseString(PendingKey);
            loading = false;
        }
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        var requested = SessionState.GetString(PendingKey, "");
        SessionState.EraseString(PendingKey);
        if (requested.Length > 0) Launch(requested);
        else if (SceneManager.GetActiveScene().name == EndingCutscene.SceneName) Open();
    }

    static async void Launch(string endingId)
    {
        loading = true;
        var exit = Application.exitCancellationToken;
        try
        {
            await SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene);
            exit.ThrowIfCancellationRequested();
            var sim = FindFirstObjectByType<BunkerSimulation>();
            await sim.InitializeAsync();
            exit.ThrowIfCancellationRequested();
            var catalog = GameDevEndingCatalog.FromTable(GameData.GetTable(GameDevKeys.EndingsTable));
            var ending = catalog.Active.Single(e => e.Id == endingId);
            var manager = FindFirstObjectByType<GameDevManager>();
            manager.SetScoreCalculator(new GameDevScoreCalculator());
            if (!manager.FinishDevelopment() || !manager.SubmitFinalScore(ending.MinimumScore))
                throw new InvalidOperationException("테스트 점수를 적용할 수 없습니다.");
            while (SceneManager.GetActiveScene().name != EndingCutscene.SceneName)
                await Awaitable.NextFrameAsync(exit);
        }
        catch (OperationCanceledException) when (exit.IsCancellationRequested) { }
        catch (Exception error) { Debug.LogException(error); }
        finally
        {
            loading = false;
            foreach (var window in Resources.FindObjectsOfTypeAll<EndingPreviewWindow>()) window.Repaint();
        }
    }
}
