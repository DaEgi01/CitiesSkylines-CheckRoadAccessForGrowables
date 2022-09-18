using ColossalFramework;
using UnityEngine;

namespace CheckRoadAccessForGrowables
{
    public class TestAI : BuildingAI
    {
        public override void CheckRoadAccess(ushort buildingID, ref Building data)
        {
            bool noPedestrianZone = false;
            bool noRoadConnection = true;
            if ((data.m_flags & Building.Flags.Collapsed) == 0 && data.m_parentBuilding == 0 && !(this is DummyBuildingAI))
            {
                Vector3 position = ((m_info.m_zoningMode == BuildingInfo.ZoningMode.CornerLeft) ? data.CalculateSidewalkPosition((float)data.Width * 4f, 4f) : ((m_info.m_zoningMode != BuildingInfo.ZoningMode.CornerRight) ? data.CalculateSidewalkPosition(0f, 4f) : data.CalculateSidewalkPosition((float)data.Width * -4f, 4f)));
                if (FindRoadAccess(position, out var segmentID))
                {
                    noRoadConnection = false;
                    data.m_accessSegment = segmentID;
                    CheckVehicleAccess(buildingID, ref data, data.m_position, segmentID, out var _, out noPedestrianZone);
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
            CheckServicePoints(buildingID, ref data);
            if (data.m_problems != problems)
            {
                Singleton<BuildingManager>.instance.UpdateNotifications(buildingID, problems, data.m_problems);
            }
        }

        private bool FindRoadAccess(Vector3 position, out ushort segmentID)
        {
            segmentID = 0;
            return true;
        }

        protected void CheckVehicleAccess(
            ushort buildingID,
            ref Building data,
            Vector3 position,
            ushort segmentID,
            out bool noAccess,
            out bool noPedestrianZone)
        {
            noAccess = false;
            noPedestrianZone = false;
        }
    }
}