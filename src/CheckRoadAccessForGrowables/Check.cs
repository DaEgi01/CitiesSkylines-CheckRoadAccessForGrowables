using System;
using JetBrains.Annotations;

namespace CheckRoadAccessForGrowables
{
	public static class Check
	{
		[AssertionMethod]
		public static void RequireNotNull<T>(T value, string name)
		{
			if (value is null)
				throw new ArgumentNullException(name);
		}
	}
}
