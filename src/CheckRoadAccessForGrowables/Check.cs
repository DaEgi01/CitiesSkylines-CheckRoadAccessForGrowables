using System;

namespace CheckRoadAccessForGrowables
{
	public static class Check
	{
		public static void RequireNotNull<T>(T value, string name)
		{
			if (value == null)
				throw new NullReferenceException(name + " is null.");
		}
	}
}
