namespace ShelterRun
{
    public enum FarmingObjectType { Person = 0, Item = 1 }
    public enum FarmingPhase { Loading = 0, Ready = 1, Running = 2, Finished = 3, Failed = 4, Preparing = 5 }
    public enum PickupState { Available = 0, Carried = 1, InFlight = 2, Deposited = 3 }

    public static class FarmingDataKeys
    {
        public const string SpaghettiCan = "canfood";
        public const string Database = "data/farming/database";
        public const string Rules = "data/farming/rules";
    }
}
