//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            /// <summary>
            /// 加载资源代理。
            /// </summary>
            private sealed partial class LoadResourceAgent : ITaskAgent<LoadResourceTaskBase>
            {
                private static readonly Dictionary<string, string> s_CachedResourceNames = new Dictionary<string, string>(StringComparer.Ordinal);
                private static readonly HashSet<string> s_LoadingAssetNames = new HashSet<string>(StringComparer.Ordinal);
                private static readonly HashSet<string> s_LoadingResourceNames = new HashSet<string>(StringComparer.Ordinal);

                private readonly ILoadResourceAgentHelper _Helper;
                private readonly IResourceHelper _ResourceHelper;
                private readonly ResourceLoader _ResourceLoader;
                private readonly string _ReadOnlyPath;
                private readonly string _ReadWritePath;
                private readonly DecryptResourceCallback _DecryptResourceCallback;
                private LoadResourceTaskBase _Task;

                /// <summary>
                /// 初始化加载资源代理的新实例。
                /// </summary>
                /// <param name="loadResourceAgentHelper">加载资源代理辅助器。</param>
                /// <param name="resourceHelper">资源辅助器。</param>
                /// <param name="resourceLoader">加载资源器。</param>
                /// <param name="readOnlyPath">资源只读区路径。</param>
                /// <param name="readWritePath">资源读写区路径。</param>
                /// <param name="decryptResourceCallback">解密资源回调函数。</param>
                public LoadResourceAgent(ILoadResourceAgentHelper loadResourceAgentHelper, IResourceHelper resourceHelper, ResourceLoader resourceLoader, string readOnlyPath, string readWritePath, DecryptResourceCallback decryptResourceCallback)
                {
                    if (loadResourceAgentHelper == null)
                    {
                        throw new GameFrameworkException("Load resource agent helper is invalid.");
                    }

                    if (resourceHelper == null)
                    {
                        throw new GameFrameworkException("Resource helper is invalid.");
                    }

                    if (resourceLoader == null)
                    {
                        throw new GameFrameworkException("Resource loader is invalid.");
                    }

                    if (decryptResourceCallback == null)
                    {
                        throw new GameFrameworkException("Decrypt resource callback is invalid.");
                    }

                    _Helper = loadResourceAgentHelper;
                    _ResourceHelper = resourceHelper;
                    _ResourceLoader = resourceLoader;
                    _ReadOnlyPath = readOnlyPath;
                    _ReadWritePath = readWritePath;
                    _DecryptResourceCallback = decryptResourceCallback;
                    _Task = null;
                }

                public ILoadResourceAgentHelper Helper
                {
                    get
                    {
                        return _Helper;
                    }
                }

                /// <summary>
                /// 获取加载资源任务。
                /// </summary>
                public LoadResourceTaskBase Task
                {
                    get
                    {
                        return _Task;
                    }
                }

                /// <summary>
                /// 初始化加载资源代理。
                /// </summary>
                public void Initialize()
                {
                    _Helper.LoadResourceAgentHelperUpdate += OnLoadResourceAgentHelperUpdate;
                    _Helper.LoadResourceAgentHelperReadFileComplete += OnLoadResourceAgentHelperReadFileComplete;
                    _Helper.LoadResourceAgentHelperReadBytesComplete += OnLoadResourceAgentHelperReadBytesComplete;
                    _Helper.LoadResourceAgentHelperParseBytesComplete += OnLoadResourceAgentHelperParseBytesComplete;
                    _Helper.LoadResourceAgentHelperLoadComplete += OnLoadResourceAgentHelperLoadComplete;
                    _Helper.LoadResourceAgentHelperError += OnLoadResourceAgentHelperError;
                }

                /// <summary>
                /// 加载资源代理轮询。
                /// </summary>
                /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
                /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
                public void Update(float elapseSeconds, float realElapseSeconds)
                {
                }

                /// <summary>
                /// 关闭并清理加载资源代理。
                /// </summary>
                public void Shutdown()
                {
                    Reset();
                    _Helper.LoadResourceAgentHelperUpdate -= OnLoadResourceAgentHelperUpdate;
                    _Helper.LoadResourceAgentHelperReadFileComplete -= OnLoadResourceAgentHelperReadFileComplete;
                    _Helper.LoadResourceAgentHelperReadBytesComplete -= OnLoadResourceAgentHelperReadBytesComplete;
                    _Helper.LoadResourceAgentHelperParseBytesComplete -= OnLoadResourceAgentHelperParseBytesComplete;
                    _Helper.LoadResourceAgentHelperLoadComplete -= OnLoadResourceAgentHelperLoadComplete;
                    _Helper.LoadResourceAgentHelperError -= OnLoadResourceAgentHelperError;
                }

                public static void Clear()
                {
                    s_CachedResourceNames.Clear();
                    s_LoadingAssetNames.Clear();
                    s_LoadingResourceNames.Clear();
                }

                /// <summary>
                /// 开始处理加载资源任务。
                /// </summary>
                /// <param name="task">要处理的加载资源任务。</param>
                /// <returns>开始处理任务的状态。</returns>
                public StartTaskStatus Start(LoadResourceTaskBase task)
                {
                    if (task == null)
                    {
                        throw new GameFrameworkException("Task is invalid.");
                    }

                    _Task = task;
                    _Task.StartTime = DateTime.UtcNow;
                    ResourceInfo resourceInfo = _Task.ResourceInfo;

                    if (!resourceInfo.Ready)
                    {
                        _Task.StartTime = default(DateTime);
                        return StartTaskStatus.HasToWait;
                    }

                    if (IsAssetLoading(_Task.AssetName))
                    {
                        _Task.StartTime = default(DateTime);
                        return StartTaskStatus.HasToWait;
                    }

                    if (!_Task.IsScene)
                    {
                        AssetObject assetObject = _ResourceLoader._AssetPool.Spawn(_Task.AssetName);
                        if (assetObject != null)
                        {
                            OnAssetObjectReady(assetObject);
                            return StartTaskStatus.Done;
                        }
                    }

                    foreach (string dependencyAssetName in _Task.GetDependencyAssetNames())
                    {
                        if (!_ResourceLoader._AssetPool.CanSpawn(dependencyAssetName))
                        {
                            _Task.StartTime = default(DateTime);
                            return StartTaskStatus.HasToWait;
                        }
                    }

                    string resourceName = resourceInfo.ResourceName.Name;
                    if (IsResourceLoading(resourceName))
                    {
                        _Task.StartTime = default(DateTime);
                        return StartTaskStatus.HasToWait;
                    }

                    s_LoadingAssetNames.Add(_Task.AssetName);

                    ResourceObject resourceObject = _ResourceLoader._ResourcePool.Spawn(resourceName);
                    if (resourceObject != null)
                    {
                        OnResourceObjectReady(resourceObject);
                        return StartTaskStatus.CanResume;
                    }

                    s_LoadingResourceNames.Add(resourceName);

                    string fullPath = null;
                    if (!s_CachedResourceNames.TryGetValue(resourceName, out fullPath))
                    {
                        fullPath = Utility.Path.GetRegularPath(Path.Combine(resourceInfo.StorageInReadOnly ? _ReadOnlyPath : _ReadWritePath, resourceInfo.UseFileSystem ? resourceInfo.FileSystemName : resourceInfo.ResourceName.FullName));
                        s_CachedResourceNames.Add(resourceName, fullPath);
                    }

                    if (resourceInfo.LoadType == LoadType.LoadFromFile)
                    {
                        if (resourceInfo.UseFileSystem)
                        {
                            IFileSystem fileSystem = _ResourceLoader._ResourceManager.GetFileSystem(resourceInfo.FileSystemName, resourceInfo.StorageInReadOnly);
                            _Helper.ReadFile(fileSystem, resourceInfo.ResourceName.FullName);
                        }
                        else
                        {
                            _Helper.ReadFile(fullPath);
                        }
                    }
                    else if (resourceInfo.LoadType == LoadType.LoadFromMemory || resourceInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || resourceInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt)
                    {
                        if (resourceInfo.UseFileSystem)
                        {
                            IFileSystem fileSystem = _ResourceLoader._ResourceManager.GetFileSystem(resourceInfo.FileSystemName, resourceInfo.StorageInReadOnly);
                            _Helper.ReadBytes(fileSystem, resourceInfo.ResourceName.FullName);
                        }
                        else
                        {
                            _Helper.ReadBytes(fullPath);
                        }
                    }
                    else
                    {
                        throw new GameFrameworkException(Utility.Text.Format("Resource load type '{0}' is not supported.", resourceInfo.LoadType));
                    }

                    return StartTaskStatus.CanResume;
                }

                /// <summary>
                /// 重置加载资源代理。
                /// </summary>
                public void Reset()
                {
                    _Helper.Reset();
                    _Task = null;
                }

                private static bool IsAssetLoading(string assetName)
                {
                    return s_LoadingAssetNames.Contains(assetName);
                }

                private static bool IsResourceLoading(string resourceName)
                {
                    return s_LoadingResourceNames.Contains(resourceName);
                }

                private void OnAssetObjectReady(AssetObject assetObject)
                {
                    _Helper.Reset();

                    object asset = assetObject.Target;
                    if (_Task.IsScene)
                    {
                        _ResourceLoader._SceneToAssetMap.Add(_Task.AssetName, asset);
                    }

                    _Task.OnLoadAssetSuccess(this, asset, (float)(DateTime.UtcNow - _Task.StartTime).TotalSeconds);
                    _Task.Done = true;
                }

                private void OnResourceObjectReady(ResourceObject resourceObject)
                {
                    _Task.LoadMain(this, resourceObject);
                }

                private void OnError(LoadResourceStatus status, string errorMessage)
                {
                    _Helper.Reset();
                    _Task.OnLoadAssetFailure(this, status, errorMessage);
                    s_LoadingAssetNames.Remove(_Task.AssetName);
                    s_LoadingResourceNames.Remove(_Task.ResourceInfo.ResourceName.Name);
                    _Task.Done = true;
                }

                private void OnLoadResourceAgentHelperUpdate(object sender, LoadResourceAgentHelperUpdateEventArgs e)
                {
                    _Task.OnLoadAssetUpdate(this, e.Type, e.Progress);
                }

                private void OnLoadResourceAgentHelperReadFileComplete(object sender, LoadResourceAgentHelperReadFileCompleteEventArgs e)
                {
                    ResourceObject resourceObject = ResourceObject.Create(_Task.ResourceInfo.ResourceName.Name, e.Resource, _ResourceHelper, _ResourceLoader);
                    _ResourceLoader._ResourcePool.Register(resourceObject, true);
                    s_LoadingResourceNames.Remove(_Task.ResourceInfo.ResourceName.Name);
                    OnResourceObjectReady(resourceObject);
                }

                private void OnLoadResourceAgentHelperReadBytesComplete(object sender, LoadResourceAgentHelperReadBytesCompleteEventArgs e)
                {
                    byte[] bytes = e.GetBytes();
                    ResourceInfo resourceInfo = _Task.ResourceInfo;
                    if (resourceInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || resourceInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt)
                    {
                        _DecryptResourceCallback(bytes, 0, bytes.Length, resourceInfo.ResourceName.Name, resourceInfo.ResourceName.Variant, resourceInfo.ResourceName.Extension, resourceInfo.StorageInReadOnly, resourceInfo.FileSystemName, (byte)resourceInfo.LoadType, resourceInfo.Length, resourceInfo.HashCode);
                    }

                    _Helper.ParseBytes(bytes);
                }

                private void OnLoadResourceAgentHelperParseBytesComplete(object sender, LoadResourceAgentHelperParseBytesCompleteEventArgs e)
                {
                    ResourceObject resourceObject = ResourceObject.Create(_Task.ResourceInfo.ResourceName.Name, e.Resource, _ResourceHelper, _ResourceLoader);
                    _ResourceLoader._ResourcePool.Register(resourceObject, true);
                    s_LoadingResourceNames.Remove(_Task.ResourceInfo.ResourceName.Name);
                    OnResourceObjectReady(resourceObject);
                }

                private void OnLoadResourceAgentHelperLoadComplete(object sender, LoadResourceAgentHelperLoadCompleteEventArgs e)
                {
                    AssetObject assetObject = null;
                    if (_Task.IsScene)
                    {
                        assetObject = _ResourceLoader._AssetPool.Spawn(_Task.AssetName);
                    }

                    if (assetObject == null)
                    {
                        List<object> dependencyAssets = _Task.GetDependencyAssets();
                        assetObject = AssetObject.Create(_Task.AssetName, e.Asset, dependencyAssets, _Task.ResourceObject.Target, _ResourceHelper, _ResourceLoader);
                        _ResourceLoader._AssetPool.Register(assetObject, true);
                        _ResourceLoader._AssetToResourceMap.Add(e.Asset, _Task.ResourceObject.Target);
                        foreach (object dependencyAsset in dependencyAssets)
                        {
                            object dependencyResource = null;
                            if (_ResourceLoader._AssetToResourceMap.TryGetValue(dependencyAsset, out dependencyResource))
                            {
                                _Task.ResourceObject.AddDependencyResource(dependencyResource);
                            }
                            else
                            {
                                throw new GameFrameworkException("Can not find dependency resource.");
                            }
                        }
                    }

                    s_LoadingAssetNames.Remove(_Task.AssetName);
                    OnAssetObjectReady(assetObject);
                }

                private void OnLoadResourceAgentHelperError(object sender, LoadResourceAgentHelperErrorEventArgs e)
                {
                    OnError(e.Status, e.ErrorMessage);
                }
            }
        }
    }
}
