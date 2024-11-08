//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace GameFramework
{
    /// <summary>
    /// 引用池信息。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ReferencePoolInfo
    {
        private readonly Type _Type;
        private readonly int _UnusedReferenceCount;
        private readonly int _UsingReferenceCount;
        private readonly int _AcquireReferenceCount;
        private readonly int _ReleaseReferenceCount;
        private readonly int _AddReferenceCount;
        private readonly int _RemoveReferenceCount;

        /// <summary>
        /// 初始化引用池信息的新实例。
        /// </summary>
        /// <param name="type">引用池类型。</param>
        /// <param name="unusedReferenceCount">未使用引用数量。</param>
        /// <param name="usingReferenceCount">正在使用引用数量。</param>
        /// <param name="acquireReferenceCount">获取引用数量。</param>
        /// <param name="releaseReferenceCount">归还引用数量。</param>
        /// <param name="addReferenceCount">增加引用数量。</param>
        /// <param name="removeReferenceCount">移除引用数量。</param>
        public ReferencePoolInfo(Type type, int unusedReferenceCount, int usingReferenceCount, int acquireReferenceCount, int releaseReferenceCount, int addReferenceCount, int removeReferenceCount)
        {
            _Type = type;
            _UnusedReferenceCount = unusedReferenceCount;
            _UsingReferenceCount = usingReferenceCount;
            _AcquireReferenceCount = acquireReferenceCount;
            _ReleaseReferenceCount = releaseReferenceCount;
            _AddReferenceCount = addReferenceCount;
            _RemoveReferenceCount = removeReferenceCount;
        }

        /// <summary>
        /// 获取引用池类型。
        /// </summary>
        public Type Type
        {
            get
            {
                return _Type;
            }
        }

        /// <summary>
        /// 获取未使用引用数量。
        /// </summary>
        public int UnusedReferenceCount
        {
            get
            {
                return _UnusedReferenceCount;
            }
        }

        /// <summary>
        /// 获取正在使用引用数量。
        /// </summary>
        public int UsingReferenceCount
        {
            get
            {
                return _UsingReferenceCount;
            }
        }

        /// <summary>
        /// 获取获取引用数量。
        /// </summary>
        public int AcquireReferenceCount
        {
            get
            {
                return _AcquireReferenceCount;
            }
        }

        /// <summary>
        /// 获取归还引用数量。
        /// </summary>
        public int ReleaseReferenceCount
        {
            get
            {
                return _ReleaseReferenceCount;
            }
        }

        /// <summary>
        /// 获取增加引用数量。
        /// </summary>
        public int AddReferenceCount
        {
            get
            {
                return _AddReferenceCount;
            }
        }

        /// <summary>
        /// 获取移除引用数量。
        /// </summary>
        public int RemoveReferenceCount
        {
            get
            {
                return _RemoveReferenceCount;
            }
        }
    }
}
