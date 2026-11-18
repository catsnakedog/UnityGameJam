using UnityEditor;
using UnityEngine;
using UnityGameJam.Shelter;

[CustomEditor(typeof(BunkerSimulation))]
public sealed class BunkerSimulationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var simulation = (BunkerSimulation)target;
        if (GUILayout.Button("Open Bunker Rules"))
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<BunkerRules>(BunkerSetup.RulesPath);
        if (!Application.isPlaying) return;
        EditorGUILayout.LabelField("Tick", simulation.CurrentTick + " / " + (simulation.Rules ? simulation.Rules.TotalTicks : 0));
        EditorGUILayout.LabelField("Remaining", simulation.RemainingTicks.ToString());
        simulation.Paused = EditorGUILayout.Toggle("Paused", simulation.Paused);
        if (!string.IsNullOrEmpty(simulation.LastError)) EditorGUILayout.HelpBox(simulation.LastError, MessageType.Error);
        Repaint();
    }
}
