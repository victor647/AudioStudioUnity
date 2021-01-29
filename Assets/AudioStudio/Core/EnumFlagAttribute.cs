[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true)]
public class EnumFlagAttribute : UnityEngine.PropertyAttribute
{
	public System.Type Type;

	public EnumFlagAttribute(System.Type type)
	{
		Type = type;
	}
}
