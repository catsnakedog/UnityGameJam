using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameJam.Data;
using GameJam.Data.Editor;
using ShelterRun;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityGameJam.Events;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

public static class EventPopupChecks
{
    static void Check(bool ok,string name,List<string> results) { if(!ok) throw new Exception("FAIL "+name); results.Add("PASS "+name); }
    static EventCatalog Catalog()
    {
        var plan=DataImporter.Preview(DataToolSettings.instance.csvFolder);
        return EventCatalog.FromTable(plan.Tables.Single(t=>t.Name==EventCatalog.TableName),new ShelterItemCatalog(plan.Tables.Single(t=>t.Name==ShelterItemCatalog.TableName)));
    }
    public static string RunEdit()
    {
        var r=new List<string>(); var catalog=Catalog();
        Check(catalog.Entries.Count==12,"All 12 user event rows",r);
        Check(catalog.Find("ev_party").RequiredItem=="choco" && catalog.Find("ev_party").RequiredCount==1,"Party chocolate eligibility is CSV data",r);
        Check(catalog.Entries.Where(e=>e.Id!="ev_party").All(e=>e.RequiredCount==0),"No accidental conditions on other events",r);
        foreach(string path in new[]{"Events.csv","ShelterItems.csv"}) Check(File.ReadAllBytes("Assets/Settings/Data/Csv/"+path).Take(3).SequenceEqual(new byte[]{239,187,191}),path+" BOM",r);
        foreach(string text in new[]{"MENTAL(everyone, -5)","PROGRESS(other, +1)","NEXT_WAVE_HP(4)","ITEM(choco, NaN)","System.Exit()","MENTAL(all, -5)junk"})
        { bool failed=false; try { EventEffect.Parse(text); } catch(FormatException) { failed=true; } Check(failed,"Reject malformed effect "+text,r); }
        bool rejected=false; try { EventEffect.ParseMany(new[]{"NONE","ITEM(choco, +1)"}); } catch(FormatException) {rejected=true;}
        Check(rejected,"NONE cannot mix with mutations",r);
        return Save("edit",r);
    }
    static async Task Frame() { int start=Time.frameCount; while(Time.frameCount==start) await Task.Delay(15); }
    static async Task Reveal(EventLaptopPopup popup) { await Frame(); popup.Confirm(); await Frame(); }
    public static async Task<string> CheckPresentationInPlay(bool pressEnter)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Open a scheduled presentation in Play mode.");
        var results = new List<string>();
        var controller = UnityEngine.Object.FindFirstObjectByType<EventPopupController>();
        var sim = UnityEngine.Object.FindFirstObjectByType<BunkerSimulation>();
        var commander = UnityEngine.Object.FindFirstObjectByType<ShelterCommander>();
        var popup = controller.Popup;
        int tick = sim.CurrentTick;
        Check(popup.ShowingPresentation && !popup.CanChoose && !popup.IsTyping, "Presentation opens directly on its evaluation", results);
        var buttons = popup.GetComponentsInChildren<Button>();
        Check(buttons.Length == 1 && buttons[0].name == "Confirm" && buttons[0].GetComponentInChildren<TMP_Text>().text == "계속", "Only Continue is available", results);
        var progress = sim.GetComponent<GameDevManager>().Progress;
        var calculator = new GameDevScoreCalculator(sim.Rules.EvaluationTargetProgress, sim.Rules.TotalTicks, () => tick);
        Check(calculator.TryCalculate(progress, out double score), "Scheduled tick has a score", results);
        var grade = sim.Rules.GradePresentation(score);
        var labels = popup.GetComponentsInChildren<TMP_Text>().Where(t => t.name != "Label").ToDictionary(t => t.name);
        string content = labels["Content"].text;
        Check(labels["Title"].text == "중간 발표" && string.IsNullOrEmpty(labels["Number"].text) && string.IsNullOrEmpty(labels["Hint"].text) &&
            new[] { "기획", "프로그래밍", "그래픽", $"평가 점수  {score:0.000}", $"멘탈  {grade.MentalDelta:+0;-0;0}" }.All(content.Contains) &&
            labels["Result"].text == grade.Label && ((Color32)labels["Result"].color).Equals((Color32)grade.Color),
            "Presentation shows its evaluation without the round, tick or keyboard hint", results);
        labels["Result"].ForceMeshUpdate();
        Check(labels["Result"].textInfo.characterInfo[0].pointSize > labels["Title"].fontSize && !labels["Result"].isTextOverflowing,
            "Grade is larger than the title and fits its label", results);
        Check(sim.IsPaused && commander.InputBlocked && !commander.ClickWorld(Vector2.zero), "Presentation blocks world commands", results);
        sim.Advance(sim.Rules.SecondsPerTick);
        Check(sim.CurrentTick == tick, "Presentation holds its tick", results);

