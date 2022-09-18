using System.Reflection;
using HarmonyLib;

namespace CheckRoadAccessForGrowables
{
	public static class Patcher
	{
		private const string _harmonyId = "egi.citiesskylinesmods.checkroadaccessforgrowables";
		private static bool _patched;

		public static void PatchAll()
		{
			if (_patched)
				return;

			var harmony = new Harmony(_harmonyId);

			harmony.PatchAll(typeof(Patcher).Assembly);

			var commonBuildingAICheckRoadAccess = typeof(CommonBuildingAI)
				.GetMethod("CheckRoadAccess", BindingFlags.Public | BindingFlags.Instance);
			Check.RequireNotNull(commonBuildingAICheckRoadAccess, nameof(commonBuildingAICheckRoadAccess));
			// var commonBuildingAICheckRoadAccessTranspiler = typeof(CheckRoadAccessTranspiler)
			// 	.GetMethod(nameof(CheckRoadAccessTranspiler.Transpiler), BindingFlags.Public | BindingFlags.Static);
			// Check.RequireNotNull(commonBuildingAICheckRoadAccessTranspiler, nameof(commonBuildingAICheckRoadAccessTranspiler));
			// CheckRoadAccessTranspiler.Init();
			// harmony.Patch(commonBuildingAICheckRoadAccess, transpiler: new HarmonyMethod(commonBuildingAICheckRoadAccessTranspiler));

			CheckRoadAccessPrefix.Init();
			var checkRoadAccessPrefix = typeof(CheckRoadAccessPrefix)
				.GetMethod(nameof(CheckRoadAccessPrefix.CheckRoadAccess), BindingFlags.Public | BindingFlags.Static);
			Check.RequireNotNull(checkRoadAccessPrefix, nameof(checkRoadAccessPrefix));
			harmony.Patch(commonBuildingAICheckRoadAccess, prefix: new HarmonyMethod(checkRoadAccessPrefix));

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