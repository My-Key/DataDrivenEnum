using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace DDEnum.Editor
{
    // Based on ScriptableObjectCreator from RPG Window sample
    public static class ScriptableObjectCreator
    {
        public static void ShowDialog<T>(string defaultDestinationPath, Action<T> onScritpableObjectCreated = null, IEnumerable<Type> skipTypes = null,
        bool limitToDefaultDestinationPath = false, bool showPopupIfThereIsOnlyOneOption = false, string filePrefix = "New ")
            where T : ScriptableObject
        {
            var selector = new ScriptableObjectSelector<T>(defaultDestinationPath, onScritpableObjectCreated, skipTypes,
                limitToDefaultDestinationPath, filePrefix);

            if (!showPopupIfThereIsOnlyOneOption && selector.SelectionTree.EnumerateTree().Count() == 1)
            {
                selector.SelectionTree.EnumerateTree().First().Select();
                selector.SelectionTree.Selection.ConfirmSelection();
            }
            else
            {
                selector.ShowInPopup(200);
            }
        }

        private class ScriptableObjectSelector<T> : OdinSelector<Type> where T : ScriptableObject
        {
            private event Action<T> OnScritpableObjectCreated;
            private string m_defaultDestinationPath;
            private IEnumerable<Type> m_skipTypes;
            private string m_filePrefix;
            private bool m_limitToDefaultDestinationPath;

            public ScriptableObjectSelector(string mDefaultDestinationPath, Action<T> mOnScritpableObjectCreated = null, 
                IEnumerable<Type> skipTypes = null, bool limitToDefaultDestinationPath = false, string filePrefix = "New ")
            {
                OnScritpableObjectCreated = mOnScritpableObjectCreated;
                
                m_defaultDestinationPath = mDefaultDestinationPath;
                m_skipTypes = skipTypes;
                m_filePrefix = filePrefix;
                m_limitToDefaultDestinationPath = limitToDefaultDestinationPath;
                
                SelectionTree.Config.SelectMenuItemsOnMouseDown = true;
                SelectionTree.Config.ConfirmSelectionOnDoubleClick = true;
                
                SelectionConfirmed += ShowSaveFileDialog;
            }

            protected override void BuildSelectionTree(OdinMenuTree tree)
            {
                var scriptableObjectTypes = AssemblyUtilities.GetTypes(AssemblyCategory.ProjectSpecific)
                    .Where(x => x.IsClass && !x.IsAbstract && x.InheritsFrom(typeof(T)) && (m_skipTypes == null || !m_skipTypes.Contains(x)));

                tree.Selection.SupportsMultiSelect = false;
                tree.Config.DrawSearchToolbar = true;
                tree.AddRange(scriptableObjectTypes, x => x.GetNiceName())
                    .AddThumbnailIcons();
            }

            private void ShowSaveFileDialog(IEnumerable<Type> selection)
            {
                var selectedType = selection.FirstOrDefault();
                
                var dest = m_defaultDestinationPath.TrimEnd('/');

                if (!Directory.Exists(dest))
                {
                    Directory.CreateDirectory(dest);
                    AssetDatabase.Refresh();
                }

                dest = EditorUtility.SaveFilePanel("Save object as", dest, m_filePrefix + selectedType.GetNiceName(), "asset");

                if (!string.IsNullOrEmpty(dest) && PathUtilities.TryMakeRelative(Path.GetDirectoryName(Application.dataPath), dest, out dest) 
                                                && (!m_limitToDefaultDestinationPath || dest.StartsWith(m_defaultDestinationPath)))
                {
                    var instance = ScriptableObject.CreateInstance(selectedType) as T;
                    AssetDatabase.CreateAsset(instance, dest);
                    AssetDatabase.Refresh();

                    OnScritpableObjectCreated?.Invoke(instance);
                }
            }
        }
    }
}
#endif
