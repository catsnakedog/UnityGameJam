using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace UnityGameJam.Events
{
    public sealed class EventEffect
    {
        public EventEffectKind Kind { get; }
        public string Target { get; }
        public int Delta { get; }
        EventEffect(EventEffectKind kind, string target, int delta) { Kind=kind; Target=target; Delta=delta; }
        static readonly Regex Call = new Regex(@"^(MENTAL|PROGRESS|ITEM)\(\s*([a-z0-9_:]+)\s*,\s*([+-]\d+)\s*\)$", RegexOptions.CultureInvariant);
        static readonly Regex Wave = new Regex(@"^NEXT_WAVE_HP\(\s*([+-]\d+)\s*\)$", RegexOptions.CultureInvariant);
        public static EventEffect Parse(string source)
        {
            source = source?.Trim();
            if(source=="NONE") return new EventEffect(EventEffectKind.None,null,0);
            var wave=Wave.Match(source??"");
            if(wave.Success) return new EventEffect(EventEffectKind.NextWaveHp,null,Amount(wave.Groups[1].Value));
            var call=Call.Match(source??"");
            if(!call.Success) throw new FormatException("Invalid event effect: "+source);
            string target=call.Groups[2].Value;
            EventEffectKind kind;
            switch(call.Groups[1].Value)
            {
                case "MENTAL": kind=EventEffectKind.Mental;
                    if(target!="all" && target!="random1" && target!="room:art" && target!="room:prog" && target!="room:plan") throw new FormatException("Unknown mental target: "+target);
                    break;
                case "PROGRESS": kind=EventEffectKind.Progress;
                    if(target!="open" && target!="art" && target!="prog" && target!="plan") throw new FormatException("Unknown progress target: "+target);
                    break;
                default: kind=EventEffectKind.Item;
                    if(!Regex.IsMatch(target,@"^[a-z][a-z0-9_]*$")) throw new FormatException("Invalid item ID: "+target);
                    break;
            }
            return new EventEffect(kind,target,Amount(call.Groups[3].Value));
        }
        static int Amount(string text) => int.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        public static IReadOnlyList<EventEffect> ParseMany(IReadOnlyList<string> source)
        {
            if(source==null || source.Count==0) throw new FormatException("Every event choice requires effects or NONE.");
            var result=new List<EventEffect>();
            foreach(var text in source) result.Add(Parse(text));
            if(result.Count>1 && result.Exists(e=>e.Kind==EventEffectKind.None)) throw new FormatException("NONE must be the only effect.");
            return result.AsReadOnly();
        }
    }
}
