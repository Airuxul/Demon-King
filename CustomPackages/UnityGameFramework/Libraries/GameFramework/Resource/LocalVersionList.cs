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
    /// 本地版本资源列表。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public partial struct LocalVersionList
    {
        private static readonly Resource[] EmptyResourceArray = new Resource[] { };
        private static readonly FileSystem[] EmptyFileSystemArray = new FileSystem[] { };

        private readonly bool _IsValid;
        private readonly Resource[] _Resources;
        private readonly FileSystem[] _FileSystems;

        /// <summary>
        /// 初始化本地版本资源列表的新实例。
        /// </summary>
        /// <param name="resources">包含的资源集合。</param>
        /// <param name="fileSystems">包含的文件系统集合。</param>
        public LocalVersionList(Resource[] resources, FileSystem[] fileSystems)
        {
            _IsValid = true;
            _Resources = resources ?? EmptyResourceArray;
            _FileSystems = fileSystems ?? EmptyFileSystemArray;
        }

        /// <summary>
        /// 获取本地版本资源列表是否有效。
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _IsValid;
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

        /// <summary>
        /// 获取包含的文件系统集合。
        /// </summary>
        /// <returns>包含的文件系统集合。</returns>
        public FileSystem[] GetFileSystems()
        {
            if (!_IsValid)
            {
                throw new GameFrameworkException("Data is invalid.");
            }

            return _FileSystems;
        }
    }
}
