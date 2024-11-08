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

namespace UnityGameFramework.Editor.ResourceTools
{
    /// <summary>
    /// 资源分析器。
    /// </summary>
    internal sealed class ResourceAnalyzer : EditorWindow
    {
        private ResourceAnalyzerController _Controller = null;
        private bool _Analyzed = false;
        private int _ToolbarIndex = 0;

        private int _AssetCount = 0;
        private string[] _CachedAssetNames = null;
        private int _SelectedAssetIndex = -1;
        private string _SelectedAssetName = null;
        private DependencyData _SelectedDependencyData = null;
        private AssetsOrder _AssetsOrder = AssetsOrder.AssetNameAsc;
        private string _AssetsFilter = null;
        private Vector2 _AssetsScroll = Vector2.zero;
        private Vector2 _DependencyResourcesScroll = Vector2.zero;
        private Vector2 _DependencyAssetsScroll = Vector2.zero;
        private Vector2 _ScatteredDependencyAssetsScroll = Vector2.zero;

        private int _ScatteredAssetCount = 0;
        private string[] _CachedScatteredAssetNames = null;
        private int _SelectedScatteredAssetIndex = -1;
        private string _SelectedScatteredAssetName = null;
        private Asset[] _SelectedHostAssets = null;
        private ScatteredAssetsOrder _ScatteredAssetsOrder = ScatteredAssetsOrder.AssetNameAsc;
        private string _ScatteredAssetsFilter = null;
        private Vector2 _ScatteredAssetsScroll = Vector2.zero;
        private Vector2 _HostAssetsScroll = Vector2.zero;

        private int _CircularDependencyCount = 0;
        private string[][] _CachedCircularDependencyDatas = null;
        private Vector2 _CircularDependencyScroll = Vector2.zero;

        [MenuItem("Game Framework/Resource Tools/Resource Analyzer", false, 42)]
        private static void Open()
        {
            ResourceAnalyzer window = GetWindow<ResourceAnalyzer>("Resource Analyzer", true);
            window.minSize = new Vector2(800f, 600f);
        }

