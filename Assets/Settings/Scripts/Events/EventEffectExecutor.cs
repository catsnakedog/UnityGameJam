using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityGameJam.Characters;
using UnityGameJam.GameDev;
using UnityGameJam.Shelter;

namespace UnityGameJam.Events
{
    /// <summary>Validate and calculate the whole choice before writing any gameplay state.</summary>
    public static class EventEffectExecutor
    {
        public static string Apply(IReadOnlyList<EventEffect> effects, ShelterRoster roster, GameDevManager development, ShelterSupplies supplies, BunkerSimulation simulation)
        {
            if(development.Phase!=GameDevPhase.Developing) throw new InvalidOperationException("개발이 종료되었습니다.");
            var actors=roster.Characters.Where(c=>c && c.isActiveAndEnabled && c.Definition!=null).ToArray();
            var mental=new Dictionary<CharacterIdentity,int>();
            var progress=new Dictionary<GameDevField,float>();
            var items=new Dictionary<string,int>(StringComparer.Ordinal);
            int wave=simulation.PendingWaveHpModifier;
            var fields=new[]{GameDevField.Planning,GameDevField.Programming,GameDevField.Graphics};
            var open=fields.Where(f=>development.Progress.Get(f)<GameDevProgress.MaximumPercent).ToArray();
            foreach(var effect in effects)
            {
                switch(effect.Kind)
                {
                    case EventEffectKind.Mental:
                        IEnumerable<CharacterIdentity> targets=actors;
                        if(effect.Target=="random1") targets=actors.Length==0?Array.Empty<CharacterIdentity>():new[]{actors[UnityEngine.Random.Range(0,actors.Length)]};
                        else if(effect.Target.StartsWith("room:",StringComparison.Ordinal))
                        {
                            var room=Room(effect.Target.Substring(5));
                            targets=actors.Where(c=> { var walker=c.GetComponent<ShelterWalker>(); return walker && walker.CurrentRoom && walker.CurrentRoom.roomType==room; });
                        }
                        foreach(var actor in targets)
                        {
                            int before=mental.TryGetValue(actor,out int staged)?staged:actor.CurrentMental;
                            mental[actor]=actor.MentalAfterChange(before, effect.Delta);
                        }
                        break;
                    case EventEffectKind.Progress:
                        foreach(var field in effect.Target=="open"?open:new[]{Field(effect.Target)})
                        {
                            float before=progress.TryGetValue(field,out float staged)?staged:development.Progress.Get(field);
                            progress[field]=(float)Math.Max(0,Math.Min(GameDevProgress.MaximumPercent,(double)before+effect.Delta));
                        }
                        break;
                    case EventEffectKind.Item:
                        int count=items.TryGetValue(effect.Target,out int stagedCount)?stagedCount:supplies.GetCount(effect.Target);
                        int next=checked(count+effect.Delta);
                        if(next<0) throw new InvalidOperationException(supplies.Catalog.Name(effect.Target)+"이(가) 부족합니다.");
                        items[effect.Target]=next;
                        break;
                    case EventEffectKind.NextWaveHp: wave=checked(wave+effect.Delta); break;
                }
            }
            var result=new List<string>();
            foreach(var group in mental.GroupBy(pair=>pair.Value-pair.Key.CurrentMental))
            {
                int delta=group.Key;
                if(delta!=0) result.Add("멘탈 "+group.Count()+"명 "+Signed(delta));
            }
            foreach(var pair in progress)
            {
                float delta=pair.Value-development.Progress.Get(pair.Key);
                if(delta!=0) result.Add(FieldName(pair.Key)+" "+Signed(delta)+"%p");
            }
            foreach(var pair in items)
            {
                int delta=pair.Value-supplies.GetCount(pair.Key);
                if(delta!=0) result.Add(supplies.Catalog.Name(pair.Key)+" "+Signed(delta));
            }
            long waveDelta=(long)wave-simulation.PendingWaveHpModifier;
            if(waveDelta!=0) result.Add("다음 적 HP "+waveDelta.ToString("+0;-0;0",CultureInfo.InvariantCulture));
            // No writes happen above this point, including for insufficient stock or overflow.
            foreach(var pair in mental) pair.Key.SetMental(pair.Value, SurvivorEmotion.Cause.Event);
            foreach(var pair in progress) development.SetProgress(pair.Key,pair.Value);
            supplies.SetCounts(items);
            simulation.SetPendingWaveHpModifier(wave);
            return result.Count==0?"변화 없음":string.Join(" · ",result);
        }
        static string Signed(float value)=>value.ToString("+0.##;-0.##;0",CultureInfo.InvariantCulture);
        static GameDevField Field(string target)=>target=="art"?GameDevField.Graphics:target=="prog"?GameDevField.Programming:GameDevField.Planning;
        static ShelterRoomType Room(string target)=>target=="art"?ShelterRoomType.Graphics:target=="prog"?ShelterRoomType.Programming:ShelterRoomType.Planning;
        static string FieldName(GameDevField field)=>field==GameDevField.Graphics?"아트":field==GameDevField.Programming?"프로그래밍":"기획";
    }
}
