//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor.ResourceTools
{
    /// <summary>
    /// 资源生成器。
    /// </summary>
    internal sealed class ResourceBuilder : EditorWindow
    {
        private ResourceBuilderController _Controller = null;
        private bool _OrderBuildResources = false;
        private int _CompressionHelperTypeNameIndex = 0;
        private int _BuildEventHandlerTypeNameIndex = 0;

        [MenuItem("Game Framework/Resource Tools/Resource Builder", false, 40)]
        private static void Open()
        {
            ResourceBuilder window = GetWindow<ResourceBuilder>("Resource Builder", true);
#if UNITY_2019_3_OR_NEWER
            window.minSize = new Vector2(800f, 640f);
#else
            window.minSize = new Vector2(800f, 600f);
#endif
        }

        private void OnEnable()
        {
            _Controller = new ResourceBuilderController();
            _Controller.OnLoadingResource += OnLoadingResource;
            _Controller.OnLoadingAsset += OnLoadingAsset;
            _Controller.OnLoadCompleted += OnLoadCompleted;
            _Controller.OnAnalyzingAsset += OnAnalyzingAsset;
            _Controller.OnAnalyzeCompleted += OnAnalyzeCompleted;
            _Controller.ProcessingAssetBundle += OnProcessingAssetBundle;
            _Controller.ProcessingBinary += OnProcessingBinary;
            _Controller.ProcessResourceComplete += OnProcessResourceComplete;
            _Controller.BuildResourceError += OnBuildResourceError;

            _OrderBuildResources = false;

            if (_Controller.Load())
            {
                Debug.Log("Load configuration success.");

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

                _BuildEventHandlerTypeNameIndex = 0;
                string[] buildEventHandlerTypeNames = _Controller.GetBuildEventHandlerTypeNames();
                for (int i = 0; i < buildEventHandlerTypeNames.Length; i++)
                {
                    if (_Controller.BuildEventHandlerTypeName == buildEventHandlerTypeNames[i])
                    {
                        _BuildEventHandlerTypeNameIndex = i;
                        break;
                    }
                }

                _Controller.RefreshBuildEventHandler();
            }
            else
            {
                Debug.LogWarning("Load configuration failure.");
            }
        }

        private void Update()
        {
            if (_OrderBuildResources)
            {
                _OrderBuildResources = false;
                BuildResources();
            }
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
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField("Platforms", EditorStyles.boldLabel);
                        EditorGUILayout.BeginHorizontal("box");
                        {
                            EditorGUILayout.BeginVertical();
                            {
                                DrawPlatform(Platform.Windows, "Windows");
                                DrawPlatform(Platform.Windows64, "Windows x64");
                                DrawPlatform(Platform.MacOS, "macOS");
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            {
                                DrawPlatform(Platform.Linux, "Linux");
                                DrawPlatform(Platform.IOS, "iOS");
                                DrawPlatform(Platform.Android, "Android");
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            {
                                DrawPlatform(Platform.WindowsStore, "Windows Store");
                                DrawPlatform(Platform.WebGL, "WebGL");
                            }
                            EditorGUILayout.EndVertical();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5f);
                EditorGUILayout.LabelField("Compression", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("AssetBundle Compression", GUILayout.Width(160f));
                        _Controller.AssetBundleCompression = (AssetBundleCompressionType)EditorGUILayout.EnumPopup(_Controller.AssetBundleCompression);
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
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Additional Compression", GUILayout.Width(160f));
                        _Controller.AdditionalCompressionSelected = EditorGUILayout.ToggleLeft("Additional Compression for Output Full Resources with Compression Helper", _Controller.AdditionalCompressionSelected);
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
                        EditorGUILayout.LabelField("Force Rebuild AssetBundle", GUILayout.Width(160f));
                        _Controller.ForceRebuildAssetBundleSelected = EditorGUILayout.Toggle(_Controller.ForceRebuildAssetBundleSelected);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Build Event Handler", GUILayout.Width(160f));
                        string[] names = _Controller.GetBuildEventHandlerTypeNames();
                        int selectedIndex = EditorGUILayout.Popup(_BuildEventHandlerTypeNameIndex, names);
                        if (selectedIndex != _BuildEventHandlerTypeNameIndex)
                        {
                            _BuildEventHandlerTypeNameIndex = selectedIndex;
                            _Controller.BuildEventHandlerTypeName = selectedIndex <= 0 ? string.Empty : names[selectedIndex];
                            if (_Controller.RefreshBuildEventHandler())
                            {
                                Debug.Log("Set build event handler success.");
                            }
                            else
                            {
                                Debug.LogWarning("Set build event handler failure.");
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Internal Resource Version", GUILayout.Width(160f));
                        _Controller.InternalResourceVersion = EditorGUILayout.IntField(_Controller.InternalResourceVersion);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Resource Version", GUILayout.Width(160f));
                        GUILayout.Label(Utility.Text.Format("{0} ({1})", _Controller.ApplicableGameVersion, _Controller.InternalResourceVersion));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Output Directory", GUILayout.Width(160f));
                        _Controller.OutputDirectory = EditorGUILayout.TextField(_Controller.OutputDirectory);
                        if (GUILayout.Button("Browse...", GUILayout.Width(80f)))
                        {
                            BrowseOutputDirectory();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Working Path", GUILayout.Width(160f));
                        GUILayout.Label(_Controller.WorkingPath);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(!_Controller.OutputPackageSelected);
                        EditorGUILayout.LabelField("Output Package Path", GUILayout.Width(160f));
                        GUILayout.Label(_Controller.OutputPackagePath);
                        EditorGUI.EndDisabledGroup();
                        _Controller.OutputPackageSelected = EditorGUILayout.ToggleLeft("Generate", _Controller.OutputPackageSelected, GUILayout.Width(70f));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(!_Controller.OutputFullSelected);
                        EditorGUILayout.LabelField("Output Full Path", GUILayout.Width(160f));
                        GUILayout.Label(_Controller.OutputFullPath);
                        EditorGUI.EndDisabledGroup();
                        _Controller.OutputFullSelected = EditorGUILayout.ToggleLeft("Generate", _Controller.OutputFullSelected, GUILayout.Width(70f));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginDisabledGroup(!_Controller.OutputPackedSelected);
                        EditorGUILayout.LabelField("Output Packed Path", GUILayout.Width(160f));
                        GUILayout.Label(_Controller.OutputPackedPath);
                        EditorGUI.EndDisabledGroup();
                        _Controller.OutputPackedSelected = EditorGUILayout.ToggleLeft("Generate", _Controller.OutputPackedSelected, GUILayout.Width(70f));
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Build Report Path", GUILayout.Width(160f));
                        GUILayout.Label(_Controller.BuildReportPath);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                string buildMessage = string.Empty;
                MessageType buildMessageType = MessageType.None;
                GetBuildMessage(out buildMessage, out buildMessageType);
                EditorGUILayout.HelpBox(buildMessage, buildMessageType);
                GUILayout.Space(2f);
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginDisabledGroup(_Controller.Platforms == Platform.Undefined || string.IsNullOrEmpty(_Controller.CompressionHelperTypeName) || !_Controller.IsValidOutputDirectory);
                    {
                        if (GUILayout.Button("Start Build Resources"))
                        {
                            _OrderBuildResources = true;
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button("Save", GUILayout.Width(80f)))
                    {
                        SaveConfiguration();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void BrowseOutputDirectory()
        {
            string directory = EditorUtility.OpenFolderPanel("Select Output Directory", _Controller.OutputDirectory, string.Empty);
            if (!string.IsNullOrEmpty(directory))
            {
                _Controller.OutputDirectory = directory;
            }
        }

        private void GetBuildMessage(out string message, out MessageType messageType)
        {
            message = string.Empty;
            messageType = MessageType.Error;
            if (_Controller.Platforms == Platform.Undefined)
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

            if (!_Controller.IsValidOutputDirectory)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    message += Environment.NewLine;
                }

                message += "Output directory is invalid.";
            }

            if (!string.IsNullOrEmpty(message))
            {
                return;
            }

            messageType = MessageType.Info;
            if (Directory.Exists(_Controller.OutputPackagePath))
            {
                message += Utility.Text.Format("{0} will be overwritten.", _Controller.OutputPackagePath);
                messageType = MessageType.Warning;
            }

            if (Directory.Exists(_Controller.OutputFullPath))
            {
                if (message.Length > 0)
                {
                    message += " ";
                }

                message += Utility.Text.Format("{0} will be overwritten.", _Controller.OutputFullPath);
                messageType = MessageType.Warning;
            }

            if (Directory.Exists(_Controller.OutputPackedPath))
            {
                if (message.Length > 0)
                {
                    message += " ";
                }

                message += Utility.Text.Format("{0} will be overwritten.", _Controller.OutputPackedPath);
                messageType = MessageType.Warning;
            }

            if (messageType == MessageType.Warning)
            {
                return;
            }

            message = "Ready to build.";
        }

        private void BuildResources()
        {
            if (_Controller.BuildResources())
            {
                Debug.Log("Build resources success.");
                SaveConfiguration();
            }
            else
            {
                Debug.LogWarning("Build resources failure.");
            }
        }

        private void SaveConfiguration()
        {
            if (_Controller.Save())
            {
                Debug.Log("Save configuration success.");
            }
            else
            {
                Debug.LogWarning("Save configuration failure.");
            }
        }

        private void DrawPlatform(Platform platform, string platformName)
        {
            _Controller.SelectPlatform(platform, EditorGUILayout.ToggleLeft(platformName, _Controller.IsPlatformSelected(platform)));
        }

        private void OnLoadingResource(int index, int count)
        {
            EditorUtility.DisplayProgressBar("Loading Resources", Utility.Text.Format("Loading resources, {0}/{1} loaded.", index, count), (float)index / count);
        }

        private void OnLoadingAsset(int index, int count)
        {
            EditorUtility.DisplayProgressBar("Loading Assets", Utility.Text.Format("Loading assets, {0}/{1} loaded.", index, count), (float)index / count);
        }

        private void OnLoadCompleted()
        {
            EditorUtility.ClearProgressBar();
        }

        private void OnAnalyzingAsset(int index, int count)
        {
            EditorUtility.DisplayProgressBar("Analyzing Assets", Utility.Text.Format("Analyzing assets, {0}/{1} analyzed.", index, count), (float)index / count);
        }

        private void OnAnalyzeCompleted()
        {
            EditorUtility.ClearProgressBar();
        }

        private bool OnProcessingAssetBundle(string assetBundleName, float progress)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Processing AssetBundle", Utility.Text.Format("Processing '{0}'...", assetBundleName), progress))
            {
                EditorUtility.ClearProgressBar();
                return true;
            }
            else
            {
                Repaint();
                return false;
            }
        }

        private bool OnProcessingBinary(string binaryName, float progress)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Processing Binary", Utility.Text.Format("Processing '{0}'...", binaryName), progress))
            {
                EditorUtility.ClearProgressBar();
                return true;
            }
            else
            {
                Repaint();
                return false;
            }
        }

        private void OnProcessResourceComplete(Platform platform)
        {
            EditorUtility.ClearProgressBar();
            Debug.Log(Utility.Text.Format("Build resources for '{0}' complete.", platform));
        }

        private void OnBuildResourceError(string errorMessage)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogWarning(Utility.Text.Format("Build resources error with error message '{0}'.", errorMessage));
        }
    }
}
