using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameJam.Data;
using GameJam.Data.Editor;
using ShelterRun;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameJam.Characters;
using UnityGameJam.Shelter;

public static class CharacterLinkChecks
{
    static readonly List<string> checks = new List<string>();
    static void Check(bool value, string label) { if (!value) throw new Exception(label); checks.Add("PASS: " + label); }
    static async Task Until(Func<bool> predicate)
    {
        var timeout = DateTime.UtcNow.AddSeconds(25);
        while (!predicate()) { if (DateTime.UtcNow > timeout) throw new TimeoutException(); await Task.Delay(30); }
    }
    public static async Task RunPlay()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play first.");
        checks.Clear();
        try
        {
            foreach (int count in new[] { 2, 0, 6 })
            {
                await SceneManager.LoadSceneAsync("Assets/Scenes/Farming.unity");
                FarmingSession session = null;
                await Until(() => (session = UnityEngine.Object.FindFirstObjectByType<FarmingSession>()) != null && session.Phase == FarmingPhase.Ready);
                Check(!FarmingRunTransfer.HasResult && FarmingRunTransfer.People.Count == 0, "Fresh farming clears previous roster");
                session.Begin();
                await Until(() => session.CanAct);
                var people = session.Pickups.Where(p => p.Record.Type == FarmingObjectType.Person).OrderBy(p => p.RecordId).Take(count).ToArray();
                foreach (var person in people) { session.Deposit(person); session.Deposit(person); }
                session.Deposit(session.Pickups.First(p => p.Record.Type == FarmingObjectType.Item));
                var expected = people.Select(p => p.RecordId).ToArray();
                session.Tick(session.Rules.Duration);
                session.EnterShelter();
                ShelterRoster roster = null;
                await Until(() => (roster = UnityEngine.Object.FindFirstObjectByType<ShelterRoster>()) != null && (roster.IsReady || roster.LastError != null));
                Check(roster.LastError == null, "Roster data loads");
                Check(roster.Characters.Select(p => p.Id).SequenceEqual(expected), count + " rescued: exact IDs, no items/unbanked/duplicate survivors");
                Check(UnityEngine.Object.FindObjectsByType<ShelterWalker>(FindObjectsSortMode.None).Length == count, "No fixed example characters: " + count);
                foreach (var actor in roster.Characters)
                {
                    var source = GameData.Get(CharacterKeys.Table, actor.Id);
                    Check(actor.Definition.Programming == source.Get<int>("programming") && actor.Definition.Art == source.Get<int>("art") && actor.Definition.Planning == source.Get<int>("planning") && actor.CurrentMental == source.Get<int>("mental"), "CSV abilities and mental on " + actor.Id);
                    actor.SetMental(0);
                    Check(source.Get<int>("mental") == actor.Definition.Mental, "Runtime mental does not mutate DB");
                    var walker = actor.GetComponent<ShelterWalker>();
                    Check(!walker.MoveTo(UnityEngine.Object.FindObjectsByType<ShelterArea>(FindObjectsSortMode.None).Single(a => a.roomType == ShelterRoomType.ZombieInvasion)), "Exhausted survivor cannot move");
                    actor.SetMental(actor.Definition.Mental);
                    Check(walker.MoveTo(UnityEngine.Object.FindObjectsByType<ShelterArea>(FindObjectsSortMode.None).Single(a => a.roomType == ShelterRoomType.ZombieInvasion)), "Spawned survivor can navigate to combat room");
                }
                await SceneManager.LoadSceneAsync("Assets/Scenes/ShelterPrototype.unity");
                await Until(() => (roster = UnityEngine.Object.FindFirstObjectByType<ShelterRoster>()) != null && roster.IsReady);
                Check(roster.Characters.Count == count, "Shelter reload preserves IDs without duplicate spawns");
            }
            FarmingRunTransfer.Reset();
            await SceneManager.LoadSceneAsync("Assets/Scenes/ShelterPrototype.unity");
            await Until(() => UnityEngine.Object.FindFirstObjectByType<ShelterRoster>()?.IsReady == true);
            Check(UnityEngine.Object.FindObjectsByType<ShelterWalker>(FindObjectsSortMode.None).Length == 0, "Direct shelter start has no fabricated survivors");
            File.WriteAllLines("Docs/CharacterLink-play.txt", checks);
        }
        catch (Exception e) { checks.Add("FAIL: " + e); File.WriteAllLines("Docs/CharacterLink-play.txt", checks); throw; }
    }
    public static void RunEdit()
    {
        checks.Clear();
        var plan = DataImporter.Preview("Assets/Settings/Data/Csv");
        CharacterSetup.Validate(plan.Tables.Single(t => t.Name == CharacterKeys.Table));
        Check(File.ReadAllBytes("Assets/Settings/Data/Csv/Characters.csv").Take(3).SequenceEqual(new byte[] {239,187,191}), "CSV UTF-8 BOM");
        var source = DataImporter.ReadSources("Assets/Settings/Data/Csv");
        var negativeAbility = System.Text.RegularExpressions.Regex.Replace(source["Characters.csv"], @"(?m)^(person_01,[^,\r\n]*,)[^,]+", "${1}-1");
        foreach (var variant in new[] { negativeAbility, source["Characters.csv"].Replace("person_06", FarmingDataKeys.SpaghettiCan), source["Characters.csv"].Replace("person_06", "unknown_person"), source["Characters.csv"].Replace("person_06", "person_01") })
        {
            bool rejected = false;
            try { var fixture = new Dictionary<string,string>(source); fixture["Characters.csv"] = variant; CharacterSetup.Validate(DataImporter.PreviewSources("Assets/Settings/Data/Csv", fixture).Tables.Single(t => t.Name == CharacterKeys.Table)); }
            catch { rejected = true; }
            Check(rejected, "Reject negative/Item ID/missing Person/duplicate ID fixture");
        }
        File.WriteAllLines("Docs/CharacterLink-edit.txt", checks);
    }
}

[CustomEditor(typeof(CharacterIdentity))]
public class CharacterIdentityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var actor = (CharacterIdentity)target;
        if (actor.Definition == null) { EditorGUILayout.HelpBox("Populated from Characters.csv after rescue.", MessageType.Info); return; }
        EditorGUILayout.LabelField("ID", actor.Id);
        EditorGUILayout.LabelField("Name", actor.Definition.DisplayName);
        EditorGUILayout.IntField("Programming", actor.Definition.Programming);
        EditorGUILayout.IntField("Art", actor.Definition.Art);
        EditorGUILayout.IntField("Planning", actor.Definition.Planning);
        EditorGUILayout.IntField("Current Mental", actor.CurrentMental);
    }
}
