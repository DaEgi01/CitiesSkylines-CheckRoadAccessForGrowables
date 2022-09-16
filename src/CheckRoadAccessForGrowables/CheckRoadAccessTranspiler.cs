using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace CheckRoadAccessForGrowables
{
	[HarmonyPatch(typeof(CommonBuildingAI), "CheckRoadAccess")]
	public static class CheckRoadAccessTranspiler
	{
		private static FieldInfo _buildingAccessSegment;
		private static FieldInfo _buildingProblems;
		private static MethodInfo _problemStructImplicitProblem1;
		private static MethodInfo _notificationAddProblems;
		private static MethodInfo _notificationRemoveProblems;

		public static void Init()
		{
			_buildingAccessSegment = typeof(Building)
				.GetField("m_accessSegment", BindingFlags.Public | BindingFlags.Instance);
			Check.RequireNotNull(_buildingAccessSegment, nameof(_buildingAccessSegment));

			_buildingProblems = typeof(Building)
				.GetField("m_problems", BindingFlags.Public | BindingFlags.Instance);
			Check.RequireNotNull(_buildingProblems, nameof(_buildingProblems));

			_problemStructImplicitProblem1 = typeof(Notification.ProblemStruct)
				.GetMethod("op_Implicit", new [] { typeof(Notification.Problem1) });
			Check.RequireNotNull(_problemStructImplicitProblem1, nameof(_problemStructImplicitProblem1));

			_notificationAddProblems = typeof(Notification)
				.GetMethod(nameof(Notification.AddProblems), BindingFlags.Public | BindingFlags.Static);
			Check.RequireNotNull(_notificationAddProblems, nameof(_notificationAddProblems));

			_notificationRemoveProblems = typeof(Notification)
				.GetMethod(nameof(Notification.RemoveProblems), BindingFlags.Public | BindingFlags.Static);
			Check.RequireNotNull(_notificationRemoveProblems, nameof(_notificationRemoveProblems));
		}

		[UsedImplicitly]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions, ILGenerator il)
		{
			var codes = codeInstructions.ToList();

			var firstAccessSegment = codes.FirstOrDefault(c => c.StoresField(_buildingAccessSegment));
			if (firstAccessSegment is null)
				throw new Exception(nameof(firstAccessSegment) + " not found.");

			var firstProblemsFieldInfo = codes.FirstOrDefault(c => c.LoadsField(_buildingProblems));
			if (firstProblemsFieldInfo is null)
				throw new Exception(nameof(firstProblemsFieldInfo) + " not found.");

			// bool noRoadConnection = true;
			var boolNoRoadConnection = il.DeclareLocal(typeof(bool));
			codes.Insert(0, new CodeInstruction(OpCodes.Ldc_I4_1));
			codes.Insert(1, new CodeInstruction(OpCodes.Stloc, boolNoRoadConnection));

			for (var i = 0; i < codes.Count; i++)
			{
				var code = codes[i];

				if (code == firstAccessSegment)
				{
					// data.m_accessSegment = segmentID;
					i++;
					// noRoadConnection = false;
					codes.Insert(i++, new CodeInstruction(OpCodes.Ldc_I4_0));
					codes.Insert(i++, new CodeInstruction(OpCodes.Stloc, boolNoRoadConnection));
				}

				if (code == firstProblemsFieldInfo)
				{
					// Notification.ProblemStruct problems = data.m_problems;
					i++;
					i++;

					// data.m_problems = Notification.RemoveProblems(data.m_problems, Notification.Problem1.RoadNotConnected);
					codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_2));
					codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_2));
					codes.Insert(i++, new CodeInstruction(OpCodes.Ldfld, _buildingProblems));
					codes.Insert(i++, new CodeInstruction(OpCodes.Ldc_I4, (int)Notification.Problem1.RoadNotConnected));
					codes.Insert(i++, new CodeInstruction(OpCodes.Conv_I8));
					codes.Insert(i++, new CodeInstruction(OpCodes.Call, _problemStructImplicitProblem1));
					codes.Insert(i++, new CodeInstruction(OpCodes.Call, _notificationRemoveProblems));
					codes.Insert(i++, new CodeInstruction(OpCodes.Stfld, _buildingProblems));

					// Label if (noPedestrianZone)
					var ifNoPedestrianZoneLabel = il.DefineLabel();
					codes[i].labels.Add(ifNoPedestrianZoneLabel);

					// if (noRoadConnection)
					codes.Insert(i++, new CodeInstruction(OpCodes.Ldloc, boolNoRoadConnection));
					codes.Insert(i++, new CodeInstruction(OpCodes.Brfalse, ifNoPedestrianZoneLabel));

					// data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem1.RoadNotConnected);
					codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_2));
					codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_2));
					codes.Insert(i++, new CodeInstruction(OpCodes.Ldfld, _buildingProblems));
					codes.Insert(i++, new CodeInstruction(OpCodes.Ldc_I4, (int)Notification.Problem1.RoadNotConnected));
					codes.Insert(i++, new CodeInstruction(OpCodes.Conv_I8));
					codes.Insert(i++, new CodeInstruction(OpCodes.Call, _problemStructImplicitProblem1));
					codes.Insert(i++, new CodeInstruction(OpCodes.Call, _notificationAddProblems));
					codes.Insert(i++, new CodeInstruction(OpCodes.Stfld, _buildingProblems));
				}
			}

			return codes;
		}
	}
}
