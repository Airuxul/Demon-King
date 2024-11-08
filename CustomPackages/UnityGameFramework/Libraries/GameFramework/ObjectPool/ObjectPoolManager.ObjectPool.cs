//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace GameFramework.ObjectPool
{
    internal sealed partial class ObjectPoolManager : GameFrameworkModule, IObjectPoolManager
    {
        /// <summary>
        /// 对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        private sealed class ObjectPool<T> : ObjectPoolBase, IObjectPool<T> where T : ObjectBase
        {
            private readonly GameFrameworkMultiDictionary<string, Object<T>> _Objects;
            private readonly Dictionary<object, Object<T>> _ObjectMap;
            private readonly ReleaseObjectFilterCallback<T> _DefaultReleaseObjectFilterCallback;
            private readonly List<T> _CachedCanReleaseObjects;
            private readonly List<T> _CachedToReleaseObjects;
            private readonly bool _AllowMultiSpawn;
            private float _AutoReleaseInterval;
            private int _Capacity;
            private float _ExpireTime;
            private int _Priority;
            private float _AutoReleaseTime;

            /// <summary>
            /// 初始化对象池的新实例。
            /// </summary>
            /// <param name="name">对象池名称。</param>
            /// <param name="allowMultiSpawn">是否允许对象被多次获取。</param>
            /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
            /// <param name="capacity">对象池的容量。</param>
            /// <param name="expireTime">对象池对象过期秒数。</param>
            /// <param name="priority">对象池的优先级。</param>
            public ObjectPool(string name, bool allowMultiSpawn, float autoReleaseInterval, int capacity, float expireTime, int priority)
                : base(name)
            {
                _Objects = new GameFrameworkMultiDictionary<string, Object<T>>();
                _ObjectMap = new Dictionary<object, Object<T>>();
                _DefaultReleaseObjectFilterCallback = DefaultReleaseObjectFilterCallback;
                _CachedCanReleaseObjects = new List<T>();
                _CachedToReleaseObjects = new List<T>();
                _AllowMultiSpawn = allowMultiSpawn;
                _AutoReleaseInterval = autoReleaseInterval;
                Capacity = capacity;
                ExpireTime = expireTime;
                _Priority = priority;
                _AutoReleaseTime = 0f;
            }

            /// <summary>
            /// 获取对象池对象类型。
            /// </summary>
            public override Type ObjectType
            {
                get
                {
                    return typeof(T);
                }
            }

            /// <summary>
            /// 获取对象池中对象的数量。
            /// </summary>
            public override int Count
            {
                get
                {
                    return _ObjectMap.Count;
                }
            }

            /// <summary>
            /// 获取对象池中能被释放的对象的数量。
            /// </summary>
            public override int CanReleaseCount
            {
                get
                {
                    GetCanReleaseObjects(_CachedCanReleaseObjects);
                    return _CachedCanReleaseObjects.Count;
                }
            }

            /// <summary>
            /// 获取是否允许对象被多次获取。
            /// </summary>
            public override bool AllowMultiSpawn
            {
                get
                {
                    return _AllowMultiSpawn;
                }
            }

            /// <summary>
            /// 获取或设置对象池自动释放可释放对象的间隔秒数。
            /// </summary>
            public override float AutoReleaseInterval
            {
                get
                {
                    return _AutoReleaseInterval;
                }
                set
                {
                    _AutoReleaseInterval = value;
                }
            }

            /// <summary>
            /// 获取或设置对象池的容量。
            /// </summary>
            public override int Capacity
            {
                get
                {
                    return _Capacity;
                }
                set
                {
                    if (value < 0)
                    {
                        throw new GameFrameworkException("Capacity is invalid.");
                    }

                    if (_Capacity == value)
                    {
                        return;
                    }

                    _Capacity = value;
                    Release();
                }
            }

            /// <summary>
            /// 获取或设置对象池对象过期秒数。
            /// </summary>
            public override float ExpireTime
            {
                get
                {
                    return _ExpireTime;
                }

                set
                {
                    if (value < 0f)
                    {
                        throw new GameFrameworkException("ExpireTime is invalid.");
                    }

                    if (ExpireTime == value)
                    {
                        return;
                    }

                    _ExpireTime = value;
                    Release();
                }
            }

            /// <summary>
            /// 获取或设置对象池的优先级。
            /// </summary>
            public override int Priority
            {
                get
                {
                    return _Priority;
                }
                set
                {
                    _Priority = value;
                }
            }

            /// <summary>
            /// 创建对象。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <param name="spawned">对象是否已被获取。</param>
            public void Register(T obj, bool spawned)
            {
                if (obj == null)
                {
                    throw new GameFrameworkException("Object is invalid.");
                }

                Object<T> internalObject = Object<T>.Create(obj, spawned);
                _Objects.Add(obj.Name, internalObject);
                _ObjectMap.Add(obj.Target, internalObject);

                if (Count > _Capacity)
                {
                    Release();
                }
            }

            /// <summary>
            /// 检查对象。
            /// </summary>
            /// <returns>要检查的对象是否存在。</returns>
            public bool CanSpawn()
            {
                return CanSpawn(string.Empty);
            }

            /// <summary>
            /// 检查对象。
            /// </summary>
            /// <param name="name">对象名称。</param>
            /// <returns>要检查的对象是否存在。</returns>
            public bool CanSpawn(string name)
            {
                if (name == null)
                {
                    throw new GameFrameworkException("Name is invalid.");
                }

                GameFrameworkLinkedListRange<Object<T>> objectRange = default(GameFrameworkLinkedListRange<Object<T>>);
                if (_Objects.TryGetValue(name, out objectRange))
                {
                    foreach (Object<T> internalObject in objectRange)
                    {
                        if (_AllowMultiSpawn || !internalObject.IsInUse)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// 获取对象。
            /// </summary>
            /// <returns>要获取的对象。</returns>
            public T Spawn()
            {
                return Spawn(string.Empty);
            }

            /// <summary>
            /// 获取对象。
            /// </summary>
            /// <param name="name">对象名称。</param>
            /// <returns>要获取的对象。</returns>
            public T Spawn(string name)
            {
                if (name == null)
                {
                    throw new GameFrameworkException("Name is invalid.");
                }

                GameFrameworkLinkedListRange<Object<T>> objectRange = default(GameFrameworkLinkedListRange<Object<T>>);
                if (_Objects.TryGetValue(name, out objectRange))
                {
                    foreach (Object<T> internalObject in objectRange)
                    {
                        if (_AllowMultiSpawn || !internalObject.IsInUse)
                        {
                            return internalObject.Spawn();
                        }
                    }
                }

                return null;
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            /// <param name="obj">要回收的对象。</param>
            public void Unspawn(T obj)
            {
                if (obj == null)
                {
                    throw new GameFrameworkException("Object is invalid.");
                }

                Unspawn(obj.Target);
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            /// <param name="target">要回收的对象。</param>
            public void Unspawn(object target)
            {
                if (target == null)
                {
                    throw new GameFrameworkException("Target is invalid.");
                }

                Object<T> internalObject = GetObject(target);
                if (internalObject != null)
                {
                    internalObject.Unspawn();
                    if (Count > _Capacity && internalObject.SpawnCount <= 0)
                    {
                        Release();
                    }
                }
                else
                {
                    throw new GameFrameworkException(Utility.Text.Format("Can not find target in object pool '{0}', target type is '{1}', target value is '{2}'.", new TypeNamePair(typeof(T), Name), target.GetType().FullName, target));
                }
            }

            /// <summary>
            /// 设置对象是否被加锁。
            /// </summary>
            /// <param name="obj">要设置被加锁的对象。</param>
            /// <param name="locked">是否被加锁。</param>
            public void SetLocked(T obj, bool locked)
            {
                if (obj == null)
                {
                    throw new GameFrameworkException("Object is invalid.");
                }

                SetLocked(obj.Target, locked);
            }

            /// <summary>
            /// 设置对象是否被加锁。
            /// </summary>
            /// <param name="target">要设置被加锁的对象。</param>
            /// <param name="locked">是否被加锁。</param>
            public void SetLocked(object target, bool locked)
            {
                if (target == null)
                {
                    throw new GameFrameworkException("Target is invalid.");
                }

                Object<T> internalObject = GetObject(target);
                if (internalObject != null)
                {
                    internalObject.Locked = locked;
                }
                else
                {
                    throw new GameFrameworkException(Utility.Text.Format("Can not find target in object pool '{0}', target type is '{1}', target value is '{2}'.", new TypeNamePair(typeof(T), Name), target.GetType().FullName, target));
                }
            }

            /// <summary>
            /// 设置对象的优先级。
            /// </summary>
            /// <param name="obj">要设置优先级的对象。</param>
            /// <param name="priority">优先级。</param>
            public void SetPriority(T obj, int priority)
            {
                if (obj == null)
                {
                    throw new GameFrameworkException("Object is invalid.");
                }

                SetPriority(obj.Target, priority);
            }

            /// <summary>
            /// 设置对象的优先级。
            /// </summary>
            /// <param name="target">要设置优先级的对象。</param>
            /// <param name="priority">优先级。</param>
            public void SetPriority(object target, int priority)
            {
                if (target == null)
                {
                    throw new GameFrameworkException("Target is invalid.");
                }

                Object<T> internalObject = GetObject(target);
                if (internalObject != null)
                {
                    internalObject.Priority = priority;
                }
                else
                {
                    throw new GameFrameworkException(Utility.Text.Format("Can not find target in object pool '{0}', target type is '{1}', target value is '{2}'.", new TypeNamePair(typeof(T), Name), target.GetType().FullName, target));
                }
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            /// <param name="obj">要释放的对象。</param>
            /// <returns>释放对象是否成功。</returns>
            public bool ReleaseObject(T obj)
            {
                if (obj == null)
                {
                    throw new GameFrameworkException("Object is invalid.");
                }

                return ReleaseObject(obj.Target);
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            /// <param name="target">要释放的对象。</param>
            /// <returns>释放对象是否成功。</returns>
            public bool ReleaseObject(object target)
            {
                if (target == null)
                {
                    throw new GameFrameworkException("Target is invalid.");
                }

                Object<T> internalObject = GetObject(target);
                if (internalObject == null)
                {
                    return false;
                }

                if (internalObject.IsInUse || internalObject.Locked || !internalObject.CustomCanReleaseFlag)
                {
                    return false;
                }

                _Objects.Remove(internalObject.Name, internalObject);
                _ObjectMap.Remove(internalObject.Peek().Target);

                internalObject.Release(false);
                ReferencePool.Release(internalObject);
                return true;
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            public override void Release()
            {
                Release(Count - _Capacity, _DefaultReleaseObjectFilterCallback);
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            /// <param name="toReleaseCount">尝试释放对象数量。</param>
            public override void Release(int toReleaseCount)
            {
                Release(toReleaseCount, _DefaultReleaseObjectFilterCallback);
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            /// <param name="releaseObjectFilterCallback">释放对象筛选函数。</param>
            public void Release(ReleaseObjectFilterCallback<T> releaseObjectFilterCallback)
            {
                Release(Count - _Capacity, releaseObjectFilterCallback);
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            /// <param name="toReleaseCount">尝试释放对象数量。</param>
            /// <param name="releaseObjectFilterCallback">释放对象筛选函数。</param>
            public void Release(int toReleaseCount, ReleaseObjectFilterCallback<T> releaseObjectFilterCallback)
            {
                if (releaseObjectFilterCallback == null)
                {
                    throw new GameFrameworkException("Release object filter callback is invalid.");
                }

                if (toReleaseCount < 0)
                {
                    toReleaseCount = 0;
                }

                DateTime expireTime = DateTime.MinValue;
                if (_ExpireTime < float.MaxValue)
                {
                    expireTime = DateTime.UtcNow.AddSeconds(-_ExpireTime);
                }

                _AutoReleaseTime = 0f;
                GetCanReleaseObjects(_CachedCanReleaseObjects);
                List<T> toReleaseObjects = releaseObjectFilterCallback(_CachedCanReleaseObjects, toReleaseCount, expireTime);
                if (toReleaseObjects == null || toReleaseObjects.Count <= 0)
                {
                    return;
                }

                foreach (T toReleaseObject in toReleaseObjects)
                {
                    ReleaseObject(toReleaseObject);
                }
            }

            /// <summary>
            /// 释放对象池中的所有未使用对象。
            /// </summary>
            public override void ReleaseAllUnused()
            {
                _AutoReleaseTime = 0f;
                GetCanReleaseObjects(_CachedCanReleaseObjects);
                foreach (T toReleaseObject in _CachedCanReleaseObjects)
                {
                    ReleaseObject(toReleaseObject);
                }
            }

            /// <summary>
            /// 获取所有对象信息。
            /// </summary>
            /// <returns>所有对象信息。</returns>
            public override ObjectInfo[] GetAllObjectInfos()
            {
                List<ObjectInfo> results = new List<ObjectInfo>();
                foreach (KeyValuePair<string, GameFrameworkLinkedListRange<Object<T>>> objectRanges in _Objects)
                {
                    foreach (Object<T> internalObject in objectRanges.Value)
                    {
                        results.Add(new ObjectInfo(internalObject.Name, internalObject.Locked, internalObject.CustomCanReleaseFlag, internalObject.Priority, internalObject.LastUseTime, internalObject.SpawnCount));
                    }
                }

                return results.ToArray();
            }

            internal override void Update(float elapseSeconds, float realElapseSeconds)
            {
                _AutoReleaseTime += realElapseSeconds;
                if (_AutoReleaseTime < _AutoReleaseInterval)
                {
                    return;
                }

                Release();
            }

            internal override void Shutdown()
            {
                foreach (KeyValuePair<object, Object<T>> objectInMap in _ObjectMap)
                {
                    objectInMap.Value.Release(true);
                    ReferencePool.Release(objectInMap.Value);
                }

                _Objects.Clear();
                _ObjectMap.Clear();
                _CachedCanReleaseObjects.Clear();
                _CachedToReleaseObjects.Clear();
            }

            private Object<T> GetObject(object target)
            {
                if (target == null)
                {
                    throw new GameFrameworkException("Target is invalid.");
                }

                Object<T> internalObject = null;
                if (_ObjectMap.TryGetValue(target, out internalObject))
                {
                    return internalObject;
                }

                return null;
            }

            private void GetCanReleaseObjects(List<T> results)
            {
                if (results == null)
                {
                    throw new GameFrameworkException("Results is invalid.");
                }

                results.Clear();
                foreach (KeyValuePair<object, Object<T>> objectInMap in _ObjectMap)
                {
                    Object<T> internalObject = objectInMap.Value;
                    if (internalObject.IsInUse || internalObject.Locked || !internalObject.CustomCanReleaseFlag)
                    {
                        continue;
                    }

                    results.Add(internalObject.Peek());
                }
            }

            private List<T> DefaultReleaseObjectFilterCallback(List<T> candidateObjects, int toReleaseCount, DateTime expireTime)
            {
                _CachedToReleaseObjects.Clear();

                if (expireTime > DateTime.MinValue)
                {
                    for (int i = candidateObjects.Count - 1; i >= 0; i--)
                    {
                        if (candidateObjects[i].LastUseTime <= expireTime)
                        {
                            _CachedToReleaseObjects.Add(candidateObjects[i]);
                            candidateObjects.RemoveAt(i);
                            continue;
                        }
                    }

                    toReleaseCount -= _CachedToReleaseObjects.Count;
                }

                for (int i = 0; toReleaseCount > 0 && i < candidateObjects.Count; i++)
                {
                    for (int j = i + 1; j < candidateObjects.Count; j++)
                    {
                        if (candidateObjects[i].Priority > candidateObjects[j].Priority
                            || candidateObjects[i].Priority == candidateObjects[j].Priority && candidateObjects[i].LastUseTime > candidateObjects[j].LastUseTime)
                        {
                            T temp = candidateObjects[i];
                            candidateObjects[i] = candidateObjects[j];
                            candidateObjects[j] = temp;
                        }
                    }

                    _CachedToReleaseObjects.Add(candidateObjects[i]);
                    toReleaseCount--;
                }

                return _CachedToReleaseObjects;
            }
        }
    }
}
