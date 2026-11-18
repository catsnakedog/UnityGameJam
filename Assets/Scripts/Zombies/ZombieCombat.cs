using System;

namespace UnityGameJam.Zombies
{
    public enum CombatOutcome { Tie, SurvivorWins, ZombieWins }
    public readonly struct CombatRoll
    {
        public readonly int SurvivorDie, ZombieDie, SurvivorTotal, ZombieTotal;
        public readonly CombatOutcome Outcome;
        public CombatRoll(int survivorDie, int zombieDie, int survivorCombat, int zombieCombat)
        {
            SurvivorDie = survivorDie; ZombieDie = zombieDie;
            SurvivorTotal = checked(survivorDie + survivorCombat); ZombieTotal = checked(zombieDie + zombieCombat);
            Outcome = SurvivorTotal == ZombieTotal ? CombatOutcome.Tie : SurvivorTotal > ZombieTotal ? CombatOutcome.SurvivorWins : CombatOutcome.ZombieWins;
        }
    }
    public static class ZombieCombat
    {
        // One pair only. A tie leaves the encounter pending, so even an always-tied RNG cannot hang a frame.
        public static CombatRoll Roll(ZombieData rules, int survivorCombat, int zombieCombat, Func<int, int> die)
        {
            int a = die(rules.DiceSides), b = die(rules.DiceSides);
            if (a < 1 || a > rules.DiceSides || b < 1 || b > rules.DiceSides) throw new ArgumentOutOfRangeException(nameof(die));
            return new CombatRoll(a, b, survivorCombat, zombieCombat);
        }
    }
}
