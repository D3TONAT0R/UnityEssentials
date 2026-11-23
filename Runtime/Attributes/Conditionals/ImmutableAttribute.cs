using System;

namespace UnityEssentials
{
	/// <summary>
	/// Marks this inspector field as immutable, making it editable only before the game object initializes.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class ImmutableAttribute : PropertyModifierAttribute
	{
	}
}
