﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor
{
    [CustomEditor(typeof(ReferencePoolComponent))]
    internal sealed class ReferencePoolComponentInspector : GameFrameworkInspector
    {
        private readonly Dictionary<string, List<ReferencePoolInfo>> _ReferencePoolInfos = new Dictionary<string, List<ReferencePoolInfo>>(StringComparer.Ordinal);
        private readonly HashSet<string> _OpenedItems = new HashSet<string>();

        private SerializedProperty _EnableStrictCheck = null;

        private bool _ShowFullClassName = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            ReferencePoolComponent t = (ReferencePoolComponent)target;

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                bool enableStrictCheck = EditorGUILayout.Toggle("Enable Strict Check", t.EnableStrictCheck);
                if (enableStrictCheck != t.EnableStrictCheck)
                {
                    t.EnableStrictCheck = enableStrictCheck;
                }

                EditorGUILayout.LabelField("Reference Pool Count", ReferencePool.Count.ToString());
                _ShowFullClassName = EditorGUILayout.Toggle("Show Full Class Name", _ShowFullClassName);
                _ReferencePoolInfos.Clear();
                ReferencePoolInfo[] referencePoolInfos = ReferencePool.GetAllReferencePoolInfos();
                foreach (ReferencePoolInfo referencePoolInfo in referencePoolInfos)
                {
                    string assemblyName = referencePoolInfo.Type.Assembly.GetName().Name;
                    List<ReferencePoolInfo> results = null;
                    if (!_ReferencePoolInfos.TryGetValue(assemblyName, out results))
                    {
                        results = new List<ReferencePoolInfo>();
                        _ReferencePoolInfos.Add(assemblyName, results);
                    }

                    results.Add(referencePoolInfo);
                }

                foreach (KeyValuePair<string, List<ReferencePoolInfo>> assemblyReferencePoolInfo in _ReferencePoolInfos)
                {
                    bool lastState = _OpenedItems.Contains(assemblyReferencePoolInfo.Key);
                    bool currentState = EditorGUILayout.Foldout(lastState, assemblyReferencePoolInfo.Key);
                    if (currentState != lastState)
                    {
                        if (currentState)
                        {
                            _OpenedItems.Add(assemblyReferencePoolInfo.Key);
                        }
                        else
                        {
                            _OpenedItems.Remove(assemblyReferencePoolInfo.Key);
                        }
                    }

                    if (currentState)
                    {
                        EditorGUILayout.BeginVertical("box");
                        {
                            EditorGUILayout.LabelField(_ShowFullClassName ? "Full Class Name" : "Class Name", "Unused\tUsing\tAcquire\tRelease\tAdd\tRemove");
                            assemblyReferencePoolInfo.Value.Sort(Comparison);
                            foreach (ReferencePoolInfo referencePoolInfo in assemblyReferencePoolInfo.Value)
                            {
                                DrawReferencePoolInfo(referencePoolInfo);
                            }

                            if (GUILayout.Button("Export CSV Data"))
                            {
                                string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty, Utility.Text.Format("Reference Pool Data - {0}.csv", assemblyReferencePoolInfo.Key), string.Empty);
                                if (!string.IsNullOrEmpty(exportFileName))
                                {
                                    try
                                    {
                                        int index = 0;
                                        string[] data = new string[assemblyReferencePoolInfo.Value.Count + 1];
                                        data[index++] = "Class Name,Full Class Name,Unused,Using,Acquire,Release,Add,Remove";
                                        foreach (ReferencePoolInfo referencePoolInfo in assemblyReferencePoolInfo.Value)
                                        {
                                            data[index++] = Utility.Text.Format("{0},{1},{2},{3},{4},{5},{6},{7}", referencePoolInfo.Type.Name, referencePoolInfo.Type.FullName, referencePoolInfo.UnusedReferenceCount, referencePoolInfo.UsingReferenceCount, referencePoolInfo.AcquireReferenceCount, referencePoolInfo.ReleaseReferenceCount, referencePoolInfo.AddReferenceCount, referencePoolInfo.RemoveReferenceCount);
                                        }

                                        File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                                        Debug.Log(Utility.Text.Format("Export reference pool CSV data to '{0}' success.", exportFileName));
                                    }
                                    catch (Exception exception)
                                    {
                                        Debug.LogError(Utility.Text.Format("Export reference pool CSV data to '{0}' failure, exception is '{1}'.", exportFileName, exception));
                                    }
                                }
                            }
                        }
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.Separator();
                    }
                }
            }
            else
            {
                EditorGUILayout.PropertyField(_EnableStrictCheck);
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        private void OnEnable()
        {
            _EnableStrictCheck = serializedObject.FindProperty("_EnableStrictCheck");
        }

        private void DrawReferencePoolInfo(ReferencePoolInfo referencePoolInfo)
        {
            EditorGUILayout.LabelField(_ShowFullClassName ? referencePoolInfo.Type.FullName : referencePoolInfo.Type.Name, Utility.Text.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", referencePoolInfo.UnusedReferenceCount, referencePoolInfo.UsingReferenceCount, referencePoolInfo.AcquireReferenceCount, referencePoolInfo.ReleaseReferenceCount, referencePoolInfo.AddReferenceCount, referencePoolInfo.RemoveReferenceCount));
        }

        private int Comparison(ReferencePoolInfo a, ReferencePoolInfo b)
        {
            if (_ShowFullClassName)
            {
                return a.Type.FullName.CompareTo(b.Type.FullName);
            }
            else
            {
                return a.Type.Name.CompareTo(b.Type.Name);
            }
        }
    }
}
