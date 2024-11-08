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
        /// 文件系统。
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public struct FileSystem
        {
            private static readonly int[] EmptyIntArray = new int[] { };

            private readonly string _Name;
            private readonly int[] _ResourceIndexes;

            /// <summary>
            /// 初始化文件系统的新实例。
            /// </summary>
            /// <param name="name">文件系统名称。</param>
            /// <param name="resourceIndexes">文件系统包含的资源索引集合。</param>
            public FileSystem(string name, int[] resourceIndexes)
            {
                if (name == null)
                {
                    throw new GameFrameworkException("Name is invalid.");
                }

                _Name = name;
                _ResourceIndexes = resourceIndexes ?? EmptyIntArray;
            }

            /// <summary>
            /// 获取文件系统名称。
            /// </summary>
            public string Name
            {
                get
                {
                    return _Name;
                }
            }

            /// <summary>
            /// 获取文件系统包含的资源索引集合。
            /// </summary>
            /// <returns>文件系统包含的资源索引集合。</returns>
            public int[] GetResourceIndexes()
            {
                return _ResourceIndexes;
            }
        }
    }
}
