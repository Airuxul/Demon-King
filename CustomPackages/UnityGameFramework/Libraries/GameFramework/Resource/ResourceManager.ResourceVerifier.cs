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
        /// 资源校验器。
        /// </summary>
        private sealed partial class ResourceVerifier
        {
            private const int CachedHashBytesLength = 4;

            private readonly ResourceManager _ResourceManager;
            private readonly List<VerifyInfo> _VerifyInfos;
            private readonly byte[] _CachedHashBytes;
            private bool _LoadReadWriteVersionListComplete;
            private int _VerifyResourceLengthPerFrame;
            private int _VerifyResourceIndex;
            private bool _FailureFlag;

            public GameFrameworkAction<int, long> ResourceVerifyStart;
            public GameFrameworkAction<ResourceName, int> ResourceVerifySuccess;
            public GameFrameworkAction<ResourceName> ResourceVerifyFailure;
            public GameFrameworkAction<bool> ResourceVerifyComplete;

            /// <summary>
            /// 初始化资源校验器的新实例。
            /// </summary>
            /// <param name="resourceManager">资源管理器。</param>
            public ResourceVerifier(ResourceManager resourceManager)
            {
                _ResourceManager = resourceManager;
                _VerifyInfos = new List<VerifyInfo>();
                _CachedHashBytes = new byte[CachedHashBytesLength];
                _LoadReadWriteVersionListComplete = false;
                _VerifyResourceLengthPerFrame = 0;
                _VerifyResourceIndex = 0;
                _FailureFlag = false;

                ResourceVerifyStart = null;
                ResourceVerifySuccess = null;
                ResourceVerifyFailure = null;
                ResourceVerifyComplete = null;
            }

            /// <summary>
            /// 资源校验器轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (!_LoadReadWriteVersionListComplete)
                {
                    return;
                }

                int length = 0;
                while (_VerifyResourceIndex < _VerifyInfos.Count)
                {
                    VerifyInfo verifyInfo = _VerifyInfos[_VerifyResourceIndex];
                    length += verifyInfo.Length;
                    if (VerifyResource(verifyInfo))
                    {
                        _VerifyResourceIndex++;
                        if (ResourceVerifySuccess != null)
                        {
                            ResourceVerifySuccess(verifyInfo.ResourceName, verifyInfo.Length);
                        }
                    }
                    else
                    {
                        _FailureFlag = true;
                        _VerifyInfos.RemoveAt(_VerifyResourceIndex);
                        if (ResourceVerifyFailure != null)
                        {
                            ResourceVerifyFailure(verifyInfo.ResourceName);
                        }
                    }

                    if (length >= _VerifyResourceLengthPerFrame)
                    {
                        return;
                    }
                }

                _LoadReadWriteVersionListComplete = false;
                if (_FailureFlag)
                {
                    GenerateReadWriteVersionList();
                }

                if (ResourceVerifyComplete != null)
                {
                    ResourceVerifyComplete(!_FailureFlag);
                }
            }

            /// <summary>
            /// 关闭并清理资源校验器。
            /// </summary>
            public void Shutdown()
            {
                _VerifyInfos.Clear();
                _LoadReadWriteVersionListComplete = false;
                _VerifyResourceLengthPerFrame = 0;
                _VerifyResourceIndex = 0;
                _FailureFlag = false;
            }

            /// <summary>
            /// 校验资源。
            /// </summary>
            /// <param name="verifyResourceLengthPerFrame">每帧至少校验资源的大小，以字节为单位。</param>
            public void VerifyResources(int verifyResourceLengthPerFrame)
            {
                if (verifyResourceLengthPerFrame < 0)
                {
                    throw new GameFrameworkException("Verify resource count per frame is invalid.");
                }

                if (_ResourceManager._ResourceHelper == null)
                {
                    throw new GameFrameworkException("Resource helper is invalid.");
                }

                if (string.IsNullOrEmpty(_ResourceManager._ReadWritePath))
                {
                    throw new GameFrameworkException("Read-write path is invalid.");
                }

                _VerifyResourceLengthPerFrame = verifyResourceLengthPerFrame;
                _ResourceManager._ResourceHelper.LoadBytes(Utility.Path.GetRemotePath(Path.Combine(_ResourceManager._ReadWritePath, LocalVersionListFileName)), new LoadBytesCallbacks(OnLoadReadWriteVersionListSuccess, OnLoadReadWriteVersionListFailure), null);
            }

            private bool VerifyResource(VerifyInfo verifyInfo)
            {
                if (verifyInfo.UseFileSystem)
                {
                    IFileSystem fileSystem = _ResourceManager.GetFileSystem(verifyInfo.FileSystemName, false);
                    string fileName = verifyInfo.ResourceName.FullName;
                    FileSystem.FileInfo fileInfo = fileSystem.GetFileInfo(fileName);
                    if (!fileInfo.IsValid)
                    {
                        return false;
                    }

                    int length = fileInfo.Length;
                    if (length == verifyInfo.Length)
                    {
                        _ResourceManager.PrepareCachedStream();
                        fileSystem.ReadFile(fileName, _ResourceManager._CachedStream);
                        _ResourceManager._CachedStream.Position = 0L;
                        int hashCode = 0;
                        if (verifyInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || verifyInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt
                            || verifyInfo.LoadType == LoadType.LoadFromBinaryAndQuickDecrypt || verifyInfo.LoadType == LoadType.LoadFromBinaryAndDecrypt)
                        {
                            Utility.Converter.GetBytes(verifyInfo.HashCode, _CachedHashBytes);
                            if (verifyInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || verifyInfo.LoadType == LoadType.LoadFromBinaryAndQuickDecrypt)
                            {
                                hashCode = Utility.Verifier.GetCrc32(_ResourceManager._CachedStream, _CachedHashBytes, Utility.Encryption.QuickEncryptLength);
                            }
                            else if (verifyInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt || verifyInfo.LoadType == LoadType.LoadFromBinaryAndDecrypt)
                            {
                                hashCode = Utility.Verifier.GetCrc32(_ResourceManager._CachedStream, _CachedHashBytes, length);
                            }

                            Array.Clear(_CachedHashBytes, 0, CachedHashBytesLength);
                        }
                        else
                        {
                            hashCode = Utility.Verifier.GetCrc32(_ResourceManager._CachedStream);
                        }

                        if (hashCode == verifyInfo.HashCode)
                        {
                            return true;
                        }
                    }

                    fileSystem.DeleteFile(fileName);
                    return false;
                }
                else
                {
                    string resourcePath = Utility.Path.GetRegularPath(Path.Combine(_ResourceManager.ReadWritePath, verifyInfo.ResourceName.FullName));
                    if (!File.Exists(resourcePath))
                    {
                        return false;
                    }

                    using (FileStream fileStream = new FileStream(resourcePath, FileMode.Open, FileAccess.Read))
                    {
                        int length = (int)fileStream.Length;
                        if (length == verifyInfo.Length)
                        {
                            int hashCode = 0;
                            if (verifyInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || verifyInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt
                                || verifyInfo.LoadType == LoadType.LoadFromBinaryAndQuickDecrypt || verifyInfo.LoadType == LoadType.LoadFromBinaryAndDecrypt)
                            {
                                Utility.Converter.GetBytes(verifyInfo.HashCode, _CachedHashBytes);
                                if (verifyInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || verifyInfo.LoadType == LoadType.LoadFromBinaryAndQuickDecrypt)
                                {
                                    hashCode = Utility.Verifier.GetCrc32(fileStream, _CachedHashBytes, Utility.Encryption.QuickEncryptLength);
                                }
                                else if (verifyInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt || verifyInfo.LoadType == LoadType.LoadFromBinaryAndDecrypt)
                                {
                                    hashCode = Utility.Verifier.GetCrc32(fileStream, _CachedHashBytes, length);
                                }

                                Array.Clear(_CachedHashBytes, 0, CachedHashBytesLength);
                            }
                            else
                            {
                                hashCode = Utility.Verifier.GetCrc32(fileStream);
                            }

                            if (hashCode == verifyInfo.HashCode)
                            {
                                return true;
                            }
                        }
                    }

                    File.Delete(resourcePath);
                    return false;
                }
            }

            private void GenerateReadWriteVersionList()
            {
                string readWriteVersionListFileName = Utility.Path.GetRegularPath(Path.Combine(_ResourceManager._ReadWritePath, LocalVersionListFileName));
                string readWriteVersionListTempFileName = Utility.Text.Format("{0}.{1}", readWriteVersionListFileName, TempExtension);
                SortedDictionary<string, List<int>> cachedFileSystemsForGenerateReadWriteVersionList = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);
                FileStream fileStream = null;
                try
                {
                    fileStream = new FileStream(readWriteVersionListTempFileName, FileMode.Create, FileAccess.Write);
                    LocalVersionList.Resource[] resources = _VerifyInfos.Count > 0 ? new LocalVersionList.Resource[_VerifyInfos.Count] : null;
                    if (resources != null)
                    {
                        int index = 0;
                        foreach (VerifyInfo i in _VerifyInfos)
                        {
                            resources[index] = new LocalVersionList.Resource(i.ResourceName.Name, i.ResourceName.Variant, i.ResourceName.Extension, (byte)i.LoadType, i.Length, i.HashCode);
                            if (i.UseFileSystem)
                            {
                                List<int> resourceIndexes = null;
                                if (!cachedFileSystemsForGenerateReadWriteVersionList.TryGetValue(i.FileSystemName, out resourceIndexes))
                                {
                                    resourceIndexes = new List<int>();
                                    cachedFileSystemsForGenerateReadWriteVersionList.Add(i.FileSystemName, resourceIndexes);
                                }

                                resourceIndexes.Add(index);
                            }

                            index++;
                        }
                    }

                    LocalVersionList.FileSystem[] fileSystems = cachedFileSystemsForGenerateReadWriteVersionList.Count > 0 ? new LocalVersionList.FileSystem[cachedFileSystemsForGenerateReadWriteVersionList.Count] : null;
                    if (fileSystems != null)
                    {
                        int index = 0;
                        foreach (KeyValuePair<string, List<int>> i in cachedFileSystemsForGenerateReadWriteVersionList)
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

                    if (File.Exists(readWriteVersionListTempFileName))
                    {
                        File.Delete(readWriteVersionListTempFileName);
                    }

                    throw new GameFrameworkException(Utility.Text.Format("Generate read-write version list exception '{0}'.", exception), exception);
                }

                if (File.Exists(readWriteVersionListFileName))
                {
                    File.Delete(readWriteVersionListFileName);
                }

                File.Move(readWriteVersionListTempFileName, readWriteVersionListFileName);
            }

            private void OnLoadReadWriteVersionListSuccess(string fileUri, byte[] bytes, float duration, object userData)
            {
                MemoryStream memoryStream = null;
                try
                {
                    memoryStream = new MemoryStream(bytes, false);
                    LocalVersionList versionList = _ResourceManager._ReadWriteVersionListSerializer.Deserialize(memoryStream);
                    if (!versionList.IsValid)
                    {
                        throw new GameFrameworkException("Deserialize read write version list failure.");
                    }

                    LocalVersionList.Resource[] resources = versionList.GetResources();
                    LocalVersionList.FileSystem[] fileSystems = versionList.GetFileSystems();
                    Dictionary<ResourceName, string> resourceInFileSystemNames = new Dictionary<ResourceName, string>();
                    foreach (LocalVersionList.FileSystem fileSystem in fileSystems)
                    {
                        int[] resourceIndexes = fileSystem.GetResourceIndexes();
                        foreach (int resourceIndex in resourceIndexes)
                        {
                            LocalVersionList.Resource resource = resources[resourceIndex];
                            resourceInFileSystemNames.Add(new ResourceName(resource.Name, resource.Variant, resource.Extension), fileSystem.Name);
                        }
                    }

                    long totalLength = 0L;
                    foreach (LocalVersionList.Resource resource in resources)
                    {
                        ResourceName resourceName = new ResourceName(resource.Name, resource.Variant, resource.Extension);
                        string fileSystemName = null;
                        resourceInFileSystemNames.TryGetValue(resourceName, out fileSystemName);
                        totalLength += resource.Length;
                        _VerifyInfos.Add(new VerifyInfo(resourceName, fileSystemName, (LoadType)resource.LoadType, resource.Length, resource.HashCode));
                    }

                    _LoadReadWriteVersionListComplete = true;
                    if (ResourceVerifyStart != null)
                    {
                        ResourceVerifyStart(_VerifyInfos.Count, totalLength);
                    }
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
                if (ResourceVerifyComplete != null)
                {
                    ResourceVerifyComplete(true);
                }
            }
        }
    }
}
