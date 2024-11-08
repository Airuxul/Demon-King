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
    [CustomEditor(typeof(EditorResourceComponent))]
    internal sealed class EditorResourceComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty _EnableCachedAssets = null;
        private SerializedProperty _LoadAssetCountPerFrame = null;
        private SerializedProperty _MinLoadAssetRandomDelaySeconds = null;
        private SerializedProperty _MaxLoadAssetRandomDelaySeconds = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorResourceComponent t = (EditorResourceComponent)target;

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Load Waiting Asset Count", t.LoadWaitingAssetCount.ToString());
            }

            EditorGUILayout.PropertyField(_EnableCachedAssets);
            EditorGUILayout.PropertyField(_LoadAssetCountPerFrame);
            EditorGUILayout.PropertyField(_MinLoadAssetRandomDelaySeconds);
            EditorGUILayout.PropertyField(_MaxLoadAssetRandomDelaySeconds);

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        private void OnEnable()
        {
            _EnableCachedAssets = serializedObject.FindProperty("_EnableCachedAssets");
            _LoadAssetCountPerFrame = serializedObject.FindProperty("_LoadAssetCountPerFrame");
            _MinLoadAssetRandomDelaySeconds = serializedObject.FindProperty("_MinLoadAssetRandomDelaySeconds");
            _MaxLoadAssetRandomDelaySeconds = serializedObject.FindProperty("_MaxLoadAssetRandomDelaySeconds");
        }
    }
}
