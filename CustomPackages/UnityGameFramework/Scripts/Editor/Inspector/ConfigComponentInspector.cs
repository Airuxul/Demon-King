//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEditor;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor
{
    [CustomEditor(typeof(ConfigComponent))]
    internal sealed class ConfigComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty _EnableLoadConfigUpdateEvent = null;
        private SerializedProperty _EnableLoadConfigDependencyAssetEvent = null;
        private SerializedProperty _CachedBytesSize = null;

        private HelperInfo<ConfigHelperBase> _ConfigHelperInfo = new HelperInfo<ConfigHelperBase>("Config");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            ConfigComponent t = (ConfigComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(_EnableLoadConfigUpdateEvent);
                EditorGUILayout.PropertyField(_EnableLoadConfigDependencyAssetEvent);
                _ConfigHelperInfo.Draw();
                EditorGUILayout.PropertyField(_CachedBytesSize);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Config Count", t.Count.ToString());
                EditorGUILayout.LabelField("Cached Bytes Size", t.CachedBytesSize.ToString());
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
            _EnableLoadConfigUpdateEvent = serializedObject.FindProperty("_EnableLoadConfigUpdateEvent");
            _EnableLoadConfigDependencyAssetEvent = serializedObject.FindProperty("_EnableLoadConfigDependencyAssetEvent");
            _CachedBytesSize = serializedObject.FindProperty("_CachedBytesSize");

            _ConfigHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            _ConfigHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
