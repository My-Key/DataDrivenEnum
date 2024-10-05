using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEngine;
using UnityEngine.Serialization;
using yaSingleton.Helpers;

namespace DDEnum
{
	public abstract class DDEnumAssetBase : ScriptableObject
	{
		public const string DATA_DIRECTORY_PATH = "Assets/DataDrivenEnums";

		public virtual SdfIconType GetIcon() => SdfIconType.GearFill;
		
		[Serializable]
		public class Entry
		{
			[SerializeField]
			private string m_name;
			public string Name => m_name;
			
			[SerializeField, MultiLineProperty, OnValueChanged(nameof(ClearMessage))]
			private string m_tooltip;
			public string Tooltip => m_tooltip;

			private void ClearMessage()
			{
				m_message = null;
			}

			private string m_message;
			
			public string Message
			{
				get
				{
					if (m_message != null)
						return m_message;
					
					m_message = string.Empty;
					
					if (m_obsolete)
					{
						m_message = "Obsolete";
						
						if (!string.IsNullOrWhiteSpace(m_obsoleteMessage))
							m_message += ": " + m_obsoleteMessage;
					}

					if (!string.IsNullOrWhiteSpace(m_tooltip))
					{
						if (!string.IsNullOrWhiteSpace(m_message))
							m_message += "\n\nTooltip: ";

						m_message += m_tooltip;
					}

					return m_message;
				}
			}

			[SerializeField] 
			[TableColumnWidth(100, false)]
			private SdfIconType m_icon = SdfIconType.None;
			public SdfIconType Icon => m_icon;
			
			[SerializeField, OnValueChanged(nameof(ClearMessage))]
			[HorizontalGroup("Obsolete", width:25f)]
			[HideLabel]
			private bool m_obsolete = false;
			public bool Obsolete => m_obsolete;
			
			[SerializeField, MultiLineProperty, ShowIf(nameof(m_obsolete)), OnValueChanged(nameof(ClearMessage))] 
			[HorizontalGroup("Obsolete")]
			[HideLabel]
			private string m_obsoleteMessage;
			public string ObsoleteMessage => m_obsoleteMessage;
		}
		
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		private static void LoadPreloadedAssetsInEditor() {
			UnityEditor.PlayerSettings.GetPreloadedAssets();
		}
#endif
	}
	
	public abstract class DDEnumAssetBase<T> : DDEnumAssetBase where T : DDEnumAssetBase<T>
	{
		public static T Instance { get; private set; }

		public const int MAX_LENGTH = 64;

		protected const string TAB_GROUP_NAME = "TabGroup";
		
		[TabGroup(TAB_GROUP_NAME, "Values", HideTabGroupIfTabGroupOnlyHasOneTab = true)]
		[SerializeField]
		[ValidateInput(nameof(FindNameDuplicates))]
		[TableList(ShowIndexLabels = true, AlwaysExpanded = true, ShowPaging = true, NumberOfItemsPerPage = 8, IsReadOnly = true)]
		private Entry[] m_values = new Entry[MAX_LENGTH];

		private bool FindNameDuplicates(Entry[] names, ref string errorMessage)
		{
			var duplicates = names.Where(x => !string.IsNullOrWhiteSpace(x.Name)).GroupBy(x => x.Name)
				.Where(g => g.Count() > 1).ToList();
			
			if (!duplicates.Any())
				return true;

			var listOfNames = new List<string>();

			foreach (var grouped in duplicates)
			{
				foreach (var entry in grouped) 
					listOfNames.Add("\"" + entry.Name + "\" at index " + Array.IndexOf(names, entry));
			}

			errorMessage = "Duplicate names:\n" + string.Join("\n", listOfNames);

			return false;
		}
		
		[SerializeField, HideInInspector] private long m_setValuesMask = 0;
		public long SetValuesMask => m_setValuesMask;
		
		[SerializeField, HideInInspector] private long m_obsoleteValuesMask = 0;
		public long ObsoleteValuesMask => m_obsoleteValuesMask;

		[SerializeField, HideInInspector] private int m_maxValueIndex = 0;
		public int MaxValueIndex => m_maxValueIndex;

		private void OnValidate()
		{
			m_setValuesMask = 0;
			m_maxValueIndex = 0;
			m_obsoleteValuesMask = 0;

			for (int i = 0; i < m_values.Length; i++)
			{
				if (!string.IsNullOrWhiteSpace(m_values[i].Name))
				{
					m_setValuesMask |= 1L << i;
					m_maxValueIndex = i;
				}

				if (m_values[i].Obsolete) 
					m_obsoleteValuesMask |= 1L << i;
			}
		}

		public IEnumerable<string> ValidNames =>
			m_values.Where(s => !string.IsNullOrWhiteSpace(s.Name)).Select(s => s.Name);

		public IEnumerable<int> ValidBits => ValidNames.Select(x => Array.IndexOf(m_values.Select(s => s.Name).ToArray(), x));

		public string IndexToName(int index) => IndexToEntry(index).Name;
		
		public Entry IndexToEntry(int index) => m_values[index];
		
		protected virtual void OnEnable()
		{
			Instance = (T)this;
			
#if UNITY_EDITOR
			if(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				this.AddToPreloadedAssets();
#endif
		}

		protected virtual void OnDisable()
		{
#if UNITY_EDITOR
			if(!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				ScriptableObjectExtensions.RemoveEmptyPreloadedAssets();
#endif
		}
	}
}