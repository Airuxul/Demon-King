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
            private sealed class DownloadCounterNode : IReference
            {
                private long _DeltaLength;
                private float _ElapseSeconds;

                public DownloadCounterNode()
                {
                    _DeltaLength = 0L;
                    _ElapseSeconds = 0f;
                }

                public long DeltaLength
                {
                    get
                    {
                        return _DeltaLength;
                    }
                }

                public float ElapseSeconds
                {
                    get
                    {
                        return _ElapseSeconds;
                    }
                }

                public static DownloadCounterNode Create()
                {
                    return ReferencePool.Acquire<DownloadCounterNode>();
                }

                public void Update(float elapseSeconds, float realElapseSeconds)
                {
                    _ElapseSeconds += realElapseSeconds;
                }

                public void AddDeltaLength(int deltaLength)
                {
                    _DeltaLength += deltaLength;
                }

                public void Clear()
                {
                    _DeltaLength = 0L;
                    _ElapseSeconds = 0f;
                }
            }
        }
    }
}
