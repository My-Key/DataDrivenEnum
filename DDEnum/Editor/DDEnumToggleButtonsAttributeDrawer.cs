using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace DDEnum.Editor
{
	public abstract class DDEnumToggleButtonsAttributeDrawerBase<TDDEnumAsset, TType> : OdinAttributeDrawer<EnumToggleButtonsAttribute, TType> 
		where TDDEnumAsset : DDEnumAssetBase<TDDEnumAsset>
	{
		private static readonly Color OBSOLETE_COLOR = new Color(1f,0.7f,0.7f);
		
		private float m_previousControlRectWidth;
		
		private ButtonContentGroup m_buttonContentGroup;

		private class ButtonContent
		{
			public GUIContent Content{ get; private set;}
			public float CalculatedWidth{ get; private set; }
			public int Index{ get; }
			public DDEnumAssetBase.Entry Entry { get; }
			public string[] Group  { get; private set; }
			private string Name { get; set; }

			public ButtonContent(DDEnumAssetBase.Entry entry, int index)
			{
				Entry = entry;
				Index = index;
				
				CalculateWidth();
				
				CreateContent();
			}

			private void CreateContent()
			{
				var name = Entry.Name;
				
				if (name.EndsWith('/'))
					name = name.Substring(0, name.Length - 1);
				
				var nameSplitIndex = name.LastIndexOf('/');

				if (nameSplitIndex != -1)
				{
					Name = name.Substring(nameSplitIndex + 1);
					Group = name.Substring(0, nameSplitIndex).Split('/');
				}
				else
				{
					Name = name;
					Group = null;
				}
				
				Content = new GUIContent(Name, Entry.Message);
			}

			private bool CalculateWidth()
			{
				var name = Entry.Name;
				
				if (name.EndsWith('/'))
					name = name.Substring(0, name.Length - 1);
				
				var nameSplitIndex = name.LastIndexOf('/');

				if (nameSplitIndex != -1)
					name = name.Substring(nameSplitIndex + 1);
				else
					name = name;

				SirenixEditorGUI.CalculateMinimumSDFIconButtonWidth(name, SirenixGUIStyles.MiniButton,
					Entry.Icon != SdfIconType.None, EditorGUIUtility.singleLineHeight, out _, 
					out _, out _, out var size);

				var newWidth = size + 8f;
				var widthChanged = CalculatedWidth != newWidth;
				CalculatedWidth = newWidth;

				return widthChanged;
			}

			public bool Update()
			{
				if (CalculateWidth())
				{
					CreateContent();
					return true;
				}

				return false;
			}
		}
		
		private class ButtonContentGroup
		{
			public const float INDENT_SIZE = 15f;
			
			public List<ButtonContent> Buttons{ get; } = new();
			public string Group { get; private set; }

			private int IndentLevel { get; set; }
			
			public float IndentSize => IndentLevel * INDENT_SIZE;

			public Dictionary<string, ButtonContentGroup> ButtonContentGroups { get; } = new();
			
			public List<int> RowsCount { get; } = new();
			
			public List<float> RowLength { get; } =  new();
			
			public int RowCount => RowsCount.Count + (string.IsNullOrEmpty(Group) ? 0 : 1) + ButtonContentGroups.Sum(x => x.Value.RowCount);
			
			public int ButtonsCount => Buttons.Count + ButtonContentGroups.Values.Sum(x => x.ButtonsCount);

			public void AddButton(ButtonContent button)
			{
				if (button.Group == null || button.Group.Length == 0)
					Buttons.Add(button);
				else
					AddButton(button, 0);
			}

			private void AddButton(ButtonContent button, int depthLevel)
			{
				if (button.Group.Length <= depthLevel)
					Buttons.Add(button);
				else
				{
					var groupName = button.Group[depthLevel];
					
					if (!ButtonContentGroups.TryGetValue(groupName, out var group))
					{
						group = new ButtonContentGroup();
						group.Group = groupName;
						group.IndentLevel = depthLevel + 1;

						ButtonContentGroups.Add(groupName, group);
					}

					group.AddButton(button, depthLevel + 1);
				}
			}

			public void Initialize()
			{
				if (Buttons.Count > 0)
					RowsCount.Add(Buttons.Count);
				
				RowLength.Add(Buttons.Sum(x => x.CalculatedWidth));

				foreach (var buttonContentGroup in ButtonContentGroups.Values) 
					buttonContentGroup.Initialize();
			}

			public void Clear()
			{
				Buttons.Clear();
				ButtonContentGroups.Clear();
			}
			
			public void CalculateRows(float previousControlRectWidth)
			{
				RowLength.Clear();
				RowsCount.Clear();

				var namesSum = 0f;
				var rowCount = 0;

				for (var index = 0; index < Buttons.Count; index++)
				{
					var nameSize = Buttons[index].CalculatedWidth;

					if (namesSum + nameSize > previousControlRectWidth - IndentSize)
					{
						RowLength.Add(namesSum);
						RowsCount.Add(rowCount);
						rowCount = 0;
						namesSum = 0f;
					}

					namesSum += nameSize;
					rowCount++;
				}
			
				RowLength.Add(namesSum);
				
				if (rowCount > 0)
					RowsCount.Add(rowCount);

				foreach (var buttonGroup in ButtonContentGroups.Values) 
					buttonGroup.CalculateRows(previousControlRectWidth);
			}

			public bool Update()
			{
				var needToRecalculateRows = false;
				
				foreach (var button in Buttons)
				{
					if (button.Update())
						needToRecalculateRows = true;
				}

				foreach (var group in ButtonContentGroups.Values) 
					needToRecalculateRows = group.Update();

				return needToRecalculateRows;
			}
		}

		protected override void Initialize()
		{
			base.Initialize();
			
			var instance = DDEnumAssetBase<TDDEnumAsset>.Instance;
			var validBits = instance.ValidBits;
			
			m_buttonContentGroup = new ButtonContentGroup();
			
			CreateButtons(validBits, instance);
		}

		private void CreateButtons(IEnumerable<int> validBits, TDDEnumAsset instance)
		{
			m_buttonContentGroup.Clear();

			foreach (var validBit in validBits)
			{
				var entry = instance.IndexToEntry(validBit);
				var buttonContent = new ButtonContent(entry, validBit);
				
				m_buttonContentGroup.AddButton(buttonContent);
			}
			
			m_buttonContentGroup.Initialize();
		}

		private void UpdateNames()
		{
			var instance = DDEnumAssetBase<TDDEnumAsset>.Instance;
			var validBits = instance.ValidBits;
			var count = validBits.Count();

			var needToRecalculateRows = false;
			
			if (count != m_buttonContentGroup.ButtonsCount)
			{
				CreateButtons(validBits, instance);
				needToRecalculateRows = true;
			}
			else
				needToRecalculateRows |= m_buttonContentGroup.Update();
			
			if (needToRecalculateRows)
			{
				CalculateRows();
				
				m_buttonContentGroup.CalculateRows(m_previousControlRectWidth);
			}
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Event.current.type != EventType.Layout)
				UpdateNames();
			
			var buttonIndex = 0;
			var rect = new Rect();

			SirenixEditorGUI.GetFeatureRichControlRect(label,
			Mathf.CeilToInt(EditorGUIUtility.singleLineHeight * m_buttonContentGroup.RowCount),
			out int _, out bool _, out var valueRect);

			valueRect.height = EditorGUIUtility.singleLineHeight;
			
			rect = valueRect;
			
			DrawGroup(valueRect, rect, m_buttonContentGroup);
			
			if (Event.current.type != EventType.Repaint || m_previousControlRectWidth == rect.width)
				return;
		
			m_previousControlRectWidth = rect.width;
			
			CalculateRows();
		}

		private Rect DrawGroup(Rect valueRect, Rect rect, ButtonContentGroup group)
		{
			valueRect.height = EditorGUIUtility.singleLineHeight;
			
			if (!string.IsNullOrEmpty(group.Group))
			{
				valueRect.xMin = rect.xMin + group.IndentSize;
				valueRect.xMax = rect.xMax;
				EditorGUI.LabelField(valueRect, group.Group, EditorStyles.boldLabel);
				valueRect.y += valueRect.height;
			}
			
			int buttonIndex = 0;
			
			for (int rowIndex = 0; rowIndex < group.RowsCount.Count; ++rowIndex)
			{
				valueRect.xMin = rect.xMin + group.IndentSize;
				valueRect.xMax = rect.xMax;

				var xMax = valueRect.xMax;

				for (int columnIndex = 0; columnIndex < group.RowsCount[rowIndex]; ++columnIndex)
				{
					var button = group.Buttons[buttonIndex];
					valueRect.width = (rect.width - group.IndentSize) * button.CalculatedWidth / group.RowLength[rowIndex];
					valueRect = DrawButton(button, valueRect, columnIndex, rowIndex, xMax, group.RowsCount);
					++buttonIndex;
				}

				valueRect.y += valueRect.height;
			}

			foreach (var buttonGroup in group.ButtonContentGroups.Values) 
				valueRect = DrawGroup(valueRect, rect, buttonGroup);

			return valueRect;
		}

		private void CalculateRows()
		{
			m_buttonContentGroup.CalculateRows(m_previousControlRectWidth);
		}

		private Rect DrawButton(ButtonContent button, Rect valueRect, int columnIndex, int rowIndex,
			float xMax, List<int> rowsCount)
		{
			var position = valueRect;
			GUIStyle style = SirenixGUIStyles.MiniButtonMid;

			if (columnIndex == 0 && columnIndex == rowsCount[rowIndex] - 1)
			{
				style = SirenixGUIStyles.MiniButton;
				--position.x;
				position.xMax = xMax + 1f;
			}
			else if (columnIndex == 0)
				style = SirenixGUIStyles.MiniButtonLeft;
			else if (columnIndex == rowsCount[rowIndex] - 1)
			{
				style = SirenixGUIStyles.MiniButtonRight;
				position.xMax = xMax;
			}
			
			if (button.Entry.Obsolete)
				GUIHelper.PushColor(OBSOLETE_COLOR);

			if (SirenixEditorGUI.SDFIconButton(position, button.Content, button.Entry.Icon, IconAlignment.LeftOfText, 
				    style, IsSelected(button.Index)))
			{
				Select(button.Index);
			}
			
			if (button.Entry.Obsolete) 
				GUIHelper.PopColor();
			
			valueRect.x += valueRect.width;

			return valueRect;
		}
		
		protected abstract bool IsSelected(int bitIndex);

		protected abstract void Select(int bitIndex);
	}

	[DrawerPriority(DrawerPriorityLevel.AttributePriority)]
	public class DDEnumToggleButtonsAttributeDrawerValue<TDDEnumAsset, TValue> : DDEnumToggleButtonsAttributeDrawerBase<TDDEnumAsset, TValue>
		where TDDEnumAsset : DDEnumAssetBase<TDDEnumAsset>
		where TValue : struct, IDDEnumValue<TDDEnumAsset, TValue>
	{
		protected override bool IsSelected(int bitIndex) => ValueEntry.SmartValue.Value == bitIndex;
		
		protected override void Select(int bitIndex)
		{
			var valueEntrySmartValue = ValueEntry.SmartValue;
			valueEntrySmartValue.Value = bitIndex;
			ValueEntry.SmartValue = valueEntrySmartValue;
		}
	}
	

	[DrawerPriority(DrawerPriorityLevel.AttributePriority)]
	public class DDEnumToggleButtonsAttributeDrawerMask<TDDEnumAsset, TValue, TMask> : DDEnumToggleButtonsAttributeDrawerBase<TDDEnumAsset, TMask>
		where TDDEnumAsset : DDEnumAssetBase<TDDEnumAsset>
		where TValue : struct, IDDEnumValue<TDDEnumAsset, TValue>
		where TMask : struct, IDDEnumMask<TDDEnumAsset, TValue, TMask>
	{
		protected override bool IsSelected(int bitIndex) => (ValueEntry.SmartValue.Value & (1L << bitIndex)) != 0L;
		
		protected override void Select(int bitIndex)
		{
			var valueEntrySmartValue = ValueEntry.SmartValue;
			valueEntrySmartValue.Value ^= 1L << bitIndex;
			ValueEntry.SmartValue = valueEntrySmartValue;
		}
	}
}