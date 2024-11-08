//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Procedure;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor
{
    [CustomEditor(typeof(ProcedureComponent))]
    internal sealed class ProcedureComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty _AvailableProcedureTypeNames = null;
        private SerializedProperty _EntranceProcedureTypeName = null;

        private string[] _ProcedureTypeNames = null;
        private List<string> _CurrentAvailableProcedureTypeNames = null;
        private int _EntranceProcedureIndex = -1;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            ProcedureComponent t = (ProcedureComponent)target;

            if (string.IsNullOrEmpty(_EntranceProcedureTypeName.stringValue))
            {
                EditorGUILayout.HelpBox("Entrance procedure is invalid.", MessageType.Error);
            }
            else if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("Current Procedure", t.CurrentProcedure == null ? "None" : t.CurrentProcedure.GetType().ToString());
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                GUILayout.Label("Available Procedures", EditorStyles.boldLabel);
                if (_ProcedureTypeNames.Length > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    {
                        foreach (string procedureTypeName in _ProcedureTypeNames)
                        {
                            bool selected = _CurrentAvailableProcedureTypeNames.Contains(procedureTypeName);
                            if (selected != EditorGUILayout.ToggleLeft(procedureTypeName, selected))
                            {
                                if (!selected)
                                {
                                    _CurrentAvailableProcedureTypeNames.Add(procedureTypeName);
                                    WriteAvailableProcedureTypeNames();
                                }
                                else if (procedureTypeName != _EntranceProcedureTypeName.stringValue)
                                {
                                    _CurrentAvailableProcedureTypeNames.Remove(procedureTypeName);
                                    WriteAvailableProcedureTypeNames();
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("There is no available procedure.", MessageType.Warning);
                }

                if (_CurrentAvailableProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Separator();

                    int selectedIndex = EditorGUILayout.Popup("Entrance Procedure", _EntranceProcedureIndex, _CurrentAvailableProcedureTypeNames.ToArray());
                    if (selectedIndex != _EntranceProcedureIndex)
                    {
                        _EntranceProcedureIndex = selectedIndex;
                        _EntranceProcedureTypeName.stringValue = _CurrentAvailableProcedureTypeNames[selectedIndex];
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select available procedures first.", MessageType.Info);
                }
            }
            EditorGUI.EndDisabledGroup();

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
            _AvailableProcedureTypeNames = serializedObject.FindProperty("_AvailableProcedureTypeNames");
            _EntranceProcedureTypeName = serializedObject.FindProperty("_EntranceProcedureTypeName");

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            _ProcedureTypeNames = Type.GetRuntimeTypeNames(typeof(ProcedureBase));
            ReadAvailableProcedureTypeNames();
            int oldCount = _CurrentAvailableProcedureTypeNames.Count;
            _CurrentAvailableProcedureTypeNames = _CurrentAvailableProcedureTypeNames.Where(x => _ProcedureTypeNames.Contains(x)).ToList();
            if (_CurrentAvailableProcedureTypeNames.Count != oldCount)
            {
                WriteAvailableProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(_EntranceProcedureTypeName.stringValue))
            {
                _EntranceProcedureIndex = _CurrentAvailableProcedureTypeNames.IndexOf(_EntranceProcedureTypeName.stringValue);
                if (_EntranceProcedureIndex < 0)
                {
                    _EntranceProcedureTypeName.stringValue = null;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ReadAvailableProcedureTypeNames()
        {
            _CurrentAvailableProcedureTypeNames = new List<string>();
            int count = _AvailableProcedureTypeNames.arraySize;
            for (int i = 0; i < count; i++)
            {
                _CurrentAvailableProcedureTypeNames.Add(_AvailableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue);
            }
        }

        private void WriteAvailableProcedureTypeNames()
        {
            _AvailableProcedureTypeNames.ClearArray();
            if (_CurrentAvailableProcedureTypeNames == null)
            {
                return;
            }

            _CurrentAvailableProcedureTypeNames.Sort();
            int count = _CurrentAvailableProcedureTypeNames.Count;
            for (int i = 0; i < count; i++)
            {
                _AvailableProcedureTypeNames.InsertArrayElementAtIndex(i);
                _AvailableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue = _CurrentAvailableProcedureTypeNames[i];
            }

            if (!string.IsNullOrEmpty(_EntranceProcedureTypeName.stringValue))
            {
                _EntranceProcedureIndex = _CurrentAvailableProcedureTypeNames.IndexOf(_EntranceProcedureTypeName.stringValue);
                if (_EntranceProcedureIndex < 0)
                {
                    _EntranceProcedureTypeName.stringValue = null;
                }
            }
        }
    }
}
