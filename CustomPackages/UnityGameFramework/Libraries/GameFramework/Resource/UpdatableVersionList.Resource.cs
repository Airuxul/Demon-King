//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace GameFramework.Resource
{
    public partial struct UpdatableVersionList
    {
        /// <summary>
        /// 资源。
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public struct Resource
        {
            private static readonly int[] EmptyIntArray = new int[] { };

            private readonly string _Name;
            private readonly string _Variant;
            private readonly string _Extension;
            private readonly byte _LoadType;
            private readonly int _Length;
            private readonly int _HashCode;
            private readonly int _CompressedLength;
            private readonly int _CompressedHashCode;
            private readonly int[] _AssetIndexes;

            /// <summary>
            /// 初始化资源的新实例。
            /// </summary>
            /// <param name="name">资源名称。</param>
            /// <param name="variant">资源变体名称。</param>
            /// <param name="extension">资源扩展名称。</param>
            /// <param name="loadType">资源加载方式。</param>
            /// <param name="length">资源长度。</param>
            /// <param name="hashCode">资源哈希值。</param>
            /// <param name="compressedLength">资源压缩后长度。</param>
            /// <param name="compressedHashCode">资源压缩后哈希值。</param>
            /// <param name="assetIndexes">资源包含的资源索引集合。</param>
            public Resource(string name, string variant, string extension, byte loadType, int length, int hashCode, int compressedLength, int compressedHashCode, int[] assetIndexes)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new GameFrameworkException("Name is invalid.");
                }

                _Name = name;
                _Variant = variant;
                _Extension = extension;
                _LoadType = loadType;
                _Length = length;
                _HashCode = hashCode;
                _CompressedLength = compressedLength;
                _CompressedHashCode = compressedHashCode;
                _AssetIndexes = assetIndexes ?? EmptyIntArray;
            }

            /// <summary>
            /// 获取资源名称。
            /// </summary>
            public string Name
            {
                get
                {
                    return _Name;
                }
            }

            /// <summary>
            /// 获取资源变体名称。
            /// </summary>
            public string Variant
            {
                get
                {
                    return _Variant;
                }
            }

            /// <summary>
            /// 获取资源扩展名称。
            /// </summary>
            public string Extension
            {
                get
                {
                    return _Extension;
                }
            }

            /// <summary>
            /// 获取资源加载方式。
            /// </summary>
            public byte LoadType
            {
                get
                {
                    return _LoadType;
                }
            }

            /// <summary>
            /// 获取资源长度。
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
            /// 获取资源压缩后长度。
            /// </summary>
            public int CompressedLength
            {
                get
                {
                    return _CompressedLength;
                }
            }

            /// <summary>
            /// 获取资源压缩后哈希值。
            /// </summary>
            public int CompressedHashCode
            {
                get
                {
                    return _CompressedHashCode;
                }
            }

            /// <summary>
            /// 获取资源包含的资源索引集合。
            /// </summary>
            /// <returns>资源包含的资源索引集合。</returns>
            public int[] GetAssetIndexes()
            {
                return _AssetIndexes;
            }
        }
    }
}
