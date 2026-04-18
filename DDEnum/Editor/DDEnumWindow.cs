using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace DDEnum.Editor
{
	public class DDEnumWindow : OdinMenuEditorWindow
	{
		[MenuItem("Tools/Data Driven Enums")]
		public static DDEnumWindow OpenWindow()
		{
			var window =  GetWindow<DDEnumWindow>("Data Driven Enums");
			window.Show();

			return window;
		}

		[OnOpenAsset(1)]
		public static bool OpenAsset(int instanceID, int line) => OpenAssetBase(instanceID, line);
		
		protected static bool OpenAssetBase(int instanceID, int line)
		{
			if (EditorUtility.InstanceIDToObject(instanceID) is not DDEnumAssetBase asset)
				return false;

			OpenAndSelect(asset);
			return true;
		}

		public static void OpenAndSelect(DDEnumAssetBase objectBase) => OpenWindow().TrySelectMenuItemWithObject(objectBase);

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree();

			tree.AddAllAssetsAtPath("", DDEnumAssetBase.DATA_DIRECTORY_PATH, typeof(DDEnumAssetBase));
			
			foreach (var menuItem in tree.EnumerateTree())
			{
				if (menuItem.Value is DDEnumAssetBase singletonBase)
					menuItem.AddIcon(singletonBase.GetIcon());
			}
			
			return tree;
		}

		protected override void OnBeginDrawEditors()
		{
			base.OnBeginDrawEditors();
			
			var selected = MenuTree.Selection.FirstOrDefault();
			var toolbarHeight = MenuTree.Config.SearchToolbarHeight;

			SirenixEditorGUI.BeginHorizontalToolbar(toolbarHeight);

			if (selected != null)
			{
				
				EditorGUILayout.LabelField(selected.SmartName);
				
				GUIHelper.PushColor(Color.cyan);
				
				if (SirenixEditorGUI.ToolbarButton(new GUIContent("Select asset")))
					Selection.activeObject = selected.Value as DDEnumAssetBase;
				
				GUIHelper.PopColor();
				
				GUILayout.Space(10);
				
			}else
				GUILayout.FlexibleSpace();

			if (SirenixEditorGUI.ToolbarButton(new GUIContent("Create DDEnum asset")))
			{
				var skipTypes = GetTypesToSkip();

				ScriptableObjectCreator.ShowDialog<DDEnumAssetBase>(DDEnumAssetBase.DATA_DIRECTORY_PATH,
					TrySelectMenuItemWithObject, skipTypes, true, true, null);
			}
			
			SirenixEditorGUI.EndHorizontalToolbar();
		}

		private IEnumerable<Type> GetTypesToSkip() => MenuTree.MenuItems.Where(x => x.Value != null).Select(x => x.Value.GetType());
	}
}