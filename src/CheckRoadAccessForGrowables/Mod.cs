using ColossalFramework.Threading;
using ColossalFramework.UI;
using Harmony;
using ICities;
using System.Reflection;
using UnityEngine.SceneManagement;

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
			if (SceneManager.GetActiveScene().name == "Game")
			{
				helper.AddGroup(Name)
					.AddButton("Recheck road access", () =>
					{
						SimulationManager.instance.AddAction(() =>
						{
							var buildings = BuildingManager.instance.m_buildings;
							for (ushort i = 0; i < buildings.m_size; i++)
							{
								var building = buildings.m_buffer[i];
								building.Info.m_buildingAI.CheckRoadAccess(i, ref building);
							}

							SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(() =>
							{
								DebugOutputPanel.AddMessage(
									ColossalFramework.Plugins.PluginManager.MessageType.Warning,
									"Recheck road access completed. Look for 'No Road Access' icons on your map."
								);
							});
						});
					});
			}
			else
			{
				var mainGroupHelper = helper.AddGroup(Name) as UIHelper;
				var mainPanel = mainGroupHelper.self as UIPanel;
				var mainLabel = mainPanel.AddUIComponent<UILabel>();
				mainLabel.text = "This mod can only be used during gameplay.";
			}
		}
	}
}