        private void OnEnable()
        {
            _Controller = new ResourceAnalyzerController();
            _Controller.OnLoadingResource += OnLoadingResource;
            _Controller.OnLoadingAsset += OnLoadingAsset;
            _Controller.OnLoadCompleted += OnLoadCompleted;
            _Controller.OnAnalyzingAsset += OnAnalyzingAsset;
            _Controller.OnAnalyzeCompleted += OnAnalyzeCompleted;

            _Analyzed = false;
            _ToolbarIndex = 0;

            _AssetCount = 0;
            _CachedAssetNames = null;
            _SelectedAssetIndex = -1;
            _SelectedAssetName = null;
            _SelectedDependencyData = new DependencyData();
            _AssetsOrder = AssetsOrder.ScatteredDependencyAssetCountDesc;
            _AssetsFilter = null;
            _AssetsScroll = Vector2.zero;
            _DependencyResourcesScroll = Vector2.zero;
            _DependencyAssetsScroll = Vector2.zero;
            _ScatteredDependencyAssetsScroll = Vector2.zero;

            _ScatteredAssetCount = 0;
            _CachedScatteredAssetNames = null;
            _SelectedScatteredAssetIndex = -1;
            _SelectedScatteredAssetName = null;
            _SelectedHostAssets = new Asset[] { };
            _ScatteredAssetsOrder = ScatteredAssetsOrder.HostAssetCountDesc;
            _ScatteredAssetsFilter = null;
            _ScatteredAssetsScroll = Vector2.zero;
            _HostAssetsScroll = Vector2.zero;

            _CircularDependencyCount = 0;
            _CachedCircularDependencyDatas = null;
            _CircularDependencyScroll = Vector2.zero;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width), GUILayout.Height(position.height));
            {
                GUILayout.Space(5f);
                int toolbarIndex = GUILayout.Toolbar(_ToolbarIndex, new string[] { "Summary", "Asset Dependency Viewer", "Scattered Asset Viewer", "Circular Dependency Viewer" }, GUILayout.Height(30f));
                if (toolbarIndex != _ToolbarIndex)
                {
                    _ToolbarIndex = toolbarIndex;
                    GUI.FocusControl(null);
                }

                switch (_ToolbarIndex)
                {
                    case 0:
                        DrawSummary();
                        break;

                    case 1:
                        DrawAssetDependencyViewer();
                        break;

                    case 2:
                        DrawScatteredAssetViewer();
                        break;

                    case 3:
                        DrawCircularDependencyViewer();
                        break;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAnalyzeButton()
        {
            if (!_Analyzed)
            {
                EditorGUILayout.HelpBox("Please analyze first.", MessageType.Info);
            }

            if (GUILayout.Button("Analyze", GUILayout.Height(30f)))
            {
                _Controller.Clear();

                _SelectedAssetIndex = -1;
                _SelectedAssetName = null;
                _SelectedDependencyData = new DependencyData();

                _SelectedScatteredAssetIndex = -1;
                _SelectedScatteredAssetName = null;
                _SelectedHostAssets = new Asset[] { };

                if (_Controller.Prepare())
                {
                    _Controller.Analyze();
                    _Analyzed = true;
                    _AssetCount = _Controller.GetAssetNames().Length;
                    _ScatteredAssetCount = _Controller.GetScatteredAssetNames().Length;
                    _CachedCircularDependencyDatas = _Controller.GetCircularDependencyDatas();
                    _CircularDependencyCount = _CachedCircularDependencyDatas.Length;
                    OnAssetsOrderOrFilterChanged();
                    OnScatteredAssetsOrderOrFilterChanged();
                }
                else
                {
                    EditorUtility.DisplayDialog("Resource Analyze", "Can not parse 'ResourceCollection.xml', please use 'Resource Editor' tool first.", "OK");
                }
            }
        }

        private void DrawSummary()
        {
            DrawAnalyzeButton();
        }

        private void DrawAssetDependencyViewer()
        {
            if (!_Analyzed)
            {
                DrawAnalyzeButton();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(5f);
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
                {
                    GUILayout.Space(5f);
                    string title = null;
                    if (string.IsNullOrEmpty(_AssetsFilter))
                    {
                        title = Utility.Text.Format("Assets In Resources ({0})", _AssetCount);
                    }
                    else
                    {
                        title = Utility.Text.Format("Assets In Resources ({0}/{1})", _CachedAssetNames.Length, _AssetCount);
                    }
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height - 150f));
                    {
                        _AssetsScroll = EditorGUILayout.BeginScrollView(_AssetsScroll);
                        {
                            int selectedIndex = GUILayout.SelectionGrid(_SelectedAssetIndex, _CachedAssetNames, 1, "toggle");
                            if (selectedIndex != _SelectedAssetIndex)
                            {
                                _SelectedAssetIndex = selectedIndex;
                                _SelectedAssetName = _CachedAssetNames[selectedIndex];
                                _SelectedDependencyData = _Controller.GetDependencyData(_SelectedAssetName);
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.LabelField("Asset Name", _SelectedAssetName ?? "<None>");
                        EditorGUILayout.LabelField("Resource Name", _SelectedAssetName == null ? "<None>" : _Controller.GetAsset(_SelectedAssetName).Resource.FullName);
                        EditorGUILayout.BeginHorizontal();
                        {
                            AssetsOrder assetsOrder = (AssetsOrder)EditorGUILayout.EnumPopup("Order by", _AssetsOrder);
                            if (assetsOrder != _AssetsOrder)
                            {
                                _AssetsOrder = assetsOrder;
                                OnAssetsOrderOrFilterChanged();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            string assetsFilter = EditorGUILayout.TextField("Assets Filter", _AssetsFilter);
                            if (assetsFilter != _AssetsFilter)
                            {
                                _AssetsFilter = assetsFilter;
                                OnAssetsOrderOrFilterChanged();
                            }
                            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_AssetsFilter));
                            {
                                if (GUILayout.Button("x", GUILayout.Width(20f)))
                                {
                                    _AssetsFilter = null;
                                    GUI.FocusControl(null);
                                    OnAssetsOrderOrFilterChanged();
                                }
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f - 14f));
                {
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField(Utility.Text.Format("Dependency Resources ({0})", _SelectedDependencyData.DependencyResourceCount), EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height * 0.2f));
                    {
                        _DependencyResourcesScroll = EditorGUILayout.BeginScrollView(_DependencyResourcesScroll);
                        {
                            Resource[] dependencyResources = _SelectedDependencyData.GetDependencyResources();
                            foreach (Resource dependencyResource in dependencyResources)
                            {
                                GUILayout.Label(dependencyResource.FullName);
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.LabelField(Utility.Text.Format("Dependency Assets ({0})", _SelectedDependencyData.DependencyAssetCount), EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height * 0.3f));
                    {
                        _DependencyAssetsScroll = EditorGUILayout.BeginScrollView(_DependencyAssetsScroll);
                        {
                            Asset[] dependencyAssets = _SelectedDependencyData.GetDependencyAssets();
                            foreach (Asset dependencyAsset in dependencyAssets)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    if (GUILayout.Button("GO", GUILayout.Width(30f)))
                                    {
                                        _SelectedAssetName = dependencyAsset.Name;
                                        _SelectedAssetIndex = new List<string>(_CachedAssetNames).IndexOf(_SelectedAssetName);
                                        _SelectedDependencyData = _Controller.GetDependencyData(_SelectedAssetName);
                                    }

                                    GUILayout.Label(dependencyAsset.Name);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.LabelField(Utility.Text.Format("Scattered Dependency Assets ({0})", _SelectedDependencyData.ScatteredDependencyAssetCount), EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height * 0.5f - 116f));
                    {
                        _ScatteredDependencyAssetsScroll = EditorGUILayout.BeginScrollView(_ScatteredDependencyAssetsScroll);
                        {
                            string[] scatteredDependencyAssetNames = _SelectedDependencyData.GetScatteredDependencyAssetNames();
                            foreach (string scatteredDependencyAssetName in scatteredDependencyAssetNames)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    int count = _Controller.GetHostAssets(scatteredDependencyAssetName).Length;
                                    EditorGUI.BeginDisabledGroup(count < 2);
                                    {
                                        if (GUILayout.Button("GO", GUILayout.Width(30f)))
                                        {
                                            _SelectedScatteredAssetName = scatteredDependencyAssetName;
                                            _SelectedScatteredAssetIndex = new List<string>(_CachedScatteredAssetNames).IndexOf(_SelectedScatteredAssetName);
                                            _SelectedHostAssets = _Controller.GetHostAssets(_SelectedScatteredAssetName);
                                            _ToolbarIndex = 2;
                                            GUI.FocusControl(null);
                                        }
                                    }
                                    EditorGUI.EndDisabledGroup();
                                    GUILayout.Label(count > 1 ? Utility.Text.Format("{0} ({1})", scatteredDependencyAssetName, count) : scatteredDependencyAssetName);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawScatteredAssetViewer()
        {
            if (!_Analyzed)
            {
                DrawAnalyzeButton();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(5f);
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
                {
                    GUILayout.Space(5f);
                    string title = null;
                    if (string.IsNullOrEmpty(_ScatteredAssetsFilter))
                    {
                        title = Utility.Text.Format("Scattered Assets ({0})", _ScatteredAssetCount);
                    }
                    else
                    {
                        title = Utility.Text.Format("Scattered Assets ({0}/{1})", _CachedScatteredAssetNames.Length, _ScatteredAssetCount);
                    }
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height - 132f));
                    {
                        _ScatteredAssetsScroll = EditorGUILayout.BeginScrollView(_ScatteredAssetsScroll);
                        {
                            int selectedIndex = GUILayout.SelectionGrid(_SelectedScatteredAssetIndex, _CachedScatteredAssetNames, 1, "toggle");
                            if (selectedIndex != _SelectedScatteredAssetIndex)
                            {
                                _SelectedScatteredAssetIndex = selectedIndex;
                                _SelectedScatteredAssetName = _CachedScatteredAssetNames[selectedIndex];
                                _SelectedHostAssets = _Controller.GetHostAssets(_SelectedScatteredAssetName);
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.LabelField("Scattered Asset Name", _SelectedScatteredAssetName ?? "<None>");
                        EditorGUILayout.BeginHorizontal();
                        {
                            ScatteredAssetsOrder scatteredAssetsOrder = (ScatteredAssetsOrder)EditorGUILayout.EnumPopup("Order by", _ScatteredAssetsOrder);
                            if (scatteredAssetsOrder != _ScatteredAssetsOrder)
                            {
                                _ScatteredAssetsOrder = scatteredAssetsOrder;
                                OnScatteredAssetsOrderOrFilterChanged();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            string scatteredAssetsFilter = EditorGUILayout.TextField("Assets Filter", _ScatteredAssetsFilter);
                            if (scatteredAssetsFilter != _ScatteredAssetsFilter)
                            {
                                _ScatteredAssetsFilter = scatteredAssetsFilter;
                                OnScatteredAssetsOrderOrFilterChanged();
                            }
                            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_ScatteredAssetsFilter));
                            {
                                if (GUILayout.Button("x", GUILayout.Width(20f)))
                                {
                                    _ScatteredAssetsFilter = null;
                                    GUI.FocusControl(null);
                                    OnScatteredAssetsOrderOrFilterChanged();
                                }
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f - 14f));
                {
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField(Utility.Text.Format("Host Assets ({0})", _SelectedHostAssets.Length), EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box", GUILayout.Height(position.height - 68f));
                    {
                        _HostAssetsScroll = EditorGUILayout.BeginScrollView(_HostAssetsScroll);
                        {
                            foreach (Asset hostAsset in _SelectedHostAssets)
                            {
                                EditorGUILayout.BeginHorizontal();
                                {
                                    if (GUILayout.Button("GO", GUILayout.Width(30f)))
                                    {
                                        _SelectedAssetName = hostAsset.Name;
                                        _SelectedAssetIndex = new List<string>(_CachedAssetNames).IndexOf(_SelectedAssetName);
                                        _SelectedDependencyData = _Controller.GetDependencyData(_SelectedAssetName);
                                        _ToolbarIndex = 1;
                                        GUI.FocusControl(null);
                                    }

                                    GUILayout.Label(Utility.Text.Format("{0} [{1}]", hostAsset.Name, hostAsset.Resource.FullName));
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCircularDependencyViewer()
        {
            if (!_Analyzed)
            {
                DrawAnalyzeButton();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(5f);
                EditorGUILayout.BeginVertical();
                {
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField(Utility.Text.Format("Circular Dependency ({0})", _CircularDependencyCount), EditorStyles.boldLabel);
                    _CircularDependencyScroll = EditorGUILayout.BeginScrollView(_CircularDependencyScroll);
                    {
                        int count = 0;
                        foreach (string[] circularDependencyData in _CachedCircularDependencyDatas)
                        {
                            GUILayout.Label(Utility.Text.Format("{0}) {1}", ++count, circularDependencyData[circularDependencyData.Length - 1]), EditorStyles.boldLabel);
                            EditorGUILayout.BeginVertical("box");
                            {
                                foreach (string circularDependency in circularDependencyData)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    {
                                        GUILayout.Label(circularDependency);
                                        if (GUILayout.Button("GO", GUILayout.Width(30f)))
                                        {
                                            _SelectedAssetName = circularDependency;
                                            _SelectedAssetIndex = new List<string>(_CachedAssetNames).IndexOf(_SelectedAssetName);
                                            _SelectedDependencyData = _Controller.GetDependencyData(_SelectedAssetName);
                                            _ToolbarIndex = 1;
                                            GUI.FocusControl(null);
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                            }
                            EditorGUILayout.EndVertical();
                            GUILayout.Space(5f);
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnAssetsOrderOrFilterChanged()
        {
            _CachedAssetNames = _Controller.GetAssetNames(_AssetsOrder, _AssetsFilter);
            if (!string.IsNullOrEmpty(_SelectedAssetName))
            {
                _SelectedAssetIndex = new List<string>(_CachedAssetNames).IndexOf(_SelectedAssetName);
            }
        }

        private void OnScatteredAssetsOrderOrFilterChanged()
        {
            _CachedScatteredAssetNames = _Controller.GetScatteredAssetNames(_ScatteredAssetsOrder, _ScatteredAssetsFilter);
            if (!string.IsNullOrEmpty(_SelectedScatteredAssetName))
            {
                _SelectedScatteredAssetIndex = new List<string>(_CachedScatteredAssetNames).IndexOf(_SelectedScatteredAssetName);
            }
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
    }
}
