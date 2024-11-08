//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor.ResourceTools
{
    /// <summary>
    /// 资源包生成器。
    /// </summary>
    internal sealed class ResourcePackBuilder : EditorWindow
    {
        private static readonly string[] PlatformForDisplay = new string[] { "Windows", "Windows x64", "macOS", "Linux", "iOS", "Android", "Windows Store", "WebGL" };
        private static readonly int[] LengthLimit = new int[] { 0, 128, 256, 512, 1024, 2048, 4096 };
        private static readonly string[] LengthLimitForDisplay = new string[] { "<Unlimited>", "128 MB", "256 MB", "512 MB", "1 GB", "2 GB", "4 GB", "<Custom>" };

        private ResourcePackBuilderController _Controller = null;
        private string[] _VersionNames = null;
        private string[] _VersionNamesForTargetDisplay = null;
        private string[] _VersionNamesForSourceDisplay = null;
        private int _PlatformIndex = 0;
        private int _CompressionHelperTypeNameIndex = 0;
        private int _LengthLimitIndex = 0;
        private int _TargetVersionIndex = 0;
        private bool[] _SourceVersionIndexes = null;
        private int _SourceVersionCount = 0;

        [MenuItem("Game Framework/Resource Tools/Resource Pack Builder", false, 43)]
        private static void Open()
        {
            ResourcePackBuilder window = GetWindow<ResourcePackBuilder>("Resource Pack Builder", true);
            window.minSize = new Vector2(800f, 400f);
        }

        private void OnEnable()
        {
            _Controller = new ResourcePackBuilderController();
            _Controller.OnBuildResourcePacksStarted += OnBuildResourcePacksStarted;
            _Controller.OnBuildResourcePacksCompleted += OnBuildResourcePacksCompleted;
            _Controller.OnBuildResourcePackSuccess += OnBuildResourcePackSuccess;
            _Controller.OnBuildResourcePackFailure += OnBuildResourcePackFailure;

            _Controller.Load();
            RefreshVersionNames();

            _CompressionHelperTypeNameIndex = 0;
            string[] compressionHelperTypeNames = _Controller.GetCompressionHelperTypeNames();
            for (int i = 0; i < compressionHelperTypeNames.Length; i++)
            {
                if (_Controller.CompressionHelperTypeName == compressionHelperTypeNames[i])
                {
                    _CompressionHelperTypeNameIndex = i;
                    break;
                }
            }

            _Controller.RefreshCompressionHelper();
        }

        private void Update()
        {
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width), GUILayout.Height(position.height));
            {
                GUILayout.Space(5f);
                EditorGUILayout.LabelField("Environment Information", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Product Name", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_Controller.ProductName);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Company Name", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_Controller.CompanyName);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Game Identifier", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_Controller.GameIdentifier);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Game Framework Version", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_Controller.GameFrameworkVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Unity Version", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_Controller.UnityVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Applicable Game Version", GUILayout.Width(160f));
                        EditorGUILayout.LabelField(_Controller.ApplicableGameVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5f);
                EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Working Directory", GUILayout.Width(160f));
                        string directory = EditorGUILayout.TextField(_Controller.WorkingDirectory);
                        if (_Controller.WorkingDirectory != directory)
                        {
                            _Controller.WorkingDirectory = directory;
                            RefreshVersionNames();
                        }
                        if (GUILayout.Button("Browse...", GUILayout.Width(80f)))
                        {
                            BrowseWorkingDirectory();
                            RefreshVersionNames();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Platform", GUILayout.Width(160f));
                        int platformIndex = EditorGUILayout.Popup(_PlatformIndex, PlatformForDisplay);
                        if (_PlatformIndex != platformIndex)
                        {
                            _PlatformIndex = platformIndex;
                            _Controller.Platform = (Platform)(1 << platformIndex);
                            RefreshVersionNames();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Compression Helper", GUILayout.Width(160f));
                        string[] names = _Controller.GetCompressionHelperTypeNames();
                        int selectedIndex = EditorGUILayout.Popup(_CompressionHelperTypeNameIndex, names);
                        if (selectedIndex != _CompressionHelperTypeNameIndex)
                        {
                            _CompressionHelperTypeNameIndex = selectedIndex;
                            _Controller.CompressionHelperTypeName = selectedIndex <= 0 ? string.Empty : names[selectedIndex];
                            if (_Controller.RefreshCompressionHelper())
                            {
                                Debug.Log("Set compression helper success.");
                            }
                            else
                            {
                                Debug.LogWarning("Set compression helper failure.");
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    if (_Controller.Platform == Platform.Undefined || string.IsNullOrEmpty(_Controller.CompressionHelperTypeName) || !_Controller.IsValidWorkingDirectory)
                    {
                        string message = string.Empty;
                        if (!_Controller.IsValidWorkingDirectory)
                        {
                            if (!string.IsNullOrEmpty(message))
                            {
                                message += Environment.NewLine;
                            }

                            message += "Working directory is invalid.";
                        }

                        if (_Controller.Platform == Platform.Undefined)
                        {
                            if (!string.IsNullOrEmpty(message))
                            {
                                message += Environment.NewLine;
                            }

                            message += "Platform is invalid.";
                        }

                        if (string.IsNullOrEmpty(_Controller.CompressionHelperTypeName))
                        {
                            if (!string.IsNullOrEmpty(message))
                            {
                                message += Environment.NewLine;
                            }

                            message += "Compression helper is invalid.";
                        }

                        EditorGUILayout.HelpBox(message, MessageType.Error);
                    }
                    else if (_VersionNamesForTargetDisplay.Length <= 0)
                    {
                        EditorGUILayout.HelpBox("No version was found in the specified working directory and platform.", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Source Path", GUILayout.Width(160f));
                            GUILayout.Label(_Controller.SourcePathForDisplay);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Output Path", GUILayout.Width(160f));
                            GUILayout.Label(_Controller.OutputPath);
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Backup Diff", GUILayout.Width(160f));
                            _Controller.BackupDiff = EditorGUILayout.Toggle(_Controller.BackupDiff);
                        }
                        EditorGUILayout.EndHorizontal();
                        if (_Controller.BackupDiff)
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField("Backup Version", GUILayout.Width(160f));
                                _Controller.BackupVersion = EditorGUILayout.Toggle(_Controller.BackupVersion);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Length Limit", GUILayout.Width(160f));
                            EditorGUILayout.BeginVertical();
                            {
                                int lengthLimitIndex = EditorGUILayout.Popup(_LengthLimitIndex, LengthLimitForDisplay);
                                if (_LengthLimitIndex != lengthLimitIndex)
                                {
                                    _LengthLimitIndex = lengthLimitIndex;
                                    if (_LengthLimitIndex < LengthLimit.Length)
                                    {
                                        _Controller.LengthLimit = LengthLimit[_LengthLimitIndex];
                                    }
                                }

                                if (_LengthLimitIndex >= LengthLimit.Length)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    {
                                        _Controller.LengthLimit = EditorGUILayout.IntField(_Controller.LengthLimit);
                                        if (_Controller.LengthLimit < 0)
                                        {
                                            _Controller.LengthLimit = 0;
                                        }

                                        GUILayout.Label(" MB", GUILayout.Width(30f));
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                            }
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Target Version", GUILayout.Width(160f));
                            int value = EditorGUILayout.Popup(_TargetVersionIndex, _VersionNamesForTargetDisplay);
                            if (_TargetVersionIndex != value)
                            {
                                _TargetVersionIndex = value;
                                RefreshSourceVersionCount();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Source Version", GUILayout.Width(160f));
                            EditorGUILayout.BeginVertical();
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    EditorGUILayout.LabelField(_SourceVersionCount.ToString() + (_SourceVersionCount > 1 ? " items" : " item") + " selected.");
                                    if (GUILayout.Button("Select All Except <None>", GUILayout.Width(180f)))
                                    {
                                        _SourceVersionIndexes[0] = false;
                                        for (int i = 1; i < _SourceVersionIndexes.Length; i++)
                                        {
                                            _SourceVersionIndexes[i] = true;
                                        }

                                        RefreshSourceVersionCount();
                                    }
                                    if (GUILayout.Button("Select All", GUILayout.Width(100f)))
                                    {
                                        for (int i = 0; i < _SourceVersionIndexes.Length; i++)
                                        {
                                            _SourceVersionIndexes[i] = true;
                                        }

                                        RefreshSourceVersionCount();
                                    }
                                    if (GUILayout.Button("Select None", GUILayout.Width(100f)))
                                    {
                                        for (int i = 0; i < _SourceVersionIndexes.Length; i++)
                                        {
                                            _SourceVersionIndexes[i] = false;
                                        }

                                        RefreshSourceVersionCount();
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                                {
                                    int count = _VersionNamesForSourceDisplay.Length;
                                    int column = 5;
                                    int row = (count - 1) / column + 1;
                                    for (int i = 0; i < column && i < count; i++)
                                    {
                                        EditorGUILayout.BeginVertical();
                                        {
                                            for (int j = 0; j < row; j++)
                                            {
                                                int index = j * column + i;
                                                if (index < count)
                                                {
                                                    bool isTarget = index - 1 == _TargetVersionIndex;
                                                    EditorGUI.BeginDisabledGroup(isTarget);
                                                    {
                                                        bool selected = GUILayout.Toggle(_SourceVersionIndexes[index], isTarget ? _VersionNamesForSourceDisplay[index] + " [Target]" : _VersionNamesForSourceDisplay[index], "button");
                                                        if (_SourceVersionIndexes[index] != selected)
                                                        {
                                                            _SourceVersionIndexes[index] = selected;
                                                            RefreshSourceVersionCount();
                                                        }
                                                    }
                                                    EditorGUI.EndDisabledGroup();
                                                }
                                            }
                                        }
                                        EditorGUILayout.EndVertical();
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    GUILayout.Space(2f);
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(2f);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginDisabledGroup(_Controller.Platform == Platform.Undefined || string.IsNullOrEmpty(_Controller.CompressionHelperTypeName) || !_Controller.IsValidWorkingDirectory || _SourceVersionCount <= 0);
                    {
                        if (GUILayout.Button("Start Build Resource Packs"))
                        {
                            string[] sourceVersions = new string[_SourceVersionCount];
                            int count = 0;
                            for (int i = 0; i < _SourceVersionIndexes.Length; i++)
                            {
                                if (_SourceVersionIndexes[i])
                                {
                                    sourceVersions[count++] = i > 0 ? _VersionNames[i - 1] : null;
                                }
                            }

                            _Controller.BuildResourcePacks(sourceVersions, _VersionNames[_TargetVersionIndex]);
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void BrowseWorkingDirectory()
        {
            string directory = EditorUtility.OpenFolderPanel("Select Working Directory", _Controller.WorkingDirectory, string.Empty);
            if (!string.IsNullOrEmpty(directory))
            {
                _Controller.WorkingDirectory = directory;
            }
        }

        private void RefreshVersionNames()
        {
            _VersionNames = _Controller.GetVersionNames();
            _VersionNamesForTargetDisplay = new string[_VersionNames.Length];
            _VersionNamesForSourceDisplay = new string[_VersionNames.Length + 1];
            _VersionNamesForSourceDisplay[0] = "<None>";
            for (int i = 0; i < _VersionNames.Length; i++)
            {
                string versionNameForDisplay = GetVersionNameForDisplay(_VersionNames[i]);
                _VersionNamesForTargetDisplay[i] = versionNameForDisplay;
                _VersionNamesForSourceDisplay[i + 1] = versionNameForDisplay;
            }

            _TargetVersionIndex = _VersionNames.Length - 1;
            _SourceVersionIndexes = new bool[_VersionNames.Length + 1];
            _SourceVersionCount = 0;
        }

        private void RefreshSourceVersionCount()
        {
            _SourceVersionIndexes[_TargetVersionIndex + 1] = false;
            _SourceVersionCount = 0;
            if (_SourceVersionIndexes == null)
            {
                return;
            }

            for (int i = 0; i < _SourceVersionIndexes.Length; i++)
            {
                if (_SourceVersionIndexes[i])
                {
                    _SourceVersionCount++;
                }
            }
        }

        private string GetVersionNameForDisplay(string versionName)
        {
            if (string.IsNullOrEmpty(versionName))
            {
                return "<None>";
            }

            string[] splitedVersionNames = versionName.Split('_');
            if (splitedVersionNames.Length < 2)
            {
                return null;
            }

            string text = splitedVersionNames[0];
            for (int i = 1; i < splitedVersionNames.Length - 1; i++)
            {
                text += "." + splitedVersionNames[i];
            }

            return Utility.Text.Format("{0} ({1})", text, splitedVersionNames[splitedVersionNames.Length - 1]);
        }

        private void OnBuildResourcePacksStarted(int count)
        {
            Debug.Log(Utility.Text.Format("Build resource packs started, '{0}' items to be built.", count));
            EditorUtility.DisplayProgressBar("Build Resource Packs", Utility.Text.Format("Build resource packs, {0} items to be built.", count), 0f);
        }

        private void OnBuildResourcePacksCompleted(int successCount, int count)
        {
            int failureCount = count - successCount;
            string str = Utility.Text.Format("Build resource packs completed, '{0}' items, '{1}' success, '{2}' failure.", count, successCount, failureCount);
            if (failureCount > 0)
            {
                Debug.LogWarning(str);
            }
            else
            {
                Debug.Log(str);
            }

            EditorUtility.ClearProgressBar();
        }

        private void OnBuildResourcePackSuccess(int index, int count, string sourceVersion, string targetVersion)
        {
            Debug.Log(Utility.Text.Format("Build resource packs success, source version '{0}', target version '{1}'.", GetVersionNameForDisplay(sourceVersion), GetVersionNameForDisplay(targetVersion)));
            EditorUtility.DisplayProgressBar("Build Resource Packs", Utility.Text.Format("Build resource packs, {0}/{1} completed.", index + 1, count), (float)index / count);
        }

        private void OnBuildResourcePackFailure(int index, int count, string sourceVersion, string targetVersion)
        {
            Debug.LogWarning(Utility.Text.Format("Build resource packs failure, source version '{0}', target version '{1}'.", GetVersionNameForDisplay(sourceVersion), GetVersionNameForDisplay(targetVersion)));
            EditorUtility.DisplayProgressBar("Build Resource Packs", Utility.Text.Format("Build resource packs, {0}/{1} completed.", index + 1, count), (float)index / count);
        }
    }
}
