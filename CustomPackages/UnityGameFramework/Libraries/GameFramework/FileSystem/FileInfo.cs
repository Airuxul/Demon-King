//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace GameFramework.FileSystem
{
    /// <summary>
    /// 文件信息。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct FileInfo
    {
        private readonly string _Name;
        private readonly long _Offset;
        private readonly int _Length;

        /// <summary>
        /// 初始化文件信息的新实例。
        /// </summary>
        /// <param name="name">文件名称。</param>
        /// <param name="offset">文件偏移。</param>
        /// <param name="length">文件长度。</param>
        public FileInfo(string name, long offset, int length)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            if (offset < 0L)
            {
                throw new GameFrameworkException("Offset is invalid.");
            }

            if (length < 0)
            {
                throw new GameFrameworkException("Length is invalid.");
            }

            _Name = name;
            _Offset = offset;
            _Length = length;
        }

        /// <summary>
        /// 获取文件信息是否有效。
        /// </summary>
        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(_Name) && _Offset >= 0L && _Length >= 0;
            }
        }

        /// <summary>
        /// 获取文件名称。
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }
        }

        /// <summary>
        /// 获取文件偏移。
        /// </summary>
        public long Offset
        {
            get
            {
                return _Offset;
            }
        }

        /// <summary>
        /// 获取文件长度。
        /// </summary>
        public int Length
        {
            get
            {
                return _Length;
            }
        }
    }
}
