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
        /// <summary>
        /// 资源检查器。
        /// </summary>
        private sealed partial class ResourceChecker
        {
            private readonly ResourceManager _ResourceManager;
            private readonly Dictionary<ResourceName, CheckInfo> _CheckInfos;
            private string _CurrentVariant;
            private bool _IgnoreOtherVariant;
            private bool _UpdatableVersionListReady;
            private bool _ReadOnlyVersionListReady;
            private bool _ReadWriteVersionListReady;

            public GameFrameworkAction<ResourceName, string, LoadType, int, int, int, int> ResourceNeedUpdate;
            public GameFrameworkAction<int, int, int, long, long> ResourceCheckComplete;

            /// <summary>
            /// 初始化资源检查器的新实例。
            /// </summary>
            /// <param name="resourceManager">资源管理器。</param>
            public ResourceChecker(ResourceManager resourceManager)
            {
                _ResourceManager = resourceManager;
                _CheckInfos = new Dictionary<ResourceName, CheckInfo>();
                _CurrentVariant = null;
                _IgnoreOtherVariant = false;
                _UpdatableVersionListReady = false;
                _ReadOnlyVersionListReady = false;
                _ReadWriteVersionListReady = false;

                ResourceNeedUpdate = null;
                ResourceCheckComplete = null;
            }

            /// <summary>
            /// 关闭并清理资源检查器。
            /// </summary>
            public void Shutdown()
            {
                _CheckInfos.Clear();
            }

            /// <summary>
            /// 检查资源。
            /// </summary>
            /// <param name="currentVariant">当前使用的变体。</param>
            /// <param name="ignoreOtherVariant">是否忽略处理其它变体的资源，若不忽略，将会移除其它变体的资源。</param>
            public void CheckResources(string currentVariant, bool ignoreOtherVariant)
            {
                if (_ResourceManager._ResourceHelper == null)
                {
                    throw new GameFrameworkException("Resource helper is invalid.");
                }

                if (string.IsNullOrEmpty(_ResourceManager._ReadOnlyPath))
                {
                    throw new GameFrameworkException("Read-only path is invalid.");
                }

                if (string.IsNullOrEmpty(_ResourceManager._ReadWritePath))
                {
                    throw new GameFrameworkException("Read-write path is invalid.");
                }

                _CurrentVariant = currentVariant;
                _IgnoreOtherVariant = ignoreOtherVariant;
                _ResourceManager._ResourceHelper.LoadBytes(Utility.Path.GetRemotePath(Path.Combine(_ResourceManager._ReadWritePath, RemoteVersionListFileName)), new LoadBytesCallbacks(OnLoadUpdatableVersionListSuccess, OnLoadUpdatableVersionListFailure), null);
                _ResourceManager._ResourceHelper.LoadBytes(Utility.Path.GetRemotePath(Path.Combine(_ResourceManager._ReadOnlyPath, LocalVersionListFileName)), new LoadBytesCallbacks(OnLoadReadOnlyVersionListSuccess, OnLoadReadOnlyVersionListFailure), null);
                _ResourceManager._ResourceHelper.LoadBytes(Utility.Path.GetRemotePath(Path.Combine(_ResourceManager._ReadWritePath, LocalVersionListFileName)), new LoadBytesCallbacks(OnLoadReadWriteVersionListSuccess, OnLoadReadWriteVersionListFailure), null);
            }

            private void SetCachedFileSystemName(ResourceName resourceName, string fileSystemName)
            {
                GetOrAddCheckInfo(resourceName).SetCachedFileSystemName(fileSystemName);
            }

            private void SetVersionInfo(ResourceName resourceName, LoadType loadType, int length, int hashCode, int compressedLength, int compressedHashCode)
            {
                GetOrAddCheckInfo(resourceName).SetVersionInfo(loadType, length, hashCode, compressedLength, compressedHashCode);
            }

            private void SetReadOnlyInfo(ResourceName resourceName, LoadType loadType, int length, int hashCode)
            {
                GetOrAddCheckInfo(resourceName).SetReadOnlyInfo(loadType, length, hashCode);
            }

            private void SetReadWriteInfo(ResourceName resourceName, LoadType loadType, int length, int hashCode)
            {
                GetOrAddCheckInfo(resourceName).SetReadWriteInfo(loadType, length, hashCode);
            }

            private CheckInfo GetOrAddCheckInfo(ResourceName resourceName)
            {
                CheckInfo checkInfo = null;
                if (_CheckInfos.TryGetValue(resourceName, out checkInfo))
                {
                    return checkInfo;
                }

                checkInfo = new CheckInfo(resourceName);
                _CheckInfos.Add(checkInfo.ResourceName, checkInfo);

                return checkInfo;
            }

            private void RefreshCheckInfoStatus()
            {
                if (!_UpdatableVersionListReady || !_ReadOnlyVersionListReady || !_ReadWriteVersionListReady)
                {
                    return;
                }

                int movedCount = 0;
                int removedCount = 0;
                int updateCount = 0;
                long updateTotalLength = 0L;
                long updateTotalCompressedLength = 0L;
                foreach (KeyValuePair<ResourceName, CheckInfo> checkInfo in _CheckInfos)
                {
                    CheckInfo ci = checkInfo.Value;
                    ci.RefreshStatus(_CurrentVariant, _IgnoreOtherVariant);
                    if (ci.Status == CheckInfo.CheckStatus.StorageInReadOnly)
                    {
                        _ResourceManager._ResourceInfos.Add(ci.ResourceName, new ResourceInfo(ci.ResourceName, ci.FileSystemName, ci.LoadType, ci.Length, ci.HashCode, ci.CompressedLength, true, true));
                    }
                    else if (ci.Status == CheckInfo.CheckStatus.StorageInReadWrite)
                    {
                        if (ci.NeedMoveToDisk || ci.NeedMoveToFileSystem)
                        {
                            movedCount++;
                            string resourceFullName = ci.ResourceName.FullName;
                            string resourcePath = Utility.Path.GetRegularPath(Path.Combine(_ResourceManager._ReadWritePath, resourceFullName));
                            if (ci.NeedMoveToDisk)
                            {
                                IFileSystem fileSystem = _ResourceManager.GetFileSystem(ci.ReadWriteFileSystemName, false);
                                if (!fileSystem.SaveAsFile(resourceFullName, resourcePath))
                                {
                                    throw new GameFrameworkException(Utility.Text.Format("Save as file '{0}' to '{1}' from file system '{2}' error.", resourceFullName, resourcePath, fileSystem.FullPath));
                                }

                                fileSystem.DeleteFile(resourceFullName);
                            }

                            if (ci.NeedMoveToFileSystem)
                            {
                                IFileSystem fileSystem = _ResourceManager.GetFileSystem(ci.FileSystemName, false);
                                if (!fileSystem.WriteFile(resourceFullName, resourcePath))
                                {
                                    throw new GameFrameworkException(Utility.Text.Format("Write resource '{0}' to file system '{1}' error.", resourceFullName, fileSystem.FullPath));
                                }

                                if (File.Exists(resourcePath))
                                {
                                    File.Delete(resourcePath);
                                }
                            }
                        }

                        _ResourceManager._ResourceInfos.Add(ci.ResourceName, new ResourceInfo(ci.ResourceName, ci.FileSystemName, ci.LoadType, ci.Length, ci.HashCode, ci.CompressedLength, false, true));
                        _ResourceManager._ReadWriteResourceInfos.Add(ci.ResourceName, new ReadWriteResourceInfo(ci.FileSystemName, ci.LoadType, ci.Length, ci.HashCode));
                    }
                    else if (ci.Status == CheckInfo.CheckStatus.Update)
                    {
                        _ResourceManager._ResourceInfos.Add(ci.ResourceName, new ResourceInfo(ci.ResourceName, ci.FileSystemName, ci.LoadType, ci.Length, ci.HashCode, ci.CompressedLength, false, false));
                        updateCount++;
                        updateTotalLength += ci.Length;
                        updateTotalCompressedLength += ci.CompressedLength;
                        if (ResourceNeedUpdate != null)
                        {
                            ResourceNeedUpdate(ci.ResourceName, ci.FileSystemName, ci.LoadType, ci.Length, ci.HashCode, ci.CompressedLength, ci.CompressedHashCode);
                        }
                    }
                    else if (ci.Status == CheckInfo.CheckStatus.Unavailable || ci.Status == CheckInfo.CheckStatus.Disuse)
                    {
                        // Do nothing.
                    }
                    else
                    {
                        throw new GameFrameworkException(Utility.Text.Format("Check resources '{0}' error with unknown status.", ci.ResourceName.FullName));
                    }

                    if (ci.NeedRemove)
                    {
                        removedCount++;
                        if (ci.ReadWriteUseFileSystem)
                        {
                            IFileSystem fileSystem = _ResourceManager.GetFileSystem(ci.ReadWriteFileSystemName, false);
                            fileSystem.DeleteFile(ci.ResourceName.FullName);
                        }
                        else
                        {
                            string resourcePath = Utility.Path.GetRegularPath(Path.Combine(_ResourceManager._ReadWritePath, ci.ResourceName.FullName));
                            if (File.Exists(resourcePath))
                            {
                                File.Delete(resourcePath);
                            }
                        }
                    }
                }

                if (movedCount > 0 || removedCount > 0)
                {
                    RemoveEmptyFileSystems();
                    Utility.Path.RemoveEmptyDirectory(_ResourceManager._ReadWritePath);
                }

                if (ResourceCheckComplete != null)
                {
                    ResourceCheckComplete(movedCount, removedCount, updateCount, updateTotalLength, updateTotalCompressedLength);
                }
            }

            private void RemoveEmptyFileSystems()
            {
                List<string> removedFileSystemNames = null;
                foreach (KeyValuePair<string, IFileSystem> fileSystem in _ResourceManager._ReadWriteFileSystems)
                {
                    if (fileSystem.Value.FileCount <= 0)
                    {
                        if (removedFileSystemNames == null)
                        {
                            removedFileSystemNames = new List<string>();
                        }

                        _ResourceManager._FileSystemManager.DestroyFileSystem(fileSystem.Value, true);
                        removedFileSystemNames.Add(fileSystem.Key);
                    }
                }

                if (removedFileSystemNames != null)
                {
                    foreach (string removedFileSystemName in removedFileSystemNames)
                    {
                        _ResourceManager._ReadWriteFileSystems.Remove(removedFileSystemName);
                    }
                }
            }

            private void OnLoadUpdatableVersionListSuccess(string fileUri, byte[] bytes, float duration, object userData)
            {
                if (_UpdatableVersionListReady)
                {
                    throw new GameFrameworkException("Updatable version list has been parsed.");
                }

                MemoryStream memoryStream = null;
                try
                {
                    memoryStream = new MemoryStream(bytes, false);
                    UpdatableVersionList versionList = _ResourceManager._UpdatableVersionListSerializer.Deserialize(memoryStream);
                    if (!versionList.IsValid)
                    {
                        throw new GameFrameworkException("Deserialize updatable version list failure.");
                    }

                    UpdatableVersionList.Asset[] assets = versionList.GetAssets();
                    UpdatableVersionList.Resource[] resources = versionList.GetResources();
                    UpdatableVersionList.FileSystem[] fileSystems = versionList.GetFileSystems();
                    UpdatableVersionList.ResourceGroup[] resourceGroups = versionList.GetResourceGroups();
                    _ResourceManager._ApplicableGameVersion = versionList.ApplicableGameVersion;
                    _ResourceManager._InternalResourceVersion = versionList.InternalResourceVersion;
                    _ResourceManager._AssetInfos = new Dictionary<string, AssetInfo>(assets.Length, StringComparer.Ordinal);
                    _ResourceManager._ResourceInfos = new Dictionary<ResourceName, ResourceInfo>(resources.Length, new ResourceNameComparer());
                    _ResourceManager._ReadWriteResourceInfos = new SortedDictionary<ResourceName, ReadWriteResourceInfo>(new ResourceNameComparer());
                    ResourceGroup defaultResourceGroup = _ResourceManager.GetOrAddResourceGroup(string.Empty);

                    foreach (UpdatableVersionList.FileSystem fileSystem in fileSystems)
                    {
                        int[] resourceIndexes = fileSystem.GetResourceIndexes();
                        foreach (int resourceIndex in resourceIndexes)
                        {
                            UpdatableVersionList.Resource resource = resources[resourceIndex];
                            if (resource.Variant != null && resource.Variant != _CurrentVariant)
                            {
                                continue;
                            }

                            SetCachedFileSystemName(new ResourceName(resource.Name, resource.Variant, resource.Extension), fileSystem.Name);
                        }
                    }

                    foreach (UpdatableVersionList.Resource resource in resources)
                    {
                        if (resource.Variant != null && resource.Variant != _CurrentVariant)
                        {
                            continue;
                        }

                        ResourceName resourceName = new ResourceName(resource.Name, resource.Variant, resource.Extension);
                        int[] assetIndexes = resource.GetAssetIndexes();
                        foreach (int assetIndex in assetIndexes)
                        {
                            UpdatableVersionList.Asset asset = assets[assetIndex];
                            int[] dependencyAssetIndexes = asset.GetDependencyAssetIndexes();
                            int index = 0;
                            string[] dependencyAssetNames = new string[dependencyAssetIndexes.Length];
                            foreach (int dependencyAssetIndex in dependencyAssetIndexes)
                            {
                                dependencyAssetNames[index++] = assets[dependencyAssetIndex].Name;
                            }

                            _ResourceManager._AssetInfos.Add(asset.Name, new AssetInfo(asset.Name, resourceName, dependencyAssetNames));
                        }

                        SetVersionInfo(resourceName, (LoadType)resource.LoadType, resource.Length, resource.HashCode, resource.CompressedLength, resource.CompressedHashCode);
                        defaultResourceGroup.AddResource(resourceName, resource.Length, resource.CompressedLength);
                    }

                    foreach (UpdatableVersionList.ResourceGroup resourceGroup in resourceGroups)
                    {
                        ResourceGroup group = _ResourceManager.GetOrAddResourceGroup(resourceGroup.Name);
                        int[] resourceIndexes = resourceGroup.GetResourceIndexes();
                        foreach (int resourceIndex in resourceIndexes)
                        {
                            UpdatableVersionList.Resource resource = resources[resourceIndex];
                            if (resource.Variant != null && resource.Variant != _CurrentVariant)
                            {
                                continue;
                            }

                            group.AddResource(new ResourceName(resource.Name, resource.Variant, resource.Extension), resource.Length, resource.CompressedLength);
                        }
                    }

                    _UpdatableVersionListReady = true;
                    RefreshCheckInfoStatus();
                }
                catch (Exception exception)
                {
                    if (exception is GameFrameworkException)
                    {
                        throw;
                    }

                    throw new GameFrameworkException(Utility.Text.Format("Parse updatable version list exception '{0}'.", exception), exception);
                }
                finally
                {
                    if (memoryStream != null)
                    {
                        memoryStream.Dispose();
                        memoryStream = null;
                    }
                }
            }

            private void OnLoadUpdatableVersionListFailure(string fileUri, string errorMessage, object userData)
            {
                throw new GameFrameworkException(Utility.Text.Format("Updatable version list '{0}' is invalid, error message is '{1}'.", fileUri, string.IsNullOrEmpty(errorMessage) ? "<Empty>" : errorMessage));
            }

            private void OnLoadReadOnlyVersionListSuccess(string fileUri, byte[] bytes, float duration, object userData)
            {
                if (_ReadOnlyVersionListReady)
                {
                    throw new GameFrameworkException("Read-only version list has been parsed.");
                }

                MemoryStream memoryStream = null;
                try
                {
                    memoryStream = new MemoryStream(bytes, false);
                    LocalVersionList versionList = _ResourceManager._ReadOnlyVersionListSerializer.Deserialize(memoryStream);
                    if (!versionList.IsValid)
                    {
                        throw new GameFrameworkException("Deserialize read-only version list failure.");
                    }

                    LocalVersionList.Resource[] resources = versionList.GetResources();
                    LocalVersionList.FileSystem[] fileSystems = versionList.GetFileSystems();

                    foreach (LocalVersionList.FileSystem fileSystem in fileSystems)
                    {
                        int[] resourceIndexes = fileSystem.GetResourceIndexes();
                        foreach (int resourceIndex in resourceIndexes)
                        {
                            LocalVersionList.Resource resource = resources[resourceIndex];
                            SetCachedFileSystemName(new ResourceName(resource.Name, resource.Variant, resource.Extension), fileSystem.Name);
                        }
                    }

                    foreach (LocalVersionList.Resource resource in resources)
                    {
                        SetReadOnlyInfo(new ResourceName(resource.Name, resource.Variant, resource.Extension), (LoadType)resource.LoadType, resource.Length, resource.HashCode);
                    }

                    _ReadOnlyVersionListReady = true;
                    RefreshCheckInfoStatus();
                }
                catch (Exception exception)
                {
                    if (exception is GameFrameworkException)
                    {
                        throw;
                    }

                    throw new GameFrameworkException(Utility.Text.Format("Parse read-only version list exception '{0}'.", exception), exception);
                }
                finally
                {
                    if (memoryStream != null)
                    {
                        memoryStream.Dispose();
                        memoryStream = null;
                    }
                }
            }

            private void OnLoadReadOnlyVersionListFailure(string fileUri, string errorMessage, object userData)
            {
                if (_ReadOnlyVersionListReady)
                {
                    throw new GameFrameworkException("Read-only version list has been parsed.");
                }

                _ReadOnlyVersionListReady = true;
                RefreshCheckInfoStatus();
            }

            private void OnLoadReadWriteVersionListSuccess(string fileUri, byte[] bytes, float duration, object userData)
            {
                if (_ReadWriteVersionListReady)
                {
                    throw new GameFrameworkException("Read-write version list has been parsed.");
                }

                MemoryStream memoryStream = null;
                try
                {
                    memoryStream = new MemoryStream(bytes, false);
                    LocalVersionList versionList = _ResourceManager._ReadWriteVersionListSerializer.Deserialize(memoryStream);
                    if (!versionList.IsValid)
                    {
                        throw new GameFrameworkException("Deserialize read-write version list failure.");
                    }

                    LocalVersionList.Resource[] resources = versionList.GetResources();
                    LocalVersionList.FileSystem[] fileSystems = versionList.GetFileSystems();

                    foreach (LocalVersionList.FileSystem fileSystem in fileSystems)
                    {
                        int[] resourceIndexes = fileSystem.GetResourceIndexes();
                        foreach (int resourceIndex in resourceIndexes)
                        {
                            LocalVersionList.Resource resource = resources[resourceIndex];
                            SetCachedFileSystemName(new ResourceName(resource.Name, resource.Variant, resource.Extension), fileSystem.Name);
                        }
                    }

                    foreach (LocalVersionList.Resource resource in resources)
                    {
                        SetReadWriteInfo(new ResourceName(resource.Name, resource.Variant, resource.Extension), (LoadType)resource.LoadType, resource.Length, resource.HashCode);
                    }

                    _ReadWriteVersionListReady = true;
                    RefreshCheckInfoStatus();
                }
                catch (Exception exception)
                {
                    if (exception is GameFrameworkException)
                    {
                        throw;
                    }

                    throw new GameFrameworkException(Utility.Text.Format("Parse read-write version list exception '{0}'.", exception), exception);
                }
                finally
                {
                    if (memoryStream != null)
                    {
                        memoryStream.Dispose();
                        memoryStream = null;
                    }
                }
            }

            private void OnLoadReadWriteVersionListFailure(string fileUri, string errorMessage, object userData)
            {
                if (_ReadWriteVersionListReady)
                {
                    throw new GameFrameworkException("Read-write version list has been parsed.");
                }

                _ReadWriteVersionListReady = true;
                RefreshCheckInfoStatus();
            }
        }
    }
}
