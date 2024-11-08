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
    /// 资源编辑器。
    /// </summary>
    internal sealed partial class ResourceEditor : EditorWindow
    {
        private ResourceEditorController _Controller = null;
        private MenuState _MenuState = MenuState.Normal;
        private Resource _SelectedResource = null;
        private ResourceFolder _ResourceRoot = null;
        private HashSet<string> _ExpandedResourceFolderNames = null;
        private HashSet<Asset> _SelectedAssetsInSelectedResource = null;
        private HashSet<SourceFolder> _ExpandedSourceFolders = null;
        private HashSet<SourceAsset> _SelectedSourceAssets = null;
        private Texture _MissingSourceAssetIcon = null;

        private HashSet<SourceFolder> _CachedSelectedSourceFolders = null;
        private HashSet<SourceFolder> _CachedUnselectedSourceFolders = null;
        private HashSet<SourceFolder> _CachedAssignedSourceFolders = null;
        private HashSet<SourceFolder> _CachedUnassignedSourceFolders = null;
        private HashSet<SourceAsset> _CachedAssignedSourceAssets = null;
        private HashSet<SourceAsset> _CachedUnassignedSourceAssets = null;

        private Vector2 _ResourcesViewScroll = Vector2.zero;
        private Vector2 _ResourceViewScroll = Vector2.zero;
        private Vector2 _SourceAssetsViewScroll = Vector2.zero;
        private string _InputResourceName = null;
        private string _InputResourceVariant = null;
        private bool _HideAssignedSourceAssets = false;
        private int _CurrentResourceContentCount = 0;
        private int _CurrentResourceRowOnDraw = 0;
        private int _CurrentSourceRowOnDraw = 0;

        [MenuItem("Game Framework/Resource Tools/Resource Editor", false, 41)]
        private static void Open()
        {
            ResourceEditor window = GetWindow<ResourceEditor>("Resource Editor", true);
            window.minSize = new Vector2(1400f, 600f);
        }

        private void OnEnable()
        {
            _Controller = new ResourceEditorController();
            _Controller.OnLoadingResource += OnLoadingResource;
            _Controller.OnLoadingAsset += OnLoadingAsset;
            _Controller.OnLoadCompleted += OnLoadCompleted;
            _Controller.OnAssetAssigned += OnAssetAssigned;
            _Controller.OnAssetUnassigned += OnAssetUnassigned;

            _MenuState = MenuState.Normal;
            _SelectedResource = null;
            _ResourceRoot = new ResourceFolder("Resources", null);
            _ExpandedResourceFolderNames = new HashSet<string>();
            _SelectedAssetsInSelectedResource = new HashSet<Asset>();
            _ExpandedSourceFolders = new HashSet<SourceFolder>();
            _SelectedSourceAssets = new HashSet<SourceAsset>();
            _MissingSourceAssetIcon = EditorGUIUtility.IconContent("console.warnicon.sml").image;

            _CachedSelectedSourceFolders = new HashSet<SourceFolder>();
            _CachedUnselectedSourceFolders = new HashSet<SourceFolder>();
            _CachedAssignedSourceFolders = new HashSet<SourceFolder>();
            _CachedUnassignedSourceFolders = new HashSet<SourceFolder>();
            _CachedAssignedSourceAssets = new HashSet<SourceAsset>();
            _CachedUnassignedSourceAssets = new HashSet<SourceAsset>();

            _ResourcesViewScroll = Vector2.zero;
            _ResourceViewScroll = Vector2.zero;
            _SourceAssetsViewScroll = Vector2.zero;
            _InputResourceName = null;
            _InputResourceVariant = null;
            _HideAssignedSourceAssets = false;
            _CurrentResourceContentCount = 0;
            _CurrentResourceRowOnDraw = 0;
            _CurrentSourceRowOnDraw = 0;

            if (_Controller.Load())
            {
                Debug.Log("Load configuration success.");
            }
            else
            {
                Debug.LogWarning("Load configuration failure.");
            }

            EditorUtility.DisplayProgressBar("Prepare Resource Editor", "Processing...", 0f);
            RefreshResourceTree();
            EditorUtility.ClearProgressBar();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(position.width), GUILayout.Height(position.height));
            {
                GUILayout.Space(2f);
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.25f));
                {
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField(Utility.Text.Format("Resource List ({0})", _Controller.ResourceCount), EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal("box", GUILayout.Height(position.height - 52f));
                    {
                        DrawResourcesView();
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(5f);
                        DrawResourcesMenu();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.25f));
                {
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField(Utility.Text.Format("Resource Content ({0})", _CurrentResourceContentCount), EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal("box", GUILayout.Height(position.height - 52f));
                    {
                        DrawResourceView();
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(5f);
                        DrawResourceMenu();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 16f));
                {
                    GUILayout.Space(5f);
                    EditorGUILayout.LabelField("Asset List", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal("box", GUILayout.Height(position.height - 52f));
                    {
                        DrawSourceAssetsView();
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(5f);
                        DrawSourceAssetsMenu();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5f);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawResourcesView()
        {
            _CurrentResourceRowOnDraw = 0;
            _ResourcesViewScroll = EditorGUILayout.BeginScrollView(_ResourcesViewScroll);
            {
                DrawResourceFolder(_ResourceRoot);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawResourceFolder(ResourceFolder folder)
        {
            bool expand = IsExpandedResourceFolder(folder);
            EditorGUILayout.BeginHorizontal();
            {
#if UNITY_2019_3_OR_NEWER
                bool foldout = EditorGUI.Foldout(new Rect(18f + 14f * folder.Depth, 20f * _CurrentResourceRowOnDraw + 4f, int.MaxValue, 14f), expand, string.Empty, true);
#else
                bool foldout = EditorGUI.Foldout(new Rect(18f + 14f * folder.Depth, 20f * _CurrentResourceRowOnDraw + 2f, int.MaxValue, 14f), expand, string.Empty, true);
#endif
                if (expand != foldout)
                {
                    expand = !expand;
                    SetExpandedResourceFolder(folder, expand);
                }

#if UNITY_2019_3_OR_NEWER
                GUI.DrawTexture(new Rect(32f + 14f * folder.Depth, 20f * _CurrentResourceRowOnDraw + 3f, 16f, 16f), ResourceFolder.Icon);
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(44f + 14f * folder.Depth), GUILayout.Height(18f));
#else
                GUI.DrawTexture(new Rect(32f + 14f * folder.Depth, 20f * _CurrentResourceRowOnDraw + 1f, 16f, 16f), ResourceFolder.Icon);
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(40f + 14f * folder.Depth), GUILayout.Height(18f));
#endif
                EditorGUILayout.LabelField(folder.Name);
            }
            EditorGUILayout.EndHorizontal();

            _CurrentResourceRowOnDraw++;

            if (expand)
            {
                foreach (ResourceFolder subFolder in folder.GetFolders())
                {
                    DrawResourceFolder(subFolder);
                }

                foreach (ResourceItem resourceItem in folder.GetItems())
                {
                    DrawResourceItem(resourceItem);
                }
            }
        }

        private void DrawResourceItem(ResourceItem resourceItem)
        {
            EditorGUILayout.BeginHorizontal();
            {
                string title = resourceItem.Name;
                if (resourceItem.Resource.Packed)
                {
                    title = "[Packed] " + title;
                }

                float emptySpace = position.width;
                if (EditorGUILayout.Toggle(_SelectedResource == resourceItem.Resource, GUILayout.Width(emptySpace - 12f)))
                {
                    ChangeSelectedResource(resourceItem.Resource);
                }
                else if (_SelectedResource == resourceItem.Resource)
                {
                    ChangeSelectedResource(null);
                }

                GUILayout.Space(-emptySpace + 24f);
#if UNITY_2019_3_OR_NEWER
                GUI.DrawTexture(new Rect(32f + 14f * resourceItem.Depth, 20f * _CurrentResourceRowOnDraw + 3f, 16f, 16f), resourceItem.Icon);
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(30f + 14f * resourceItem.Depth), GUILayout.Height(18f));
#else
                GUI.DrawTexture(new Rect(32f + 14f * resourceItem.Depth, 20f * _CurrentResourceRowOnDraw + 1f, 16f, 16f), resourceItem.Icon);
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(26f + 14f * resourceItem.Depth), GUILayout.Height(18f));
#endif
                EditorGUILayout.LabelField(title);
            }
            EditorGUILayout.EndHorizontal();
            _CurrentResourceRowOnDraw++;
        }

        private void DrawResourcesMenu()
        {
            switch (_MenuState)
            {
                case MenuState.Normal:
                    DrawResourcesMenu_Normal();
                    break;

                case MenuState.Add:
                    DrawResourcesMenu_Add();
                    break;

                case MenuState.Rename:
                    DrawResourcesMenu_Rename();
                    break;

                case MenuState.Remove:
                    DrawResourcesMenu_Remove();
                    break;
            }
        }

        private void DrawResourcesMenu_Normal()
        {
            if (GUILayout.Button("Add", GUILayout.Width(65f)))
            {
                _MenuState = MenuState.Add;
                _InputResourceName = null;
                _InputResourceVariant = null;
                GUI.FocusControl(null);
            }
            EditorGUI.BeginDisabledGroup(_SelectedResource == null);
            {
                if (GUILayout.Button("Rename", GUILayout.Width(65f)))
                {
                    _MenuState = MenuState.Rename;
                    _InputResourceName = _SelectedResource != null ? _SelectedResource.Name : null;
                    _InputResourceVariant = _SelectedResource != null ? _SelectedResource.Variant : null;
                    GUI.FocusControl(null);
                }
                if (GUILayout.Button("Remove", GUILayout.Width(65f)))
                {
                    _MenuState = MenuState.Remove;
                }
                if (_SelectedResource == null)
                {
                    EditorGUILayout.EnumPopup(LoadType.LoadFromFile);
                }
                else
                {
                    LoadType loadType = (LoadType)EditorGUILayout.EnumPopup(_SelectedResource.LoadType);
                    if (loadType != _SelectedResource.LoadType)
                    {
                        SetResourceLoadType(loadType);
                    }
                }
                bool packed = EditorGUILayout.ToggleLeft("Packed", _SelectedResource != null && _SelectedResource.Packed, GUILayout.Width(65f));
                if (_SelectedResource != null && packed != _SelectedResource.Packed)
                {
                    SetResourcePacked(packed);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawResourcesMenu_Add()
        {
            GUI.SetNextControlName("NewResourceNameTextField");
            _InputResourceName = EditorGUILayout.TextField(_InputResourceName);
            GUI.SetNextControlName("NewResourceVariantTextField");
            _InputResourceVariant = EditorGUILayout.TextField(_InputResourceVariant, GUILayout.Width(60f));

            if (GUI.GetNameOfFocusedControl() == "NewResourceNameTextField" || GUI.GetNameOfFocusedControl() == "NewResourceVariantTextField")
            {
                if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
                {
                    EditorUtility.DisplayProgressBar("Add Resource", "Processing...", 0f);
                    AddResource(_InputResourceName, _InputResourceVariant, true);
                    EditorUtility.ClearProgressBar();
                    Repaint();
                }
            }

            if (GUILayout.Button("Add", GUILayout.Width(50f)))
            {
                EditorUtility.DisplayProgressBar("Add Resource", "Processing...", 0f);
                AddResource(_InputResourceName, _InputResourceVariant, true);
                EditorUtility.ClearProgressBar();
            }

            if (GUILayout.Button("Back", GUILayout.Width(50f)))
            {
                _MenuState = MenuState.Normal;
            }
        }

        private void DrawResourcesMenu_Rename()
        {
            if (_SelectedResource == null)
            {
                _MenuState = MenuState.Normal;
                return;
            }

            GUI.SetNextControlName("RenameResourceNameTextField");
            _InputResourceName = EditorGUILayout.TextField(_InputResourceName);
            GUI.SetNextControlName("RenameResourceVariantTextField");
            _InputResourceVariant = EditorGUILayout.TextField(_InputResourceVariant, GUILayout.Width(60f));

            if (GUI.GetNameOfFocusedControl() == "RenameResourceNameTextField" || GUI.GetNameOfFocusedControl() == "RenameResourceVariantTextField")
            {
                if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
                {
                    EditorUtility.DisplayProgressBar("Rename Resource", "Processing...", 0f);
                    RenameResource(_SelectedResource, _InputResourceName, _InputResourceVariant);
                    EditorUtility.ClearProgressBar();
                    Repaint();
                }
            }

            if (GUILayout.Button("OK", GUILayout.Width(50f)))
            {
                EditorUtility.DisplayProgressBar("Rename Resource", "Processing...", 0f);
                RenameResource(_SelectedResource, _InputResourceName, _InputResourceVariant);
                EditorUtility.ClearProgressBar();
            }

            if (GUILayout.Button("Back", GUILayout.Width(50f)))
            {
                _MenuState = MenuState.Normal;
            }
        }

        private void DrawResourcesMenu_Remove()
        {
            if (_SelectedResource == null)
            {
                _MenuState = MenuState.Normal;
                return;
            }

            GUILayout.Label(Utility.Text.Format("Remove '{0}' ?", _SelectedResource.FullName));

            if (GUILayout.Button("Yes", GUILayout.Width(50f)))
            {
                EditorUtility.DisplayProgressBar("Remove Resource", "Processing...", 0f);
                RemoveResource();
                EditorUtility.ClearProgressBar();
                _MenuState = MenuState.Normal;
            }

            if (GUILayout.Button("No", GUILayout.Width(50f)))
            {
                _MenuState = MenuState.Normal;
            }
        }

        private void DrawResourceView()
        {
            _ResourceViewScroll = EditorGUILayout.BeginScrollView(_ResourceViewScroll);
            {
                if (_SelectedResource != null)
                {
                    int index = 0;
                    Asset[] assets = _Controller.GetAssets(_SelectedResource.Name, _SelectedResource.Variant);
                    _CurrentResourceContentCount = assets.Length;
                    foreach (Asset asset in assets)
                    {
                        SourceAsset sourceAsset = _Controller.GetSourceAsset(asset.Guid);
                        string assetName = sourceAsset != null ? (_Controller.AssetSorter == AssetSorterType.Path ? sourceAsset.Path : (_Controller.AssetSorter == AssetSorterType.Name ? sourceAsset.Name : sourceAsset.Guid)) : asset.Guid;
                        EditorGUILayout.BeginHorizontal();
                        {
                            float emptySpace = position.width;
                            bool select = IsSelectedAssetInSelectedResource(asset);
                            if (select != EditorGUILayout.Toggle(select, GUILayout.Width(emptySpace - 12f)))
                            {
                                select = !select;
                                SetSelectedAssetInSelectedResource(asset, select);
                            }

                            GUILayout.Space(-emptySpace + 24f);
#if UNITY_2019_3_OR_NEWER
                            GUI.DrawTexture(new Rect(20f, 20f * index++ + 3f, 16f, 16f), sourceAsset != null ? sourceAsset.Icon : _MissingSourceAssetIcon);
                            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(16f), GUILayout.Height(18f));
#else
                            GUI.DrawTexture(new Rect(20f, 20f * index++ + 1f, 16f, 16f), sourceAsset != null ? sourceAsset.Icon : _MissingSourceAssetIcon);
                            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(14f), GUILayout.Height(18f));
#endif
                            EditorGUILayout.LabelField(assetName);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    _CurrentResourceContentCount = 0;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawResourceMenu()
        {
            if (GUILayout.Button("All", GUILayout.Width(50f)) && _SelectedResource != null)
            {
                Asset[] assets = _Controller.GetAssets(_SelectedResource.Name, _SelectedResource.Variant);
                foreach (Asset asset in assets)
                {
                    SetSelectedAssetInSelectedResource(asset, true);
                }
            }
            if (GUILayout.Button("None", GUILayout.Width(50f)))
            {
                _SelectedAssetsInSelectedResource.Clear();
            }
            _Controller.AssetSorter = (AssetSorterType)EditorGUILayout.EnumPopup(_Controller.AssetSorter, GUILayout.Width(60f));
            GUILayout.Label(string.Empty);
            EditorGUI.BeginDisabledGroup(_SelectedResource == null || _SelectedAssetsInSelectedResource.Count <= 0);
            {
                if (GUILayout.Button(Utility.Text.Format("{0} >>", _SelectedAssetsInSelectedResource.Count), GUILayout.Width(80f)))
                {
                    foreach (Asset asset in _SelectedAssetsInSelectedResource)
                    {
                        UnassignAsset(asset);
                    }

                    _SelectedAssetsInSelectedResource.Clear();
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawSourceAssetsView()
        {
            _CurrentSourceRowOnDraw = 0;
            _SourceAssetsViewScroll = EditorGUILayout.BeginScrollView(_SourceAssetsViewScroll);
            {
                DrawSourceFolder(_Controller.SourceAssetRoot);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawSourceAssetsMenu()
        {
            HashSet<SourceAsset> selectedSourceAssets = GetSelectedSourceAssets();
            EditorGUI.BeginDisabledGroup(_SelectedResource == null || selectedSourceAssets.Count <= 0);
            {
                if (GUILayout.Button(Utility.Text.Format("<< {0}", selectedSourceAssets.Count), GUILayout.Width(80f)))
                {
                    foreach (SourceAsset sourceAsset in selectedSourceAssets)
                    {
                        AssignAsset(sourceAsset, _SelectedResource);
                    }

                    _SelectedSourceAssets.Clear();
                    _CachedSelectedSourceFolders.Clear();
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(selectedSourceAssets.Count <= 0);
            {
                if (GUILayout.Button(Utility.Text.Format("<<< {0}", selectedSourceAssets.Count), GUILayout.Width(80f)))
                {
                    int index = 0;
                    int count = selectedSourceAssets.Count;
                    foreach (SourceAsset sourceAsset in selectedSourceAssets)
                    {
                        EditorUtility.DisplayProgressBar("Add Resources", Utility.Text.Format("{0}/{1} processing...", ++index, count), (float)index / count);
                        int dotIndex = sourceAsset.FromRootPath.IndexOf('.');
                        string name = dotIndex > 0 ? sourceAsset.FromRootPath.Substring(0, dotIndex) : sourceAsset.FromRootPath;
                        AddResource(name, null, false);
                        Resource resource = _Controller.GetResource(name, null);
                        if (resource == null)
                        {
                            continue;
                        }

                        AssignAsset(sourceAsset, resource);
                    }

                    EditorUtility.DisplayProgressBar("Add Resources", "Complete processing...", 1f);
                    RefreshResourceTree();
                    EditorUtility.ClearProgressBar();
                    _SelectedSourceAssets.Clear();
                    _CachedSelectedSourceFolders.Clear();
                }
            }
            EditorGUI.EndDisabledGroup();
            bool hideAssignedSourceAssets = EditorGUILayout.ToggleLeft("Hide Assigned", _HideAssignedSourceAssets, GUILayout.Width(100f));
            if (hideAssignedSourceAssets != _HideAssignedSourceAssets)
            {
                _HideAssignedSourceAssets = hideAssignedSourceAssets;
                _CachedSelectedSourceFolders.Clear();
                _CachedUnselectedSourceFolders.Clear();
                _CachedAssignedSourceFolders.Clear();
                _CachedUnassignedSourceFolders.Clear();
            }

            GUILayout.Label(string.Empty);
            if (GUILayout.Button("Clean", GUILayout.Width(80f)))
            {
                EditorUtility.DisplayProgressBar("Clean", "Processing...", 0f);
                CleanResource();
                EditorUtility.ClearProgressBar();
            }
            if (GUILayout.Button("Save", GUILayout.Width(80f)))
            {
                EditorUtility.DisplayProgressBar("Save", "Processing...", 0f);
                SaveConfiguration();
                EditorUtility.ClearProgressBar();
            }
        }

        private void DrawSourceFolder(SourceFolder sourceFolder)
        {
            if (_HideAssignedSourceAssets && IsAssignedSourceFolder(sourceFolder))
            {
                return;
            }

            bool expand = IsExpandedSourceFolder(sourceFolder);
            EditorGUILayout.BeginHorizontal();
            {
                bool select = IsSelectedSourceFolder(sourceFolder);
                if (select != EditorGUILayout.Toggle(select, GUILayout.Width(12f + 14f * sourceFolder.Depth)))
                {
                    select = !select;
                    SetSelectedSourceFolder(sourceFolder, select);
                }

                GUILayout.Space(-14f * sourceFolder.Depth);
#if UNITY_2019_3_OR_NEWER
                bool foldout = EditorGUI.Foldout(new Rect(18f + 14f * sourceFolder.Depth, 20f * _CurrentSourceRowOnDraw + 4f, int.MaxValue, 14f), expand, string.Empty, true);
#else
                bool foldout = EditorGUI.Foldout(new Rect(18f + 14f * sourceFolder.Depth, 20f * _CurrentSourceRowOnDraw + 2f, int.MaxValue, 14f), expand, string.Empty, true);
#endif
                if (expand != foldout)
                {
                    expand = !expand;
                    SetExpandedSourceFolder(sourceFolder, expand);
                }

#if UNITY_2019_3_OR_NEWER
                GUI.DrawTexture(new Rect(32f + 14f * sourceFolder.Depth, 20f * _CurrentSourceRowOnDraw + 3f, 16f, 16f), SourceFolder.Icon);
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(30f + 14f * sourceFolder.Depth), GUILayout.Height(18f));
#else
                GUI.DrawTexture(new Rect(32f + 14f * sourceFolder.Depth, 20f * _CurrentSourceRowOnDraw + 1f, 16f, 16f), SourceFolder.Icon);
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(26f + 14f * sourceFolder.Depth), GUILayout.Height(18f));
#endif
                EditorGUILayout.LabelField(sourceFolder.Name);
            }
            EditorGUILayout.EndHorizontal();

            _CurrentSourceRowOnDraw++;

            if (expand)
            {
                foreach (SourceFolder subSourceFolder in sourceFolder.GetFolders())
                {
                    DrawSourceFolder(subSourceFolder);
                }

                foreach (SourceAsset sourceAsset in sourceFolder.GetAssets())
                {
                    DrawSourceAsset(sourceAsset);
                }
            }
        }

        private void DrawSourceAsset(SourceAsset sourceAsset)
        {
            if (_HideAssignedSourceAssets && IsAssignedSourceAsset(sourceAsset))
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                float emptySpace = position.width;
                bool select = IsSelectedSourceAsset(sourceAsset);
                if (select != EditorGUILayout.Toggle(select, GUILayout.Width(emptySpace - 12f)))
                {
                    select = !select;
                    SetSelectedSourceAsset(sourceAsset, select);
                }

                GUILayout.Space(-emptySpace + 24f);
#if UNITY_2019_3_OR_NEWER
                GUI.DrawTexture(new Rect(32f + 14f * sourceAsset.Depth, 20f * _CurrentSourceRowOnDraw + 3f, 16f, 16f), sourceAsset.Icon);
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(30f + 14f * sourceAsset.Depth), GUILayout.Height(18f));
#else
                GUI.DrawTexture(new Rect(32f + 14f * sourceAsset.Depth, 20f * _CurrentSourceRowOnDraw + 1f, 16f, 16f), sourceAsset.Icon);
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(26f + 14f * sourceAsset.Depth), GUILayout.Height(18f));
#endif
                EditorGUILayout.LabelField(sourceAsset.Name);
                Asset asset = _Controller.GetAsset(sourceAsset.Guid);
                EditorGUILayout.LabelField(asset != null ? GetResourceFullName(asset.Resource.Name, asset.Resource.Variant) : string.Empty, GUILayout.Width(position.width * 0.15f));
            }
            EditorGUILayout.EndHorizontal();
            _CurrentSourceRowOnDraw++;
        }

        private void ChangeSelectedResource(Resource resource)
        {
            if (_SelectedResource == resource)
            {
                return;
            }

            _SelectedResource = resource;
            _SelectedAssetsInSelectedResource.Clear();
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

        private void AddResource(string name, string variant, bool refresh)
        {
            if (variant == string.Empty)
            {
                variant = null;
            }

            string fullName = GetResourceFullName(name, variant);
            if (_Controller.AddResource(name, variant, null, LoadType.LoadFromFile, false))
            {
                if (refresh)
                {
                    RefreshResourceTree();
                }

                Debug.Log(Utility.Text.Format("Add resource '{0}' success.", fullName));
                _MenuState = MenuState.Normal;
            }
            else
            {
                Debug.LogWarning(Utility.Text.Format("Add resource '{0}' failure.", fullName));
            }
        }

        private void RenameResource(Resource resource, string newName, string newVariant)
        {
            if (resource == null)
            {
                Debug.LogWarning("Resource is invalid.");
                return;
            }

            if (newVariant == string.Empty)
            {
                newVariant = null;
            }

            string oldFullName = resource.FullName;
            string newFullName = GetResourceFullName(newName, newVariant);
            if (_Controller.RenameResource(resource.Name, resource.Variant, newName, newVariant))
            {
                RefreshResourceTree();
                Debug.Log(Utility.Text.Format("Rename resource '{0}' to '{1}' success.", oldFullName, newFullName));
                _MenuState = MenuState.Normal;
            }
            else
            {
                Debug.LogWarning(Utility.Text.Format("Rename resource '{0}' to '{1}' failure.", oldFullName, newFullName));
            }
        }

        private void RemoveResource()
        {
            string fullName = _SelectedResource.FullName;
            if (_Controller.RemoveResource(_SelectedResource.Name, _SelectedResource.Variant))
            {
                ChangeSelectedResource(null);
                RefreshResourceTree();
                Debug.Log(Utility.Text.Format("Remove resource '{0}' success.", fullName));
            }
            else
            {
                Debug.LogWarning(Utility.Text.Format("Remove resource '{0}' failure.", fullName));
            }
        }

        private void SetResourceLoadType(LoadType loadType)
        {
            string fullName = _SelectedResource.FullName;
            if (_Controller.SetResourceLoadType(_SelectedResource.Name, _SelectedResource.Variant, loadType))
            {
                Debug.Log(Utility.Text.Format("Set resource '{0}' load type to '{1}' success.", fullName, loadType));
            }
            else
            {
                Debug.LogWarning(Utility.Text.Format("Set resource '{0}' load type to '{1}' failure.", fullName, loadType));
            }
        }

        private void SetResourcePacked(bool packed)
        {
            string fullName = _SelectedResource.FullName;
            if (_Controller.SetResourcePacked(_SelectedResource.Name, _SelectedResource.Variant, packed))
            {
                Debug.Log(Utility.Text.Format("{1} resource '{0}' success.", fullName, packed ? "Pack" : "Unpack"));
            }
            else
            {
                Debug.LogWarning(Utility.Text.Format("{1} resource '{0}' failure.", fullName, packed ? "Pack" : "Unpack"));
            }
        }

        private void AssignAsset(SourceAsset sourceAsset, Resource resource)
        {
            if (!_Controller.AssignAsset(sourceAsset.Guid, resource.Name, resource.Variant))
            {
                Debug.LogWarning(Utility.Text.Format("Assign asset '{0}' to resource '{1}' failure.", sourceAsset.Name, resource.FullName));
            }
        }

        private void UnassignAsset(Asset asset)
        {
            if (!_Controller.UnassignAsset(asset.Guid))
            {
                Debug.LogWarning(Utility.Text.Format("Unassign asset '{0}' from resource '{1}' failure.", asset.Guid, _SelectedResource.FullName));
            }
        }

        private void CleanResource()
        {
            int unknownAssetCount = _Controller.RemoveUnknownAssets();
            int unusedResourceCount = _Controller.RemoveUnusedResources();
            RefreshResourceTree();

            Debug.Log(Utility.Text.Format("Clean complete, {0} unknown assets and {1} unused resources has been removed.", unknownAssetCount, unusedResourceCount));
        }

        private void RefreshResourceTree()
        {
            _ResourceRoot.Clear();
            Resource[] resources = _Controller.GetResources();
            foreach (Resource resource in resources)
            {
                string[] splitedPath = resource.Name.Split('/');
                ResourceFolder folder = _ResourceRoot;
                for (int i = 0; i < splitedPath.Length - 1; i++)
                {
                    ResourceFolder subFolder = folder.GetFolder(splitedPath[i]);
                    folder = subFolder == null ? folder.AddFolder(splitedPath[i]) : subFolder;
                }

                string fullName = resource.Variant != null ? Utility.Text.Format("{0}.{1}", splitedPath[splitedPath.Length - 1], resource.Variant) : splitedPath[splitedPath.Length - 1];
                folder.AddItem(fullName, resource);
            }
        }

        private bool IsExpandedResourceFolder(ResourceFolder folder)
        {
            return _ExpandedResourceFolderNames.Contains(folder.FromRootPath);
        }

        private void SetExpandedResourceFolder(ResourceFolder folder, bool expand)
        {
            if (expand)
            {
                _ExpandedResourceFolderNames.Add(folder.FromRootPath);
            }
            else
            {
                _ExpandedResourceFolderNames.Remove(folder.FromRootPath);
            }
        }

        private bool IsSelectedAssetInSelectedResource(Asset asset)
        {
            return _SelectedAssetsInSelectedResource.Contains(asset);
        }

        private void SetSelectedAssetInSelectedResource(Asset asset, bool select)
        {
            if (select)
            {
                _SelectedAssetsInSelectedResource.Add(asset);
            }
            else
            {
                _SelectedAssetsInSelectedResource.Remove(asset);
            }
        }

        private bool IsExpandedSourceFolder(SourceFolder sourceFolder)
        {
            return _ExpandedSourceFolders.Contains(sourceFolder);
        }

        private void SetExpandedSourceFolder(SourceFolder sourceFolder, bool expand)
        {
            if (expand)
            {
                _ExpandedSourceFolders.Add(sourceFolder);
            }
            else
            {
                _ExpandedSourceFolders.Remove(sourceFolder);
            }
        }

        private bool IsSelectedSourceFolder(SourceFolder sourceFolder)
        {
            if (_CachedSelectedSourceFolders.Contains(sourceFolder))
            {
                return true;
            }

            if (_CachedUnselectedSourceFolders.Contains(sourceFolder))
            {
                return false;
            }

            foreach (SourceAsset sourceAsset in sourceFolder.GetAssets())
            {
                if (_HideAssignedSourceAssets && IsAssignedSourceAsset(sourceAsset))
                {
                    continue;
                }

                if (!IsSelectedSourceAsset(sourceAsset))
                {
                    _CachedUnselectedSourceFolders.Add(sourceFolder);
                    return false;
                }
            }

            foreach (SourceFolder subSourceFolder in sourceFolder.GetFolders())
            {
                if (_HideAssignedSourceAssets && IsAssignedSourceFolder(sourceFolder))
                {
                    continue;
                }

                if (!IsSelectedSourceFolder(subSourceFolder))
                {
                    _CachedUnselectedSourceFolders.Add(sourceFolder);
                    return false;
                }
            }

            _CachedSelectedSourceFolders.Add(sourceFolder);
            return true;
        }

        private void SetSelectedSourceFolder(SourceFolder sourceFolder, bool select)
        {
            if (select)
            {
                _CachedSelectedSourceFolders.Add(sourceFolder);
                _CachedUnselectedSourceFolders.Remove(sourceFolder);

                SourceFolder folder = sourceFolder;
                while (folder != null)
                {
                    _CachedUnselectedSourceFolders.Remove(folder);
                    folder = folder.Folder;
                }
            }
            else
            {
                _CachedSelectedSourceFolders.Remove(sourceFolder);
                _CachedUnselectedSourceFolders.Add(sourceFolder);

                SourceFolder folder = sourceFolder;
                while (folder != null)
                {
                    _CachedSelectedSourceFolders.Remove(folder);
                    folder = folder.Folder;
                }
            }

            foreach (SourceAsset sourceAsset in sourceFolder.GetAssets())
            {
                if (_HideAssignedSourceAssets && IsAssignedSourceAsset(sourceAsset))
                {
                    continue;
                }

                SetSelectedSourceAsset(sourceAsset, select);
            }

            foreach (SourceFolder subSourceFolder in sourceFolder.GetFolders())
            {
                if (_HideAssignedSourceAssets && IsAssignedSourceFolder(subSourceFolder))
                {
                    continue;
                }

                SetSelectedSourceFolder(subSourceFolder, select);
            }
        }

        private bool IsSelectedSourceAsset(SourceAsset sourceAsset)
        {
            return _SelectedSourceAssets.Contains(sourceAsset);
        }

        private void SetSelectedSourceAsset(SourceAsset sourceAsset, bool select)
        {
            if (select)
            {
                _SelectedSourceAssets.Add(sourceAsset);

                SourceFolder folder = sourceAsset.Folder;
                while (folder != null)
                {
                    _CachedUnselectedSourceFolders.Remove(folder);
                    folder = folder.Folder;
                }
            }
            else
            {
                _SelectedSourceAssets.Remove(sourceAsset);

                SourceFolder folder = sourceAsset.Folder;
                while (folder != null)
                {
                    _CachedSelectedSourceFolders.Remove(folder);
                    folder = folder.Folder;
                }
            }
        }

        private bool IsAssignedSourceAsset(SourceAsset sourceAsset)
        {
            if (_CachedAssignedSourceAssets.Contains(sourceAsset))
            {
                return true;
            }

            if (_CachedUnassignedSourceAssets.Contains(sourceAsset))
            {
                return false;
            }

            return _Controller.GetAsset(sourceAsset.Guid) != null;
        }

        private bool IsAssignedSourceFolder(SourceFolder sourceFolder)
        {
            if (_CachedAssignedSourceFolders.Contains(sourceFolder))
            {
                return true;
            }

            if (_CachedUnassignedSourceFolders.Contains(sourceFolder))
            {
                return false;
            }

            foreach (SourceAsset sourceAsset in sourceFolder.GetAssets())
            {
                if (!IsAssignedSourceAsset(sourceAsset))
                {
                    _CachedUnassignedSourceFolders.Add(sourceFolder);
                    return false;
                }
            }

            foreach (SourceFolder subSourceFolder in sourceFolder.GetFolders())
            {
                if (!IsAssignedSourceFolder(subSourceFolder))
                {
                    _CachedUnassignedSourceFolders.Add(sourceFolder);
                    return false;
                }
            }

            _CachedAssignedSourceFolders.Add(sourceFolder);
            return true;
        }

        private HashSet<SourceAsset> GetSelectedSourceAssets()
        {
            if (!_HideAssignedSourceAssets)
            {
                return _SelectedSourceAssets;
            }

            HashSet<SourceAsset> selectedUnassignedSourceAssets = new HashSet<SourceAsset>();
            foreach (SourceAsset sourceAsset in _SelectedSourceAssets)
            {
                if (!IsAssignedSourceAsset(sourceAsset))
                {
                    selectedUnassignedSourceAssets.Add(sourceAsset);
                }
            }

            return selectedUnassignedSourceAssets;
        }

        private string GetResourceFullName(string name, string variant)
        {
            return variant != null ? Utility.Text.Format("{0}.{1}", name, variant) : name;
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

        private void OnAssetAssigned(SourceAsset[] sourceAssets)
        {
            HashSet<SourceFolder> affectedFolders = new HashSet<SourceFolder>();
            foreach (SourceAsset sourceAsset in sourceAssets)
            {
                _CachedAssignedSourceAssets.Add(sourceAsset);
                _CachedUnassignedSourceAssets.Remove(sourceAsset);

                affectedFolders.Add(sourceAsset.Folder);
            }

            foreach (SourceFolder sourceFolder in affectedFolders)
            {
                SourceFolder folder = sourceFolder;
                while (folder != null)
                {
                    _CachedUnassignedSourceFolders.Remove(folder);
                    folder = folder.Folder;
                }
            }
        }

        private void OnAssetUnassigned(SourceAsset[] sourceAssets)
        {
            HashSet<SourceFolder> affectedFolders = new HashSet<SourceFolder>();
            foreach (SourceAsset sourceAsset in sourceAssets)
            {
                _CachedAssignedSourceAssets.Remove(sourceAsset);
                _CachedUnassignedSourceAssets.Add(sourceAsset);

                affectedFolders.Add(sourceAsset.Folder);
            }

            foreach (SourceFolder sourceFolder in affectedFolders)
            {
                SourceFolder folder = sourceFolder;
                while (folder != null)
                {
                    _CachedSelectedSourceFolders.Remove(folder);
                    _CachedAssignedSourceFolders.Remove(folder);
                    _CachedUnassignedSourceFolders.Add(folder);
                    folder = folder.Folder;
                }
            }
        }
    }
}
