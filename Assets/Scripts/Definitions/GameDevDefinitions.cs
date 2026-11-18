namespace UnityGameJam.GameDev
{
    // These are the three fixed disciplines, not an extensible content database.
    public enum GameDevField { Planning = 0, Programming = 1, Graphics = 2 }
    public enum GameDevPhase { Loading = 0, Developing = 1, AwaitingScore = 2, Ended = 3, Failed = 4 }

    public static class GameDevKeys
    {
        public const string EndingsTable = "GameDevEndings";
        public const string ShelterScene = "Assets/Scenes/ShelterPrototype.unity";
    }
}
