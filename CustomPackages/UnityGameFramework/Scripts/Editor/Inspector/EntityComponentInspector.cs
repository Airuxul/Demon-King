//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Entity;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor
{
    [CustomEditor(typeof(EntityComponent))]
    internal sealed class EntityComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty _EnableShowEntityUpdateEvent = null;
        private SerializedProperty _EnableShowEntityDependencyAssetEvent = null;
        private SerializedProperty _InstanceRoot = null;
        private SerializedProperty _EntityGroups = null;

        private HelperInfo<EntityHelperBase> _EntityHelperInfo = new HelperInfo<EntityHelperBase>("Entity");
        private HelperInfo<EntityGroupHelperBase> _EntityGroupHelperInfo = new HelperInfo<EntityGroupHelperBase>("EntityGroup");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EntityComponent t = (EntityComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(_EnableShowEntityUpdateEvent);
                EditorGUILayout.PropertyField(_EnableShowEntityDependencyAssetEvent);
                EditorGUILayout.PropertyField(_InstanceRoot);
                _EntityHelperInfo.Draw();
                _EntityGroupHelperInfo.Draw();
                EditorGUILayout.PropertyField(_EntityGroups, true);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Entity Group Count", t.EntityGroupCount.ToString());
                EditorGUILayout.LabelField("Entity Count (Total)", t.EntityCount.ToString());
                IEntityGroup[] entityGroups = t.GetAllEntityGroups();
                foreach (IEntityGroup entityGroup in entityGroups)
                {
                    EditorGUILayout.LabelField(Utility.Text.Format("Entity Count ({0})", entityGroup.Name), entityGroup.EntityCount.ToString());
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
            _EnableShowEntityUpdateEvent = serializedObject.FindProperty("_EnableShowEntityUpdateEvent");
            _EnableShowEntityDependencyAssetEvent = serializedObject.FindProperty("_EnableShowEntityDependencyAssetEvent");
            _InstanceRoot = serializedObject.FindProperty("_InstanceRoot");
            _EntityGroups = serializedObject.FindProperty("_EntityGroups");

            _EntityHelperInfo.Init(serializedObject);
            _EntityGroupHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            _EntityHelperInfo.Refresh();
            _EntityGroupHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
