//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.DataTable;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor
{
    [CustomEditor(typeof(DataTableComponent))]
    internal sealed class DataTableComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty _EnableLoadDataTableUpdateEvent = null;
        private SerializedProperty _EnableLoadDataTableDependencyAssetEvent = null;
        private SerializedProperty _CachedBytesSize = null;

        private HelperInfo<DataTableHelperBase> _DataTableHelperInfo = new HelperInfo<DataTableHelperBase>("DataTable");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            DataTableComponent t = (DataTableComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(_EnableLoadDataTableUpdateEvent);
                EditorGUILayout.PropertyField(_EnableLoadDataTableDependencyAssetEvent);
                _DataTableHelperInfo.Draw();
                EditorGUILayout.PropertyField(_CachedBytesSize);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Data Table Count", t.Count.ToString());
                EditorGUILayout.LabelField("Cached Bytes Size", t.CachedBytesSize.ToString());

                DataTableBase[] dataTables = t.GetAllDataTables();
                foreach (DataTableBase dataTable in dataTables)
                {
                    DrawDataTable(dataTable);
                }
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        private void OnEnable()
        {
            _EnableLoadDataTableUpdateEvent = serializedObject.FindProperty("_EnableLoadDataTableUpdateEvent");
            _EnableLoadDataTableDependencyAssetEvent = serializedObject.FindProperty("_EnableLoadDataTableDependencyAssetEvent");
            _CachedBytesSize = serializedObject.FindProperty("_CachedBytesSize");

            _DataTableHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void DrawDataTable(DataTableBase dataTable)
        {
            EditorGUILayout.LabelField(dataTable.FullName, Utility.Text.Format("{0} Rows", dataTable.Count));
        }

        private void RefreshTypeNames()
        {
            _DataTableHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
