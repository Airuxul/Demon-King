//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        /// <summary>
        /// 资源初始化器。
        /// </summary>
        private sealed class ResourceIniter
        {
            private readonly ResourceManager _ResourceManager;
            private readonly Dictionary<ResourceName, string> _CachedFileSystemNames;
            private string _CurrentVariant;

            public GameFrameworkAction ResourceInitComplete;

            /// <summary>
            /// 初始化资源初始化器的新实例。
            /// </summary>
            /// <param name="resourceManager">资源管理器。</param>
            public ResourceIniter(ResourceManager resourceManager)
            {
                _ResourceManager = resourceManager;
                _CachedFileSystemNames = new Dictionary<ResourceName, string>();
                _CurrentVariant = null;

                ResourceInitComplete = null;
            }

            /// <summary>
            /// 关闭并清理资源初始化器。
            /// </summary>
            public void Shutdown()
            {
            }

            /// <summary>
            /// 初始化资源。
            /// </summary>
            public void InitResources(string currentVariant)
            {
                _CurrentVariant = currentVariant;

                if (_ResourceManager._ResourceHelper == null)
                {
                    throw new GameFrameworkException("Resource helper is invalid.");
                }

                if (string.IsNullOrEmpty(_ResourceManager._ReadOnlyPath))
                {
                    throw new GameFrameworkException("Read-only path is invalid.");
                }

                _ResourceManager._ResourceHelper.LoadBytes(Utility.Path.GetRemotePath(Path.Combine(_ResourceManager._ReadOnlyPath, RemoteVersionListFileName)), new LoadBytesCallbacks(OnLoadPackageVersionListSuccess, OnLoadPackageVersionListFailure), null);
            }

            private void OnLoadPackageVersionListSuccess(string fileUri, byte[] bytes, float duration, object userData)
            {
                MemoryStream memoryStream = null;
                try
                {
                    memoryStream = new MemoryStream(bytes, false);
                    PackageVersionList versionList = _ResourceManager._PackageVersionListSerializer.Deserialize(memoryStream);
                    if (!versionList.IsValid)
                    {
                        throw new GameFrameworkException("Deserialize package version list failure.");
                    }

                    PackageVersionList.Asset[] assets = versionList.GetAssets();
                    PackageVersionList.Resource[] resources = versionList.GetResources();
                    PackageVersionList.FileSystem[] fileSystems = versionList.GetFileSystems();
                    PackageVersionList.ResourceGroup[] resourceGroups = versionList.GetResourceGroups();
                    _ResourceManager._ApplicableGameVersion = versionList.ApplicableGameVersion;
                    _ResourceManager._InternalResourceVersion = versionList.InternalResourceVersion;
                    _ResourceManager._AssetInfos = new Dictionary<string, AssetInfo>(assets.Length, StringComparer.Ordinal);
                    _ResourceManager._ResourceInfos = new Dictionary<ResourceName, ResourceInfo>(resources.Length, new ResourceNameComparer());
                    ResourceGroup defaultResourceGroup = _ResourceManager.GetOrAddResourceGroup(string.Empty);

                    foreach (PackageVersionList.FileSystem fileSystem in fileSystems)
                    {
                        int[] resourceIndexes = fileSystem.GetResourceIndexes();
                        foreach (int resourceIndex in resourceIndexes)
                        {
                            PackageVersionList.Resource resource = resources[resourceIndex];
                            if (resource.Variant != null && resource.Variant != _CurrentVariant)
                            {
                                continue;
                            }

                            _CachedFileSystemNames.Add(new ResourceName(resource.Name, resource.Variant, resource.Extension), fileSystem.Name);
                        }
                    }

                    foreach (PackageVersionList.Resource resource in resources)
                    {
                        if (resource.Variant != null && resource.Variant != _CurrentVariant)
                        {
                            continue;
                        }

                        ResourceName resourceName = new ResourceName(resource.Name, resource.Variant, resource.Extension);
                        int[] assetIndexes = resource.GetAssetIndexes();
                        foreach (int assetIndex in assetIndexes)
                        {
                            PackageVersionList.Asset asset = assets[assetIndex];
                            int[] dependencyAssetIndexes = asset.GetDependencyAssetIndexes();
                            int index = 0;
                            string[] dependencyAssetNames = new string[dependencyAssetIndexes.Length];
                            foreach (int dependencyAssetIndex in dependencyAssetIndexes)
                            {
                                dependencyAssetNames[index++] = assets[dependencyAssetIndex].Name;
                            }

                            _ResourceManager._AssetInfos.Add(asset.Name, new AssetInfo(asset.Name, resourceName, dependencyAssetNames));
                        }

                        string fileSystemName = null;
                        if (!_CachedFileSystemNames.TryGetValue(resourceName, out fileSystemName))
                        {
                            fileSystemName = null;
                        }

                        _ResourceManager._ResourceInfos.Add(resourceName, new ResourceInfo(resourceName, fileSystemName, (LoadType)resource.LoadType, resource.Length, resource.HashCode, resource.Length, true, true));
                        defaultResourceGroup.AddResource(resourceName, resource.Length, resource.Length);
                    }

                    foreach (PackageVersionList.ResourceGroup resourceGroup in resourceGroups)
                    {
                        ResourceGroup group = _ResourceManager.GetOrAddResourceGroup(resourceGroup.Name);
                        int[] resourceIndexes = resourceGroup.GetResourceIndexes();
                        foreach (int resourceIndex in resourceIndexes)
                        {
                            PackageVersionList.Resource resource = resources[resourceIndex];
                            if (resource.Variant != null && resource.Variant != _CurrentVariant)
                            {
                                continue;
                            }

                            group.AddResource(new ResourceName(resource.Name, resource.Variant, resource.Extension), resource.Length, resource.Length);
                        }
                    }

                    ResourceInitComplete();
                }
                catch (Exception exception)
                {
                    if (exception is GameFrameworkException)
                    {
                        throw;
                    }

                    throw new GameFrameworkException(Utility.Text.Format("Parse package version list exception '{0}'.", exception), exception);
                }
                finally
                {
                    _CachedFileSystemNames.Clear();
                    if (memoryStream != null)
                    {
                        memoryStream.Dispose();
                        memoryStream = null;
                    }
                }
            }

            private void OnLoadPackageVersionListFailure(string fileUri, string errorMessage, object userData)
            {
                throw new GameFrameworkException(Utility.Text.Format("Package version list '{0}' is invalid, error message is '{1}'.", fileUri, string.IsNullOrEmpty(errorMessage) ? "<Empty>" : errorMessage));
            }
        }
    }
}
