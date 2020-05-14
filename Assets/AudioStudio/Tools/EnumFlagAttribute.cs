[System.AttributeUsage(System.AttributeTargets.Field)]
public class EnumFlagAttribute : UnityEngine.PropertyAttribute
{
	public System.Type Type;

	public EnumFlagAttribute(System.Type type)
	{
		Type = type;
	}
}
