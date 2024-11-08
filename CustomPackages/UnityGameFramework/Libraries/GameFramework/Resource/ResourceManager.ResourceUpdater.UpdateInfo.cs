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
        private sealed partial class ResourceUpdater
        {
            /// <summary>
            /// 资源更新信息。
            /// </summary>
            private sealed class UpdateInfo
            {
                private readonly ResourceName _ResourceName;
                private readonly string _FileSystemName;
                private readonly LoadType _LoadType;
                private readonly int _Length;
                private readonly int _HashCode;
                private readonly int _CompressedLength;
                private readonly int _CompressedHashCode;
                private readonly string _ResourcePath;
                private bool _Downloading;
                private int _RetryCount;

                /// <summary>
                /// 初始化资源更新信息的新实例。
                /// </summary>
                /// <param name="resourceName">资源名称。</param>
                /// <param name="fileSystemName">资源所在的文件系统名称。</param>
                /// <param name="loadType">资源加载方式。</param>
                /// <param name="length">资源大小。</param>
                /// <param name="hashCode">资源哈希值。</param>
                /// <param name="compressedLength">压缩后大小。</param>
                /// <param name="compressedHashCode">压缩后哈希值。</param>
                /// <param name="resourcePath">资源路径。</param>
                public UpdateInfo(ResourceName resourceName, string fileSystemName, LoadType loadType, int length, int hashCode, int compressedLength, int compressedHashCode, string resourcePath)
                {
                    _ResourceName = resourceName;
                    _FileSystemName = fileSystemName;
                    _LoadType = loadType;
                    _Length = length;
                    _HashCode = hashCode;
                    _CompressedLength = compressedLength;
                    _CompressedHashCode = compressedHashCode;
                    _ResourcePath = resourcePath;
                    _Downloading = false;
                    _RetryCount = 0;
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
                /// 获取资源是否使用文件系统。
                /// </summary>
                public bool UseFileSystem
                {
                    get
                    {
                        return !string.IsNullOrEmpty(_FileSystemName);
                    }
                }

                /// <summary>
                /// 获取资源所在的文件系统名称。
                /// </summary>
                public string FileSystemName
                {
                    get
                    {
                        return _FileSystemName;
                    }
                }

                /// <summary>
                /// 获取资源加载方式。
                /// </summary>
                public LoadType LoadType
                {
                    get
                    {
                        return _LoadType;
                    }
                }

                /// <summary>
                /// 获取资源大小。
                /// </summary>
                public int Length
                {
                    get
                    {
                        return _Length;
                    }
                }

                /// <summary>
                /// 获取资源哈希值。
                /// </summary>
                public int HashCode
                {
                    get
                    {
                        return _HashCode;
                    }
                }

                /// <summary>
                /// 获取压缩后大小。
                /// </summary>
                public int CompressedLength
                {
                    get
                    {
                        return _CompressedLength;
                    }
                }

                /// <summary>
                /// 获取压缩后哈希值。
                /// </summary>
                public int CompressedHashCode
                {
                    get
                    {
                        return _CompressedHashCode;
                    }
                }

                /// <summary>
                /// 获取资源路径。
                /// </summary>
                public string ResourcePath
                {
                    get
                    {
                        return _ResourcePath;
                    }
                }

                /// <summary>
                /// 获取或设置下载状态。
                /// </summary>
                public bool Downloading
                {
                    get
                    {
                        return _Downloading;
                    }
                    set
                    {
                        _Downloading = value;
                    }
                }

                /// <summary>
                /// 获取或设置已重试次数。
                /// </summary>
                public int RetryCount
                {
                    get
                    {
                        return _RetryCount;
                    }
                    set
                    {
                        _RetryCount = value;
                    }
                }
            }
        }
    }
}
