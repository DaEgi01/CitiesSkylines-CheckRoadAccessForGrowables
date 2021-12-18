using CitiesHarmony.API;
using ColossalFramework.UI;
using ICities;
using UnityEngine.SceneManagement;

namespace CheckRoadAccessForGrowables
{
	public class Mod : LoadingExtensionBase, IUserMod
	{
		public string Name => "Check Road Access for Growables";
		public string Description => "Shows if you have cut the road access for growables.";

		public void OnEnabled()
		{
			HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
		}

		public void OnDisabled()
		{
			if (!HarmonyHelper.IsHarmonyInstalled)
				return;

			Patcher.UnpatchAll();
		}

		public void OnSettingsUI(UIHelperBase uiHelper)
		{
			if (SceneManager.GetActiveScene().name == "Game")
			{
				AddRecheckRoadAccessButtonToOptions(uiHelper);
			}
			else
			{
				AddModCanOnlyBeUsedDuringGameplayMessageToOptions(uiHelper);
			}
		}

		private void AddRecheckRoadAccessButtonToOptions(UIHelperBase uiHelper)
        {
			uiHelper
				.AddGroup(Name)
				.AddButton("Recheck road access", () =>
				{
					SimulationManager.instance.AddAction(() =>
					{
						RecheckRoadAccessForAllBuildings();

						SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(() =>
						{
							NotifyUserToRecheckIcons();
						});
					});
				});
		}

		private void RecheckRoadAccessForAllBuildings()
		{
			var buildings = BuildingManager.instance.m_buildings.m_buffer;
			for (ushort i = 1; i < buildings.Length; i++)
			{
				ref var building = ref buildings[i];
				
				if ((building.m_flags & (Building.Flags.Created | Building.Flags.Deleted)) != Building.Flags.Created)
					continue;

				building.Info.m_buildingAI.CheckRoadAccess(i, ref building);
			}
		}

		private void NotifyUserToRecheckIcons()
		{
			DebugOutputPanel.AddMessage(
				ColossalFramework.Plugins.PluginManager.MessageType.Warning,
				"Recheck road access completed. Look for 'No Road Access' icons on your map."
			);
		}

		private void AddModCanOnlyBeUsedDuringGameplayMessageToOptions(UIHelperBase uiHelper)
		{
			var mainGroupHelper = uiHelper.AddGroup(Name) as UIHelper;
			var mainPanel = mainGroupHelper.self as UIPanel;
			var mainLabel = mainPanel.AddUIComponent<UILabel>();
			mainLabel.text = "This mod can only be used during gameplay.";
		}
	}
}
