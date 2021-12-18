using HarmonyLib;
using System.Reflection;

namespace CheckRoadAccessForGrowables
{
    public static class Patcher
    {
        private const string _harmonyId = "egi.citiesskylinesmods.checkroadaccessforgrowables";
        private static bool _patched = false;

        public static void PatchAll()
        {
            if (_patched)
                return;

            var harmony = new Harmony(_harmonyId);

            var checkRoadAccess = typeof(BuildingAI).GetMethod(
                "CheckRoadAccess", 
                BindingFlags.Public | BindingFlags.Instance);
            var checkRoadAccessPostfix = typeof(GameMethods).GetMethod(
                nameof(GameMethods.CheckRoadAccess),
                BindingFlags.Public | BindingFlags.Static);
            harmony.Patch(checkRoadAccess, null, new HarmonyMethod(checkRoadAccessPostfix));

            var createBuilding = typeof(BuildingAI).GetMethod(
                "CreateBuilding",
                BindingFlags.Public | BindingFlags.Instance);
            var createBuildingPostfix = typeof(GameMethods).GetMethod(
                nameof(GameMethods.CreateBuilding),
                BindingFlags.Public | BindingFlags.Static);
            harmony.Patch(createBuilding, null, new HarmonyMethod(createBuildingPostfix));

            _patched = true;
        }

        public static void UnpatchAll()
        {
            if (!_patched)
                return;

            var harmony = new Harmony(_harmonyId);
            harmony.UnpatchAll(_harmonyId);

            _patched = false;
        }
    }
}
