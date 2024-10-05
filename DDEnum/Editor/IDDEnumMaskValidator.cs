using System.Collections.Generic;
using DDEnum.Editor;
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidator(typeof(IDDEnumMaskValidator<,,>))]

namespace DDEnum.Editor
{
	public class IDDEnumMaskValidator<TMask, TDDEnumAsset, TValue> : ValueValidator<TMask> 
		where TMask : struct, IDDEnumMask<TDDEnumAsset, TValue, TMask>
		where TDDEnumAsset : DDEnumAssetBase<TDDEnumAsset>
		where TValue :  struct, IDDEnumValue<TDDEnumAsset, TValue>
	{
		protected override void Validate(ValidationResult result)
		{
			var extraBits = Value.Value & ~DDEnumAssetBase<TDDEnumAsset>.Instance.SetValuesMask;
			var assetInstance = DDEnumAssetBase<TDDEnumAsset>.Instance;
			
			if (extraBits != 0L)
			{
				var bitIndexes = new List<int>();

				for (int i = 0; i < DDEnumAssetBase<TDDEnumAsset>.MAX_LENGTH; i++)
				{
					if ((extraBits & (1L << i)) != 0L)
						bitIndexes.Add(i);
				}

				result.AddWarning("Contains bits that are not set in " + assetInstance.name +
				                  "\nBits: " + string.Join(", ", bitIndexes)).WithButton("Remove extra bits", RemoveExtraBits)
					.WithFix("Remove extra bits", RemoveExtraBits);
			}
			
			var obsoleteBits = Value.Value & DDEnumAssetBase<TDDEnumAsset>.Instance.ObsoleteValuesMask;
			
			if (obsoleteBits != 0L)
			{
				var obsoleteIndexes = new List<int>();

				for (int i = 0; i < DDEnumAssetBase<TDDEnumAsset>.MAX_LENGTH; i++)
				{
					if ((obsoleteBits & (1L << i)) != 0L) 
						obsoleteIndexes.Add(i);
				}
				
				result.AddWarning("Contains bits that are obsolete" +
				                  "\nBits: " + string.Join(", ", obsoleteIndexes));
			}
		}

		private void RemoveExtraBits()
		{
			var copy = ValueEntry.SmartValue;
			copy.Value &= DDEnumAssetBase<TDDEnumAsset>.Instance.SetValuesMask;
			ValueEntry.SmartValue = copy;
		}
	}
}