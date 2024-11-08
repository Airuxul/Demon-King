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
        private sealed partial class ResourceChecker
        {
            /// <summary>
            /// 资源检查信息。
            /// </summary>
            private sealed partial class CheckInfo
            {
                private readonly ResourceName _ResourceName;
                private CheckStatus _Status;
                private bool _NeedRemove;
                private bool _NeedMoveToDisk;
                private bool _NeedMoveToFileSystem;
                private RemoteVersionInfo _VersionInfo;
                private LocalVersionInfo _ReadOnlyInfo;
                private LocalVersionInfo _ReadWriteInfo;
                private string _CachedFileSystemName;

                /// <summary>
                /// 初始化资源检查信息的新实例。
                /// </summary>
                /// <param name="resourceName">资源名称。</param>
                public CheckInfo(ResourceName resourceName)
                {
                    _ResourceName = resourceName;
                    _Status = CheckStatus.Unknown;
                    _NeedRemove = false;
                    _NeedMoveToDisk = false;
                    _NeedMoveToFileSystem = false;
                    _VersionInfo = default(RemoteVersionInfo);
                    _ReadOnlyInfo = default(LocalVersionInfo);
                    _ReadWriteInfo = default(LocalVersionInfo);
                    _CachedFileSystemName = null;
                }

                /// <summary>
                /// 获取资源名称。
                /// </summary>
                public ResourceName ResourceName
                {
                    get
                    {
                        return _ResourceName;
                    }
                }

                /// <summary>
                /// 获取资源检查状态。
                /// </summary>
                public CheckStatus Status
                {
                    get
                    {
                        return _Status;
                    }
                }

                /// <summary>
                /// 获取是否需要移除读写区的资源。
                /// </summary>
                public bool NeedRemove
                {
                    get
                    {
                        return _NeedRemove;
                    }
                }

                /// <summary>
                /// 获取是否需要将读写区的资源移动到磁盘。
                /// </summary>
                public bool NeedMoveToDisk
                {
                    get
                    {
                        return _NeedMoveToDisk;
                    }
                }

                /// <summary>
                /// 获取是否需要将读写区的资源移动到文件系统。
                /// </summary>
                public bool NeedMoveToFileSystem
                {
                    get
                    {
                        return _NeedMoveToFileSystem;
                    }
                }

                /// <summary>
                /// 获取资源所在的文件系统名称。
                /// </summary>
                public string FileSystemName
                {
                    get
                    {
                        return _VersionInfo.FileSystemName;
                    }
                }

                /// <summary>
                /// 获取资源是否使用文件系统。
                /// </summary>
                public bool ReadWriteUseFileSystem
                {
                    get
                    {
                        return _ReadWriteInfo.UseFileSystem;
                    }
                }

                /// <summary>
                /// 获取读写资源所在的文件系统名称。
                /// </summary>
                public string ReadWriteFileSystemName
                {
                    get
                    {
                        return _ReadWriteInfo.FileSystemName;
                    }
                }

                /// <summary>
                /// 获取资源加载方式。
                /// </summary>
                public LoadType LoadType
                {
                    get
                    {
                        return _VersionInfo.LoadType;
                    }
                }

                /// <summary>
                /// 获取资源大小。
                /// </summary>
                public int Length
                {
                    get
                    {
                        return _VersionInfo.Length;
                    }
                }

                /// <summary>
                /// 获取资源哈希值。
                /// </summary>
                public int HashCode
                {
                    get
                    {
                        return _VersionInfo.HashCode;
                    }
                }

                /// <summary>
                /// 获取压缩后大小。
                /// </summary>
                public int CompressedLength
                {
                    get
                    {
                        return _VersionInfo.CompressedLength;
                    }
                }

                /// <summary>
                /// 获取压缩后哈希值。
                /// </summary>
                public int CompressedHashCode
                {
                    get
                    {
                        return _VersionInfo.CompressedHashCode;
                    }
                }

                /// <summary>
                /// 临时缓存资源所在的文件系统名称。
                /// </summary>
                /// <param name="fileSystemName">资源所在的文件系统名称。</param>
                public void SetCachedFileSystemName(string fileSystemName)
                {
                    _CachedFileSystemName = fileSystemName;
                }

                /// <summary>
                /// 设置资源在版本中的信息。
                /// </summary>
                /// <param name="loadType">资源加载方式。</param>
                /// <param name="length">资源大小。</param>
                /// <param name="hashCode">资源哈希值。</param>
                /// <param name="compressedLength">压缩后大小。</param>
                /// <param name="compressedHashCode">压缩后哈希值。</param>
                public void SetVersionInfo(LoadType loadType, int length, int hashCode, int compressedLength, int compressedHashCode)
                {
                    if (_VersionInfo.Exist)
                    {
                        throw new GameFrameworkException(Utility.Text.Format("You must set version info of '{0}' only once.", _ResourceName.FullName));
                    }

                    _VersionInfo = new RemoteVersionInfo(_CachedFileSystemName, loadType, length, hashCode, compressedLength, compressedHashCode);
                    _CachedFileSystemName = null;
                }

                /// <summary>
                /// 设置资源在只读区中的信息。
                /// </summary>
                /// <param name="loadType">资源加载方式。</param>
                /// <param name="length">资源大小。</param>
                /// <param name="hashCode">资源哈希值。</param>
                public void SetReadOnlyInfo(LoadType loadType, int length, int hashCode)
                {
                    if (_ReadOnlyInfo.Exist)
                    {
                        throw new GameFrameworkException(Utility.Text.Format("You must set read-only info of '{0}' only once.", _ResourceName.FullName));
                    }

                    _ReadOnlyInfo = new LocalVersionInfo(_CachedFileSystemName, loadType, length, hashCode);
                    _CachedFileSystemName = null;
                }

                /// <summary>
                /// 设置资源在读写区中的信息。
                /// </summary>
                /// <param name="loadType">资源加载方式。</param>
                /// <param name="length">资源大小。</param>
                /// <param name="hashCode">资源哈希值。</param>
                public void SetReadWriteInfo(LoadType loadType, int length, int hashCode)
                {
                    if (_ReadWriteInfo.Exist)
                    {
                        throw new GameFrameworkException(Utility.Text.Format("You must set read-write info of '{0}' only once.", _ResourceName.FullName));
                    }

                    _ReadWriteInfo = new LocalVersionInfo(_CachedFileSystemName, loadType, length, hashCode);
                    _CachedFileSystemName = null;
                }

                /// <summary>
                /// 刷新资源信息状态。
                /// </summary>
                /// <param name="currentVariant">当前变体。</param>
                /// <param name="ignoreOtherVariant">是否忽略处理其它变体的资源，若不忽略则移除。</param>
                public void RefreshStatus(string currentVariant, bool ignoreOtherVariant)
                {
                    if (!_VersionInfo.Exist)
                    {
                        _Status = CheckStatus.Disuse;
                        _NeedRemove = _ReadWriteInfo.Exist;
                        return;
                    }

                    if (_ResourceName.Variant == null || _ResourceName.Variant == currentVariant)
                    {
                        if (_ReadOnlyInfo.Exist && _ReadOnlyInfo.FileSystemName == _VersionInfo.FileSystemName && _ReadOnlyInfo.LoadType == _VersionInfo.LoadType && _ReadOnlyInfo.Length == _VersionInfo.Length && _ReadOnlyInfo.HashCode == _VersionInfo.HashCode)
                        {
                            _Status = CheckStatus.StorageInReadOnly;
                            _NeedRemove = _ReadWriteInfo.Exist;
                        }
                        else if (_ReadWriteInfo.Exist && _ReadWriteInfo.LoadType == _VersionInfo.LoadType && _ReadWriteInfo.Length == _VersionInfo.Length && _ReadWriteInfo.HashCode == _VersionInfo.HashCode)
                        {
                            bool differentFileSystem = _ReadWriteInfo.FileSystemName != _VersionInfo.FileSystemName;
                            _Status = CheckStatus.StorageInReadWrite;
                            _NeedMoveToDisk = _ReadWriteInfo.UseFileSystem && differentFileSystem;
                            _NeedMoveToFileSystem = _VersionInfo.UseFileSystem && differentFileSystem;
                        }
                        else
                        {
                            _Status = CheckStatus.Update;
                            _NeedRemove = _ReadWriteInfo.Exist;
                        }
                    }
                    else
                    {
                        _Status = CheckStatus.Unavailable;
                        _NeedRemove = !ignoreOtherVariant && _ReadWriteInfo.Exist;
                    }
                }
            }
        }
    }
}
