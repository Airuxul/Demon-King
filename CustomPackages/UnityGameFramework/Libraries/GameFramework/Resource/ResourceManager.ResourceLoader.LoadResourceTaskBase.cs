//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            private abstract class LoadResourceTaskBase : TaskBase
            {
                private static int s_Serial = 0;

                private string _AssetName;
                private Type _AssetType;
                private ResourceInfo _ResourceInfo;
                private string[] _DependencyAssetNames;
                private readonly List<object> _DependencyAssets;
                private ResourceObject _ResourceObject;
                private DateTime _StartTime;
                private int _TotalDependencyAssetCount;

                public LoadResourceTaskBase()
                {
                    _AssetName = null;
                    _AssetType = null;
                    _ResourceInfo = null;
                    _DependencyAssetNames = null;
                    _DependencyAssets = new List<object>();
                    _ResourceObject = null;
                    _StartTime = default(DateTime);
                    _TotalDependencyAssetCount = 0;
                }

                public string AssetName
                {
                    get
                    {
                        return _AssetName;
                    }
                }

                public Type AssetType
                {
                    get
                    {
                        return _AssetType;
                    }
                }

                public ResourceInfo ResourceInfo
                {
                    get
                    {
                        return _ResourceInfo;
                    }
                }

                public ResourceObject ResourceObject
                {
                    get
                    {
                        return _ResourceObject;
                    }
                }

                public abstract bool IsScene
                {
                    get;
                }

                public DateTime StartTime
                {
                    get
                    {
                        return _StartTime;
                    }
                    set
                    {
                        _StartTime = value;
                    }
                }

                public int LoadedDependencyAssetCount
                {
                    get
                    {
                        return _DependencyAssets.Count;
                    }
                }

                public int TotalDependencyAssetCount
                {
                    get
                    {
                        return _TotalDependencyAssetCount;
                    }
                    set
                    {
                        _TotalDependencyAssetCount = value;
                    }
                }

                public override string Description
                {
                    get
                    {
                        return _AssetName;
                    }
                }

                public override void Clear()
                {
                    base.Clear();
                    _AssetName = null;
                    _AssetType = null;
                    _ResourceInfo = null;
                    _DependencyAssetNames = null;
                    _DependencyAssets.Clear();
                    _ResourceObject = null;
                    _StartTime = default(DateTime);
                    _TotalDependencyAssetCount = 0;
                }

                public string[] GetDependencyAssetNames()
                {
                    return _DependencyAssetNames;
                }

                public List<object> GetDependencyAssets()
                {
                    return _DependencyAssets;
                }

                public void LoadMain(LoadResourceAgent agent, ResourceObject resourceObject)
                {
                    _ResourceObject = resourceObject;
                    agent.Helper.LoadAsset(resourceObject.Target, AssetName, AssetType, IsScene);
                }

                public virtual void OnLoadAssetSuccess(LoadResourceAgent agent, object asset, float duration)
                {
                }

                public virtual void OnLoadAssetFailure(LoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
                {
                }

                public virtual void OnLoadAssetUpdate(LoadResourceAgent agent, LoadResourceProgress type, float progress)
                {
                }

                public virtual void OnLoadDependencyAsset(LoadResourceAgent agent, string dependencyAssetName, object dependencyAsset)
                {
                    _DependencyAssets.Add(dependencyAsset);
                }

                protected void Initialize(string assetName, Type assetType, int priority, ResourceInfo resourceInfo, string[] dependencyAssetNames, object userData)
                {
                    Initialize(++s_Serial, null, priority, userData);
                    _AssetName = assetName;
                    _AssetType = assetType;
                    _ResourceInfo = resourceInfo;
                    _DependencyAssetNames = dependencyAssetNames;
                }
            }
        }
    }
}
