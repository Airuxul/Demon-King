//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace GameFramework.FileSystem
{
    internal sealed partial class FileSystem : IFileSystem
    {
        /// <summary>
        /// 块数据。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct BlockData
        {
            public static readonly BlockData Empty = new BlockData(0, 0);

            private readonly int _StringIndex;
            private readonly int _ClusterIndex;
            private readonly int _Length;

            public BlockData(int clusterIndex, int length)
                : this(-1, clusterIndex, length)
            {
            }

            public BlockData(int stringIndex, int clusterIndex, int length)
            {
                _StringIndex = stringIndex;
                _ClusterIndex = clusterIndex;
                _Length = length;
            }

            public bool Using
            {
                get
                {
                    return _StringIndex >= 0;
                }
            }

            public int StringIndex
            {
                get
                {
                    return _StringIndex;
                }
            }

            public int ClusterIndex
            {
                get
                {
                    return _ClusterIndex;
                }
            }

            public int Length
            {
                get
                {
                    return _Length;
                }
            }

            public BlockData Free()
            {
                return new BlockData(_ClusterIndex, (int)GetUpBoundClusterOffset(_Length));
            }
        }
    }
}
