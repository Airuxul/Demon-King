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
    [CustomEditor(typeof(SoundComponent))]
    internal sealed class SoundComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty _EnablePlaySoundUpdateEvent = null;
        private SerializedProperty _EnablePlaySoundDependencyAssetEvent = null;
        private SerializedProperty _InstanceRoot = null;
        private SerializedProperty _AudioMixer = null;
        private SerializedProperty _SoundGroups = null;

        private HelperInfo<SoundHelperBase> _SoundHelperInfo = new HelperInfo<SoundHelperBase>("Sound");
        private HelperInfo<SoundGroupHelperBase> _SoundGroupHelperInfo = new HelperInfo<SoundGroupHelperBase>("SoundGroup");
        private HelperInfo<SoundAgentHelperBase> _SoundAgentHelperInfo = new HelperInfo<SoundAgentHelperBase>("SoundAgent");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            SoundComponent t = (SoundComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(_EnablePlaySoundUpdateEvent);
                EditorGUILayout.PropertyField(_EnablePlaySoundDependencyAssetEvent);
                EditorGUILayout.PropertyField(_InstanceRoot);
                EditorGUILayout.PropertyField(_AudioMixer);
                _SoundHelperInfo.Draw();
                _SoundGroupHelperInfo.Draw();
                _SoundAgentHelperInfo.Draw();
                EditorGUILayout.PropertyField(_SoundGroups, true);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Sound Group Count", t.SoundGroupCount.ToString());
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
            _EnablePlaySoundUpdateEvent = serializedObject.FindProperty("_EnablePlaySoundUpdateEvent");
            _EnablePlaySoundDependencyAssetEvent = serializedObject.FindProperty("_EnablePlaySoundDependencyAssetEvent");
            _InstanceRoot = serializedObject.FindProperty("_InstanceRoot");
            _AudioMixer = serializedObject.FindProperty("_AudioMixer");
            _SoundGroups = serializedObject.FindProperty("_SoundGroups");

            _SoundHelperInfo.Init(serializedObject);
            _SoundGroupHelperInfo.Init(serializedObject);
            _SoundAgentHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            _SoundHelperInfo.Refresh();
            _SoundGroupHelperInfo.Refresh();
            _SoundAgentHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
