//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// 事件池。
    /// </summary>
    /// <typeparam name="T">事件类型。</typeparam>
    internal sealed partial class EventPool<T> where T : BaseEventArgs
    {
        private readonly GameFrameworkMultiDictionary<int, EventHandler<T>> _EventHandlers;
        private readonly Queue<Event> _Events;
        private readonly Dictionary<object, LinkedListNode<EventHandler<T>>> _CachedNodes;
        private readonly Dictionary<object, LinkedListNode<EventHandler<T>>> _TempNodes;
        private readonly EventPoolMode _EventPoolMode;
        private EventHandler<T> _DefaultHandler;

        /// <summary>
        /// 初始化事件池的新实例。
        /// </summary>
        /// <param name="mode">事件池模式。</param>
        public EventPool(EventPoolMode mode)
        {
            _EventHandlers = new GameFrameworkMultiDictionary<int, EventHandler<T>>();
            _Events = new Queue<Event>();
            _CachedNodes = new Dictionary<object, LinkedListNode<EventHandler<T>>>();
            _TempNodes = new Dictionary<object, LinkedListNode<EventHandler<T>>>();
            _EventPoolMode = mode;
            _DefaultHandler = null;
        }

        /// <summary>
        /// 获取事件处理函数的数量。
        /// </summary>
        public int EventHandlerCount
        {
            get
            {
                return _EventHandlers.Count;
            }
        }

        /// <summary>
        /// 获取事件数量。
        /// </summary>
        public int EventCount
        {
            get
            {
                return _Events.Count;
            }
        }

        /// <summary>
        /// 事件池轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            lock (_Events)
            {
                while (_Events.Count > 0)
                {
                    Event eventNode = _Events.Dequeue();
                    HandleEvent(eventNode.Sender, eventNode.EventArgs);
                    ReferencePool.Release(eventNode);
                }
            }
        }

        /// <summary>
        /// 关闭并清理事件池。
        /// </summary>
        public void Shutdown()
        {
            Clear();
            _EventHandlers.Clear();
            _CachedNodes.Clear();
            _TempNodes.Clear();
            _DefaultHandler = null;
        }

        /// <summary>
        /// 清理事件。
        /// </summary>
        public void Clear()
        {
            lock (_Events)
            {
                _Events.Clear();
            }
        }

        /// <summary>
        /// 获取事件处理函数的数量。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <returns>事件处理函数的数量。</returns>
        public int Count(int id)
        {
            GameFrameworkLinkedListRange<EventHandler<T>> range = default(GameFrameworkLinkedListRange<EventHandler<T>>);
            if (_EventHandlers.TryGetValue(id, out range))
            {
                return range.Count;
            }

            return 0;
        }

        /// <summary>
        /// 检查是否存在事件处理函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要检查的事件处理函数。</param>
        /// <returns>是否存在事件处理函数。</returns>
        public bool Check(int id, EventHandler<T> handler)
        {
            if (handler == null)
            {
                throw new GameFrameworkException("Event handler is invalid.");
            }

            return _EventHandlers.Contains(id, handler);
        }

        /// <summary>
        /// 订阅事件处理函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要订阅的事件处理函数。</param>
        public void Subscribe(int id, EventHandler<T> handler)
        {
            if (handler == null)
            {
                throw new GameFrameworkException("Event handler is invalid.");
            }

            if (!_EventHandlers.Contains(id))
            {
                _EventHandlers.Add(id, handler);
            }
            else if ((_EventPoolMode & EventPoolMode.AllowMultiHandler) != EventPoolMode.AllowMultiHandler)
            {
                throw new GameFrameworkException(Utility.Text.Format("Event '{0}' not allow multi handler.", id));
            }
            else if ((_EventPoolMode & EventPoolMode.AllowDuplicateHandler) != EventPoolMode.AllowDuplicateHandler && Check(id, handler))
            {
                throw new GameFrameworkException(Utility.Text.Format("Event '{0}' not allow duplicate handler.", id));
            }
            else
            {
                _EventHandlers.Add(id, handler);
            }
        }

        /// <summary>
        /// 取消订阅事件处理函数。
        /// </summary>
        /// <param name="id">事件类型编号。</param>
        /// <param name="handler">要取消订阅的事件处理函数。</param>
        public void Unsubscribe(int id, EventHandler<T> handler)
        {
            if (handler == null)
            {
                throw new GameFrameworkException("Event handler is invalid.");
            }

            if (_CachedNodes.Count > 0)
            {
                foreach (KeyValuePair<object, LinkedListNode<EventHandler<T>>> cachedNode in _CachedNodes)
                {
                    if (cachedNode.Value != null && cachedNode.Value.Value == handler)
                    {
                        _TempNodes.Add(cachedNode.Key, cachedNode.Value.Next);
                    }
                }

                if (_TempNodes.Count > 0)
                {
                    foreach (KeyValuePair<object, LinkedListNode<EventHandler<T>>> cachedNode in _TempNodes)
                    {
                        _CachedNodes[cachedNode.Key] = cachedNode.Value;
                    }

                    _TempNodes.Clear();
                }
            }

            if (!_EventHandlers.Remove(id, handler))
            {
                throw new GameFrameworkException(Utility.Text.Format("Event '{0}' not exists specified handler.", id));
            }
        }

        /// <summary>
        /// 设置默认事件处理函数。
        /// </summary>
        /// <param name="handler">要设置的默认事件处理函数。</param>
        public void SetDefaultHandler(EventHandler<T> handler)
        {
            _DefaultHandler = handler;
        }

        /// <summary>
        /// 抛出事件，这个操作是线程安全的，即使不在主线程中抛出，也可保证在主线程中回调事件处理函数，但事件会在抛出后的下一帧分发。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        public void Fire(object sender, T e)
        {
            if (e == null)
            {
                throw new GameFrameworkException("Event is invalid.");
            }

            Event eventNode = Event.Create(sender, e);
            lock (_Events)
            {
                _Events.Enqueue(eventNode);
            }
        }

        /// <summary>
        /// 抛出事件立即模式，这个操作不是线程安全的，事件会立刻分发。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        public void FireNow(object sender, T e)
        {
            if (e == null)
            {
                throw new GameFrameworkException("Event is invalid.");
            }

            HandleEvent(sender, e);
        }

        /// <summary>
        /// 处理事件结点。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">事件参数。</param>
        private void HandleEvent(object sender, T e)
        {
            bool noHandlerException = false;
            GameFrameworkLinkedListRange<EventHandler<T>> range = default(GameFrameworkLinkedListRange<EventHandler<T>>);
            if (_EventHandlers.TryGetValue(e.Id, out range))
            {
                LinkedListNode<EventHandler<T>> current = range.First;
                while (current != null && current != range.Terminal)
                {
                    _CachedNodes[e] = current.Next != range.Terminal ? current.Next : null;
                    current.Value(sender, e);
                    current = _CachedNodes[e];
                }

                _CachedNodes.Remove(e);
            }
            else if (_DefaultHandler != null)
            {
                _DefaultHandler(sender, e);
            }
            else if ((_EventPoolMode & EventPoolMode.AllowNoHandler) == 0)
            {
                noHandlerException = true;
            }

            ReferencePool.Release(e);

            if (noHandlerException)
            {
                throw new GameFrameworkException(Utility.Text.Format("Event '{0}' not allow no handler.", e.Id));
            }
        }
    }
}
