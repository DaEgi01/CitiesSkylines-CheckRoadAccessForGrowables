using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace CheckRoadAccessForGrowables
{
	public static class CheckRoadAccessTranspiler
	{
		private static FieldInfo _buildingFlags;
		private static FieldInfo _buildingParentBuilding;
		private static FieldInfo _buildingAccessSegment;
		private static FieldInfo _buildingProblems;
		private static MethodInfo _problemStructImplicitProblem1;
		private static MethodInfo _problemStructImplicitProblem2;
		private static MethodInfo _notificationAddProblems;
		private static MethodInfo _notificationRemoveProblems;
		private static MethodInfo _buildingAiCheckServicePoints;
		private static ConstructorInfo _problemStructCtor;

		public static void Init()
		{
			_buildingFlags = typeof(Building)
				.GetField("m_flags", BindingFlags.Public | BindingFlags.Instance);
			Check.RequireNotNull(_buildingFlags, nameof(_buildingFlags));

			_buildingParentBuilding = typeof(Building)
				.GetField("m_parentBuilding", BindingFlags.Public | BindingFlags.Instance);
			Check.RequireNotNull(_buildingParentBuilding, nameof(_buildingParentBuilding));

			_buildingAccessSegment = typeof(Building)
				.GetField("m_accessSegment", BindingFlags.Public | BindingFlags.Instance);
			Check.RequireNotNull(_buildingAccessSegment, nameof(_buildingAccessSegment));

			_buildingProblems = typeof(Building)
				.GetField("m_problems", BindingFlags.Public | BindingFlags.Instance);
			Check.RequireNotNull(_buildingProblems, nameof(_buildingProblems));

			_problemStructImplicitProblem1 = typeof(Notification.ProblemStruct)
				.GetMethod("op_Implicit", new [] { typeof(Notification.Problem1) });
			Check.RequireNotNull(_problemStructImplicitProblem1, nameof(_problemStructImplicitProblem1));

			_problemStructImplicitProblem2 = typeof(Notification.ProblemStruct)
				.GetMethod("op_Implicit", new [] { typeof(Notification.Problem2) });
			Check.RequireNotNull(_problemStructImplicitProblem2, nameof(_problemStructImplicitProblem2));

			_notificationAddProblems = typeof(Notification)
				.GetMethod(nameof(Notification.AddProblems), BindingFlags.Public | BindingFlags.Static);
			Check.RequireNotNull(_notificationAddProblems, nameof(_notificationAddProblems));

			_notificationRemoveProblems = typeof(Notification)
				.GetMethod(nameof(Notification.RemoveProblems), BindingFlags.Public | BindingFlags.Static);
			Check.RequireNotNull(_notificationRemoveProblems, nameof(_notificationRemoveProblems));

			_problemStructCtor = typeof(Notification.ProblemStruct)
				.GetConstructor(new[] {typeof(Notification.Problem1), typeof(Notification.Problem2)});
			Check.RequireNotNull(_problemStructCtor, nameof(_problemStructCtor));

			_buildingAiCheckServicePoints = typeof(BuildingAI)
				.GetMethod("CheckServicePoints", BindingFlags.NonPublic | BindingFlags.Instance);
			Check.RequireNotNull(_buildingAiCheckServicePoints, nameof(_buildingAiCheckServicePoints));
		}

		[UsedImplicitly]
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions, ILGenerator il)
		{
			var codes = codeInstructions.ToList();

			var labelToNotCollapsedNotSubBuildingElseCase = il.DefineLabel();
			var labelToNotificationProblemStructProblems = il.DefineLabel();

			// bool noPedestrianZone = false;
			// -> bool noRoadConnection = true;
			// -> bool noPedestrianZone = false;
			var boolNoRoadConnection = il.DeclareLocal(typeof(bool));
			codes.InsertRange(0, new []
			{
				new CodeInstruction(OpCodes.Ldc_I4_1),
				new CodeInstruction(OpCodes.Stloc, boolNoRoadConnection),
			});

			// if ((data.m_flags & Building.Flags.Collapsed) == 0)
			// -> if ((data.m_flags & Building.Flags.Collapsed) == 0 && data.m_parentBuilding == 0)
			// -> else case jumps to also added new part: else { boolNoRoadConnection = false; }
			var firstBuildingFlagsIndex = codes.FindIndex(c => c.LoadsField(_buildingFlags));
			codes[firstBuildingFlagsIndex + 3].operand = labelToNotCollapsedNotSubBuildingElseCase;
			codes.InsertRange(firstBuildingFlagsIndex + 4, new []
			{
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Ldfld, _buildingParentBuilding),
				new CodeInstruction(OpCodes.Brtrue, labelToNotCollapsedNotSubBuildingElseCase)
			});

			// data.m_accessSegment = segmentID;
			// -> data.m_accessSegment = segmentID;
			// -> boolNoRoadConnection = false;
			var firstDataAccessSegmentIndex = codes.FindIndex(c => c.StoresField(_buildingAccessSegment));
			codes[firstDataAccessSegmentIndex - 1].operand = labelToNotificationProblemStructProblems;
			codes.InsertRange(firstDataAccessSegmentIndex + 1, new []
			{
				new CodeInstruction(OpCodes.Ldc_I4_0),
				new CodeInstruction(OpCodes.Stloc, boolNoRoadConnection)
			});

			// Notification.ProblemStruct problems = data.m_problems;
			// -> else { boolNoRoadConnection = false; } WITH LABEL
			// -> Notification.ProblemStruct problems = data.m_problems; WITH LABEL
			var firstBuildingProblemsIndex = codes.FindIndex(c => c.LoadsField(_buildingProblems));
			codes[firstBuildingProblemsIndex - 1].labels.Add(labelToNotificationProblemStructProblems);
			var notCollapsedAndNotSubBuildingElseCase = new[]
			{
				new CodeInstruction(OpCodes.Br_S, labelToNotificationProblemStructProblems),
				new CodeInstruction(OpCodes.Ldc_I4_0).WithLabels(labelToNotCollapsedNotSubBuildingElseCase),
				new CodeInstruction(OpCodes.Stloc, boolNoRoadConnection)
			};
			codes.InsertRange(firstBuildingProblemsIndex - 1, notCollapsedAndNotSubBuildingElseCase);

			/* Notification.ProblemStruct problems = data.m_problems;
			-> REMOVE
				if (noPedestrianZone)
				{
					data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem2.NotInPedestrianZone);
					CheckServicePoints(buildingID, ref data);
				}
				else
				{
					data.m_problems = Notification.RemoveProblems(data.m_problems, Notification.Problem2.NotInPedestrianZone);
					CheckServicePoints(buildingID, ref data);
				}
			*/
			firstBuildingProblemsIndex = codes.FindIndex(c => c.LoadsField(_buildingProblems));
			codes.RemoveRange(firstBuildingProblemsIndex + 2, 27);

			var labelToIfNoPedestrianZone = il.DefineLabel();
			var labelToCheckServicePoints = il.DefineLabel();

			//Notification.ProblemStruct problems = data.m_problems;
			// -> ADD ...
			codes.InsertRange(firstBuildingProblemsIndex + 2, new []
			{
				// -> data.m_problems = Notification.RemoveProblems(data.m_problems, new Notification.ProblemStruct(Notification.Problem1.RoadNotConnected, Notification.Problem2.NotInPedestrianZone));
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Ldfld, _buildingProblems),
				new CodeInstruction(OpCodes.Ldc_I4, (int)Notification.Problem1.RoadNotConnected),
				new CodeInstruction(OpCodes.Conv_I8),
				new CodeInstruction(OpCodes.Ldc_I4_1), // Notification.Problem2.NotInPedestrianZone
				new CodeInstruction(OpCodes.Conv_I8),
				new CodeInstruction(OpCodes.Newobj, _problemStructCtor),
				new CodeInstruction(OpCodes.Call, _notificationRemoveProblems),
				new CodeInstruction(OpCodes.Stfld, _buildingProblems),

				/*
				-> if (noRoadConnection)
				-> {
				->     data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem1.RoadNotConnected);
				-> }
				*/
				new CodeInstruction(OpCodes.Ldloc, boolNoRoadConnection),
				new CodeInstruction(OpCodes.Brfalse_S, labelToIfNoPedestrianZone),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Ldfld, _buildingProblems),
				new CodeInstruction(OpCodes.Ldc_I4, (int)Notification.Problem1.RoadNotConnected),
				new CodeInstruction(OpCodes.Conv_I8),
				new CodeInstruction(OpCodes.Call, _problemStructImplicitProblem1),
				new CodeInstruction(OpCodes.Call, _notificationAddProblems),
				new CodeInstruction(OpCodes.Stfld, _buildingProblems),

				/*
				-> if (noPedestrianZone)
				-> {
				->    data.m_problems = Notification.AddProblems(data.m_problems, Notification.Problem2.NotInPedestrianZone);
				-> }
				*/
				new CodeInstruction(OpCodes.Ldloc_0).WithLabels(labelToIfNoPedestrianZone),
				new CodeInstruction(OpCodes.Brfalse_S, labelToCheckServicePoints),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Ldfld, _buildingProblems),
				new CodeInstruction(OpCodes.Ldc_I4_1), // Notification.Problem2.NotInPedestrianZone
				new CodeInstruction(OpCodes.Conv_I8),
				new CodeInstruction(OpCodes.Call, _problemStructImplicitProblem2),
				new CodeInstruction(OpCodes.Call, _notificationAddProblems),
				new CodeInstruction(OpCodes.Stfld, _buildingProblems),

				// -> CheckServicePoints(buildingID, ref data);
				new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labelToCheckServicePoints),
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Call, _buildingAiCheckServicePoints)
			});

			return codes;
		}
	}
}
