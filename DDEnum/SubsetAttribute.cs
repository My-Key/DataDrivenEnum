using System;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;

[assembly: OdinVisualDesignerAttributeItem("Data Driven Enum", typeof(SubsetAttribute))]
#endif

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[Conditional("UNITY_EDITOR")]
public class SubsetAttribute : PropertyAttribute
{
	[OdinDesignerBinding(nameof(Subset))]
	[ShowInInspector]
	public string Subset { get; protected set; }

	/// <param name="subset">String that will resolve into IEnumerable{int}, IDDEnumMask will also work</param>
	public SubsetAttribute(string subset)
	{
		Subset = subset;
	}
}
