//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor
{
    [CustomEditor(typeof(BaseComponent))]
    internal sealed class BaseComponentInspector : GameFrameworkInspector
    {
        private const string NoneOptionName = "<None>";
        private static readonly float[] GameSpeed = new float[] { 0f, 0.01f, 0.1f, 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f };
        private static readonly string[] GameSpeedForDisplay = new string[] { "0x", "0.01x", "0.1x", "0.25x", "0.5x", "1x", "1.5x", "2x", "4x", "8x" };

        private SerializedProperty _EditorResourceMode = null;
        private SerializedProperty _EditorLanguage = null;
        private SerializedProperty _TextHelperTypeName = null;
        private SerializedProperty _VersionHelperTypeName = null;
        private SerializedProperty _LogHelperTypeName = null;
        private SerializedProperty _CompressionHelperTypeName = null;
        private SerializedProperty _JsonHelperTypeName = null;
        private SerializedProperty _FrameRate = null;
        private SerializedProperty _GameSpeed = null;
        private SerializedProperty _RunInBackground = null;
        private SerializedProperty _NeverSleep = null;

        private string[] _TextHelperTypeNames = null;
        private int _TextHelperTypeNameIndex = 0;
        private string[] _VersionHelperTypeNames = null;
        private int _VersionHelperTypeNameIndex = 0;
        private string[] _LogHelperTypeNames = null;
        private int _LogHelperTypeNameIndex = 0;
        private string[] _CompressionHelperTypeNames = null;
        private int _CompressionHelperTypeNameIndex = 0;
        private string[] _JsonHelperTypeNames = null;
        private int _JsonHelperTypeNameIndex = 0;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            BaseComponent t = (BaseComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                _EditorResourceMode.boolValue = EditorGUILayout.BeginToggleGroup("Editor Resource Mode", _EditorResourceMode.boolValue);
                {
                    EditorGUILayout.HelpBox("Editor resource mode option is only for editor mode. Game Framework will use editor resource files, which you should validate first.", MessageType.Warning);
                    EditorGUILayout.PropertyField(_EditorLanguage);
                    EditorGUILayout.HelpBox("Editor language option is only use for localization test in editor mode.", MessageType.Info);
                }
                EditorGUILayout.EndToggleGroup();

                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("Global Helpers", EditorStyles.boldLabel);

                    int textHelperSelectedIndex = EditorGUILayout.Popup("Text Helper", _TextHelperTypeNameIndex, _TextHelperTypeNames);
                    if (textHelperSelectedIndex != _TextHelperTypeNameIndex)
                    {
                        _TextHelperTypeNameIndex = textHelperSelectedIndex;
                        _TextHelperTypeName.stringValue = textHelperSelectedIndex <= 0 ? null : _TextHelperTypeNames[textHelperSelectedIndex];
                    }

                    int versionHelperSelectedIndex = EditorGUILayout.Popup("Version Helper", _VersionHelperTypeNameIndex, _VersionHelperTypeNames);
                    if (versionHelperSelectedIndex != _VersionHelperTypeNameIndex)
                    {
                        _VersionHelperTypeNameIndex = versionHelperSelectedIndex;
                        _VersionHelperTypeName.stringValue = versionHelperSelectedIndex <= 0 ? null : _VersionHelperTypeNames[versionHelperSelectedIndex];
                    }

                    int logHelperSelectedIndex = EditorGUILayout.Popup("Log Helper", _LogHelperTypeNameIndex, _LogHelperTypeNames);
                    if (logHelperSelectedIndex != _LogHelperTypeNameIndex)
                    {
                        _LogHelperTypeNameIndex = logHelperSelectedIndex;
                        _LogHelperTypeName.stringValue = logHelperSelectedIndex <= 0 ? null : _LogHelperTypeNames[logHelperSelectedIndex];
                    }

                    int compressionHelperSelectedIndex = EditorGUILayout.Popup("Compression Helper", _CompressionHelperTypeNameIndex, _CompressionHelperTypeNames);
                    if (compressionHelperSelectedIndex != _CompressionHelperTypeNameIndex)
                    {
                        _CompressionHelperTypeNameIndex = compressionHelperSelectedIndex;
                        _CompressionHelperTypeName.stringValue = compressionHelperSelectedIndex <= 0 ? null : _CompressionHelperTypeNames[compressionHelperSelectedIndex];
                    }

                    int jsonHelperSelectedIndex = EditorGUILayout.Popup("JSON Helper", _JsonHelperTypeNameIndex, _JsonHelperTypeNames);
                    if (jsonHelperSelectedIndex != _JsonHelperTypeNameIndex)
                    {
                        _JsonHelperTypeNameIndex = jsonHelperSelectedIndex;
                        _JsonHelperTypeName.stringValue = jsonHelperSelectedIndex <= 0 ? null : _JsonHelperTypeNames[jsonHelperSelectedIndex];
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndDisabledGroup();

            int frameRate = EditorGUILayout.IntSlider("Frame Rate", _FrameRate.intValue, 1, 120);
            if (frameRate != _FrameRate.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.FrameRate = frameRate;
                }
                else
                {
                    _FrameRate.intValue = frameRate;
                }
            }

            EditorGUILayout.BeginVertical("box");
            {
                float gameSpeed = EditorGUILayout.Slider("Game Speed", _GameSpeed.floatValue, 0f, 8f);
                int selectedGameSpeed = GUILayout.SelectionGrid(GetSelectedGameSpeed(gameSpeed), GameSpeedForDisplay, 5);
                if (selectedGameSpeed >= 0)
                {
                    gameSpeed = GetGameSpeed(selectedGameSpeed);
                }

                if (gameSpeed != _GameSpeed.floatValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.GameSpeed = gameSpeed;
                    }
                    else
                    {
                        _GameSpeed.floatValue = gameSpeed;
                    }
                }
            }
            EditorGUILayout.EndVertical();

            bool runInBackground = EditorGUILayout.Toggle("Run in Background", _RunInBackground.boolValue);
            if (runInBackground != _RunInBackground.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.RunInBackground = runInBackground;
                }
                else
                {
                    _RunInBackground.boolValue = runInBackground;
                }
            }

            bool neverSleep = EditorGUILayout.Toggle("Never Sleep", _NeverSleep.boolValue);
            if (neverSleep != _NeverSleep.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.NeverSleep = neverSleep;
                }
                else
                {
                    _NeverSleep.boolValue = neverSleep;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        private void OnEnable()
        {
            _EditorResourceMode = serializedObject.FindProperty("_EditorResourceMode");
            _EditorLanguage = serializedObject.FindProperty("_EditorLanguage");
            _TextHelperTypeName = serializedObject.FindProperty("_TextHelperTypeName");
            _VersionHelperTypeName = serializedObject.FindProperty("_VersionHelperTypeName");
            _LogHelperTypeName = serializedObject.FindProperty("_LogHelperTypeName");
            _CompressionHelperTypeName = serializedObject.FindProperty("_CompressionHelperTypeName");
            _JsonHelperTypeName = serializedObject.FindProperty("_JsonHelperTypeName");
            _FrameRate = serializedObject.FindProperty("_FrameRate");
            _GameSpeed = serializedObject.FindProperty("_GameSpeed");
            _RunInBackground = serializedObject.FindProperty("_RunInBackground");
            _NeverSleep = serializedObject.FindProperty("_NeverSleep");

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            List<string> textHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            textHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Utility.Text.ITextHelper)));
            _TextHelperTypeNames = textHelperTypeNames.ToArray();
            _TextHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(_TextHelperTypeName.stringValue))
            {
                _TextHelperTypeNameIndex = textHelperTypeNames.IndexOf(_TextHelperTypeName.stringValue);
                if (_TextHelperTypeNameIndex <= 0)
                {
                    _TextHelperTypeNameIndex = 0;
                    _TextHelperTypeName.stringValue = null;
                }
            }

            List<string> versionHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            versionHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Version.IVersionHelper)));
            _VersionHelperTypeNames = versionHelperTypeNames.ToArray();
            _VersionHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(_VersionHelperTypeName.stringValue))
            {
                _VersionHelperTypeNameIndex = versionHelperTypeNames.IndexOf(_VersionHelperTypeName.stringValue);
                if (_VersionHelperTypeNameIndex <= 0)
                {
                    _VersionHelperTypeNameIndex = 0;
                    _VersionHelperTypeName.stringValue = null;
                }
            }

            List<string> logHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            logHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(GameFrameworkLog.ILogHelper)));
            _LogHelperTypeNames = logHelperTypeNames.ToArray();
            _LogHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(_LogHelperTypeName.stringValue))
            {
                _LogHelperTypeNameIndex = logHelperTypeNames.IndexOf(_LogHelperTypeName.stringValue);
                if (_LogHelperTypeNameIndex <= 0)
                {
                    _LogHelperTypeNameIndex = 0;
                    _LogHelperTypeName.stringValue = null;
                }
            }

            List<string> compressionHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            compressionHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Utility.Compression.ICompressionHelper)));
            _CompressionHelperTypeNames = compressionHelperTypeNames.ToArray();
            _CompressionHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(_CompressionHelperTypeName.stringValue))
            {
                _CompressionHelperTypeNameIndex = compressionHelperTypeNames.IndexOf(_CompressionHelperTypeName.stringValue);
                if (_CompressionHelperTypeNameIndex <= 0)
                {
                    _CompressionHelperTypeNameIndex = 0;
                    _CompressionHelperTypeName.stringValue = null;
                }
            }

            List<string> jsonHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            jsonHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Utility.Json.IJsonHelper)));
            _JsonHelperTypeNames = jsonHelperTypeNames.ToArray();
            _JsonHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(_JsonHelperTypeName.stringValue))
            {
                _JsonHelperTypeNameIndex = jsonHelperTypeNames.IndexOf(_JsonHelperTypeName.stringValue);
                if (_JsonHelperTypeNameIndex <= 0)
                {
                    _JsonHelperTypeNameIndex = 0;
                    _JsonHelperTypeName.stringValue = null;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private float GetGameSpeed(int selectedGameSpeed)
        {
            if (selectedGameSpeed < 0)
            {
                return GameSpeed[0];
            }

            if (selectedGameSpeed >= GameSpeed.Length)
            {
                return GameSpeed[GameSpeed.Length - 1];
            }

            return GameSpeed[selectedGameSpeed];
        }

        private int GetSelectedGameSpeed(float gameSpeed)
        {
            for (int i = 0; i < GameSpeed.Length; i++)
            {
                if (gameSpeed == GameSpeed[i])
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
