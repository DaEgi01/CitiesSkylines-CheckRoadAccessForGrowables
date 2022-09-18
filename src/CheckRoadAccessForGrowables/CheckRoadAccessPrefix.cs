using System;
using System.Reflection;
using ColossalFramework;
using UnityEngine;

namespace CheckRoadAccessForGrowables
{
    public static class CheckRoadAccessPrefix
    {
        private delegate bool FindRoadAccess(CommonBuildingAI _commonBuildingAI, Vector3 position, out ushort segmentID);
        private delegate void CheckVehicleAccess(CommonBuildingAI _commonBuildingAI, ushort buildingID, ref Building data, Vector3 position, ushort segmentID, out bool noAccess, out bool noPedestrianZone);
        private delegate void CheckServicePoints(BuildingAI _buildingAI, ushort buildingID, ref Building data);

        private static FindRoadAccess _findRoadAccess;
        private static CheckVehicleAccess _checkVehicleAccess;
        private static CheckServicePoints _checkServicePoints;

        public static void Init()
        {
            var findRoadAccessMethodInfo = typeof(CommonBuildingAI)
                .GetMethod("FindRoadAccess", BindingFlags.NonPublic | BindingFlags.Instance);
            if (findRoadAccessMethodInfo is null)
                throw new NullReferenceException(nameof(findRoadAccessMethodInfo) + " is null.");
            _findRoadAccess = (FindRoadAccess)Delegate
                .CreateDelegate(typeof(FindRoadAccess), findRoadAccessMethodInfo);

            var checkVehicleAccessMethodInfo = typeof(CommonBuildingAI)
                .GetMethod("CheckVehicleAccess", BindingFlags.NonPublic | BindingFlags.Instance);
            if (checkVehicleAccessMethodInfo is null)
                throw new NullReferenceException(nameof(checkVehicleAccessMethodInfo) + " is null.");
            _checkVehicleAccess = (CheckVehicleAccess)Delegate
                .CreateDelegate(typeof(CheckVehicleAccess), checkVehicleAccessMethodInfo);

            var checkServicePointsMethodInfo = typeof(CommonBuildingAI)
                .GetMethod("CheckServicePoints", BindingFlags.NonPublic | BindingFlags.Instance);
            if (checkServicePointsMethodInfo is null)
                throw new NullReferenceException(nameof(checkServicePointsMethodInfo) + " is null.");
            _checkServicePoints = (CheckServicePoints)Delegate
                .CreateDelegate(typeof(CheckServicePoints), checkServicePointsMethodInfo);
        }

        public static bool CheckRoadAccess(ushort buildingID, ref Building data, CommonBuildingAI __instance)
        {
            bool noPedestrianZone = false;
            bool noRoadConnection = true;
            if ((data.m_flags & Building.Flags.Collapsed) == 0 && data.m_parentBuilding == 0 && !(__instance is DummyBuildingAI))
            {
                Vector3 position = ((__instance.m_info.m_zoningMode == BuildingInfo.ZoningMode.CornerLeft)
                    ? data.CalculateSidewalkPosition((float)data.Width * 4f, 4f)
                    : ((__instance.m_info.m_zoningMode != BuildingInfo.ZoningMode.CornerRight)
                        ? data.CalculateSidewalkPosition(0f, 4f)
                        : data.CalculateSidewalkPosition((float)data.Width * -4f, 4f)));
                if (_findRoadAccess.Invoke(__instance, position, out var segmentID))
                {
                    noRoadConnection = false;
                    data.m_accessSegment = segmentID;
                    _checkVehicleAccess.Invoke(__instance, buildingID, ref data, data.m_position, segmentID, out var _, out noPedestrianZone);
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
            _checkServicePoints(__instance, buildingID, ref data);
            if (data.m_problems != problems)
            {
                Singleton<BuildingManager>.instance.UpdateNotifications(buildingID, problems, data.m_problems);
            }

            return false;
        }
    }
}