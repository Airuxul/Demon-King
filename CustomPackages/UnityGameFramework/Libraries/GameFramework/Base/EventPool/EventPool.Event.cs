//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework
{
    internal sealed partial class EventPool<T> where T : BaseEventArgs
    {
        /// <summary>
        /// 事件结点。
        /// </summary>
        private sealed class Event : IReference
        {
            private object _Sender;
            private T _EventArgs;

            public Event()
            {
                _Sender = null;
                _EventArgs = null;
            }

            public object Sender
            {
                get
                {
                    return _Sender;
                }
            }

            public T EventArgs
            {
                get
                {
                    return _EventArgs;
                }
            }

            public static Event Create(object sender, T e)
            {
                Event eventNode = ReferencePool.Acquire<Event>();
                eventNode._Sender = sender;
                eventNode._EventArgs = e;
                return eventNode;
            }

            public void Clear()
            {
                _Sender = null;
                _EventArgs = null;
            }
        }
    }
}
