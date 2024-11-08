//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceUpdater
        {
            /// <summary>
            /// 资源应用信息。
            /// </summary>
            [StructLayout(LayoutKind.Auto)]
            private struct ApplyInfo
            {
                private readonly ResourceName _ResourceName;
                private readonly string _FileSystemName;
                private readonly LoadType _LoadType;
                private readonly long _Offset;
                private readonly int _Length;
                private readonly int _HashCode;
                private readonly int _CompressedLength;
                private readonly int _CompressedHashCode;
                private readonly string _ResourcePath;

                /// <summary>
                /// 初始化资源应用信息的新实例。
                /// </summary>
                /// <param name="resourceName">资源名称。</param>
                /// <param name="fileSystemName">资源所在的文件系统名称。</param>
                /// <param name="loadType">资源加载方式。</param>
                /// <param name="offset">资源偏移。</param>
                /// <param name="length">资源大小。</param>
                /// <param name="hashCode">资源哈希值。</param>
                /// <param name="compressedLength">压缩后大小。</param>
                /// <param name="compressedHashCode">压缩后哈希值。</param>
                /// <param name="resourcePath">资源路径。</param>
                public ApplyInfo(ResourceName resourceName, string fileSystemName, LoadType loadType, long offset, int length, int hashCode, int compressedLength, int compressedHashCode, string resourcePath)
                {
                    _ResourceName = resourceName;
                    _FileSystemName = fileSystemName;
                    _LoadType = loadType;
                    _Offset = offset;
                    _Length = length;
                    _HashCode = hashCode;
                    _CompressedLength = compressedLength;
                    _CompressedHashCode = compressedHashCode;
                    _ResourcePath = resourcePath;
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
                /// 获取资源偏移。
                /// </summary>
                public long Offset
                {
                    get
                    {
                        return _Offset;
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
            }
        }
    }
}
