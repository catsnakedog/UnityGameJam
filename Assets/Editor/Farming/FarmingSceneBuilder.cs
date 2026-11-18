using UnityEditor;
namespace ShelterRun.Editor
{
    // Existing entry point retained; all map authoring now uses the 2D tile contract.
    public static class FarmingSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Farming.unity";
        [MenuItem("Tools/Farming/Open Farming Scene")]
        public static void Build()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
