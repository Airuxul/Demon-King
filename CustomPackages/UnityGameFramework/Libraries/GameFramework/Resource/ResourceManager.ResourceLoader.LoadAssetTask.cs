//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            private sealed class LoadAssetTask : LoadResourceTaskBase
            {
                private LoadAssetCallbacks _LoadAssetCallbacks;

                public LoadAssetTask()
                {
                    _LoadAssetCallbacks = null;
                }

                public override bool IsScene
                {
                    get
                    {
                        return false;
                    }
                }

                public static LoadAssetTask Create(string assetName, Type assetType, int priority, ResourceInfo resourceInfo, string[] dependencyAssetNames, LoadAssetCallbacks loadAssetCallbacks, object userData)
                {
                    LoadAssetTask loadAssetTask = ReferencePool.Acquire<LoadAssetTask>();
                    loadAssetTask.Initialize(assetName, assetType, priority, resourceInfo, dependencyAssetNames, userData);
                    loadAssetTask._LoadAssetCallbacks = loadAssetCallbacks;
                    return loadAssetTask;
                }

                public override void Clear()
                {
                    base.Clear();
                    _LoadAssetCallbacks = null;
                }

                public override void OnLoadAssetSuccess(LoadResourceAgent agent, object asset, float duration)
                {
                    base.OnLoadAssetSuccess(agent, asset, duration);
                    if (_LoadAssetCallbacks.LoadAssetSuccessCallback != null)
                    {
                        _LoadAssetCallbacks.LoadAssetSuccessCallback(AssetName, asset, duration, UserData);
                    }
                }

                public override void OnLoadAssetFailure(LoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
                {
                    base.OnLoadAssetFailure(agent, status, errorMessage);
                    if (_LoadAssetCallbacks.LoadAssetFailureCallback != null)
                    {
                        _LoadAssetCallbacks.LoadAssetFailureCallback(AssetName, status, errorMessage, UserData);
                    }
                }

                public override void OnLoadAssetUpdate(LoadResourceAgent agent, LoadResourceProgress type, float progress)
                {
                    base.OnLoadAssetUpdate(agent, type, progress);
                    if (type == LoadResourceProgress.LoadAsset)
                    {
                        if (_LoadAssetCallbacks.LoadAssetUpdateCallback != null)
                        {
                            _LoadAssetCallbacks.LoadAssetUpdateCallback(AssetName, progress, UserData);
                        }
                    }
                }

                public override void OnLoadDependencyAsset(LoadResourceAgent agent, string dependencyAssetName, object dependencyAsset)
                {
                    base.OnLoadDependencyAsset(agent, dependencyAssetName, dependencyAsset);
                    if (_LoadAssetCallbacks.LoadAssetDependencyAssetCallback != null)
                    {
                        _LoadAssetCallbacks.LoadAssetDependencyAssetCallback(AssetName, dependencyAssetName, LoadedDependencyAssetCount, TotalDependencyAssetCount, UserData);
                    }
                }
            }
        }
    }
}
