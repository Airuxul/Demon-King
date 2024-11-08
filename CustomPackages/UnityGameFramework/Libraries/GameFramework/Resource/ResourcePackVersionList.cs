//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace GameFramework.Resource
{
    /// <summary>
    /// 资源包版本资源列表。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public partial struct ResourcePackVersionList
    {
        private static readonly Resource[] EmptyResourceArray = new Resource[] { };

        private readonly bool _IsValid;
        private readonly int _Offset;
        private readonly long _Length;
        private readonly int _HashCode;
        private readonly Resource[] _Resources;

        /// <summary>
        /// 初始化资源包版本资源列表的新实例。
        /// </summary>
        /// <param name="offset">资源数据偏移。</param>
        /// <param name="length">资源数据长度。</param>
        /// <param name="hashCode">资源数据哈希值。</param>
        /// <param name="resources">包含的资源集合。</param>
        public ResourcePackVersionList(int offset, long length, int hashCode, Resource[] resources)
        {
            _IsValid = true;
            _Offset = offset;
            _Length = length;
            _HashCode = hashCode;
            _Resources = resources ?? EmptyResourceArray;
        }

        /// <summary>
        /// 获取资源包版本资源列表是否有效。
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _IsValid;
            }
        }

        /// <summary>
        /// 获取资源数据偏移。
        /// </summary>
        public int Offset
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _Offset;
            }
        }

        /// <summary>
        /// 获取资源数据长度。
        /// </summary>
        public long Length
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _Length;
            }
        }

        /// <summary>
        /// 获取资源数据哈希值。
        /// </summary>
        public int HashCode
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _HashCode;
            }
        }

        /// <summary>
        /// 获取包含的资源集合。
        /// </summary>
        /// <returns>包含的资源集合。</returns>
        public Resource[] GetResources()
        {
            if (!_IsValid)
            {
                throw new GameFrameworkException("Data is invalid.");
            }

            return _Resources;
        }
    }
}
