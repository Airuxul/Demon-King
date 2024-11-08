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
    /// 可更新模式版本资源列表。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public partial struct UpdatableVersionList
    {
        private static readonly Asset[] EmptyAssetArray = new Asset[] { };
        private static readonly Resource[] EmptyResourceArray = new Resource[] { };
        private static readonly FileSystem[] EmptyFileSystemArray = new FileSystem[] { };
        private static readonly ResourceGroup[] EmptyResourceGroupArray = new ResourceGroup[] { };

        private readonly bool _IsValid;
        private readonly string _ApplicableGameVersion;
        private readonly int _InternalResourceVersion;
        private readonly Asset[] _Assets;
        private readonly Resource[] _Resources;
        private readonly FileSystem[] _FileSystems;
        private readonly ResourceGroup[] _ResourceGroups;

        /// <summary>
        /// 初始化可更新模式版本资源列表的新实例。
        /// </summary>
        /// <param name="applicableGameVersion">适配的游戏版本号。</param>
        /// <param name="internalResourceVersion">内部资源版本号。</param>
        /// <param name="assets">包含的资源集合。</param>
        /// <param name="resources">包含的资源集合。</param>
        /// <param name="fileSystems">包含的文件系统集合。</param>
        /// <param name="resourceGroups">包含的资源组集合。</param>
        public UpdatableVersionList(string applicableGameVersion, int internalResourceVersion, Asset[] assets, Resource[] resources, FileSystem[] fileSystems, ResourceGroup[] resourceGroups)
        {
            _IsValid = true;
            _ApplicableGameVersion = applicableGameVersion;
            _InternalResourceVersion = internalResourceVersion;
            _Assets = assets ?? EmptyAssetArray;
            _Resources = resources ?? EmptyResourceArray;
            _FileSystems = fileSystems ?? EmptyFileSystemArray;
            _ResourceGroups = resourceGroups ?? EmptyResourceGroupArray;
        }

        /// <summary>
        /// 获取可更新模式版本资源列表是否有效。
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _IsValid;
            }
        }

        /// <summary>
        /// 获取适配的游戏版本号。
        /// </summary>
        public string ApplicableGameVersion
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _ApplicableGameVersion;
            }
        }

        /// <summary>
        /// 获取内部资源版本号。
        /// </summary>
        public int InternalResourceVersion
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _InternalResourceVersion;
            }
        }

        /// <summary>
        /// 获取包含的资源集合。
        /// </summary>
        /// <returns>包含的资源集合。</returns>
        public Asset[] GetAssets()
        {
            if (!_IsValid)
            {
                throw new GameFrameworkException("Data is invalid.");
            }

            return _Assets;
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

        /// <summary>
        /// 获取包含的资源组集合。
        /// </summary>
        /// <returns>包含的资源组集合。</returns>
        public ResourceGroup[] GetResourceGroups()
        {
            if (!_IsValid)
            {
                throw new GameFrameworkException("Data is invalid.");
            }

            return _ResourceGroups;
        }
    }
}
