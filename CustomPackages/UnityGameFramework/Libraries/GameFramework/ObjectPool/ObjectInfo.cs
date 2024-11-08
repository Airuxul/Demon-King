//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace GameFramework.ObjectPool
{
    /// <summary>
    /// 对象信息。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ObjectInfo
    {
        private readonly string _Name;
        private readonly bool _Locked;
        private readonly bool _CustomCanReleaseFlag;
        private readonly int _Priority;
        private readonly DateTime _LastUseTime;
        private readonly int _SpawnCount;

        /// <summary>
        /// 初始化对象信息的新实例。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="locked">对象是否被加锁。</param>
        /// <param name="customCanReleaseFlag">对象自定义释放检查标记。</param>
        /// <param name="priority">对象的优先级。</param>
        /// <param name="lastUseTime">对象上次使用时间。</param>
        /// <param name="spawnCount">对象的获取计数。</param>
        public ObjectInfo(string name, bool locked, bool customCanReleaseFlag, int priority, DateTime lastUseTime, int spawnCount)
        {
            _Name = name;
            _Locked = locked;
            _CustomCanReleaseFlag = customCanReleaseFlag;
            _Priority = priority;
            _LastUseTime = lastUseTime;
            _SpawnCount = spawnCount;
        }

        /// <summary>
        /// 获取对象名称。
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }
        }

        /// <summary>
        /// 获取对象是否被加锁。
        /// </summary>
        public bool Locked
        {
            get
            {
                return _Locked;
            }
        }

        /// <summary>
        /// 获取对象自定义释放检查标记。
        /// </summary>
        public bool CustomCanReleaseFlag
        {
            get
            {
                return _CustomCanReleaseFlag;
            }
        }

        /// <summary>
        /// 获取对象的优先级。
        /// </summary>
        public int Priority
        {
            get
            {
                return _Priority;
            }
        }

        /// <summary>
        /// 获取对象上次使用时间。
        /// </summary>
        public DateTime LastUseTime
        {
            get
            {
                return _LastUseTime;
            }
        }

        /// <summary>
        /// 获取对象是否正在使用。
        /// </summary>
        public bool IsInUse
        {
            get
            {
                return _SpawnCount > 0;
            }
        }

        /// <summary>
        /// 获取对象的获取计数。
        /// </summary>
        public int SpawnCount
        {
            get
            {
                return _SpawnCount;
            }
        }
    }
}
