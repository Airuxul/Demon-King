//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed class ResourceEditorController
    {
        private const string DefaultSourceAssetRootPath = "Assets";

        private readonly string _ConfigurationPath;
        private readonly ResourceCollection _ResourceCollection;
        private readonly List<string> _SourceAssetSearchPaths;
        private readonly List<string> _SourceAssetSearchRelativePaths;
        private readonly Dictionary<string, SourceAsset> _SourceAssets;
        private SourceFolder _SourceAssetRoot;
        private string _SourceAssetRootPath;
        private string _SourceAssetUnionTypeFilter;
        private string _SourceAssetUnionLabelFilter;
        private string _SourceAssetExceptTypeFilter;
        private string _SourceAssetExceptLabelFilter;
        private AssetSorterType _AssetSorter;

        public ResourceEditorController()
        {
            _ConfigurationPath = Type.GetConfigurationPath<ResourceEditorConfigPathAttribute>() ?? Utility.Path.GetRegularPath(Path.Combine(Application.dataPath, "GameFramework/Configs/ResourceEditor.xml"));
            _ResourceCollection = new ResourceCollection();
            _ResourceCollection.OnLoadingResource += delegate (int index, int count)
            {
                if (OnLoadingResource != null)
                {
                    OnLoadingResource(index, count);
                }
            };

            _ResourceCollection.OnLoadingAsset += delegate (int index, int count)
            {
                if (OnLoadingAsset != null)
                {
                    OnLoadingAsset(index, count);
                }
            };

            _ResourceCollection.OnLoadCompleted += delegate ()
            {
                if (OnLoadCompleted != null)
                {
                    OnLoadCompleted();
                }
            };

            _SourceAssetSearchPaths = new List<string>();
            _SourceAssetSearchRelativePaths = new List<string>();
            _SourceAssets = new Dictionary<string, SourceAsset>(StringComparer.Ordinal);
            _SourceAssetRoot = null;
            _SourceAssetRootPath = null;
            _SourceAssetUnionTypeFilter = null;
            _SourceAssetUnionLabelFilter = null;
            _SourceAssetExceptTypeFilter = null;
            _SourceAssetExceptLabelFilter = null;
            _AssetSorter = AssetSorterType.Path;

            SourceAssetRootPath = DefaultSourceAssetRootPath;
        }

        public int ResourceCount
        {
            get
            {
                return _ResourceCollection.ResourceCount;
            }
        }

        public int AssetCount
        {
            get
            {
                return _ResourceCollection.AssetCount;
            }
        }

        public SourceFolder SourceAssetRoot
        {
            get
            {
                return _SourceAssetRoot;
            }
        }

        public string SourceAssetRootPath
        {
            get
            {
                return _SourceAssetRootPath;
            }
            set
            {
                if (_SourceAssetRootPath == value)
                {
                    return;
                }

                _SourceAssetRootPath = value.Replace('\\', '/');
                _SourceAssetRoot = new SourceFolder(_SourceAssetRootPath, null);
                RefreshSourceAssetSearchPaths();
            }
        }

        public string SourceAssetUnionTypeFilter
        {
            get
            {
                return _SourceAssetUnionTypeFilter;
            }
            set
            {
                if (_SourceAssetUnionTypeFilter == value)
                {
                    return;
                }

                _SourceAssetUnionTypeFilter = value;
            }
        }

        public string SourceAssetUnionLabelFilter
        {
            get
            {
                return _SourceAssetUnionLabelFilter;
            }
            set
            {
                if (_SourceAssetUnionLabelFilter == value)
                {
                    return;
                }

                _SourceAssetUnionLabelFilter = value;
            }
        }

        public string SourceAssetExceptTypeFilter
        {
            get
            {
                return _SourceAssetExceptTypeFilter;
            }
            set
            {
                if (_SourceAssetExceptTypeFilter == value)
                {
                    return;
                }

                _SourceAssetExceptTypeFilter = value;
            }
        }

        public string SourceAssetExceptLabelFilter
        {
            get
            {
                return _SourceAssetExceptLabelFilter;
            }
            set
            {
                if (_SourceAssetExceptLabelFilter == value)
                {
                    return;
                }

                _SourceAssetExceptLabelFilter = value;
            }
        }

        public AssetSorterType AssetSorter
        {
            get
            {
                return _AssetSorter;
            }
            set
            {
                if (_AssetSorter == value)
                {
                    return;
                }

                _AssetSorter = value;
            }
        }

        public event GameFrameworkAction<int, int> OnLoadingResource = null;

        public event GameFrameworkAction<int, int> OnLoadingAsset = null;

        public event GameFrameworkAction OnLoadCompleted = null;

        public event GameFrameworkAction<SourceAsset[]> OnAssetAssigned = null;

        public event GameFrameworkAction<SourceAsset[]> OnAssetUnassigned = null;

        public bool Load()
        {
            if (!File.Exists(_ConfigurationPath))
            {
                return false;
            }

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(_ConfigurationPath);
                XmlNode xmlRoot = xmlDocument.SelectSingleNode("UnityGameFramework");
                XmlNode xmlEditor = xmlRoot.SelectSingleNode("ResourceEditor");
                XmlNode xmlSettings = xmlEditor.SelectSingleNode("Settings");

                XmlNodeList xmlNodeList = null;
                XmlNode xmlNode = null;

                xmlNodeList = xmlSettings.ChildNodes;
                for (int i = 0; i < xmlNodeList.Count; i++)
                {
                    xmlNode = xmlNodeList.Item(i);
                    switch (xmlNode.Name)
                    {
                        case "SourceAssetRootPath":
                            SourceAssetRootPath = xmlNode.InnerText;
                            break;

                        case "SourceAssetSearchPaths":
                            _SourceAssetSearchRelativePaths.Clear();
                            XmlNodeList xmlNodeListInner = xmlNode.ChildNodes;
                            XmlNode xmlNodeInner = null;
                            for (int j = 0; j < xmlNodeListInner.Count; j++)
                            {
                                xmlNodeInner = xmlNodeListInner.Item(j);
                                if (xmlNodeInner.Name != "SourceAssetSearchPath")
                                {
                                    continue;
                                }

                                _SourceAssetSearchRelativePaths.Add(xmlNodeInner.Attributes.GetNamedItem("RelativePath").Value);
                            }
                            break;

                        case "SourceAssetUnionTypeFilter":
                            SourceAssetUnionTypeFilter = xmlNode.InnerText;
                            break;

                        case "SourceAssetUnionLabelFilter":
                            SourceAssetUnionLabelFilter = xmlNode.InnerText;
                            break;

                        case "SourceAssetExceptTypeFilter":
                            SourceAssetExceptTypeFilter = xmlNode.InnerText;
                            break;

                        case "SourceAssetExceptLabelFilter":
                            SourceAssetExceptLabelFilter = xmlNode.InnerText;
                            break;

                        case "AssetSorter":
                            AssetSorter = (AssetSorterType)Enum.Parse(typeof(AssetSorterType), xmlNode.InnerText);
                            break;
                    }
                }

                RefreshSourceAssetSearchPaths();
            }
            catch
            {
                File.Delete(_ConfigurationPath);
                return false;
            }

            ScanSourceAssets();

            _ResourceCollection.Load();

            return true;
        }

        public bool Save()
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

                XmlElement xmlRoot = xmlDocument.CreateElement("UnityGameFramework");
                xmlDocument.AppendChild(xmlRoot);

                XmlElement xmlEditor = xmlDocument.CreateElement("ResourceEditor");
                xmlRoot.AppendChild(xmlEditor);

                XmlElement xmlSettings = xmlDocument.CreateElement("Settings");
                xmlEditor.AppendChild(xmlSettings);

                XmlElement xmlElement = null;
                XmlAttribute xmlAttribute = null;

                xmlElement = xmlDocument.CreateElement("SourceAssetRootPath");
                xmlElement.InnerText = SourceAssetRootPath.ToString();
                xmlSettings.AppendChild(xmlElement);

                xmlElement = xmlDocument.CreateElement("SourceAssetSearchPaths");
                xmlSettings.AppendChild(xmlElement);

                foreach (string sourceAssetSearchRelativePath in _SourceAssetSearchRelativePaths)
                {
                    XmlElement xmlElementInner = xmlDocument.CreateElement("SourceAssetSearchPath");
                    xmlAttribute = xmlDocument.CreateAttribute("RelativePath");
                    xmlAttribute.Value = sourceAssetSearchRelativePath;
                    xmlElementInner.Attributes.SetNamedItem(xmlAttribute);
                    xmlElement.AppendChild(xmlElementInner);
                }

                xmlElement = xmlDocument.CreateElement("SourceAssetUnionTypeFilter");
                xmlElement.InnerText = SourceAssetUnionTypeFilter ?? string.Empty;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("SourceAssetUnionLabelFilter");
                xmlElement.InnerText = SourceAssetUnionLabelFilter ?? string.Empty;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("SourceAssetExceptTypeFilter");
                xmlElement.InnerText = SourceAssetExceptTypeFilter ?? string.Empty;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("SourceAssetExceptLabelFilter");
                xmlElement.InnerText = SourceAssetExceptLabelFilter ?? string.Empty;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("AssetSorter");
                xmlElement.InnerText = AssetSorter.ToString();
                xmlSettings.AppendChild(xmlElement);

                string configurationDirectoryName = Path.GetDirectoryName(_ConfigurationPath);
                if (!Directory.Exists(configurationDirectoryName))
                {
                    Directory.CreateDirectory(configurationDirectoryName);
                }

                xmlDocument.Save(_ConfigurationPath);
                AssetDatabase.Refresh();
            }
            catch
            {
                if (File.Exists(_ConfigurationPath))
                {
                    File.Delete(_ConfigurationPath);
                }

                return false;
            }

            return _ResourceCollection.Save();
        }

        public Resource[] GetResources()
        {
            return _ResourceCollection.GetResources();
        }

        public Resource GetResource(string name, string variant)
        {
            return _ResourceCollection.GetResource(name, variant);
        }

        public bool HasResource(string name, string variant)
        {
            return _ResourceCollection.HasResource(name, variant);
        }

        public bool AddResource(string name, string variant, string fileSystem, LoadType loadType, bool packed)
        {
            return _ResourceCollection.AddResource(name, variant, fileSystem, loadType, packed);
        }

        public bool RenameResource(string oldName, string oldVariant, string newName, string newVariant)
        {
            return _ResourceCollection.RenameResource(oldName, oldVariant, newName, newVariant);
        }

        public bool RemoveResource(string name, string variant)
        {
            Asset[] assetsToRemove = _ResourceCollection.GetAssets(name, variant);
            if (_ResourceCollection.RemoveResource(name, variant))
            {
                List<SourceAsset> unassignedSourceAssets = new List<SourceAsset>();
                foreach (Asset asset in assetsToRemove)
                {
                    SourceAsset sourceAsset = GetSourceAsset(asset.Guid);
                    if (sourceAsset != null)
                    {
                        unassignedSourceAssets.Add(sourceAsset);
                    }
                }

                if (OnAssetUnassigned != null)
                {
                    OnAssetUnassigned(unassignedSourceAssets.ToArray());
                }

                return true;
            }

            return false;
        }

        public bool SetResourceLoadType(string name, string variant, LoadType loadType)
        {
            return _ResourceCollection.SetResourceLoadType(name, variant, loadType);
        }

        public bool SetResourcePacked(string name, string variant, bool packed)
        {
            return _ResourceCollection.SetResourcePacked(name, variant, packed);
        }

        public int RemoveUnusedResources()
        {
            List<Resource> resources = new List<Resource>(_ResourceCollection.GetResources());
            List<Resource> removeResources = resources.FindAll(resource => GetAssets(resource.Name, resource.Variant).Length <= 0);
            foreach (Resource removeResource in removeResources)
            {
                _ResourceCollection.RemoveResource(removeResource.Name, removeResource.Variant);
            }

            return removeResources.Count;
        }

        public Asset[] GetAssets(string name, string variant)
        {
            List<Asset> assets = new List<Asset>(_ResourceCollection.GetAssets(name, variant));
            switch (AssetSorter)
            {
                case AssetSorterType.Path:
                    assets.Sort(AssetPathComparer);
                    break;

                case AssetSorterType.Name:
                    assets.Sort(AssetNameComparer);
                    break;

                case AssetSorterType.Guid:
                    assets.Sort(AssetGuidComparer);
                    break;
            }

            return assets.ToArray();
        }

        public Asset GetAsset(string guid)
        {
            return _ResourceCollection.GetAsset(guid);
        }

        public bool AssignAsset(string guid, string name, string variant)
        {
            if (_ResourceCollection.AssignAsset(guid, name, variant))
            {
                if (OnAssetAssigned != null)
                {
                    OnAssetAssigned(new SourceAsset[] { GetSourceAsset(guid) });
                }

                return true;
            }

            return false;
        }

        public bool UnassignAsset(string guid)
        {
            if (_ResourceCollection.UnassignAsset(guid))
            {
                SourceAsset sourceAsset = GetSourceAsset(guid);
                if (sourceAsset != null)
                {
                    if (OnAssetUnassigned != null)
                    {
                        OnAssetUnassigned(new SourceAsset[] { sourceAsset });
                    }
                }

                return true;
            }

            return false;
        }

        public int RemoveUnknownAssets()
        {
            List<Asset> assets = new List<Asset>(_ResourceCollection.GetAssets());
            List<Asset> removeAssets = assets.FindAll(asset => GetSourceAsset(asset.Guid) == null);
            foreach (Asset asset in removeAssets)
            {
                _ResourceCollection.UnassignAsset(asset.Guid);
            }

            return removeAssets.Count;
        }

        public SourceAsset[] GetSourceAssets()
        {
            int count = 0;
            SourceAsset[] sourceAssets = new SourceAsset[_SourceAssets.Count];
            foreach (KeyValuePair<string, SourceAsset> sourceAsset in _SourceAssets)
            {
                sourceAssets[count++] = sourceAsset.Value;
            }

            return sourceAssets;
        }

        public SourceAsset GetSourceAsset(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            SourceAsset sourceAsset = null;
            if (_SourceAssets.TryGetValue(guid, out sourceAsset))
            {
                return sourceAsset;
            }

            return null;
        }

        public void ScanSourceAssets()
        {
            _SourceAssets.Clear();
            _SourceAssetRoot.Clear();

            string[] sourceAssetSearchPaths = _SourceAssetSearchPaths.ToArray();
            HashSet<string> tempGuids = new HashSet<string>();
            tempGuids.UnionWith(AssetDatabase.FindAssets(SourceAssetUnionTypeFilter, sourceAssetSearchPaths));
            tempGuids.UnionWith(AssetDatabase.FindAssets(SourceAssetUnionLabelFilter, sourceAssetSearchPaths));
            tempGuids.ExceptWith(AssetDatabase.FindAssets(SourceAssetExceptTypeFilter, sourceAssetSearchPaths));
            tempGuids.ExceptWith(AssetDatabase.FindAssets(SourceAssetExceptLabelFilter, sourceAssetSearchPaths));

            string[] guids = new List<string>(tempGuids).ToArray();
            foreach (string guid in guids)
            {
                string fullPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(fullPath))
                {
                    // Skip folder.
                    continue;
                }

                string assetPath = fullPath.Substring(SourceAssetRootPath.Length + 1);
                string[] splitedPath = assetPath.Split('/');
                SourceFolder folder = _SourceAssetRoot;
                for (int i = 0; i < splitedPath.Length - 1; i++)
                {
                    SourceFolder subFolder = folder.GetFolder(splitedPath[i]);
                    folder = subFolder == null ? folder.AddFolder(splitedPath[i]) : subFolder;
                }

                SourceAsset asset = folder.AddAsset(guid, fullPath, splitedPath[splitedPath.Length - 1]);
                _SourceAssets.Add(asset.Guid, asset);
            }
        }

        private void RefreshSourceAssetSearchPaths()
        {
            _SourceAssetSearchPaths.Clear();

            if (string.IsNullOrEmpty(_SourceAssetRootPath))
            {
                SourceAssetRootPath = DefaultSourceAssetRootPath;
            }

            if (_SourceAssetSearchRelativePaths.Count > 0)
            {
                foreach (string sourceAssetSearchRelativePath in _SourceAssetSearchRelativePaths)
                {
                    _SourceAssetSearchPaths.Add(Utility.Path.GetRegularPath(Path.Combine(_SourceAssetRootPath, sourceAssetSearchRelativePath)));
                }
            }
            else
            {
                _SourceAssetSearchPaths.Add(_SourceAssetRootPath);
            }
        }

        private int AssetPathComparer(Asset a, Asset b)
        {
            SourceAsset sourceAssetA = GetSourceAsset(a.Guid);
            SourceAsset sourceAssetB = GetSourceAsset(b.Guid);

            if (sourceAssetA != null && sourceAssetB != null)
            {
                return sourceAssetA.Path.CompareTo(sourceAssetB.Path);
            }

            if (sourceAssetA == null && sourceAssetB == null)
            {
                return a.Guid.CompareTo(b.Guid);
            }

            if (sourceAssetA == null)
            {
                return -1;
            }

            if (sourceAssetB == null)
            {
                return 1;
            }

            return 0;
        }

        private int AssetNameComparer(Asset a, Asset b)
        {
            SourceAsset sourceAssetA = GetSourceAsset(a.Guid);
            SourceAsset sourceAssetB = GetSourceAsset(b.Guid);

            if (sourceAssetA != null && sourceAssetB != null)
            {
                return sourceAssetA.Name.CompareTo(sourceAssetB.Name);
            }

            if (sourceAssetA == null && sourceAssetB == null)
            {
                return a.Guid.CompareTo(b.Guid);
            }

            if (sourceAssetA == null)
            {
                return -1;
            }

            if (sourceAssetB == null)
            {
                return 1;
            }

            return 0;
        }

        private int AssetGuidComparer(Asset a, Asset b)
        {
            SourceAsset sourceAssetA = GetSourceAsset(a.Guid);
            SourceAsset sourceAssetB = GetSourceAsset(b.Guid);

            if (sourceAssetA != null && sourceAssetB != null || sourceAssetA == null && sourceAssetB == null)
            {
                return a.Guid.CompareTo(b.Guid);
            }

            if (sourceAssetA == null)
            {
                return -1;
            }

            if (sourceAssetB == null)
            {
                return 1;
            }

            return 0;
        }
    }
}
