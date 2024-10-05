using DDEnum.Editor;
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidator(typeof(IDDEnumValueValidator<,>))]

namespace DDEnum.Editor
{
	public class IDDEnumValueValidator<TDDEnumAsset, TValue> : ValueValidator<TValue>
		where TDDEnumAsset : DDEnumAssetBase<TDDEnumAsset>
		where TValue : struct, IDDEnumValue<TDDEnumAsset, TValue>
	{
		protected override void Validate(ValidationResult result)
		{
			var extraBits = Value.BitValue & ~DDEnumAssetBase<TDDEnumAsset>.Instance.SetValuesMask;
			
			if (extraBits != 0L)
			{
				result.AddError("Set to value that is not set in " + DDEnumAssetBase<TDDEnumAsset>.Instance.name +
				                  "\nBit: " + string.Join(", ", Value.Value));
			}
			
			var obsoleteBits = Value.BitValue & DDEnumAssetBase<TDDEnumAsset>.Instance.ObsoleteValuesMask;

			if (obsoleteBits != 0L)
			{
				result.AddWarning("Set to value that is obsolete" + "\nBit: " + string.Join(", ", Value.Value));
			}
		}
	}
}