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
        public static int RollDie(ZombieData rules, Func<int, int> die)
        {
            int value = die(rules.DiceSides);
            if (value < 1 || value > rules.DiceSides) throw new ArgumentOutOfRangeException(nameof(die));
            return value;
        }

        public static CombatRoll Roll(ZombieData rules, int survivorCombat, int zombieCombat, Func<int, int> die)
        {
            return new CombatRoll(RollDie(rules, die), RollDie(rules, die), survivorCombat, zombieCombat);
        }
    }
}
