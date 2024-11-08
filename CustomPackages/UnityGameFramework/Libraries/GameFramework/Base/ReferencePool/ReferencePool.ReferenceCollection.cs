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
    public static partial class ReferencePool
    {
        private sealed class ReferenceCollection
        {
            private readonly Queue<IReference> _References;
            private readonly Type _ReferenceType;
            private int _UsingReferenceCount;
            private int _AcquireReferenceCount;
            private int _ReleaseReferenceCount;
            private int _AddReferenceCount;
            private int _RemoveReferenceCount;

            public ReferenceCollection(Type referenceType)
            {
                _References = new Queue<IReference>();
                _ReferenceType = referenceType;
                _UsingReferenceCount = 0;
                _AcquireReferenceCount = 0;
                _ReleaseReferenceCount = 0;
                _AddReferenceCount = 0;
                _RemoveReferenceCount = 0;
            }

            public Type ReferenceType
            {
                get
                {
                    return _ReferenceType;
                }
            }

            public int UnusedReferenceCount
            {
                get
                {
                    return _References.Count;
                }
            }

            public int UsingReferenceCount
            {
                get
                {
                    return _UsingReferenceCount;
                }
            }

            public int AcquireReferenceCount
            {
                get
                {
                    return _AcquireReferenceCount;
                }
            }

            public int ReleaseReferenceCount
            {
                get
                {
                    return _ReleaseReferenceCount;
                }
            }

            public int AddReferenceCount
            {
                get
                {
                    return _AddReferenceCount;
                }
            }

            public int RemoveReferenceCount
            {
                get
                {
                    return _RemoveReferenceCount;
                }
            }

            public T Acquire<T>() where T : class, IReference, new()
            {
                if (typeof(T) != _ReferenceType)
                {
                    throw new GameFrameworkException("Type is invalid.");
                }

                _UsingReferenceCount++;
                _AcquireReferenceCount++;
                lock (_References)
                {
                    if (_References.Count > 0)
                    {
                        return (T)_References.Dequeue();
                    }
                }

                _AddReferenceCount++;
                return new T();
            }

            public IReference Acquire()
            {
                _UsingReferenceCount++;
                _AcquireReferenceCount++;
                lock (_References)
                {
                    if (_References.Count > 0)
                    {
                        return _References.Dequeue();
                    }
                }

                _AddReferenceCount++;
                return (IReference)Activator.CreateInstance(_ReferenceType);
            }

            public void Release(IReference reference)
            {
                reference.Clear();
                lock (_References)
                {
                    if (_EnableStrictCheck && _References.Contains(reference))
                    {
                        throw new GameFrameworkException("The reference has been released.");
                    }

                    _References.Enqueue(reference);
                }

                _ReleaseReferenceCount++;
                _UsingReferenceCount--;
            }

            public void Add<T>(int count) where T : class, IReference, new()
            {
                if (typeof(T) != _ReferenceType)
                {
                    throw new GameFrameworkException("Type is invalid.");
                }

                lock (_References)
                {
                    _AddReferenceCount += count;
                    while (count-- > 0)
                    {
                        _References.Enqueue(new T());
                    }
                }
            }

            public void Add(int count)
            {
                lock (_References)
                {
                    _AddReferenceCount += count;
                    while (count-- > 0)
                    {
                        _References.Enqueue((IReference)Activator.CreateInstance(_ReferenceType));
                    }
                }
            }

            public void Remove(int count)
            {
                lock (_References)
                {
                    if (count > _References.Count)
                    {
                        count = _References.Count;
                    }

                    _RemoveReferenceCount += count;
                    while (count-- > 0)
                    {
                        _References.Dequeue();
                    }
                }
            }

            public void RemoveAll()
            {
                lock (_References)
                {
                    _RemoveReferenceCount += _References.Count;
                    _References.Clear();
                }
            }
        }
    }
}
