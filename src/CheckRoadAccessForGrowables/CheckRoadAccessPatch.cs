using System;
using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace CheckRoadAccessForGrowables
{
	public class CheckRoadAccessPatch
	{
		private readonly Harmony _harmony;

		private delegate bool FindRoadAccess(CommonBuildingAI commonBuildingAI, Vector3 position, out ushort segmentID);
		private delegate void CheckVehicleAccess(CommonBuildingAI commonBuildingAI, ushort buildingID, ref Building data, Vector3 position, ushort segmentID, out bool noAccess, out bool noPedestrianZone);
		private delegate void CheckServicePoints(BuildingAI buildingAI, ushort buildingID, ref Building data);

		private static FindRoadAccess? _findRoadAccess;
		private static CheckVehicleAccess? _checkVehicleAccess;
		private static CheckServicePoints? _checkServicePoints;

		public CheckRoadAccessPatch(Harmony harmony)
		{
			Check.RequireNotNull(harmony, nameof(harmony));
			_harmony = harmony;
		}

		public void Apply()
		{
			MethodInfo? findRoadAccessMethodInfo = typeof(CommonBuildingAI)
				.GetMethod("FindRoadAccess", BindingFlags.NonPublic | BindingFlags.Instance);
			Check.RequireNotNull(findRoadAccessMethodInfo, nameof(findRoadAccessMethodInfo));
			_findRoadAccess = (FindRoadAccess)Delegate
				.CreateDelegate(typeof(FindRoadAccess), findRoadAccessMethodInfo);

			MethodInfo? checkVehicleAccessMethodInfo = typeof(CommonBuildingAI)
				.GetMethod("CheckVehicleAccess", BindingFlags.NonPublic | BindingFlags.Instance);
			Check.RequireNotNull(checkVehicleAccessMethodInfo, nameof(checkVehicleAccessMethodInfo));
			_checkVehicleAccess = (CheckVehicleAccess)Delegate
				.CreateDelegate(typeof(CheckVehicleAccess), checkVehicleAccessMethodInfo);

			MethodInfo? checkServicePointsMethodInfo = typeof(CommonBuildingAI)
				.GetMethod("CheckServicePoints", BindingFlags.NonPublic | BindingFlags.Instance);
			Check.RequireNotNull(checkServicePointsMethodInfo, nameof(checkServicePointsMethodInfo));
			_checkServicePoints = (CheckServicePoints)Delegate
				.CreateDelegate(typeof(CheckServicePoints), checkServicePointsMethodInfo);

			MethodInfo? commonBuildingAICheckRoadAccess = typeof(CommonBuildingAI)
				.GetMethod("CheckRoadAccess", BindingFlags.Public | BindingFlags.Instance);
			Check.RequireNotNull(commonBuildingAICheckRoadAccess, nameof(commonBuildingAICheckRoadAccess));

			MethodInfo? checkRoadAccessPrefix = typeof(CheckRoadAccessPatch)
				.GetMethod(nameof(CheckRoadAccess), BindingFlags.Public | BindingFlags.Static);
			Check.RequireNotNull(checkRoadAccessPrefix, nameof(checkRoadAccessPrefix));

			_harmony.Patch(commonBuildingAICheckRoadAccess, prefix: new HarmonyMethod(checkRoadAccessPrefix));
		}

		// ReSharper disable once InconsistentNaming
		public static bool CheckRoadAccess(ushort buildingID, ref Building data, CommonBuildingAI __instance)
		{
			bool noRoadConnection = true;
			bool noPedestrianZone = false;

			bool shouldCheck = __instance is PrivateBuildingAI
				&& data.m_flags != Building.Flags.None
				&& (data.m_flags & Building.Flags.Collapsed) == 0
				&& data.m_parentBuilding == 0;
			if (shouldCheck)
			{
				Vector3 position = __instance.m_info.m_zoningMode switch
				{
					BuildingInfo.ZoningMode.CornerLeft => data.CalculateSidewalkPosition(data.Width * 4f, 4f),
					BuildingInfo.ZoningMode.CornerRight => data.CalculateSidewalkPosition(data.Width * -4f, 4f),
					BuildingInfo.ZoningMode.Straight => data.CalculateSidewalkPosition(0f, 4f),
					_ => throw new ArgumentOutOfRangeException()
				};

				if (_findRoadAccess!.Invoke(__instance, position, out ushort segmentID))
				{
					noRoadConnection = false;
					data.m_accessSegment = segmentID;
					_checkVehicleAccess!.Invoke(__instance, buildingID, ref data, data.m_position, segmentID, out _, out noPedestrianZone);
				}
			}
			else
			{
				noRoadConnection = false;
			}
			Notification.ProblemStruct problems = data.m_problems;
			data.m_problems = Notification.RemoveProblems(data.m_problems, new Notification.ProblemStruct(Notification.Problem1.RoadNotConnected, Notification.Problem2.NotInPedestrianZone));
			if (noRoadConnection)
			{
				data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem1.RoadNotConnected);
			}
			if (noPedestrianZone)
			{
				data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem2.NotInPedestrianZone);
			}

			_checkServicePoints!(__instance, buildingID, ref data);
			if (data.m_problems != problems)
			{
				Singleton<BuildingManager>.instance.UpdateNotifications(buildingID, problems, data.m_problems);
			}

			return false;
		}
	}
}
