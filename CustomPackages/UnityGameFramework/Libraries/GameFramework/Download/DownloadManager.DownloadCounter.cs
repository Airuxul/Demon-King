//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Download
{
    internal sealed partial class DownloadManager : GameFrameworkModule, IDownloadManager
    {
        private sealed partial class DownloadCounter
        {
            private readonly GameFrameworkLinkedList<DownloadCounterNode> _DownloadCounterNodes;
            private float _UpdateInterval;
            private float _RecordInterval;
            private float _CurrentSpeed;
            private float _Accumulator;
            private float _TimeLeft;

            public DownloadCounter(float updateInterval, float recordInterval)
            {
                if (updateInterval <= 0f)
                {
                    throw new GameFrameworkException("Update interval is invalid.");
                }

                if (recordInterval <= 0f)
                {
                    throw new GameFrameworkException("Record interval is invalid.");
                }

                _DownloadCounterNodes = new GameFrameworkLinkedList<DownloadCounterNode>();
                _UpdateInterval = updateInterval;
                _RecordInterval = recordInterval;
                Reset();
            }

            public float UpdateInterval
            {
                get
                {
                    return _UpdateInterval;
                }
                set
                {
                    if (value <= 0f)
                    {
                        throw new GameFrameworkException("Update interval is invalid.");
                    }

                    _UpdateInterval = value;
                    Reset();
                }
            }

            public float RecordInterval
            {
                get
                {
                    return _RecordInterval;
                }
                set
                {
                    if (value <= 0f)
                    {
                        throw new GameFrameworkException("Record interval is invalid.");
                    }

                    _RecordInterval = value;
                    Reset();
                }
            }

            public float CurrentSpeed
            {
                get
                {
                    return _CurrentSpeed;
                }
            }

            public void Shutdown()
            {
                Reset();
            }

            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (_DownloadCounterNodes.Count <= 0)
                {
                    return;
                }

                _Accumulator += realElapseSeconds;
                if (_Accumulator > _RecordInterval)
                {
                    _Accumulator = _RecordInterval;
                }

                _TimeLeft -= realElapseSeconds;
                foreach (DownloadCounterNode downloadCounterNode in _DownloadCounterNodes)
                {
                    downloadCounterNode.Update(elapseSeconds, realElapseSeconds);
                }

                while (_DownloadCounterNodes.Count > 0)
                {
                    DownloadCounterNode downloadCounterNode = _DownloadCounterNodes.First.Value;
                    if (downloadCounterNode.ElapseSeconds < _RecordInterval)
                    {
                        break;
                    }

                    ReferencePool.Release(downloadCounterNode);
                    _DownloadCounterNodes.RemoveFirst();
                }

                if (_DownloadCounterNodes.Count <= 0)
                {
                    Reset();
                    return;
                }

                if (_TimeLeft <= 0f)
                {
                    long totalDeltaLength = 0L;
                    foreach (DownloadCounterNode downloadCounterNode in _DownloadCounterNodes)
                    {
                        totalDeltaLength += downloadCounterNode.DeltaLength;
                    }

                    _CurrentSpeed = _Accumulator > 0f ? totalDeltaLength / _Accumulator : 0f;
                    _TimeLeft += _UpdateInterval;
                }
            }

            public void RecordDeltaLength(int deltaLength)
            {
                if (deltaLength <= 0)
                {
                    return;
                }

                DownloadCounterNode downloadCounterNode = null;
                if (_DownloadCounterNodes.Count > 0)
                {
                    downloadCounterNode = _DownloadCounterNodes.Last.Value;
                    if (downloadCounterNode.ElapseSeconds < _UpdateInterval)
                    {
                        downloadCounterNode.AddDeltaLength(deltaLength);
                        return;
                    }
                }

                downloadCounterNode = DownloadCounterNode.Create();
                downloadCounterNode.AddDeltaLength(deltaLength);
                _DownloadCounterNodes.AddLast(downloadCounterNode);
            }

            private void Reset()
            {
                _DownloadCounterNodes.Clear();
                _CurrentSpeed = 0f;
                _Accumulator = 0f;
                _TimeLeft = 0f;
            }
        }
    }
}
