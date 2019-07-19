[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true)]
public class EnumFlagAttribute : UnityEngine.PropertyAttribute
{
	public System.Type Type;

	public EnumFlagAttribute(System.Type type)
	{
		Type = type;
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(EnumFlagAttribute))]
	public class PropertyDrawer : UnityEditor.PropertyDrawer
	{
		public override void OnGUI(UnityEngine.Rect position, UnityEditor.SerializedProperty property, UnityEngine.GUIContent label)
		{
			UnityEditor.EditorGUI.BeginProperty(position, label, property);
			var flagAttribute = (EnumFlagAttribute)attribute;

#if UNITY_2017_3_OR_NEWER
			property.longValue = UnityEditor.EditorGUI.EnumFlagsField(position, new UnityEngine.GUIContent(label.text), (System.Enum)System.Enum.ToObject(flagAttribute.Type, property.longValue)).GetHashCode();
#else
			property.longValue = UnityEditor.EditorGUI.EnumMaskField(position, label, (System.Enum)System.Enum.ToObject(flagAttribute.Type, property.longValue)).GetHashCode();
#endif
			UnityEditor.EditorGUI.EndProperty();
		}
	}
#endif
}
