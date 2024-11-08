//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed class DependencyData
    {
        private List<Resource> _DependencyResources;
        private List<Asset> _DependencyAssets;
        private List<string> _ScatteredDependencyAssetNames;

        public DependencyData()
        {
            _DependencyResources = new List<Resource>();
            _DependencyAssets = new List<Asset>();
            _ScatteredDependencyAssetNames = new List<string>();
        }

        public int DependencyResourceCount
        {
            get
            {
                return _DependencyResources.Count;
            }
        }

        public int DependencyAssetCount
        {
            get
            {
                return _DependencyAssets.Count;
            }
        }

        public int ScatteredDependencyAssetCount
        {
            get
            {
                return _ScatteredDependencyAssetNames.Count;
            }
        }

        public void AddDependencyAsset(Asset asset)
        {
            if (!_DependencyResources.Contains(asset.Resource))
            {
                _DependencyResources.Add(asset.Resource);
            }

            _DependencyAssets.Add(asset);
        }

        public void AddScatteredDependencyAsset(string dependencyAssetName)
        {
            _ScatteredDependencyAssetNames.Add(dependencyAssetName);
        }

        public Resource[] GetDependencyResources()
        {
            return _DependencyResources.ToArray();
        }

        public Asset[] GetDependencyAssets()
        {
            return _DependencyAssets.ToArray();
        }

        public string[] GetScatteredDependencyAssetNames()
        {
            return _ScatteredDependencyAssetNames.ToArray();
        }

        public void RefreshData()
        {
            _DependencyResources.Sort(DependencyResourcesComparer);
            _DependencyAssets.Sort(DependencyAssetsComparer);
            _ScatteredDependencyAssetNames.Sort();
        }

        private int DependencyResourcesComparer(Resource a, Resource b)
        {
            return a.FullName.CompareTo(b.FullName);
        }

        private int DependencyAssetsComparer(Asset a, Asset b)
        {
            return a.Name.CompareTo(b.Name);
        }
    }
}
