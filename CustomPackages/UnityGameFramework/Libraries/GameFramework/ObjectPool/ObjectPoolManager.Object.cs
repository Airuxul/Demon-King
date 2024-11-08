//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;

namespace GameFramework.ObjectPool
{
    internal sealed partial class ObjectPoolManager : GameFrameworkModule, IObjectPoolManager
    {
        /// <summary>
        /// 内部对象。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        private sealed class Object<T> : IReference where T : ObjectBase
        {
            private T _Object;
            private int _SpawnCount;

            /// <summary>
            /// 初始化内部对象的新实例。
            /// </summary>
            public Object()
            {
                _Object = null;
                _SpawnCount = 0;
            }

            /// <summary>
            /// 获取对象名称。
            /// </summary>
            public string Name
            {
                get
                {
                    return _Object.Name;
                }
            }

            /// <summary>
            /// 获取对象是否被加锁。
            /// </summary>
            public bool Locked
            {
                get
                {
                    return _Object.Locked;
                }
                internal set
                {
                    _Object.Locked = value;
                }
            }

            /// <summary>
            /// 获取对象的优先级。
            /// </summary>
            public int Priority
            {
                get
                {
                    return _Object.Priority;
                }
                internal set
                {
                    _Object.Priority = value;
                }
            }

            /// <summary>
            /// 获取自定义释放检查标记。
            /// </summary>
            public bool CustomCanReleaseFlag
            {
                get
                {
                    return _Object.CustomCanReleaseFlag;
                }
            }

            /// <summary>
            /// 获取对象上次使用时间。
            /// </summary>
            public DateTime LastUseTime
            {
                get
                {
                    return _Object.LastUseTime;
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

            /// <summary>
            /// 创建内部对象。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <param name="spawned">对象是否已被获取。</param>
            /// <returns>创建的内部对象。</returns>
            public static Object<T> Create(T obj, bool spawned)
            {
                if (obj == null)
                {
                    throw new GameFrameworkException("Object is invalid.");
                }

                Object<T> internalObject = ReferencePool.Acquire<Object<T>>();
                internalObject._Object = obj;
                internalObject._SpawnCount = spawned ? 1 : 0;
                if (spawned)
                {
                    obj.OnSpawn();
                }

                return internalObject;
            }

            /// <summary>
            /// 清理内部对象。
            /// </summary>
            public void Clear()
            {
                _Object = null;
                _SpawnCount = 0;
            }

            /// <summary>
            /// 查看对象。
            /// </summary>
            /// <returns>对象。</returns>
            public T Peek()
            {
                return _Object;
            }

            /// <summary>
            /// 获取对象。
            /// </summary>
            /// <returns>对象。</returns>
            public T Spawn()
            {
                _SpawnCount++;
                _Object.LastUseTime = DateTime.UtcNow;
                _Object.OnSpawn();
                return _Object;
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            public void Unspawn()
            {
                _Object.OnUnspawn();
                _Object.LastUseTime = DateTime.UtcNow;
                _SpawnCount--;
                if (_SpawnCount < 0)
                {
                    throw new GameFrameworkException(Utility.Text.Format("Object '{0}' spawn count is less than 0.", Name));
                }
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            /// <param name="isShutdown">是否是关闭对象池时触发。</param>
            public void Release(bool isShutdown)
            {
                _Object.Release(isShutdown);
                ReferencePool.Release(_Object);
            }
        }
    }
}
