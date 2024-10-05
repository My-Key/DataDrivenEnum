using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace DDEnum.Editor
{
	public class IDDEnumMaskDrawer<TMask, TDDEnumAsset, TValue> : OdinValueDrawer<TMask> 
		where TMask : struct, IDDEnumMask<TDDEnumAsset, TValue, TMask>
		where TDDEnumAsset : DDEnumAssetBase<TDDEnumAsset>
		where TValue :  struct, IDDEnumValue<TDDEnumAsset, TValue>
	{
		private const string BUTTON_SEPARATOR = ", ";
		private const string TOOLTIP_SEPARATOR = "\n\n";
		
		private List<int> m_selectedList = new List<int>();
		private GUIContent m_buttonContent = new GUIContent();
		
		private List<string> m_selectedNames = new List<string>();
		private List<int> m_selectedIndexes = new List<int>();

		protected override void Initialize()
		{
			base.Initialize();

			UpdateButtonText();
		}

		private void UpdateButtonText()
		{
			var currentValue = ValueEntry.SmartValue.Value;
			
			m_selectedNames.Clear();
			m_selectedIndexes.Clear();
			
			for (int index = 0; index < DDEnumAssetBase<TDDEnumAsset>.MAX_LENGTH; index++)
			{
				if ((currentValue & 1L << index) != 0)
				{
					m_selectedNames.Add(GetMenuItemName(index));
					m_selectedIndexes.Add(index);
				}
			}
			
			UpdateButtonContent();
		}

		private void UpdateButtonContent()
		{
			m_buttonContent.text = string.Join(BUTTON_SEPARATOR, m_selectedNames);

			var tooltip = string.Empty;

			for (var index = 0; index < m_selectedNames.Count; index++)
			{
				var selectedName = m_selectedNames[index];

				var entry = DDEnumAssetBase<TDDEnumAsset>.Instance.IndexToEntry(m_selectedIndexes[index]);

				tooltip += selectedName + ": " + entry.Message;
				
				if (index < m_selectedNames.Count - 1)
					tooltip += TOOLTIP_SEPARATOR;
			}

			m_buttonContent.tooltip = tooltip;

			if (m_selectedNames.Count == 1)
			{
				var icon = DDEnumAssetBase<TDDEnumAsset>.Instance.IndexToEntry(m_selectedIndexes[0]).Icon;

				if (icon == SdfIconType.None)
					m_buttonContent.image = null;
				else
				{
					m_buttonContent.image =
						SdfIcons.CreateTransparentIconTexture(icon, EditorStyles.label.normal.textColor, 18, 18, 0);
				}
			}
			else
			{
				m_buttonContent.image = null;
			}
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			GenericSelector<int>.DrawSelectorDropdown(label, m_buttonContent, CreateSelector);
		}

		private OdinSelector<int> CreateSelector(Rect rect)
		{
			var selector = new DDEnumSelector<TDDEnumAsset>(true);

			var currentValue = ValueEntry.SmartValue.Value;

			m_selectedList.Clear();
			
			for (int i = 0; i < DDEnumAssetBase<TDDEnumAsset>.MAX_LENGTH; i++)
			{
				if ((currentValue & 1L << i) != 0)
					m_selectedList.Add(i);
			}
			
			selector.CheckboxToggle = true;
			selector.SetSelection(m_selectedList);
			selector.ShowInPopup(rect);

			selector.SelectionChanged += x =>
			{
				ValueEntry.Property.Tree.DelayAction(() =>
				{
					long value = 0;
					
					m_selectedNames.Clear();
					m_selectedIndexes.Clear();
					
					foreach (var index in x)
					{
						value |= 1L << index;

						m_selectedNames.Add(GetMenuItemName(index));;
						m_selectedIndexes.Add(index);
					}

					UpdateButtonContent();
					
					var smartValue = ValueEntry.SmartValue;
					smartValue.Value = value;
					ValueEntry.SmartValue = smartValue;

					// var smartValue = ValueEntry.SmartValue;
					// smartValue.Value = value;
					// ValueEntry.SmartValue = smartValue;
					ValueEntry.ApplyChanges();
				});
			};

			return selector;
		}

		private string GetMenuItemName(int index) => DDEnumAssetBase<TDDEnumAsset>.Instance.IndexToName(index);
	}
	
	public class DDEnumSelector<TDDEnumAsset> : GenericSelector<int> 
		where TDDEnumAsset : DDEnumAssetBase<TDDEnumAsset>
	{
		private readonly FieldInfo m_requestCheckboxUpdate;

		public DDEnumSelector(bool multiSelect) : base(DDEnumAssetBase<TDDEnumAsset>.Instance.name,
			DDEnumAssetBase<TDDEnumAsset>.Instance.ValidBits, multiSelect, GetMenuItemName)
		{
			CheckboxToggle = multiSelect;
			
			m_requestCheckboxUpdate = typeof(GenericSelector<int>).GetField("requestCheckboxUpdate",
				BindingFlags.NonPublic | BindingFlags.Instance);
		}
		
		private static string GetMenuItemName(int index) => DDEnumAssetBase<TDDEnumAsset>.Instance.IndexToName(index);

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			base.BuildSelectionTree(tree);
			
			tree.EnumerateTree().ForEach(AddInfo);
			// tree.EnumerateTree().ForEach(AddIconSDF);
			tree.EnumerateTree().AddIcons(AddIcon);
		}
		
		private void AddInfo(OdinMenuItem menuItem)
		{
			if (menuItem.Value == null)
				return;
			
			var entry = GetMenuItemEntry((int)menuItem.Value);
			
			if (entry == null || !entry.Obsolete && string.IsNullOrWhiteSpace(entry.Tooltip))
				return;
			
			menuItem.OnDrawItem += DrawInfo;
		}

		private Texture2D AddIcon(OdinMenuItem menuItem)
		{
			if (menuItem.Value == null)
				return null;
			
			var entry = GetMenuItemEntry((int)menuItem.Value);
			
			if (entry == null)
				return null;
			
			var icon = entry.Icon;
			
			if (icon == SdfIconType.None)
				return null;

			return SdfIcons.CreateTransparentIconTexture(icon, EditorStyles.label.normal.textColor, 18, 18, 0);
		}
		
		// SDF icons don't render correctly for mask selector
		private void AddIconSDF(OdinMenuItem menuItem)
		{
			if (menuItem.Value == null)
				return;
			
			var entry = GetMenuItemEntry((int)menuItem.Value);
			
			if (entry == null)
				return;
			
			var icon = entry.Icon;
			
			if (icon == SdfIconType.None)
				return;

			menuItem.SdfIcon = icon;
			menuItem.SdfIconColor = Color.white;
		}

		private void DrawInfo(OdinMenuItem thisMenuItem)
		{
			var entry = GetMenuItemEntry((int)thisMenuItem.Value);
			
			if (entry.Obsolete)
				SdfIcons.DrawIcon(thisMenuItem.LabelRect.AlignMiddle(12).AlignRight(14), SdfIconType.ExclamationCircleFill, Color.red);
			else
				SdfIcons.DrawIcon(thisMenuItem.LabelRect.AlignMiddle(12).AlignRight(14), SdfIconType.InfoCircleFill);
			
			EditorGUI.LabelField(thisMenuItem.LabelRect, new GUIContent(string.Empty, GetMenuItemEntry((int)thisMenuItem.Value).Message));
		}

		private DDEnumAssetBase.Entry GetMenuItemEntry(int index) => DDEnumAssetBase<TDDEnumAsset>.Instance.IndexToEntry(index);
		
		protected override void DrawSelectionTree()
		{
			if (CheckboxToggle)
			{
				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button("None"))
				{
					SetSelection(new List<int>());

					m_requestCheckboxUpdate.SetValue(this, true);
					TriggerSelectionChanged();
				}

				if (GUILayout.Button("All"))
				{
					SetSelection(DDEnumAssetBase<TDDEnumAsset>.Instance.ValidBits);

					m_requestCheckboxUpdate.SetValue(this, true);
					TriggerSelectionChanged();
				}

				EditorGUILayout.EndHorizontal();
			}
			
			base.DrawSelectionTree();
		}
	}
}