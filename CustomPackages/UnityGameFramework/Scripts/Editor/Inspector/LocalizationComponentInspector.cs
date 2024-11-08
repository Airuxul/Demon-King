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
    [CustomEditor(typeof(LocalizationComponent))]
    internal sealed class LocalizationComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty _EnableLoadDictionaryUpdateEvent = null;
        private SerializedProperty _EnableLoadDictionaryDependencyAssetEvent = null;
        private SerializedProperty _CachedBytesSize = null;

        private HelperInfo<LocalizationHelperBase> _LocalizationHelperInfo = new HelperInfo<LocalizationHelperBase>("Localization");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            LocalizationComponent t = (LocalizationComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(_EnableLoadDictionaryUpdateEvent);
                EditorGUILayout.PropertyField(_EnableLoadDictionaryDependencyAssetEvent);
                _LocalizationHelperInfo.Draw();
                EditorGUILayout.PropertyField(_CachedBytesSize);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Language", t.Language.ToString());
                EditorGUILayout.LabelField("System Language", t.SystemLanguage.ToString());
                EditorGUILayout.LabelField("Dictionary Count", t.DictionaryCount.ToString());
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
            _EnableLoadDictionaryUpdateEvent = serializedObject.FindProperty("_EnableLoadDictionaryUpdateEvent");
            _EnableLoadDictionaryDependencyAssetEvent = serializedObject.FindProperty("_EnableLoadDictionaryDependencyAssetEvent");
            _CachedBytesSize = serializedObject.FindProperty("_CachedBytesSize");

            _LocalizationHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            _LocalizationHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
