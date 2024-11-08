//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor
{
    [CustomEditor(typeof(ResourceComponent))]
    internal sealed class ResourceComponentInspector : GameFrameworkInspector
    {
        private static readonly string[] ResourceModeNames = new string[] { "Package", "Updatable", "Updatable While Playing" };

        private SerializedProperty _ResourceMode = null;
        private SerializedProperty _ReadWritePathType = null;
        private SerializedProperty _MinUnloadUnusedAssetsInterval = null;
        private SerializedProperty _MaxUnloadUnusedAssetsInterval = null;
        private SerializedProperty _AssetAutoReleaseInterval = null;
        private SerializedProperty _AssetCapacity = null;
        private SerializedProperty _AssetExpireTime = null;
        private SerializedProperty _AssetPriority = null;
        private SerializedProperty _ResourceAutoReleaseInterval = null;
        private SerializedProperty _ResourceCapacity = null;
        private SerializedProperty _ResourceExpireTime = null;
        private SerializedProperty _ResourcePriority = null;
        private SerializedProperty _UpdatePrefixUri = null;
        private SerializedProperty _GenerateReadWriteVersionListLength = null;
        private SerializedProperty _UpdateRetryCount = null;
        private SerializedProperty _InstanceRoot = null;
        private SerializedProperty _LoadResourceAgentHelperCount = null;

        private FieldInfo _EditorResourceModeFieldInfo = null;

        private int _ResourceModeIndex = 0;
        private HelperInfo<ResourceHelperBase> _ResourceHelperInfo = new HelperInfo<ResourceHelperBase>("Resource");
        private HelperInfo<LoadResourceAgentHelperBase> _LoadResourceAgentHelperInfo = new HelperInfo<LoadResourceAgentHelperBase>("LoadResourceAgent");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            ResourceComponent t = (ResourceComponent)target;

            bool isEditorResourceMode = (bool)_EditorResourceModeFieldInfo.GetValue(target);

            if (isEditorResourceMode)
            {
                EditorGUILayout.HelpBox("Editor resource mode is enabled. Some options are disabled.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
                {
                    EditorGUILayout.EnumPopup("Resource Mode", t.ResourceMode);
                }
                else
                {
                    int selectedIndex = EditorGUILayout.Popup("Resource Mode", _ResourceModeIndex, ResourceModeNames);
                    if (selectedIndex != _ResourceModeIndex)
                    {
                        _ResourceModeIndex = selectedIndex;
                        _ResourceMode.enumValueIndex = selectedIndex + 1;
                    }
                }

                _ReadWritePathType.enumValueIndex = (int)(ReadWritePathType)EditorGUILayout.EnumPopup("Read-Write Path Type", t.ReadWritePathType);
            }
            EditorGUI.EndDisabledGroup();

            float minUnloadUnusedAssetsInterval = EditorGUILayout.Slider("Min Unload Unused Assets Interval", _MinUnloadUnusedAssetsInterval.floatValue, 0f, 3600f);
            if (minUnloadUnusedAssetsInterval != _MinUnloadUnusedAssetsInterval.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.MinUnloadUnusedAssetsInterval = minUnloadUnusedAssetsInterval;
                }
                else
                {
                    _MinUnloadUnusedAssetsInterval.floatValue = minUnloadUnusedAssetsInterval;
                }
            }

            float maxUnloadUnusedAssetsInterval = EditorGUILayout.Slider("Max Unload Unused Assets Interval", _MaxUnloadUnusedAssetsInterval.floatValue, 0f, 3600f);
            if (maxUnloadUnusedAssetsInterval != _MaxUnloadUnusedAssetsInterval.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.MaxUnloadUnusedAssetsInterval = maxUnloadUnusedAssetsInterval;
                }
                else
                {
                    _MaxUnloadUnusedAssetsInterval.floatValue = maxUnloadUnusedAssetsInterval;
                }
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying && isEditorResourceMode);
            {
                float assetAutoReleaseInterval = EditorGUILayout.DelayedFloatField("Asset Auto Release Interval", _AssetAutoReleaseInterval.floatValue);
                if (assetAutoReleaseInterval != _AssetAutoReleaseInterval.floatValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.AssetAutoReleaseInterval = assetAutoReleaseInterval;
                    }
                    else
                    {
                        _AssetAutoReleaseInterval.floatValue = assetAutoReleaseInterval;
                    }
                }

                int assetCapacity = EditorGUILayout.DelayedIntField("Asset Capacity", _AssetCapacity.intValue);
                if (assetCapacity != _AssetCapacity.intValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.AssetCapacity = assetCapacity;
                    }
                    else
                    {
                        _AssetCapacity.intValue = assetCapacity;
                    }
                }

                float assetExpireTime = EditorGUILayout.DelayedFloatField("Asset Expire Time", _AssetExpireTime.floatValue);
                if (assetExpireTime != _AssetExpireTime.floatValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.AssetExpireTime = assetExpireTime;
                    }
                    else
                    {
                        _AssetExpireTime.floatValue = assetExpireTime;
                    }
                }

                int assetPriority = EditorGUILayout.DelayedIntField("Asset Priority", _AssetPriority.intValue);
                if (assetPriority != _AssetPriority.intValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.AssetPriority = assetPriority;
                    }
                    else
                    {
                        _AssetPriority.intValue = assetPriority;
                    }
                }

                float resourceAutoReleaseInterval = EditorGUILayout.DelayedFloatField("Resource Auto Release Interval", _ResourceAutoReleaseInterval.floatValue);
                if (resourceAutoReleaseInterval != _ResourceAutoReleaseInterval.floatValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.ResourceAutoReleaseInterval = resourceAutoReleaseInterval;
                    }
                    else
                    {
                        _ResourceAutoReleaseInterval.floatValue = resourceAutoReleaseInterval;
                    }
                }

                int resourceCapacity = EditorGUILayout.DelayedIntField("Resource Capacity", _ResourceCapacity.intValue);
                if (resourceCapacity != _ResourceCapacity.intValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.ResourceCapacity = resourceCapacity;
                    }
                    else
                    {
                        _ResourceCapacity.intValue = resourceCapacity;
                    }
                }

                float resourceExpireTime = EditorGUILayout.DelayedFloatField("Resource Expire Time", _ResourceExpireTime.floatValue);
                if (resourceExpireTime != _ResourceExpireTime.floatValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.ResourceExpireTime = resourceExpireTime;
                    }
                    else
                    {
                        _ResourceExpireTime.floatValue = resourceExpireTime;
                    }
                }

                int resourcePriority = EditorGUILayout.DelayedIntField("Resource Priority", _ResourcePriority.intValue);
                if (resourcePriority != _ResourcePriority.intValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.ResourcePriority = resourcePriority;
                    }
                    else
                    {
                        _ResourcePriority.intValue = resourcePriority;
                    }
                }

                if (_ResourceModeIndex > 0)
                {
                    string updatePrefixUri = EditorGUILayout.DelayedTextField("Update Prefix Uri", _UpdatePrefixUri.stringValue);
                    if (updatePrefixUri != _UpdatePrefixUri.stringValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            t.UpdatePrefixUri = updatePrefixUri;
                        }
                        else
                        {
                            _UpdatePrefixUri.stringValue = updatePrefixUri;
                        }
                    }

                    int generateReadWriteVersionListLength = EditorGUILayout.DelayedIntField("Generate Read-Write Version List Length", _GenerateReadWriteVersionListLength.intValue);
                    if (generateReadWriteVersionListLength != _GenerateReadWriteVersionListLength.intValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            t.GenerateReadWriteVersionListLength = generateReadWriteVersionListLength;
                        }
                        else
                        {
                            _GenerateReadWriteVersionListLength.intValue = generateReadWriteVersionListLength;
                        }
                    }

                    int updateRetryCount = EditorGUILayout.DelayedIntField("Update Retry Count", _UpdateRetryCount.intValue);
                    if (updateRetryCount != _UpdateRetryCount.intValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            t.UpdateRetryCount = updateRetryCount;
                        }
                        else
                        {
                            _UpdateRetryCount.intValue = updateRetryCount;
                        }
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(_InstanceRoot);

                _ResourceHelperInfo.Draw();
                _LoadResourceAgentHelperInfo.Draw();
                _LoadResourceAgentHelperCount.intValue = EditorGUILayout.IntSlider("Load Resource Agent Helper Count", _LoadResourceAgentHelperCount.intValue, 1, 128);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Unload Unused Assets", Utility.Text.Format("{0:F2} / {1:F2}", t.LastUnloadUnusedAssetsOperationElapseSeconds, t.MaxUnloadUnusedAssetsInterval));
                EditorGUILayout.LabelField("Read-Only Path", t.ReadOnlyPath.ToString());
                EditorGUILayout.LabelField("Read-Write Path", t.ReadWritePath.ToString());
                EditorGUILayout.LabelField("Current Variant", t.CurrentVariant ?? "<Unknwon>");
                EditorGUILayout.LabelField("Applicable Game Version", isEditorResourceMode ? "N/A" : t.ApplicableGameVersion ?? "<Unknwon>");
                EditorGUILayout.LabelField("Internal Resource Version", isEditorResourceMode ? "N/A" : t.InternalResourceVersion.ToString());
                EditorGUILayout.LabelField("Asset Count", isEditorResourceMode ? "N/A" : t.AssetCount.ToString());
                EditorGUILayout.LabelField("Resource Count", isEditorResourceMode ? "N/A" : t.ResourceCount.ToString());
                EditorGUILayout.LabelField("Resource Group Count", isEditorResourceMode ? "N/A" : t.ResourceGroupCount.ToString());
                if (_ResourceModeIndex > 0)
                {
                    EditorGUILayout.LabelField("Applying Resource Pack Path", isEditorResourceMode ? "N/A" : t.ApplyingResourcePackPath ?? "<Unknwon>");
                    EditorGUILayout.LabelField("Apply Waiting Count", isEditorResourceMode ? "N/A" : t.ApplyWaitingCount.ToString());
                    EditorGUILayout.LabelField("Updating Resource Group", isEditorResourceMode ? "N/A" : t.UpdatingResourceGroup != null ? t.UpdatingResourceGroup.Name : "<Unknwon>");
                    EditorGUILayout.LabelField("Update Waiting Count", isEditorResourceMode ? "N/A" : t.UpdateWaitingCount.ToString());
                    EditorGUILayout.LabelField("Update Waiting While Playing Count", isEditorResourceMode ? "N/A" : t.UpdateWaitingWhilePlayingCount.ToString());
                    EditorGUILayout.LabelField("Update Candidate Count", isEditorResourceMode ? "N/A" : t.UpdateCandidateCount.ToString());
                }
                EditorGUILayout.LabelField("Load Total Agent Count", isEditorResourceMode ? "N/A" : t.LoadTotalAgentCount.ToString());
                EditorGUILayout.LabelField("Load Free Agent Count", isEditorResourceMode ? "N/A" : t.LoadFreeAgentCount.ToString());
                EditorGUILayout.LabelField("Load Working Agent Count", isEditorResourceMode ? "N/A" : t.LoadWorkingAgentCount.ToString());
                EditorGUILayout.LabelField("Load Waiting Task Count", isEditorResourceMode ? "N/A" : t.LoadWaitingTaskCount.ToString());
                if (!isEditorResourceMode)
                {
                    EditorGUILayout.BeginVertical("box");
                    {
                        TaskInfo[] loadAssetInfos = t.GetAllLoadAssetInfos();
                        if (loadAssetInfos.Length > 0)
                        {
                            foreach (TaskInfo loadAssetInfo in loadAssetInfos)
                            {
                                DrawLoadAssetInfo(loadAssetInfo);
                            }

                            if (GUILayout.Button("Export CSV Data"))
                            {
                                string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty, "Load Asset Task Data.csv", string.Empty);
                                if (!string.IsNullOrEmpty(exportFileName))
                                {
                                    try
                                    {
                                        int index = 0;
                                        string[] data = new string[loadAssetInfos.Length + 1];
                                        data[index++] = "Load Asset Name,Serial Id,Priority,Status";
                                        foreach (TaskInfo loadAssetInfo in loadAssetInfos)
                                        {
                                            data[index++] = Utility.Text.Format("{0},{1},{2},{3}", loadAssetInfo.Description, loadAssetInfo.SerialId, loadAssetInfo.Priority, loadAssetInfo.Status);
                                        }

                                        File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                                        Debug.Log(Utility.Text.Format("Export load asset task CSV data to '{0}' success.", exportFileName));
                                    }
                                    catch (Exception exception)
                                    {
                                        Debug.LogError(Utility.Text.Format("Export load asset task CSV data to '{0}' failure, exception is '{1}'.", exportFileName, exception));
                                    }
                                }
                            }
                        }
                        else
                        {
                            GUILayout.Label("Load Asset Task is Empty ...");
                        }
                    }
                    EditorGUILayout.EndVertical();
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
            _ResourceMode = serializedObject.FindProperty("_ResourceMode");
            _ReadWritePathType = serializedObject.FindProperty("_ReadWritePathType");
            _MinUnloadUnusedAssetsInterval = serializedObject.FindProperty("_MinUnloadUnusedAssetsInterval");
            _MaxUnloadUnusedAssetsInterval = serializedObject.FindProperty("_MaxUnloadUnusedAssetsInterval");
            _AssetAutoReleaseInterval = serializedObject.FindProperty("_AssetAutoReleaseInterval");
            _AssetCapacity = serializedObject.FindProperty("_AssetCapacity");
            _AssetExpireTime = serializedObject.FindProperty("_AssetExpireTime");
            _AssetPriority = serializedObject.FindProperty("_AssetPriority");
            _ResourceAutoReleaseInterval = serializedObject.FindProperty("_ResourceAutoReleaseInterval");
            _ResourceCapacity = serializedObject.FindProperty("_ResourceCapacity");
            _ResourceExpireTime = serializedObject.FindProperty("_ResourceExpireTime");
            _ResourcePriority = serializedObject.FindProperty("_ResourcePriority");
            _UpdatePrefixUri = serializedObject.FindProperty("_UpdatePrefixUri");
            _GenerateReadWriteVersionListLength = serializedObject.FindProperty("_GenerateReadWriteVersionListLength");
            _UpdateRetryCount = serializedObject.FindProperty("_UpdateRetryCount");
            _InstanceRoot = serializedObject.FindProperty("_InstanceRoot");
            _LoadResourceAgentHelperCount = serializedObject.FindProperty("_LoadResourceAgentHelperCount");

            _EditorResourceModeFieldInfo = target.GetType().GetField("_EditorResourceMode", BindingFlags.NonPublic | BindingFlags.Instance);

            _ResourceHelperInfo.Init(serializedObject);
            _LoadResourceAgentHelperInfo.Init(serializedObject);

            RefreshModes();
            RefreshTypeNames();
        }

        private void DrawLoadAssetInfo(TaskInfo loadAssetInfo)
        {
            EditorGUILayout.LabelField(loadAssetInfo.Description, Utility.Text.Format("[SerialId]{0} [Priority]{1} [Status]{2}", loadAssetInfo.SerialId, loadAssetInfo.Priority, loadAssetInfo.Status));
        }

        private void RefreshModes()
        {
            _ResourceModeIndex = _ResourceMode.enumValueIndex > 0 ? _ResourceMode.enumValueIndex - 1 : 0;
        }

        private void RefreshTypeNames()
        {
            _ResourceHelperInfo.Refresh();
            _LoadResourceAgentHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