        var previousKeyboard = Keyboard.current;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var externalPause = new object();
        try
        {
            float ordinaryTimeout = new SerializedObject(popup).FindProperty("resultDisplaySeconds").floatValue;
            await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0.5f, ordinaryTimeout) + 0.2f));
            await Frame();
            Check(popup.ShowingPresentation && sim.IsPaused, "Presentation remains open beyond the ordinary event timeout", results);
            void Press(Key key)
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
                popup.SendMessage("Update");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            }
            Press(Key.Y); Press(Key.N); Press(Key.Space);
            Check(popup.ShowingPresentation && !controller.ChoiceApplied, "Y, N and Space leave the evaluation open", results);
            sim.AcquirePause(externalPause);
            if (pressEnter) Press(Key.Enter);
            else buttons[0].onClick.Invoke();
            await Frame();
            Check(!popup.IsOpen && !commander.InputBlocked && sim.IsPaused, "Confirmation releases popup input and preserves external pause", results);
            sim.ReleasePause(externalPause);
            Check(sim.IsPaused == sim.Paused, "Popup simulation pause released", results);
        }
        finally
        {
            sim.ReleasePause(externalPause);
            InputSystem.RemoveDevice(keyboard);
            previousKeyboard?.MakeCurrent();
        }
        return string.Join("\n", results);
    }
    public static async Task<string> RunPlay()
    {
        if(!EditorApplication.isPlaying) throw new Exception("Play required.");
        var r=new List<string>(); float oldScale=Time.timeScale; Time.timeScale=0;
        try
        {
            FarmingRunTransfer.Capture(new[]{"person_01","person_02","person_03"},AssetDatabase.LoadAssetAtPath<FarmingDatabase>("Assets/Settings/Farming/FarmingDatabase.asset"));
            var load=SceneManager.LoadSceneAsync(GameDevKeys.ShelterScene); while(!load.isDone) await Task.Yield();
            var sim=UnityEngine.Object.FindFirstObjectByType<BunkerSimulation>(); await sim.InitializeAsync();
            var ctrl=UnityEngine.Object.FindFirstObjectByType<EventPopupController>();
            var dev=UnityEngine.Object.FindFirstObjectByType<GameDevManager>();
            var roster=UnityEngine.Object.FindFirstObjectByType<ShelterRoster>();
            var supplies=ctrl.Supplies;
            for(int i=0;i<100&&!ctrl.IsReady;i++) await Task.Delay(30);
            Check(ctrl.IsReady && roster.Characters.Count==3,"Built catalog and rescued character roster loaded",r);
            var catalog=EventCatalog.FromTable(GameData.GetTable(EventCatalog.TableName),supplies.Catalog);
            var rooms=UnityEngine.Object.FindObjectsByType<ShelterArea>(FindObjectsSortMode.None);
            var types=new[]{ShelterRoomType.Programming,ShelterRoomType.Planning,ShelterRoomType.Graphics};
            for(int i=0;i<3;i++)
            {
                var walker=roster.Characters[i].GetComponent<ShelterWalker>();
                var room = rooms.Single(candidate => candidate.roomType == types[i]);
                // These effect fixtures need known room membership before resolving choices.
                walker.transform.position = room.ArrivalPoint(0);
                walker.ResolveInitialRoom();
                Check(walker.CurrentRoom == room, "Assigned effect fixture to " + types[i], r);
            }
            // Golden expectations from the user's table: mental sum, plan/prog/art, choco/monster/bacchus, next HP.
            string[] ids={"ev_door","ev_radio","ev_blackout","ev_vending","ev_fight","ev_bug","ev_sns","ev_idea","ev_bacchus","ev_barricade","ev_party","ev_commit"};
            int[][] yes={new[]{0,0,0,0,1,0,0,4},new[]{-15,1,1,1,0,0,0,0},new[]{-15,0,0,0,0,0,0,0},new[]{0,0,0,0,0,2,0,4},new[]{-10,0,0,0,0,0,0,0},new[]{5,0,-2,0,0,0,0,0},new[]{-15,0,0,0,0,0,0,-4},new[]{0,3,-2,-2,0,0,0,0},new[]{-10,0,0,0,0,0,2,0},new[]{0,-1,-1,-1,0,0,0,-6},new[]{15,0,0,0,-1,0,0,0},new[]{15,-1,-1,-1,0,0,0,0}};
            int[][] no={new[]{-15,0,0,0,0,0,0,0},new int[8],new[]{0,-2,-2,-2,0,0,0,0},new int[8],new[]{-20,0,0,2,0,0,0,0},new[]{-15,0,0,0,0,0,0,0},new int[8],new[]{-10,0,0,0,0,0,0,0},new int[8],new int[8],new int[8],new[]{-15,0,0,0,0,0,0,0}};
            for(int i=0;i<ids.Length;i++) for(int choice=0;choice<2;choice++)
            {
                dev.RestartDevelopment(); foreach(var actor in roster.Characters) actor.SetMental(50);
                foreach(GameDevField f in Enum.GetValues(typeof(GameDevField))) dev.SetProgress(f,40);
                foreach(string id in new[]{"choco","monster","bacchus"}) supplies.Add(id,3);
                var entry=catalog.Find(ids[i]); var expect=choice==0?yes[i]:no[i];
                var result=EventEffectExecutor.Apply(choice==0?entry.Yes:entry.No,roster,dev,supplies,sim);
                Check(roster.Characters.Sum(c=>c.CurrentMental)==150+expect[0] && dev.Progress.Planning==40+expect[1] && dev.Progress.Programming==40+expect[2] && dev.Progress.Graphics==40+expect[3] && supplies.GetCount("choco")==3+expect[4] && supplies.GetCount("monster")==3+expect[5] && supplies.GetCount("bacchus")==3+expect[6] && sim.PendingWaveHpModifier==expect[7],ids[i]+(choice==0?" Yes":" No")+" exact effects",r);
                Check(!string.IsNullOrWhiteSpace(result) && !result.Contains('\n'),"Single line actual result",r);
            }
            dev.RestartDevelopment();
            Check(ctrl.ShowEvent("ev_party")==EventShowResult.Ineligible,"Party cannot open without chocolate",r);
            Check(Enumerable.Range(0,200).All(_=>catalog.Pick("ev_door",supplies)?.Id is string id && id!="ev_door" && id!="ev_party"),"Picker excludes previous and ineligible events",r);
            var before=roster.Characters.Sum(c=>c.CurrentMental); bool rejected=false;
            try { EventEffectExecutor.Apply(catalog.Find("ev_party").Yes,roster,dev,supplies,sim); } catch(InvalidOperationException) {rejected=true;}
            Check(rejected && roster.Characters.Sum(c=>c.CurrentMental)==before && supplies.GetCount("choco")==0,"Insufficient stock rejects whole choice atomically",r);
            dev.SetProgress(GameDevField.Graphics,100); dev.SetProgress(GameDevField.Planning,99); dev.SetProgress(GameDevField.Programming,0);
            EventEffectExecutor.Apply(EventEffect.ParseMany(new[]{"PROGRESS(open, +3)"}),roster,dev,supplies,sim);
            Check(dev.Progress.Graphics==100 && dev.Progress.Planning==100 && dev.Progress.Programming==3,"Open axes only with percent bounds",r);
            EventEffectExecutor.Apply(EventEffect.ParseMany(new[]{"MENTAL(all, -999)"}),roster,dev,supplies,sim);
            Check(roster.Characters.All(c=>c.CurrentMental==0),"Mental lower bound",r);
            dev.RestartDevelopment(); supplies.Add("choco",1);
            Check(ctrl.ShowEvent("ev_party")==EventShowResult.Shown,"Eligible party opens",r);
            await Reveal(ctrl.Popup);
            ctrl.Popup.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Yes").onClick.Invoke();
            ctrl.Choose(true); ctrl.Choose(false);
            Check(supplies.GetCount("choco")==0 && ctrl.Popup.ShowingResult && sim.IsPaused,"Yes button applies once and result retains pause",r);
            Check(roster.Characters.All(c=>c.CurrentMental==c.Definition.Mental+5),"Party mental applies once",r);
            await Task.Delay(3300);
            Check(!ctrl.Popup.IsOpen && !sim.IsPaused,"Result auto-closes before time resumes",r);
            dev.RestartDevelopment(); int interval=sim.Rules.RandomEventEveryTicks;
            sim.Advance(interval*sim.Rules.SecondsPerTick);
            Check(ctrl.Popup.IsOpen && ctrl.LastEventTick==interval,"Tick interval event",r);
            string first=ctrl.CurrentEventId; await Reveal(ctrl.Popup); ctrl.Popup.RequestChoice(false); await Frame(); ctrl.Popup.Confirm();
            sim.Advance(interval*sim.Rules.SecondsPerTick);
            Check(ctrl.Popup.IsOpen && ctrl.CurrentEventId!=first,"Consecutive tick events differ",r);
            dev.RestartDevelopment();
            int firstInvasion = sim.Rules.FirstInvasionTick;
            ctrl.enabled=false; sim.Advance((firstInvasion-1)*sim.Rules.SecondsPerTick); ctrl.enabled=true; await Frame();
            Check(ctrl.ShowEvent("ev_door")==EventShowResult.Shown,"Ordinary event opens before the first scheduled invasion",r);
            sim.Advance(sim.Rules.SecondsPerTick);
            Check(sim.CurrentTick==firstInvasion-1 && sim.LastWaveTick==0,"Open event pauses the clock before the first invasion",r);
            sim.SetPendingWaveHpModifier(-4); ctrl.Popup.Close(); sim.Advance(sim.Rules.SecondsPerTick);
            if (ctrl.Popup.IsOpen) { ctrl.Popup.Close(); sim.Advance(0); }
            Check(sim.LastWaveTick==firstInvasion && sim.CurrentWaveHpModifier==-4 && sim.PendingWaveHpModifier==0 && sim.GetCurrentWaveEnemyHp(3)==1,"Next wave consumes adjustment once and HP floor",r);
            ctrl.enabled=false; sim.Advance(sim.Rules.InvasionEveryTicks*sim.Rules.SecondsPerTick);
            Check(sim.CurrentWaveHpModifier==0,"HP modifier does not leak to later waves",r); ctrl.enabled=true; await Frame();
            dev.RestartDevelopment();
            Check(supplies.GetCount("choco")==0 && sim.PendingWaveHpModifier==0 && roster.Characters.All(c=>c.CurrentMental==c.Definition.Mental),"Restart resets runtime effects without modifying DB",r);
            ctrl.ShowEvent("ev_door"); await Reveal(ctrl.Popup);
            return Save("play",r);
        }
        finally { Time.timeScale=oldScale; }
    }
    static string Save(string mode,List<string> results)
    { var report=string.Join("\n",results); File.WriteAllText("Docs/Events/Verification-effects-"+mode+".txt",report); return report; }
}
