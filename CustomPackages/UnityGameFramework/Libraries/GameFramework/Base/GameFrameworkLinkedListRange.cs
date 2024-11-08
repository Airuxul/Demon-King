//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GameFramework
{
    /// <summary>
    /// 游戏框架链表范围。
    /// </summary>
    /// <typeparam name="T">指定链表范围的元素类型。</typeparam>
    [StructLayout(LayoutKind.Auto)]
    public struct GameFrameworkLinkedListRange<T> : IEnumerable<T>, IEnumerable
    {
        private readonly LinkedListNode<T> _First;
        private readonly LinkedListNode<T> _Terminal;

        /// <summary>
        /// 初始化游戏框架链表范围的新实例。
        /// </summary>
        /// <param name="first">链表范围的开始结点。</param>
        /// <param name="terminal">链表范围的终结标记结点。</param>
        public GameFrameworkLinkedListRange(LinkedListNode<T> first, LinkedListNode<T> terminal)
        {
            if (first == null || terminal == null || first == terminal)
            {
                throw new GameFrameworkException("Range is invalid.");
            }

            _First = first;
            _Terminal = terminal;
        }

        /// <summary>
        /// 获取链表范围是否有效。
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _First != null && _Terminal != null && _First != _Terminal;
            }
        }

        /// <summary>
        /// 获取链表范围的开始结点。
        /// </summary>
        public LinkedListNode<T> First
        {
            get
            {
                return _First;
            }
        }

        /// <summary>
        /// 获取链表范围的终结标记结点。
        /// </summary>
        public LinkedListNode<T> Terminal
        {
            get
            {
                return _Terminal;
            }
        }

        /// <summary>
        /// 获取链表范围的结点数量。
        /// </summary>
        public int Count
        {
            get
            {
                if (!IsValid)
                {
                    return 0;
                }

                int count = 0;
                for (LinkedListNode<T> current = _First; current != null && current != _Terminal; current = current.Next)
                {
                    count++;
                }

                return count;
            }
        }

        /// <summary>
        /// 检查是否包含指定值。
        /// </summary>
        /// <param name="value">要检查的值。</param>
        /// <returns>是否包含指定值。</returns>
        public bool Contains(T value)
        {
            for (LinkedListNode<T> current = _First; current != null && current != _Terminal; current = current.Next)
            {
                if (current.Value.Equals(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 返回循环访问集合的枚举数。
        /// </summary>
        /// <returns>循环访问集合的枚举数。</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// 返回循环访问集合的枚举数。
        /// </summary>
        /// <returns>循环访问集合的枚举数。</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 返回循环访问集合的枚举数。
        /// </summary>
        /// <returns>循环访问集合的枚举数。</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 循环访问集合的枚举数。
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly GameFrameworkLinkedListRange<T> _GameFrameworkLinkedListRange;
            private LinkedListNode<T> _Current;
            private T _CurrentValue;

            internal Enumerator(GameFrameworkLinkedListRange<T> range)
            {
                if (!range.IsValid)
                {
                    throw new GameFrameworkException("Range is invalid.");
                }

                _GameFrameworkLinkedListRange = range;
                _Current = _GameFrameworkLinkedListRange._First;
                _CurrentValue = default(T);
            }

            /// <summary>
            /// 获取当前结点。
            /// </summary>
            public T Current
            {
                get
                {
                    return _CurrentValue;
                }
            }

            /// <summary>
            /// 获取当前的枚举数。
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    return _CurrentValue;
                }
            }

            /// <summary>
            /// 清理枚举数。
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// 获取下一个结点。
            /// </summary>
            /// <returns>返回下一个结点。</returns>
            public bool MoveNext()
            {
                if (_Current == null || _Current == _GameFrameworkLinkedListRange._Terminal)
                {
                    return false;
                }

                _CurrentValue = _Current.Value;
                _Current = _Current.Next;
                return true;
            }

            /// <summary>
            /// 重置枚举数。
            /// </summary>
            void IEnumerator.Reset()
            {
                _Current = _GameFrameworkLinkedListRange._First;
                _CurrentValue = default(T);
            }
        }
    }
}
