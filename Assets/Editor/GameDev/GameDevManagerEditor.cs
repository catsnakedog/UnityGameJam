using UnityEditor;
using UnityEngine;
using UnityGameJam.GameDev;

[CustomEditor(typeof(GameDevManager))]
public sealed class GameDevManagerEditor : Editor
{
    double testScore;

    public override void OnInspectorGUI()
    {
        var manager = (GameDevManager)target;
        EditorGUILayout.HelpBox("평가 = 진척 합 ÷ (목표 진척 합 × 현재 벙커 틱 ÷ 기준 틱). BunkerRules에서 평가 기준을 조정합니다.", MessageType.Info);
        EditorGUILayout.LabelField("상태", manager.Phase.ToString());
        if (!Application.isPlaying) return;
        EditorGUILayout.LabelField("Play Mode 검증 도구 (게임 규칙/저장값 아님)", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(manager.Phase != GameDevPhase.Developing))
        {
            Slider(manager, GameDevField.Planning, "기획");
            Slider(manager, GameDevField.Programming, "프로그래밍");
            Slider(manager, GameDevField.Graphics, "그래픽");
            if (GUILayout.Button("개발 종료 / 점수 계산 요청")) manager.FinishDevelopment();
        }
        using (new EditorGUI.DisabledScope(manager.Phase != GameDevPhase.AwaitingScore))
        {
            testScore = EditorGUILayout.DoubleField("검증용 외부 총점", testScore);
            if (GUILayout.Button("외부 총점 전달 / 엔딩 확인")) manager.SubmitFinalScore(testScore);
        }
        if (manager.Ending != null) EditorGUILayout.LabelField("엔딩", manager.Ending.Id);
        if (manager.TotalScore.HasValue) EditorGUILayout.LabelField("총점", manager.TotalScore.Value.ToString());
        if (!string.IsNullOrEmpty(manager.LastError)) EditorGUILayout.HelpBox(manager.LastError, MessageType.Error);
        if (manager.Phase != GameDevPhase.Loading && manager.Phase != GameDevPhase.Failed && GUILayout.Button("개발 진행 초기화")) manager.RestartDevelopment();
        if (manager.Phase == GameDevPhase.Failed && GUILayout.Button("데이터 로드 재시도")) Retry(manager);
        Repaint();
    }

    static void Slider(GameDevManager manager, GameDevField field, string label)
    {
        float before = manager.Progress.Get(field);
        float after = EditorGUILayout.Slider(label, before, 0, GameDevProgress.MaximumPercent);
        if (before != after) manager.SetProgress(field, after);
    }

    static async void Retry(GameDevManager manager)
    {
        try { await manager.InitializeAsync(); }
        catch (System.Exception error) { Debug.LogException(error, manager); }
    }
}
