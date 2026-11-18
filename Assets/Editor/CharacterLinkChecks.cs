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
    static async Task<FarmingSession> ReadyFarm()
    {
        FarmingSession session = null;
        await Until(() => (session = UnityEngine.Object.FindFirstObjectByType<FarmingSession>()) && session.Phase == FarmingPhase.Ready);
        return session;
    }
    static void Deliver(FarmingSession session, FarmingPickup pickup)
    {
        var map = FarmingMap2D.Instance;
        var player = session.Player;
        var room = map.RoomAt(pickup.transform.position);
        var door = map.Doors.Single(d => d.Area == map.Corridor && d.Target.Area == room);
        player.Teleport(door.transform.position);
        player.Interact();
        player.Teleport(pickup.transform.position);
        player.Interact();
        Check(player.Inventory.Carried.Contains(pickup), "Pick up " + pickup.RecordId);
        player.Teleport(door.Target.transform.position);
        player.Interact();
        player.Teleport(session.Bunker.LandingPosition);
        player.Interact();
        Check(player.Inventory.Used == 0 && session.SecuredIds.Contains(pickup.RecordId), "Deliver " + pickup.RecordId);
        session.Deposit(pickup); // A repeated notification must not credit the same instance twice.
    }
    public static async Task RunPlay()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play first.");
        checks.Clear();
        try
        {
            foreach (int additional in new[] { 0, 1, 5 })
            {
                await SceneManager.LoadSceneAsync("Assets/Scenes/Farming.unity");
                var session = await ReadyFarm();
                if (additional == 0)
                {
                    session.Begin();
                    await Until(() => session.CanAct);
                    Deliver(session, session.Pickups.First(p => p.Record.Type == FarmingObjectType.Person));
                    session.Tick(session.Rules.Duration);
                    session.Restart();
                    session = await ReadyFarm();
                    Check(session.PeopleSaved == 1 && session.ItemsSaved == 0 && session.Pickups.All(p => p.State == PickupState.Available),
                        "Restart clears the prior rescue and restores the room pickups");
                }
                string starter = session.Rules.StartingPersonId;
                Check(!FarmingRunTransfer.HasResult && FarmingRunTransfer.People.Count == 0, "Fresh farming clears previous roster");
                Check(session.Player.Inventory.Used == 0 && session.SecuredIds.SequenceEqual(new[] { starter }) &&
                      session.Pickups.All(p => p.RecordId != starter), "A fresh run starts empty-handed with only the player secured");
                session.Begin();
                await Until(() => session.CanAct);
                var people = session.Pickups.Where(p => p.Record.Type == FarmingObjectType.Person).OrderBy(p => p.RecordId).Take(additional).ToArray();
                foreach (var person in people) Deliver(session, person);
                if (additional > 0) Deliver(session, session.Pickups.First(p => p.Record.Type == FarmingObjectType.Item));
                var expected = new[] { starter }.Concat(people.Select(p => p.RecordId)).ToArray();
                Check(expected.Length == additional + 1 && session.PeopleSaved == expected.Length, "Secured count including player: " + expected.Length);
                session.Tick(session.Rules.Duration);
                session.EnterShelter();
                ShelterRoster roster = null;
                await Until(() => (roster = UnityEngine.Object.FindFirstObjectByType<ShelterRoster>()) != null && (roster.IsReady || roster.LastError != null));
                Check(roster.LastError == null, "Roster data loads");
                await UnityEngine.Object.FindFirstObjectByType<ShelterTransit>().InitializeAsync();
                Check(roster.Characters.Select(p => p.Id).SequenceEqual(expected), additional + " additional rescues: exact IDs including the player once");
                Check(FarmingRunTransfer.Items.Count == (additional == 0 ? 0 : 1), "Exact number of secured items reaches shelter");
                Check(UnityEngine.Object.FindObjectsByType<ShelterWalker>(FindObjectsSortMode.None).Length == expected.Length, "Exact number of shelter characters: " + expected.Length);
                foreach (var actor in roster.Characters)
                {
                    var source = GameData.Get(CharacterKeys.Table, actor.Id);
                    Check(actor.Definition.Programming == source.Get<int>("programming") && actor.Definition.Art == source.Get<int>("art") && actor.Definition.Planning == source.Get<int>("planning") && actor.CurrentMental == source.Get<int>("mental"), "CSV abilities and mental on " + actor.Id);
                    actor.SetMental(0);
                    Check(source.Get<int>("mental") == actor.Definition.Mental, "Runtime mental does not mutate DB");
                    var walker = actor.GetComponent<ShelterWalker>();
                    Check(!walker.MoveTo(UnityEngine.Object.FindObjectsByType<ShelterArea>(FindObjectsSortMode.None).Single(a => a.roomType == ShelterRoomType.ZombieInvasion)), "Exhausted survivor cannot move");
                    actor.ResetMental();
                    Check(walker.MoveTo(UnityEngine.Object.FindObjectsByType<ShelterArea>(FindObjectsSortMode.None).Single(a => a.roomType == ShelterRoomType.ZombieInvasion)), "Spawned survivor can navigate to combat room");
                }
                await SceneManager.LoadSceneAsync("Assets/Scenes/ShelterPrototype.unity");
                await Until(() => (roster = UnityEngine.Object.FindFirstObjectByType<ShelterRoster>()) != null && roster.IsReady);
                Check(roster.Characters.Select(p => p.Id).SequenceEqual(expected), "Shelter reload preserves IDs without duplicate spawns");
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
        DrawDefaultInspector();
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
