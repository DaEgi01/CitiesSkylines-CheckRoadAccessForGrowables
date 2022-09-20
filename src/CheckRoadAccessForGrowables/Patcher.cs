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

			new CheckRoadAccessPatch(harmony)
				.Apply();

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