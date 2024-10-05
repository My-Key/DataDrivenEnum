using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace DDEnum.Editor
{
	public class IDDEnumValueDrawer<TDDEnumAsset, TValue> : OdinValueDrawer<TValue>
		where TDDEnumAsset : DDEnumAssetBase<TDDEnumAsset>
		where TValue : struct, IDDEnumValue<TDDEnumAsset, TValue>
	{
		private GUIContent m_buttonContent = new GUIContent();
		private string m_selectedValue = "";

		protected override void Initialize()
		{
			base.Initialize();

			UpdateButtonText();
		}

		private void UpdateButtonText()
		{
			var currentValue = ValueEntry.SmartValue.Value;
			
			m_selectedValue = GetMenuItemName(currentValue);
			
			UpdateButtonContent();
		}

		private void UpdateButtonContent()
		{
			m_buttonContent.text = m_selectedValue;

			var entry = DDEnumAssetBase<TDDEnumAsset>.Instance.IndexToEntry(ValueEntry.SmartValue.Value);
			
			m_buttonContent.tooltip = entry.Message;
			
			var icon = entry.Icon;

			if (icon == SdfIconType.None)
			{
				m_buttonContent.image = null;
				return;
			}

			m_buttonContent.image =
				SdfIcons.CreateTransparentIconTexture(icon, EditorStyles.label.normal.textColor, 18, 18, 0);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			GenericSelector<int>.DrawSelectorDropdown(label, m_buttonContent, CreateSelector);
		}

		private OdinSelector<int> CreateSelector(Rect rect)
		{
			var selector = new DDEnumSelector<TDDEnumAsset>(false);
			
			selector.SetSelection(ValueEntry.SmartValue.Value);
			selector.ShowInPopup(rect);

			selector.SelectionChanged += x =>
			{
				ValueEntry.Property.Tree.DelayAction(() =>
				{
					if (!x.Any())
						return;
					
					int value = x.First();
					
					m_selectedValue = GetMenuItemName(value);

					UpdateButtonContent();
					
					var smartValue = ValueEntry.SmartValue;
					smartValue.Value = value;
					ValueEntry.SmartValue = smartValue;

					ValueEntry.ApplyChanges();
				});
			};

			return selector;
		}

		private string GetMenuItemName(int index) => DDEnumAssetBase<TDDEnumAsset>.Instance.IndexToName(index);
	}
}