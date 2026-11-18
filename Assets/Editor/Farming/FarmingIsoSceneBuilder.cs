namespace ShelterRun.Editor
{
    // Compatibility entry point: authoring now always produces the side-view map.
    public static class FarmingIsoSceneBuilder
    {
        public static void Build() => FarmingSideViewBuilder.Build();
    }
}
