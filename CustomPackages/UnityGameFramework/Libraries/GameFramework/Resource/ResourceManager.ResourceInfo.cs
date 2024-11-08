﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        /// <summary>
        /// 资源信息。
        /// </summary>
        private sealed class ResourceInfo
        {
            private readonly ResourceName _ResourceName;
            private readonly string _FileSystemName;
            private readonly LoadType _LoadType;
            private readonly int _Length;
            private readonly int _HashCode;
            private readonly int _CompressedLength;
            private readonly bool _StorageInReadOnly;
            private bool _Ready;

            /// <summary>
            /// 初始化资源信息的新实例。
            /// </summary>
            /// <param name="resourceName">资源名称。</param>
            /// <param name="fileSystemName">文件系统名称。</param>
            /// <param name="loadType">资源加载方式。</param>
            /// <param name="length">资源大小。</param>
            /// <param name="hashCode">资源哈希值。</param>
            /// <param name="compressedLength">压缩后资源大小。</param>
            /// <param name="storageInReadOnly">资源是否在只读区。</param>
            /// <param name="ready">资源是否准备完毕。</param>
            public ResourceInfo(ResourceName resourceName, string fileSystemName, LoadType loadType, int length, int hashCode, int compressedLength, bool storageInReadOnly, bool ready)
            {
                _ResourceName = resourceName;
                _FileSystemName = fileSystemName;
                _LoadType = loadType;
                _Length = length;
                _HashCode = hashCode;
                _CompressedLength = compressedLength;
                _StorageInReadOnly = storageInReadOnly;
                _Ready = ready;
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
            /// 获取文件系统名称。
            /// </summary>
            public string FileSystemName
            {
                get
                {
                    return _FileSystemName;
                }
            }

            /// <summary>
            /// 获取资源是否通过二进制方式加载。
            /// </summary>
            public bool IsLoadFromBinary
            {
                get
                {
                    return _LoadType == LoadType.LoadFromBinary || _LoadType == LoadType.LoadFromBinaryAndQuickDecrypt || _LoadType == LoadType.LoadFromBinaryAndDecrypt;
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
            /// 获取压缩后资源大小。
            /// </summary>
            public int CompressedLength
            {
                get
                {
                    return _CompressedLength;
                }
            }

            /// <summary>
            /// 获取资源是否在只读区。
            /// </summary>
            public bool StorageInReadOnly
            {
                get
                {
                    return _StorageInReadOnly;
                }
            }

            /// <summary>
            /// 获取资源是否准备完毕。
            /// </summary>
            public bool Ready
            {
                get
                {
                    return _Ready;
                }
            }

            /// <summary>
            /// 标记资源准备完毕。
            /// </summary>
            public void MarkReady()
            {
                _Ready = true;
            }
        }
    }
}
