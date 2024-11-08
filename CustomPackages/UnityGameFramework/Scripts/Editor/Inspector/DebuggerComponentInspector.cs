//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor
{
    [CustomEditor(typeof(DebuggerComponent))]
    internal sealed class DebuggerComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty _Skin = null;
        private SerializedProperty _ActiveWindow = null;
        private SerializedProperty _ShowFullWindow = null;
        private SerializedProperty _ConsoleWindow = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            DebuggerComponent t = (DebuggerComponent)target;

            EditorGUILayout.PropertyField(_Skin);

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                bool activeWindow = EditorGUILayout.Toggle("Active Window", t.ActiveWindow);
                if (activeWindow != t.ActiveWindow)
                {
                    t.ActiveWindow = activeWindow;
                }
            }
            else
            {
                EditorGUILayout.PropertyField(_ActiveWindow);
            }

            EditorGUILayout.PropertyField(_ShowFullWindow);

            if (EditorApplication.isPlaying)
            {
                if (GUILayout.Button("Reset Layout"))
                {
                    t.ResetLayout();
                }
            }

            EditorGUILayout.PropertyField(_ConsoleWindow, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            _Skin = serializedObject.FindProperty("_Skin");
            _ActiveWindow = serializedObject.FindProperty("_ActiveWindow");
            _ShowFullWindow = serializedObject.FindProperty("_ShowFullWindow");
            _ConsoleWindow = serializedObject.FindProperty("_ConsoleWindow");
        }
    }
}
