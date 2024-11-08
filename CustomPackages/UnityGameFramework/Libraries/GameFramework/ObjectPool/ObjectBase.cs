//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;

namespace GameFramework.ObjectPool
{
    /// <summary>
    /// 对象基类。
    /// </summary>
    public abstract class ObjectBase : IReference
    {
        private string _Name;
        private object _Target;
        private bool _Locked;
        private int _Priority;
        private DateTime _LastUseTime;

        /// <summary>
        /// 初始化对象基类的新实例。
        /// </summary>
        public ObjectBase()
        {
            _Name = null;
            _Target = null;
            _Locked = false;
            _Priority = 0;
            _LastUseTime = default(DateTime);
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
        /// 获取对象。
        /// </summary>
        public object Target
        {
            get
            {
                return _Target;
            }
        }

        /// <summary>
        /// 获取或设置对象是否被加锁。
        /// </summary>
        public bool Locked
        {
            get
            {
                return _Locked;
            }
            set
            {
                _Locked = value;
            }
        }

        /// <summary>
        /// 获取或设置对象的优先级。
        /// </summary>
        public int Priority
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
        /// 获取自定义释放检查标记。
        /// </summary>
        public virtual bool CustomCanReleaseFlag
        {
            get
            {
                return true;
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
            internal set
            {
                _LastUseTime = value;
            }
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="target">对象。</param>
        protected void Initialize(object target)
        {
            Initialize(null, target, false, 0);
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象。</param>
        protected void Initialize(string name, object target)
        {
            Initialize(name, target, false, 0);
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象。</param>
        /// <param name="locked">对象是否被加锁。</param>
        protected void Initialize(string name, object target, bool locked)
        {
            Initialize(name, target, locked, 0);
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象。</param>
        /// <param name="priority">对象的优先级。</param>
        protected void Initialize(string name, object target, int priority)
        {
            Initialize(name, target, false, priority);
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象。</param>
        /// <param name="locked">对象是否被加锁。</param>
        /// <param name="priority">对象的优先级。</param>
        protected void Initialize(string name, object target, bool locked, int priority)
        {
            if (target == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Target '{0}' is invalid.", name));
            }

            _Name = name ?? string.Empty;
            _Target = target;
            _Locked = locked;
            _Priority = priority;
            _LastUseTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 清理对象基类。
        /// </summary>
        public virtual void Clear()
        {
            _Name = null;
            _Target = null;
            _Locked = false;
            _Priority = 0;
            _LastUseTime = default(DateTime);
        }

        /// <summary>
        /// 获取对象时的事件。
        /// </summary>
        protected internal virtual void OnSpawn()
        {
        }

        /// <summary>
        /// 回收对象时的事件。
        /// </summary>
        protected internal virtual void OnUnspawn()
        {
        }

        /// <summary>
        /// 释放对象。
        /// </summary>
        /// <param name="isShutdown">是否是关闭对象池时触发。</param>
        protected internal abstract void Release(bool isShutdown);
    }
}
