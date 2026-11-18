var sim = UnityEngine.Object.FindFirstObjectByType<UnityGameJam.Shelter.BunkerSimulation>();
UnityEngine.Object.FindFirstObjectByType<UnityGameJam.GameDev.GameDevManager>().RestartDevelopment();
await System.Threading.Tasks.Task.Delay(1500);
if (sim.CurrentTick < 1) throw new System.Exception("Update did not advance real-time clock.");
sim.Paused = true;
var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
return new { tick = sim.CurrentTick, builder = settings.ActivePlayModeDataBuilder.Name, rules = UnityEditor.AssetDatabase.AssetPathToGUID(BunkerSetup.RulesPath) };
