//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System.Collections.Generic;

namespace UnityGameFramework.Editor.ResourceTools
{
    /// <summary>
    /// 资源。
    /// </summary>
    public sealed class Resource
    {
        private readonly List<Asset> _Assets;
        private readonly List<string> _ResourceGroups;

        private Resource(string name, string variant, string fileSystem, LoadType loadType, bool packed, string[] resourceGroups)
        {
            _Assets = new List<Asset>();
            _ResourceGroups = new List<string>();

            Name = name;
            Variant = variant;
            AssetType = AssetType.Unknown;
            FileSystem = fileSystem;
            LoadType = loadType;
            Packed = packed;

            foreach (string resourceGroup in resourceGroups)
            {
                AddResourceGroup(resourceGroup);
            }
        }

        public string Name
        {
            get;
            private set;
        }

        public string Variant
        {
            get;
            private set;
        }

        public string FullName
        {
            get
            {
                return Variant != null ? Utility.Text.Format("{0}.{1}", Name, Variant) : Name;
            }
        }

        public AssetType AssetType
        {
            get;
            private set;
        }

        public bool IsLoadFromBinary
        {
            get
            {
                return LoadType == LoadType.LoadFromBinary || LoadType == LoadType.LoadFromBinaryAndQuickDecrypt || LoadType == LoadType.LoadFromBinaryAndDecrypt;
            }
        }

        public string FileSystem
        {
            get;
            set;
        }

        public LoadType LoadType
        {
            get;
            set;
        }

        public bool Packed
        {
            get;
            set;
        }

        public static Resource Create(string name, string variant, string fileSystem, LoadType loadType, bool packed, string[] resourceGroups)
        {
            return new Resource(name, variant, fileSystem, loadType, packed, resourceGroups ?? new string[0]);
        }

        public Asset[] GetAssets()
        {
            return _Assets.ToArray();
        }

        public Asset GetFirstAsset()
        {
            return _Assets.Count > 0 ? _Assets[0] : null;
        }

        public void Rename(string name, string variant)
        {
            Name = name;
            Variant = variant;
        }

        public void AssignAsset(Asset asset, bool isScene)
        {
            if (asset.Resource != null)
            {
                asset.Resource.UnassignAsset(asset);
            }

            AssetType = isScene ? AssetType.Scene : AssetType.Asset;
            asset.Resource = this;
            _Assets.Add(asset);
            _Assets.Sort(AssetComparer);
        }

        public void UnassignAsset(Asset asset)
        {
            asset.Resource = null;
            _Assets.Remove(asset);
            if (_Assets.Count <= 0)
            {
                AssetType = AssetType.Unknown;
            }
        }

        public string[] GetResourceGroups()
        {
            return _ResourceGroups.ToArray();
        }

        public bool HasResourceGroup(string resourceGroup)
        {
            if (string.IsNullOrEmpty(resourceGroup))
            {
                return false;
            }

            return _ResourceGroups.Contains(resourceGroup);
        }

        public void AddResourceGroup(string resourceGroup)
        {
            if (string.IsNullOrEmpty(resourceGroup))
            {
                return;
            }

            if (_ResourceGroups.Contains(resourceGroup))
            {
                return;
            }

            _ResourceGroups.Add(resourceGroup);
            _ResourceGroups.Sort();
        }

        public bool RemoveResourceGroup(string resourceGroup)
        {
            if (string.IsNullOrEmpty(resourceGroup))
            {
                return false;
            }

            return _ResourceGroups.Remove(resourceGroup);
        }

        public void Clear()
        {
            foreach (Asset asset in _Assets)
            {
                asset.Resource = null;
            }

            _Assets.Clear();
            _ResourceGroups.Clear();
        }

        private int AssetComparer(Asset a, Asset b)
        {
            return a.Guid.CompareTo(b.Guid);
        }
    }
}
