//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            private sealed class LoadSceneTask : LoadResourceTaskBase
            {
                private LoadSceneCallbacks _LoadSceneCallbacks;

                public LoadSceneTask()
                {
                    _LoadSceneCallbacks = null;
                }

                public override bool IsScene
                {
                    get
                    {
                        return true;
                    }
                }

                public static LoadSceneTask Create(string sceneAssetName, int priority, ResourceInfo resourceInfo, string[] dependencyAssetNames, LoadSceneCallbacks loadSceneCallbacks, object userData)
                {
                    LoadSceneTask loadSceneTask = ReferencePool.Acquire<LoadSceneTask>();
                    loadSceneTask.Initialize(sceneAssetName, null, priority, resourceInfo, dependencyAssetNames, userData);
                    loadSceneTask._LoadSceneCallbacks = loadSceneCallbacks;
                    return loadSceneTask;
                }

                public override void Clear()
                {
                    base.Clear();
                    _LoadSceneCallbacks = null;
                }

                public override void OnLoadAssetSuccess(LoadResourceAgent agent, object asset, float duration)
                {
                    base.OnLoadAssetSuccess(agent, asset, duration);
                    if (_LoadSceneCallbacks.LoadSceneSuccessCallback != null)
                    {
                        _LoadSceneCallbacks.LoadSceneSuccessCallback(AssetName, duration, UserData);
                    }
                }

                public override void OnLoadAssetFailure(LoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
                {
                    base.OnLoadAssetFailure(agent, status, errorMessage);
                    if (_LoadSceneCallbacks.LoadSceneFailureCallback != null)
                    {
                        _LoadSceneCallbacks.LoadSceneFailureCallback(AssetName, status, errorMessage, UserData);
                    }
                }

                public override void OnLoadAssetUpdate(LoadResourceAgent agent, LoadResourceProgress type, float progress)
                {
                    base.OnLoadAssetUpdate(agent, type, progress);
                    if (type == LoadResourceProgress.LoadScene)
                    {
                        if (_LoadSceneCallbacks.LoadSceneUpdateCallback != null)
                        {
                            _LoadSceneCallbacks.LoadSceneUpdateCallback(AssetName, progress, UserData);
                        }
                    }
                }

                public override void OnLoadDependencyAsset(LoadResourceAgent agent, string dependencyAssetName, object dependencyAsset)
                {
                    base.OnLoadDependencyAsset(agent, dependencyAssetName, dependencyAsset);
                    if (_LoadSceneCallbacks.LoadSceneDependencyAssetCallback != null)
                    {
                        _LoadSceneCallbacks.LoadSceneDependencyAssetCallback(AssetName, dependencyAssetName, LoadedDependencyAssetCount, TotalDependencyAssetCount, UserData);
                    }
                }
            }
        }
    }
}
