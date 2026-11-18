using UnityEditor;
using System.IO;
using UnityEditor.Build;

namespace GameJam.Data.Editor
{
    [FilePath("ProjectSettings/GameDataTool.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class DataToolSettings : ScriptableSingleton<DataToolSettings>
    {
        public bool useLiveSheets;
        public string csvFolder = "Assets/Settings/Data/Csv";
        public string spreadConfigUrl = "";
        public void SaveSettings() => Save(true);

        internal string PlayUrl => useLiveSheets ? GoogleSheets.CsvUrl(spreadConfigUrl) : "";

        [InitializeOnLoadMethod]
        static void BindPlaySettings() => SpreadsheetPlaySource.EditorUrl = () => instance.PlayUrl;
    }

    class SpreadsheetPlayBuildProcessor : BuildPlayerProcessor
    {
        public override int callbackOrder => 0;
        public override void PrepareForBuild(BuildPlayerContext context)
        {
            var path = Path.GetFullPath(Path.Combine("Library", SpreadsheetPlaySource.BuildFile));
            File.WriteAllText(path, DataToolSettings.instance.PlayUrl);
            context.AddAdditionalPathToStreamingAssets(path, SpreadsheetPlaySource.BuildFile);
        }
    }
}
