//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Download;
using GameFramework.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        /// <summary>
        /// 资源更新器。
        /// </summary>
        private sealed partial class ResourceUpdater
        {
            private const int CachedHashBytesLength = 4;
            private const int CachedBytesLength = 0x1000;

            private readonly ResourceManager _ResourceManager;
            private readonly Queue<ApplyInfo> _ApplyWaitingInfo;
            private readonly List<UpdateInfo> _UpdateWaitingInfo;
            private readonly HashSet<UpdateInfo> _UpdateWaitingInfoWhilePlaying;
            private readonly Dictionary<ResourceName, UpdateInfo> _UpdateCandidateInfo;
            private readonly SortedDictionary<string, List<int>> _CachedFileSystemsForGenerateReadWriteVersionList;
            private readonly List<ResourceName> _CachedResourceNames;
            private readonly byte[] _CachedHashBytes;
            private readonly byte[] _CachedBytes;
            private IDownloadManager _DownloadManager;
            private bool _CheckResourcesComplete;
            private string _ApplyingResourcePackPath;
            private FileStream _ApplyingResourcePackStream;
            private ResourceGroup _UpdatingResourceGroup;
            private int _GenerateReadWriteVersionListLength;
            private int _CurrentGenerateReadWriteVersionListLength;
            private int _UpdateRetryCount;
            private bool _FailureFlag;
            private string _ReadWriteVersionListFileName;
            private string _ReadWriteVersionListTempFileName;

            public GameFrameworkAction<string, int, long> ResourceApplyStart;
            public GameFrameworkAction<ResourceName, string, string, int, int> ResourceApplySuccess;
            public GameFrameworkAction<ResourceName, string, string> ResourceApplyFailure;
            public GameFrameworkAction<string, bool> ResourceApplyComplete;
            public GameFrameworkAction<ResourceName, string, string, int, int, int> ResourceUpdateStart;
            public GameFrameworkAction<ResourceName, string, string, int, int> ResourceUpdateChanged;
            public GameFrameworkAction<ResourceName, string, string, int, int> ResourceUpdateSuccess;
            public GameFrameworkAction<ResourceName, string, int, int, string> ResourceUpdateFailure;
            public GameFrameworkAction<ResourceGroup, bool> ResourceUpdateComplete;
            public GameFrameworkAction ResourceUpdateAllComplete;

            /// <summary>
            /// 初始化资源更新器的新实例。
            /// </summary>
            /// <param name="resourceManager">资源管理器。</param>
            public ResourceUpdater(ResourceManager resourceManager)
            {
                _ResourceManager = resourceManager;
                _ApplyWaitingInfo = new Queue<ApplyInfo>();
                _UpdateWaitingInfo = new List<UpdateInfo>();
                _UpdateWaitingInfoWhilePlaying = new HashSet<UpdateInfo>();
                _UpdateCandidateInfo = new Dictionary<ResourceName, UpdateInfo>();
                _CachedFileSystemsForGenerateReadWriteVersionList = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);
                _CachedResourceNames = new List<ResourceName>();
                _CachedHashBytes = new byte[CachedHashBytesLength];
                _CachedBytes = new byte[CachedBytesLength];
                _DownloadManager = null;
                _CheckResourcesComplete = false;
                _ApplyingResourcePackPath = null;
                _ApplyingResourcePackStream = null;
                _UpdatingResourceGroup = null;
                _GenerateReadWriteVersionListLength = 0;
                _CurrentGenerateReadWriteVersionListLength = 0;
                _UpdateRetryCount = 3;
                _FailureFlag = false;
                _ReadWriteVersionListFileName = Utility.Path.GetRegularPath(Path.Combine(_ResourceManager._ReadWritePath, LocalVersionListFileName));
                _ReadWriteVersionListTempFileName = Utility.Text.Format("{0}.{1}", _ReadWriteVersionListFileName, TempExtension);

                ResourceApplyStart = null;
                ResourceApplySuccess = null;
                ResourceApplyFailure = null;
                ResourceApplyComplete = null;
                ResourceUpdateStart = null;
                ResourceUpdateChanged = null;
                ResourceUpdateSuccess = null;
                ResourceUpdateFailure = null;
                ResourceUpdateComplete = null;
                ResourceUpdateAllComplete = null;
            }

            /// <summary>
            /// 获取或设置每更新多少字节的资源，重新生成一次版本资源列表。
            /// </summary>
            public int GenerateReadWriteVersionListLength
            {
                get
                {
                    return _GenerateReadWriteVersionListLength;
                }
                set
                {
                    _GenerateReadWriteVersionListLength = value;
                }
            }

            /// <summary>
            /// 获取正在应用的资源包路径。
            /// </summary>
            public string ApplyingResourcePackPath
            {
                get
                {
                    return _ApplyingResourcePackPath;
                }
            }

            /// <summary>
            /// 获取等待应用资源数量。
            /// </summary>
            public int ApplyWaitingCount
            {
                get
                {
                    return _ApplyWaitingInfo.Count;
                }
            }

            /// <summary>
            /// 获取或设置资源更新重试次数。
            /// </summary>
            public int UpdateRetryCount
            {
                get
                {
                    return _UpdateRetryCount;
                }
                set
                {
                    _UpdateRetryCount = value;
                }
            }

            /// <summary>
            /// 获取正在更新的资源组。
            /// </summary>
            public IResourceGroup UpdatingResourceGroup
            {
                get
                {
                    return _UpdatingResourceGroup;
                }
            }

            /// <summary>
            /// 获取等待更新资源数量。
            /// </summary>
            public int UpdateWaitingCount
            {
                get
                {
                    return _UpdateWaitingInfo.Count;
                }
            }

            /// <summary>
            /// 获取使用时下载的等待更新资源数量。
            /// </summary>
            public int UpdateWaitingWhilePlayingCount
            {
                get
                {
                    return _UpdateWaitingInfoWhilePlaying.Count;
                }
            }

            /// <summary>
            /// 获取候选更新资源数量。
            /// </summary>
            public int UpdateCandidateCount
            {
                get
                {
                    return _UpdateCandidateInfo.Count;
                }
            }

            /// <summary>
            /// 资源更新器轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (_ApplyingResourcePackStream != null)
                {
                    while (_ApplyWaitingInfo.Count > 0)
                    {
                        ApplyInfo applyInfo = _ApplyWaitingInfo.Dequeue();
                        if (ApplyResource(applyInfo))
                        {
                            return;
                        }
                    }

                    Array.Clear(_CachedBytes, 0, CachedBytesLength);
                    string resourcePackPath = _ApplyingResourcePackPath;
                    _ApplyingResourcePackPath = null;
                    _ApplyingResourcePackStream.Dispose();
                    _ApplyingResourcePackStream = null;
                    if (ResourceApplyComplete != null)
                    {
                        ResourceApplyComplete(resourcePackPath, !_FailureFlag);
                    }

                    if (_UpdateCandidateInfo.Count <= 0 && ResourceUpdateAllComplete != null)
                    {
                        ResourceUpdateAllComplete();
                    }

                    return;
                }

                if (_UpdateWaitingInfo.Count > 0)
                {
                    int freeCount = _DownloadManager.FreeAgentCount - _DownloadManager.WaitingTaskCount;
                    if (freeCount > 0)
                    {
                        for (int i = 0, count = 0; i < _UpdateWaitingInfo.Count && count < freeCount; i++)
                        {
                            if (DownloadResource(_UpdateWaitingInfo[i]))
                            {
                                count++;
                            }
                        }
                    }

                    return;
                }
            }

            /// <summary>
            /// 关闭并清理资源更新器。
            /// </summary>
            public void Shutdown()
            {
                if (_DownloadManager != null)
                {
                    _DownloadManager.DownloadStart -= OnDownloadStart;
                    _DownloadManager.DownloadUpdate -= OnDownloadUpdate;
                    _DownloadManager.DownloadSuccess -= OnDownloadSuccess;
                    _DownloadManager.DownloadFailure -= OnDownloadFailure;
                }

                _UpdateWaitingInfo.Clear();
                _UpdateCandidateInfo.Clear();
                _CachedFileSystemsForGenerateReadWriteVersionList.Clear();
            }

            /// <summary>
            /// 设置下载管理器。
            /// </summary>
            /// <param name="downloadManager">下载管理器。</param>
            public void SetDownloadManager(IDownloadManager downloadManager)
            {
                if (downloadManager == null)
                {
                    throw new GameFrameworkException("Download manager is invalid.");
                }

                _DownloadManager = downloadManager;
                _DownloadManager.DownloadStart += OnDownloadStart;
                _DownloadManager.DownloadUpdate += OnDownloadUpdate;
                _DownloadManager.DownloadSuccess += OnDownloadSuccess;
                _DownloadManager.DownloadFailure += OnDownloadFailure;
            }

            /// <summary>
            /// 增加资源更新。
            /// </summary>
            /// <param name="resourceName">资源名称。</param>
            /// <param name="fileSystemName">资源所在的文件系统名称。</param>
            /// <param name="loadType">资源加载方式。</param>
            /// <param name="length">资源大小。</param>
            /// <param name="hashCode">资源哈希值。</param>
            /// <param name="compressedLength">压缩后大小。</param>
            /// <param name="compressedHashCode">压缩后哈希值。</param>
            /// <param name="resourcePath">资源路径。</param>
            public void AddResourceUpdate(ResourceName resourceName, string fileSystemName, LoadType loadType, int length, int hashCode, int compressedLength, int compressedHashCode, string resourcePath)
            {
                _UpdateCandidateInfo.Add(resourceName, new UpdateInfo(resourceName, fileSystemName, loadType, length, hashCode, compressedLength, compressedHashCode, resourcePath));
            }

            /// <summary>
            /// 检查资源完成。
            /// </summary>
            /// <param name="needGenerateReadWriteVersionList">是否需要生成读写区版本资源列表。</param>
            public void CheckResourceComplete(bool needGenerateReadWriteVersionList)
            {
                _CheckResourcesComplete = true;
                if (needGenerateReadWriteVersionList)
                {
                    GenerateReadWriteVersionList();
                }
            }

            /// <summary>
            /// 应用指定资源包的资源。
            /// </summary>
            /// <param name="resourcePackPath">要应用的资源包路径。</param>
            public void ApplyResources(string resourcePackPath)
            {
                if (!_CheckResourcesComplete)
                {
                    throw new GameFrameworkException("You must check resources complete first.");
                }

                if (_ApplyingResourcePackStream != null)
                {
                    throw new GameFrameworkException(Utility.Text.Format("There is already a resource pack '{0}' being applied.", _ApplyingResourcePackPath));
                }

                if (_UpdatingResourceGroup != null)
                {
                    throw new GameFrameworkException(Utility.Text.Format("There is already a resource group '{0}' being updated.", _UpdatingResourceGroup.Name));
                }

                if (_UpdateWaitingInfoWhilePlaying.Count > 0)
                {
                    throw new GameFrameworkException("There are already some resources being updated while playing.");
                }

                try
                {
                    long length = 0L;
                    ResourcePackVersionList versionList = default(ResourcePackVersionList);
                    using (FileStream fileStream = new FileStream(resourcePackPath, FileMode.Open, FileAccess.Read))
                    {
                        length = fileStream.Length;
                        versionList = _ResourceManager._ResourcePackVersionListSerializer.Deserialize(fileStream);
                    }

                    if (!versionList.IsValid)
                    {
                        throw new GameFrameworkException("Deserialize resource pack version list failure.");
                    }

                    if (versionList.Offset + versionList.Length != length)
                    {
                        throw new GameFrameworkException("Resource pack length is invalid.");
                    }

                    _ApplyingResourcePackPath = resourcePackPath;
                    _ApplyingResourcePackStream = new FileStream(resourcePackPath, FileMode.Open, FileAccess.Read);
                    _ApplyingResourcePackStream.Position = versionList.Offset;
                    _FailureFlag = false;

                    long totalLength = 0L;
                    ResourcePackVersionList.Resource[] resources = versionList.GetResources();
                    foreach (ResourcePackVersionList.Resource resource in resources)
                    {
                        ResourceName resourceName = new ResourceName(resource.Name, resource.Variant, resource.Extension);
                        UpdateInfo updateInfo = null;
                        if (!_UpdateCandidateInfo.TryGetValue(resourceName, out updateInfo))
                        {
                            continue;
                        }

                        if (updateInfo.LoadType == (LoadType)resource.LoadType && updateInfo.Length == resource.Length && updateInfo.HashCode == resource.HashCode)
                        {
                            totalLength += resource.Length;
                            _ApplyWaitingInfo.Enqueue(new ApplyInfo(resourceName, updateInfo.FileSystemName, (LoadType)resource.LoadType, resource.Offset, resource.Length, resource.HashCode, resource.CompressedLength, resource.CompressedHashCode, updateInfo.ResourcePath));
                        }
                    }

                    if (ResourceApplyStart != null)
                    {
                        ResourceApplyStart(_ApplyingResourcePackPath, _ApplyWaitingInfo.Count, totalLength);
                    }
                }
                catch (Exception exception)
                {
                    if (_ApplyingResourcePackStream != null)
                    {
                        _ApplyingResourcePackStream.Dispose();
                        _ApplyingResourcePackStream = null;
                    }

                    throw new GameFrameworkException(Utility.Text.Format("Apply resources '{0}' with exception '{1}'.", resourcePackPath, exception), exception);
                }
            }

            /// <summary>
            /// 更新指定资源组的资源。
            /// </summary>
            /// <param name="resourceGroup">要更新的资源组。</param>
            public void UpdateResources(ResourceGroup resourceGroup)
            {
                if (_DownloadManager == null)
                {
                    throw new GameFrameworkException("You must set download manager first.");
                }

                if (!_CheckResourcesComplete)
                {
                    throw new GameFrameworkException("You must check resources complete first.");
                }

                if (_ApplyingResourcePackStream != null)
                {
                    throw new GameFrameworkException(Utility.Text.Format("There is already a resource pack '{0}' being applied.", _ApplyingResourcePackPath));
                }

                if (_UpdatingResourceGroup != null)
                {
                    throw new GameFrameworkException(Utility.Text.Format("There is already a resource group '{0}' being updated.", _UpdatingResourceGroup.Name));
                }

                if (string.IsNullOrEmpty(resourceGroup.Name))
                {
                    foreach (KeyValuePair<ResourceName, UpdateInfo> updateInfo in _UpdateCandidateInfo)
                    {
                        _UpdateWaitingInfo.Add(updateInfo.Value);
                    }
                }
                else
                {
                    resourceGroup.InternalGetResourceNames(_CachedResourceNames);
                    foreach (ResourceName resourceName in _CachedResourceNames)
                    {
                        UpdateInfo updateInfo = null;
                        if (!_UpdateCandidateInfo.TryGetValue(resourceName, out updateInfo))
                        {
                            continue;
                        }

                        _UpdateWaitingInfo.Add(updateInfo);
                    }

                    _CachedResourceNames.Clear();
                }

                _UpdatingResourceGroup = resourceGroup;
                _FailureFlag = false;
            }

            /// <summary>
            /// 停止更新资源。
            /// </summary>
            public void StopUpdateResources()
            {
                if (_DownloadManager == null)
                {
                    throw new GameFrameworkException("You must set download manager first.");
                }

                if (!_CheckResourcesComplete)
                {
                    throw new GameFrameworkException("You must check resources complete first.");
                }

                if (_ApplyingResourcePackStream != null)
                {
                    throw new GameFrameworkException(Utility.Text.Format("There is already a resource pack '{0}' being applied.", _ApplyingResourcePackPath));
                }

                if (_UpdatingResourceGroup == null)
                {
                    throw new GameFrameworkException("There is no resource group being updated.");
                }

                _UpdateWaitingInfo.Clear();
                _UpdatingResourceGroup = null;
            }

            /// <summary>
            /// 更新指定资源。
            /// </summary>
            /// <param name="resourceName">要更新的资源名称。</param>
            public void UpdateResource(ResourceName resourceName)
            {
                if (_DownloadManager == null)
                {
                    throw new GameFrameworkException("You must set download manager first.");
                }

                if (!_CheckResourcesComplete)
                {
                    throw new GameFrameworkException("You must check resources complete first.");
                }

                if (_ApplyingResourcePackStream != null)
                {
                    throw new GameFrameworkException(Utility.Text.Format("There is already a resource pack '{0}' being applied.", _ApplyingResourcePackPath));
                }

                UpdateInfo updateInfo = null;
                if (_UpdateCandidateInfo.TryGetValue(resourceName, out updateInfo) && _UpdateWaitingInfoWhilePlaying.Add(updateInfo))
                {
                    DownloadResource(updateInfo);
                }
            }

            private bool ApplyResource(ApplyInfo applyInfo)
            {
                long position = _ApplyingResourcePackStream.Position;
                try
                {
                    bool compressed = applyInfo.Length != applyInfo.CompressedLength || applyInfo.HashCode != applyInfo.CompressedHashCode;

                    int bytesRead = 0;
                    int bytesLeft = applyInfo.CompressedLength;
                    string directory = Path.GetDirectoryName(applyInfo.ResourcePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    _ApplyingResourcePackStream.Position += applyInfo.Offset;
                    using (FileStream fileStream = new FileStream(applyInfo.ResourcePath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        while ((bytesRead = _ApplyingResourcePackStream.Read(_CachedBytes, 0, bytesLeft < CachedBytesLength ? bytesLeft : CachedBytesLength)) > 0)
                        {
                            bytesLeft -= bytesRead;
                            fileStream.Write(_CachedBytes, 0, bytesRead);
                        }

                        if (compressed)
                        {
                            fileStream.Position = 0L;
                            int hashCode = Utility.Verifier.GetCrc32(fileStream);
                            if (hashCode != applyInfo.CompressedHashCode)
                            {
                                if (ResourceApplyFailure != null)
                                {
                                    string errorMessage = Utility.Text.Format("Resource compressed hash code error, need '{0}', applied '{1}'.", applyInfo.CompressedHashCode, hashCode);
                                    ResourceApplyFailure(applyInfo.ResourceName, _ApplyingResourcePackPath, errorMessage);
                                }

                                _FailureFlag = true;
                                return false;
                            }

                            fileStream.Position = 0L;
                            _ResourceManager.PrepareCachedStream();
                            if (!Utility.Compression.Decompress(fileStream, _ResourceManager._CachedStream))
                            {
                                if (ResourceApplyFailure != null)
                                {
                                    string errorMessage = Utility.Text.Format("Unable to decompress resource '{0}'.", applyInfo.ResourcePath);
                                    ResourceApplyFailure(applyInfo.ResourceName, _ApplyingResourcePackPath, errorMessage);
                                }

                                _FailureFlag = true;
                                return false;
                            }

                            fileStream.Position = 0L;
                            fileStream.SetLength(0L);
                            fileStream.Write(_ResourceManager._CachedStream.GetBuffer(), 0, (int)_ResourceManager._CachedStream.Length);
                        }
                        else
                        {
                            int hashCode = 0;
                            fileStream.Position = 0L;
                            if (applyInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || applyInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt
                                || applyInfo.LoadType == LoadType.LoadFromBinaryAndQuickDecrypt || applyInfo.LoadType == LoadType.LoadFromBinaryAndDecrypt)
                            {
                                Utility.Converter.GetBytes(applyInfo.HashCode, _CachedHashBytes);
                                if (applyInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || applyInfo.LoadType == LoadType.LoadFromBinaryAndQuickDecrypt)
                                {
                                    hashCode = Utility.Verifier.GetCrc32(fileStream, _CachedHashBytes, Utility.Encryption.QuickEncryptLength);
                                }
                                else if (applyInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt || applyInfo.LoadType == LoadType.LoadFromBinaryAndDecrypt)
                                {
                                    hashCode = Utility.Verifier.GetCrc32(fileStream, _CachedHashBytes, applyInfo.Length);
                                }

                                Array.Clear(_CachedHashBytes, 0, CachedHashBytesLength);
                            }
                            else
                            {
                                hashCode = Utility.Verifier.GetCrc32(fileStream);
                            }

                            if (hashCode != applyInfo.HashCode)
                            {
                                if (ResourceApplyFailure != null)
                                {
                                    string errorMessage = Utility.Text.Format("Resource hash code error, need '{0}', applied '{1}'.", applyInfo.HashCode, hashCode);
                                    ResourceApplyFailure(applyInfo.ResourceName, _ApplyingResourcePackPath, errorMessage);
                                }

                                _FailureFlag = true;
                                return false;
                            }
                        }
                    }

                    if (applyInfo.UseFileSystem)
                    {
                        IFileSystem fileSystem = _ResourceManager.GetFileSystem(applyInfo.FileSystemName, false);
                        bool retVal = fileSystem.WriteFile(applyInfo.ResourceName.FullName, applyInfo.ResourcePath);
                        if (File.Exists(applyInfo.ResourcePath))
                        {
                            File.Delete(applyInfo.ResourcePath);
                        }

                        if (!retVal)
                        {
                            if (ResourceApplyFailure != null)
                            {
                                string errorMessage = Utility.Text.Format("Unable to write resource '{0}' to file system '{1}'.", applyInfo.ResourcePath, applyInfo.FileSystemName);
                                ResourceApplyFailure(applyInfo.ResourceName, _ApplyingResourcePackPath, errorMessage);
                            }

                            _FailureFlag = true;
                            return false;
                        }
                    }

                    string downloadingResource = Utility.Text.Format("{0}.download", applyInfo.ResourcePath);
                    if (File.Exists(downloadingResource))
                    {
                        File.Delete(downloadingResource);
                    }

                    _UpdateCandidateInfo.Remove(applyInfo.ResourceName);
                    _ResourceManager._ResourceInfos[applyInfo.ResourceName].MarkReady();
                    _ResourceManager._ReadWriteResourceInfos.Add(applyInfo.ResourceName, new ReadWriteResourceInfo(applyInfo.FileSystemName, applyInfo.LoadType, applyInfo.Length, applyInfo.HashCode));
                    if (ResourceApplySuccess != null)
                    {
                        ResourceApplySuccess(applyInfo.ResourceName, applyInfo.ResourcePath, _ApplyingResourcePackPath, applyInfo.Length, applyInfo.CompressedLength);
                    }

                    _CurrentGenerateReadWriteVersionListLength += applyInfo.CompressedLength;
                    if (_ApplyWaitingInfo.Count <= 0 || _CurrentGenerateReadWriteVersionListLength >= _GenerateReadWriteVersionListLength)
                    {
                        GenerateReadWriteVersionList();
                        return true;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    if (ResourceApplyFailure != null)
                    {
                        ResourceApplyFailure(applyInfo.ResourceName, _ApplyingResourcePackPath, exception.ToString());
                    }

                    _FailureFlag = true;
                    return false;
                }
                finally
                {
                    _ApplyingResourcePackStream.Position = position;
                }
            }

            private bool DownloadResource(UpdateInfo updateInfo)
            {
                if (updateInfo.Downloading)
                {
                    return false;
                }

                updateInfo.Downloading = true;
                string resourceFullNameWithCrc32 = updateInfo.ResourceName.Variant != null ? Utility.Text.Format("{0}.{1}.{2:x8}.{3}", updateInfo.ResourceName.Name, updateInfo.ResourceName.Variant, updateInfo.HashCode, DefaultExtension) : Utility.Text.Format("{0}.{1:x8}.{2}", updateInfo.ResourceName.Name, updateInfo.HashCode, DefaultExtension);
                _DownloadManager.AddDownload(updateInfo.ResourcePath, Utility.Path.GetRemotePath(Path.Combine(_ResourceManager._UpdatePrefixUri, resourceFullNameWithCrc32)), updateInfo);
                return true;
            }

            private void GenerateReadWriteVersionList()
            {
                FileStream fileStream = null;
                try
                {
                    fileStream = new FileStream(_ReadWriteVersionListTempFileName, FileMode.Create, FileAccess.Write);
                    LocalVersionList.Resource[] resources = _ResourceManager._ReadWriteResourceInfos.Count > 0 ? new LocalVersionList.Resource[_ResourceManager._ReadWriteResourceInfos.Count] : null;
                    if (resources != null)
                    {
                        int index = 0;
                        foreach (KeyValuePair<ResourceName, ReadWriteResourceInfo> i in _ResourceManager._ReadWriteResourceInfos)
                        {
                            ResourceName resourceName = i.Key;
                            ReadWriteResourceInfo resourceInfo = i.Value;
                            resources[index] = new LocalVersionList.Resource(resourceName.Name, resourceName.Variant, resourceName.Extension, (byte)resourceInfo.LoadType, resourceInfo.Length, resourceInfo.HashCode);
                            if (resourceInfo.UseFileSystem)
                            {
                                List<int> resourceIndexes = null;
                                if (!_CachedFileSystemsForGenerateReadWriteVersionList.TryGetValue(resourceInfo.FileSystemName, out resourceIndexes))
                                {
                                    resourceIndexes = new List<int>();
                                    _CachedFileSystemsForGenerateReadWriteVersionList.Add(resourceInfo.FileSystemName, resourceIndexes);
                                }

                                resourceIndexes.Add(index);
                            }

                            index++;
                        }
                    }

                    LocalVersionList.FileSystem[] fileSystems = _CachedFileSystemsForGenerateReadWriteVersionList.Count > 0 ? new LocalVersionList.FileSystem[_CachedFileSystemsForGenerateReadWriteVersionList.Count] : null;
                    if (fileSystems != null)
                    {
                        int index = 0;
                        foreach (KeyValuePair<string, List<int>> i in _CachedFileSystemsForGenerateReadWriteVersionList)
                        {
                            fileSystems[index++] = new LocalVersionList.FileSystem(i.Key, i.Value.ToArray());
                            i.Value.Clear();
                        }
                    }

                    LocalVersionList versionList = new LocalVersionList(resources, fileSystems);
                    if (!_ResourceManager._ReadWriteVersionListSerializer.Serialize(fileStream, versionList))
                    {
                        throw new GameFrameworkException("Serialize read-write version list failure.");
                    }

                    if (fileStream != null)
                    {
                        fileStream.Dispose();
                        fileStream = null;
                    }
                }
                catch (Exception exception)
                {
                    if (fileStream != null)
                    {
                        fileStream.Dispose();
                        fileStream = null;
                    }

                    if (File.Exists(_ReadWriteVersionListTempFileName))
                    {
                        File.Delete(_ReadWriteVersionListTempFileName);
                    }

                    throw new GameFrameworkException(Utility.Text.Format("Generate read-write version list exception '{0}'.", exception), exception);
                }

                if (File.Exists(_ReadWriteVersionListFileName))
                {
                    File.Delete(_ReadWriteVersionListFileName);
                }

                File.Move(_ReadWriteVersionListTempFileName, _ReadWriteVersionListFileName);
                _CurrentGenerateReadWriteVersionListLength = 0;
            }

            private void OnDownloadStart(object sender, DownloadStartEventArgs e)
            {
                UpdateInfo updateInfo = e.UserData as UpdateInfo;
                if (updateInfo == null)
                {
                    return;
                }

                if (_DownloadManager == null)
                {
                    throw new GameFrameworkException("You must set download manager first.");
                }

                if (e.CurrentLength > int.MaxValue)
                {
                    throw new GameFrameworkException(Utility.Text.Format("File '{0}' is too large.", e.DownloadPath));
                }

                if (ResourceUpdateStart != null)
                {
                    ResourceUpdateStart(updateInfo.ResourceName, e.DownloadPath, e.DownloadUri, (int)e.CurrentLength, updateInfo.CompressedLength, updateInfo.RetryCount);
                }
            }

            private void OnDownloadUpdate(object sender, DownloadUpdateEventArgs e)
            {
                UpdateInfo updateInfo = e.UserData as UpdateInfo;
                if (updateInfo == null)
                {
                    return;
                }

                if (_DownloadManager == null)
                {
                    throw new GameFrameworkException("You must set download manager first.");
                }

                if (e.CurrentLength > updateInfo.CompressedLength)
                {
                    _DownloadManager.RemoveDownload(e.SerialId);
                    string downloadFile = Utility.Text.Format("{0}.download", e.DownloadPath);
                    if (File.Exists(downloadFile))
                    {
                        File.Delete(downloadFile);
                    }

                    string errorMessage = Utility.Text.Format("When download update, downloaded length is larger than compressed length, need '{0}', downloaded '{1}'.", updateInfo.CompressedLength, e.CurrentLength);
                    DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                    OnDownloadFailure(this, downloadFailureEventArgs);
                    ReferencePool.Release(downloadFailureEventArgs);
                    return;
                }

                if (ResourceUpdateChanged != null)
                {
                    ResourceUpdateChanged(updateInfo.ResourceName, e.DownloadPath, e.DownloadUri, (int)e.CurrentLength, updateInfo.CompressedLength);
                }
            }

            private void OnDownloadSuccess(object sender, DownloadSuccessEventArgs e)
            {
                UpdateInfo updateInfo = e.UserData as UpdateInfo;
                if (updateInfo == null)
                {
                    return;
                }

                try
                {
                    using (FileStream fileStream = new FileStream(e.DownloadPath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        bool compressed = updateInfo.Length != updateInfo.CompressedLength || updateInfo.HashCode != updateInfo.CompressedHashCode;

                        int length = (int)fileStream.Length;
                        if (length != updateInfo.CompressedLength)
                        {
                            fileStream.Close();
                            string errorMessage = Utility.Text.Format("Resource compressed length error, need '{0}', downloaded '{1}'.", updateInfo.CompressedLength, length);
                            DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                            OnDownloadFailure(this, downloadFailureEventArgs);
                            ReferencePool.Release(downloadFailureEventArgs);
                            return;
                        }

                        if (compressed)
                        {
                            fileStream.Position = 0L;
                            int hashCode = Utility.Verifier.GetCrc32(fileStream);
                            if (hashCode != updateInfo.CompressedHashCode)
                            {
                                fileStream.Close();
                                string errorMessage = Utility.Text.Format("Resource compressed hash code error, need '{0}', downloaded '{1}'.", updateInfo.CompressedHashCode, hashCode);
                                DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                                OnDownloadFailure(this, downloadFailureEventArgs);
                                ReferencePool.Release(downloadFailureEventArgs);
                                return;
                            }

                            fileStream.Position = 0L;
                            _ResourceManager.PrepareCachedStream();
                            if (!Utility.Compression.Decompress(fileStream, _ResourceManager._CachedStream))
                            {
                                fileStream.Close();
                                string errorMessage = Utility.Text.Format("Unable to decompress resource '{0}'.", e.DownloadPath);
                                DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                                OnDownloadFailure(this, downloadFailureEventArgs);
                                ReferencePool.Release(downloadFailureEventArgs);
                                return;
                            }

                            int uncompressedLength = (int)_ResourceManager._CachedStream.Length;
                            if (uncompressedLength != updateInfo.Length)
                            {
                                fileStream.Close();
                                string errorMessage = Utility.Text.Format("Resource length error, need '{0}', downloaded '{1}'.", updateInfo.Length, uncompressedLength);
                                DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                                OnDownloadFailure(this, downloadFailureEventArgs);
                                ReferencePool.Release(downloadFailureEventArgs);
                                return;
                            }

                            fileStream.Position = 0L;
                            fileStream.SetLength(0L);
                            fileStream.Write(_ResourceManager._CachedStream.GetBuffer(), 0, uncompressedLength);
                        }
                        else
                        {
                            int hashCode = 0;
                            fileStream.Position = 0L;
                            if (updateInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || updateInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt
                                || updateInfo.LoadType == LoadType.LoadFromBinaryAndQuickDecrypt || updateInfo.LoadType == LoadType.LoadFromBinaryAndDecrypt)
                            {
                                Utility.Converter.GetBytes(updateInfo.HashCode, _CachedHashBytes);
                                if (updateInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || updateInfo.LoadType == LoadType.LoadFromBinaryAndQuickDecrypt)
                                {
                                    hashCode = Utility.Verifier.GetCrc32(fileStream, _CachedHashBytes, Utility.Encryption.QuickEncryptLength);
                                }
                                else if (updateInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt || updateInfo.LoadType == LoadType.LoadFromBinaryAndDecrypt)
                                {
                                    hashCode = Utility.Verifier.GetCrc32(fileStream, _CachedHashBytes, length);
                                }

                                Array.Clear(_CachedHashBytes, 0, CachedHashBytesLength);
                            }
                            else
                            {
                                hashCode = Utility.Verifier.GetCrc32(fileStream);
                            }

                            if (hashCode != updateInfo.HashCode)
                            {
                                fileStream.Close();
                                string errorMessage = Utility.Text.Format("Resource hash code error, need '{0}', downloaded '{1}'.", updateInfo.HashCode, hashCode);
                                DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                                OnDownloadFailure(this, downloadFailureEventArgs);
                                ReferencePool.Release(downloadFailureEventArgs);
                                return;
                            }
                        }
                    }

                    if (updateInfo.UseFileSystem)
                    {
                        IFileSystem fileSystem = _ResourceManager.GetFileSystem(updateInfo.FileSystemName, false);
                        bool retVal = fileSystem.WriteFile(updateInfo.ResourceName.FullName, updateInfo.ResourcePath);
                        if (File.Exists(updateInfo.ResourcePath))
                        {
                            File.Delete(updateInfo.ResourcePath);
                        }

                        if (!retVal)
                        {
                            string errorMessage = Utility.Text.Format("Write resource to file system '{0}' error.", fileSystem.FullPath);
                            DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                            OnDownloadFailure(this, downloadFailureEventArgs);
                            ReferencePool.Release(downloadFailureEventArgs);
                            return;
                        }
                    }

                    _UpdateCandidateInfo.Remove(updateInfo.ResourceName);
                    _UpdateWaitingInfo.Remove(updateInfo);
                    _UpdateWaitingInfoWhilePlaying.Remove(updateInfo);
                    _ResourceManager._ResourceInfos[updateInfo.ResourceName].MarkReady();
                    _ResourceManager._ReadWriteResourceInfos.Add(updateInfo.ResourceName, new ReadWriteResourceInfo(updateInfo.FileSystemName, updateInfo.LoadType, updateInfo.Length, updateInfo.HashCode));
                    if (ResourceUpdateSuccess != null)
                    {
                        ResourceUpdateSuccess(updateInfo.ResourceName, e.DownloadPath, e.DownloadUri, updateInfo.Length, updateInfo.CompressedLength);
                    }

                    _CurrentGenerateReadWriteVersionListLength += updateInfo.CompressedLength;
                    if (_UpdateCandidateInfo.Count <= 0 || _UpdateWaitingInfo.Count + _UpdateWaitingInfoWhilePlaying.Count <= 0 || _CurrentGenerateReadWriteVersionListLength >= _GenerateReadWriteVersionListLength)
                    {
                        GenerateReadWriteVersionList();
                    }

                    if (_UpdatingResourceGroup != null && _UpdateWaitingInfo.Count <= 0)
                    {
                        ResourceGroup updatingResourceGroup = _UpdatingResourceGroup;
                        _UpdatingResourceGroup = null;
                        if (ResourceUpdateComplete != null)
                        {
                            ResourceUpdateComplete(updatingResourceGroup, !_FailureFlag);
                        }
                    }

                    if (_UpdateCandidateInfo.Count <= 0 && ResourceUpdateAllComplete != null)
                    {
                        ResourceUpdateAllComplete();
                    }
                }
                catch (Exception exception)
                {
                    string errorMessage = Utility.Text.Format("Update resource '{0}' with error message '{1}'.", e.DownloadPath, exception);
                    DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                    OnDownloadFailure(this, downloadFailureEventArgs);
                    ReferencePool.Release(downloadFailureEventArgs);
                }
            }

            private void OnDownloadFailure(object sender, DownloadFailureEventArgs e)
            {
                UpdateInfo updateInfo = e.UserData as UpdateInfo;
                if (updateInfo == null)
                {
                    return;
                }

                if (File.Exists(e.DownloadPath))
                {
                    File.Delete(e.DownloadPath);
                }

                if (ResourceUpdateFailure != null)
                {
                    ResourceUpdateFailure(updateInfo.ResourceName, e.DownloadUri, updateInfo.RetryCount, _UpdateRetryCount, e.ErrorMessage);
                }

                if (updateInfo.RetryCount < _UpdateRetryCount)
                {
                    updateInfo.Downloading = false;
                    updateInfo.RetryCount++;
                    if (_UpdateWaitingInfoWhilePlaying.Contains(updateInfo))
                    {
                        DownloadResource(updateInfo);
                    }
                }
                else
                {
                    _FailureFlag = true;
                    updateInfo.Downloading = false;
                    updateInfo.RetryCount = 0;
                    _UpdateWaitingInfo.Remove(updateInfo);
                    _UpdateWaitingInfoWhilePlaying.Remove(updateInfo);
                }
            }
        }
    }
}
