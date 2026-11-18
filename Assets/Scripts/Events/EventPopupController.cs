using System;
using GameJam.Data;
using UnityEngine;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

namespace UnityGameJam.Events
{
    public enum EventShowResult { Shown, Busy, NotReady, NotFound, Ineligible }
    public sealed class EventPopupController : MonoBehaviour
    {
        [SerializeField] BunkerSimulation simulation;
        [SerializeField] GameDevManager development;
        [SerializeField] ShelterCommander commander;
        [SerializeField] ShelterRoster roster;
        [SerializeField] ShelterSupplies supplies;
        [SerializeField] EventLaptopPopup popup;
        EventCatalog catalog;
        EventDefinition current;
        int generation;
        string previousId;
        bool choiceApplied;
        readonly object loadingPause=new object(), popupPause=new object();
        public bool IsReady => catalog!=null;
        public string LastError { get; private set; }
        public string CurrentEventId => current?.Id;
        public int LastEventTick { get; private set; }
        public EventLaptopPopup Popup => popup;
        public ShelterSupplies Supplies => supplies;
        public string LastResult { get; private set; }
        public bool ChoiceApplied => choiceApplied;
        async void OnEnable()
        {
            int version=++generation;
            if(!simulation || !development || !commander || !roster || !supplies || !popup)
            { LastError="Event popup scene references missing."; Debug.LogError(LastError,this); return; }
            simulation.AcquirePause(loadingPause);
            simulation.RandomEventDue+=OnDue;
            simulation.PresentationDue+=OnPresentationDue;
            development.DevelopmentRestarted+=ResetRun;
            development.Changed+=OnDevelopmentChanged;
            popup.Closed+=OnClosed;
            popup.ChoiceRequested+=Choose;
            try
            {
                await simulation.InitializeAsync();
                if(!this || version!=generation || !isActiveAndEnabled) return;
                var farming = await GameData.LoadSettingsAsync<ShelterRun.FarmingDatabase>(ShelterRun.FarmingDataKeys.Database);
                if(!this || version!=generation || !isActiveAndEnabled) return;
                var items=new ShelterItemCatalog(GameData.GetTable(ShelterItemCatalog.TableName));
                var eventTable = GameData.GetTable(EventCatalog.TableName);
                items.ValidateSources(eventTable, farming.Records);
                supplies.Initialize(items, simulation.Rules);
                catalog=EventCatalog.FromTable(eventTable,items);
                LastError=null;
            }
            catch(Exception e) { if(this && version==generation) { LastError=e.Message; Debug.LogException(e,this); } }
            finally { if(simulation && version==generation) simulation.ReleasePause(loadingPause); }
        }
        void OnDisable()
        {
            generation++;
            if(simulation)
            {
                simulation.RandomEventDue-=OnDue;
                simulation.PresentationDue-=OnPresentationDue;
                simulation.ReleasePause(loadingPause);
            }
            if(development) { development.DevelopmentRestarted-=ResetRun; development.Changed-=OnDevelopmentChanged; }
            if(popup) { popup.Close(); popup.Closed-=OnClosed; popup.ChoiceRequested-=Choose; }
            OnClosed(); catalog=null;
        }
        void OnDue(int tick)
        {
            if(!IsReady || tick>=simulation.Rules.TotalTicks || development.Phase!=GameDevPhase.Developing || popup.IsOpen) return;
            var entry=catalog.Pick(previousId,supplies);
            if(entry!=null && ShowEvent(entry.Id)==EventShowResult.Shown) LastEventTick=tick;
        }
        void OnPresentationDue(int tick, double score, BunkerRules.PresentationGrade grade)
        {
            if (!popup.isActiveAndEnabled || popup.IsOpen || development.Phase != GameDevPhase.Developing) return;
            int round = 0;
            foreach (int scheduledTick in simulation.Rules.PresentationTicks)
                if (scheduledTick <= tick) round++;
            choiceApplied = false;
            LastResult = null;
            OpenPopup(() => popup.ShowPresentation(round, tick, development.Progress, score, grade));
        }
        public EventShowResult ShowEvent(string id)
        {
            if(!isActiveAndEnabled || !IsReady || !roster.IsReady || !popup.isActiveAndEnabled || development.Phase!=GameDevPhase.Developing) return EventShowResult.NotReady;
            if(popup.IsOpen) return EventShowResult.Busy;
            var entry=catalog.Find(id);
            if(entry==null) return EventShowResult.NotFound;
            if(!entry.Eligible(supplies)) return EventShowResult.Ineligible;
            current=entry; choiceApplied=false; LastResult=null;
            OpenPopup(() => popup.Show(entry));
            previousId=id;
            return EventShowResult.Shown;
        }
        void OpenPopup(Action show)
        {
            simulation.AcquirePause(popupPause);
            commander.AcquireInputBlock(popupPause);
            try { show(); }
            catch { popup.Close(); OnClosed(); throw; }
        }
        public void Choose(bool yes)
        {
            if(current==null || choiceApplied || !popup.CanChoose) return;
            try
            {
                if(!current.Eligible(supplies)) { popup.ShowUnavailable("조건이 바뀌어 선택할 수 없습니다."); return; }
                // Lock first; reentrant notifications and repeated button input cannot apply twice.
                choiceApplied=true;
                LastResult=EventEffectExecutor.Apply(yes?current.Yes:current.No,roster,development,supplies,simulation);
                popup.ShowResult(LastResult);
                LastError=null;
            }
            catch(InvalidOperationException e) { choiceApplied=false; LastError=e.Message; popup.ShowChoiceError(e.Message); }
            catch(OverflowException) { choiceApplied=false; LastError="효과 수치가 허용 범위를 초과했습니다."; popup.ShowChoiceError(LastError); }
        }
        void OnClosed()
        {
            if(simulation) simulation.ReleasePause(popupPause);
            if(commander) commander.ReleaseInputBlock(popupPause);
            current=null;
        }
        void ResetRun()
        {
            popup.Close(); previousId=null; LastEventTick=0; choiceApplied=false; LastResult=null;
            supplies.ResetRun();
            foreach(var actor in roster.Characters) if(actor && actor.Definition!=null) actor.SetMental(actor.Definition.Mental);
        }
        void OnDevelopmentChanged() { if(development.Phase!=GameDevPhase.Developing) popup.Close(); }
    }
}
