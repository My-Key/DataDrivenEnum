using System;
using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;

[assembly: OdinVisualDesignerAttributeItem("Animator", typeof(AnimatorParameterAttribute))]
#endif

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[Conditional("UNITY_EDITOR")]
public class SubsetAttribute : PropertyAttribute
{
	[OdinDesignerBinding(nameof(Subset))]
	[ShowInInspector]
	public string Subset { get; protected set; }

	public SubsetAttribute(string subset)
	{
		Subset = subset;
	}
}
