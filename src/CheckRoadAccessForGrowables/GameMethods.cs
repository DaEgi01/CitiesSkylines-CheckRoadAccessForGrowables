using ColossalFramework;
using System;
using UnityEngine;

namespace CheckRoadAccessForGrowables
{
	/// <summary>
	/// This is game code v1.13.1_f1 taken from PlayerBuildingAI.CheckRoadAccess and PlayerBuildingAI.FindRoadAccess,
	/// made static and with some minor adjustments.
	/// </summary>
	public static class GameMethods
	{
		public static void CheckRoadAccess(ref ushort buildingID, ref Building data)
		{
			if (data.m_flags == Building.Flags.None)
				return;

			if (!(data.Info.GetAI() is PrivateBuildingAI))
				return;

			bool flag = true;
			if ((data.m_flags & Building.Flags.Collapsed) == Building.Flags.None)
			{
				Vector3 position;

				if (data.Info.m_zoningMode == BuildingInfo.ZoningMode.CornerLeft)
				{
					position = data.CalculateSidewalkPosition((float)data.Width * 4f, 4f);
				}
				else if (data.Info.m_zoningMode == BuildingInfo.ZoningMode.CornerRight)
				{
					position = data.CalculateSidewalkPosition((float)data.Width * -4f, 4f);
				}
				else
				{
					position = data.CalculateSidewalkPosition(0f, 4f);
				}
				if (FindRoadAccess(buildingID, ref data, position))
				{
					flag = false;
				}
			}
			else
			{
				flag = false;
			}
			Notification.Problem problems = data.m_problems;
			if (flag)
			{
				data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem.RoadNotConnected);
			}
			else
			{
				data.m_problems = Notification.RemoveProblems(data.m_problems, Notification.Problem.RoadNotConnected);
			}
			if (data.m_problems != problems)
			{
				Singleton<BuildingManager>.instance.UpdateNotifications(buildingID, problems, data.m_problems);
			}
		}

		public static bool FindRoadAccess(ushort buildingID, ref Building data, Vector3 position)
		{
			Bounds bounds = new Bounds(position, new Vector3(40f, 40f, 40f));
			int num = Mathf.Max((int)((bounds.min.x - 64f) / 64f + 135f), 0);
			int num2 = Mathf.Max((int)((bounds.min.z - 64f) / 64f + 135f), 0);
			int num3 = Mathf.Min((int)((bounds.max.x + 64f) / 64f + 135f), 269);
			int num4 = Mathf.Min((int)((bounds.max.z + 64f) / 64f + 135f), 269);
			NetManager instance = Singleton<NetManager>.instance;
			for (int i = num2; i <= num4; i++)
			{
				for (int j = num; j <= num3; j++)
				{
					ushort num5 = instance.m_segmentGrid[i * 270 + j];
					int num6 = 0;
					while (num5 != 0)
					{
						NetInfo info = instance.m_segments.m_buffer[(int)num5].Info;
						if (info.m_class.m_service == ItemClass.Service.Road && !info.m_netAI.IsUnderground() && !info.m_netAI.IsOverground() && info.m_netAI is RoadBaseAI && info.m_hasPedestrianLanes && (info.m_hasForwardVehicleLanes || info.m_hasBackwardVehicleLanes))
						{
							ushort startNode = instance.m_segments.m_buffer[(int)num5].m_startNode;
							ushort endNode = instance.m_segments.m_buffer[(int)num5].m_endNode;
							Vector3 position2 = instance.m_nodes.m_buffer[(int)startNode].m_position;
							Vector3 position3 = instance.m_nodes.m_buffer[(int)endNode].m_position;
							float num7 = Mathf.Max(Mathf.Max(bounds.min.x - 64f - position2.x, bounds.min.z - 64f - position2.z), Mathf.Max(position2.x - bounds.max.x - 64f, position2.z - bounds.max.z - 64f));
							float num8 = Mathf.Max(Mathf.Max(bounds.min.x - 64f - position3.x, bounds.min.z - 64f - position3.z), Mathf.Max(position3.x - bounds.max.x - 64f, position3.z - bounds.max.z - 64f));
							Vector3 b;
							int num9;
							float num10;
							Vector3 vector;
							int num11;
							float num12;
							if ((num7 < 0f || num8 < 0f) && instance.m_segments.m_buffer[(int)num5].m_bounds.Intersects(bounds) && instance.m_segments.m_buffer[(int)num5].GetClosestLanePosition(position, NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle, VehicleInfo.VehicleType.Car, VehicleInfo.VehicleType.None, false, out b, out num9, out num10, out vector, out num11, out num12))
							{
								float num13 = Vector3.SqrMagnitude(position - b);
								if (num13 < 400f)
								{
									return true;
								}
							}
						}
						num5 = instance.m_segments.m_buffer[(int)num5].m_nextGridSegment;
						if (++num6 >= 36864)
						{
							CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
							break;
						}
					}
				}
			}
			data.m_flags |= Building.Flags.RoadAccessFailed;
			return false;
		}

		public static void CreateBuilding(ref ushort buildingID, ref Building data)
		{
			if (data.m_flags == Building.Flags.None)
				return;

			if (!(data.Info.GetAI() is PrivateBuildingAI))
				return;

			BuildingManager.instance.RoadCheckNeeded(buildingID);
		}
	}
}
