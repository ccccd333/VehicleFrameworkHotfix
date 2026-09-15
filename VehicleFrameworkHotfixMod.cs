using System.Reflection;
using HarmonyLib;
using Verse;

namespace VehicleFrameworkHotfix
{
    public class VehicleFrameworkHotfixMod : Mod
    {
        public VehicleFrameworkHotfixMod(ModContentPack content) : base(content)
        {
            // Mod initialized
        }
    }

    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("rimworld.vehicleframework.hotfix");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[VehicleFrameworkHotfix] Initialized successfully. 4 hotfix patches applied.");
        }
    }
}
