using Harmony;
using ICities;
using System.Reflection;

namespace CheckRoadAccessForGrowables
{
	public class Mod : LoadingExtensionBase, IUserMod
	{
		private readonly string _harmonyId = "egi.citiesskylinesmods.checkroadaccessforgrowables";

		public string Name => "Check Road Access";
		public string Description => "Shows if you have cut the road access for growables.";

		public void OnEnabled()
		{
			var harmony = HarmonyInstance.Create(_harmonyId);

			var originalMethod = typeof(BuildingAI).GetMethod("CheckRoadAccess", BindingFlags.Public | BindingFlags.Instance);
			var replacementMethod = typeof(GameMethods).GetMethod("CheckRoadAccess", BindingFlags.Public | BindingFlags.Static);

			harmony.Patch(originalMethod, null, new HarmonyMethod(replacementMethod));
		}

		public void OnDisabled()
		{
			var harmony = HarmonyInstance.Create(_harmonyId);
			harmony.UnpatchAll(_harmonyId);
		}

		public void OnSettingsUI(UIHelperBase helper)
		{
			helper.AddButton("Recheck road access", () => {
				SimulationManager.instance.AddAction(() =>
				{
					var buildings = BuildingManager.instance.m_buildings.m_buffer;
					for (ushort i = 0; i < buildings.Length; i++)
					{
						BuildingInfo info = buildings[i].Info;
						info.m_buildingAI.CheckRoadAccess(i, ref buildings[i]);
					}
				});
			});
		}
	}
}
