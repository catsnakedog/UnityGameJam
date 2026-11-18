using System;
using UnityEditor;

namespace GameJam.Data.Editor
{
    [Serializable]
    public sealed class SpreadsheetSource
    {
        public bool enabled = true;
        public string tableName = "";
        public string url = "";
    }

    [FilePath("ProjectSettings/GameDataTool.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class DataToolSettings : ScriptableSingleton<DataToolSettings>
    {
        public string csvFolder = "Assets/Settings/Data/Csv";
        public string spreadConfigUrl = "";
        public void SaveSettings() => Save(true);
    }
}
