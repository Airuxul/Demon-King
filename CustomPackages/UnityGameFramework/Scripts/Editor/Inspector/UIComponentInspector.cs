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
    [CustomEditor(typeof(UIComponent))]
    internal sealed class UIComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty _EnableOpenUIFormSuccessEvent = null;
        private SerializedProperty _EnableOpenUIFormFailureEvent = null;
        private SerializedProperty _EnableOpenUIFormUpdateEvent = null;
        private SerializedProperty _EnableOpenUIFormDependencyAssetEvent = null;
        private SerializedProperty _EnableCloseUIFormCompleteEvent = null;
        private SerializedProperty _InstanceAutoReleaseInterval = null;
        private SerializedProperty _InstanceCapacity = null;
        private SerializedProperty _InstanceExpireTime = null;
        private SerializedProperty _InstancePriority = null;
        private SerializedProperty _InstanceRoot = null;
        private SerializedProperty _UIGroups = null;

        private HelperInfo<UIFormHelperBase> _UIFormHelperInfo = new HelperInfo<UIFormHelperBase>("UIForm");
        private HelperInfo<UIGroupHelperBase> _UIGroupHelperInfo = new HelperInfo<UIGroupHelperBase>("UIGroup");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            UIComponent t = (UIComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(_EnableOpenUIFormSuccessEvent);
                EditorGUILayout.PropertyField(_EnableOpenUIFormFailureEvent);
                EditorGUILayout.PropertyField(_EnableOpenUIFormUpdateEvent);
                EditorGUILayout.PropertyField(_EnableOpenUIFormDependencyAssetEvent);
                EditorGUILayout.PropertyField(_EnableCloseUIFormCompleteEvent);
            }
            EditorGUI.EndDisabledGroup();

            float instanceAutoReleaseInterval = EditorGUILayout.DelayedFloatField("Instance Auto Release Interval", _InstanceAutoReleaseInterval.floatValue);
            if (instanceAutoReleaseInterval != _InstanceAutoReleaseInterval.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.InstanceAutoReleaseInterval = instanceAutoReleaseInterval;
                }
                else
                {
                    _InstanceAutoReleaseInterval.floatValue = instanceAutoReleaseInterval;
                }
            }

            int instanceCapacity = EditorGUILayout.DelayedIntField("Instance Capacity", _InstanceCapacity.intValue);
            if (instanceCapacity != _InstanceCapacity.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.InstanceCapacity = instanceCapacity;
                }
                else
                {
                    _InstanceCapacity.intValue = instanceCapacity;
                }
            }

            float instanceExpireTime = EditorGUILayout.DelayedFloatField("Instance Expire Time", _InstanceExpireTime.floatValue);
            if (instanceExpireTime != _InstanceExpireTime.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.InstanceExpireTime = instanceExpireTime;
                }
                else
                {
                    _InstanceExpireTime.floatValue = instanceExpireTime;
                }
            }

            int instancePriority = EditorGUILayout.DelayedIntField("Instance Priority", _InstancePriority.intValue);
            if (instancePriority != _InstancePriority.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.InstancePriority = instancePriority;
                }
                else
                {
                    _InstancePriority.intValue = instancePriority;
                }
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(_InstanceRoot);
                _UIFormHelperInfo.Draw();
                _UIGroupHelperInfo.Draw();
                EditorGUILayout.PropertyField(_UIGroups, true);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("UI Group Count", t.UIGroupCount.ToString());
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
            _EnableOpenUIFormSuccessEvent = serializedObject.FindProperty("_EnableOpenUIFormSuccessEvent");
            _EnableOpenUIFormFailureEvent = serializedObject.FindProperty("_EnableOpenUIFormFailureEvent");
            _EnableOpenUIFormUpdateEvent = serializedObject.FindProperty("_EnableOpenUIFormUpdateEvent");
            _EnableOpenUIFormDependencyAssetEvent = serializedObject.FindProperty("_EnableOpenUIFormDependencyAssetEvent");
            _EnableCloseUIFormCompleteEvent = serializedObject.FindProperty("_EnableCloseUIFormCompleteEvent");
            _InstanceAutoReleaseInterval = serializedObject.FindProperty("_InstanceAutoReleaseInterval");
            _InstanceCapacity = serializedObject.FindProperty("_InstanceCapacity");
            _InstanceExpireTime = serializedObject.FindProperty("_InstanceExpireTime");
            _InstancePriority = serializedObject.FindProperty("_InstancePriority");
            _InstanceRoot = serializedObject.FindProperty("_InstanceRoot");
            _UIGroups = serializedObject.FindProperty("_UIGroups");

            _UIFormHelperInfo.Init(serializedObject);
            _UIGroupHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            _UIFormHelperInfo.Refresh();
            _UIGroupHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
