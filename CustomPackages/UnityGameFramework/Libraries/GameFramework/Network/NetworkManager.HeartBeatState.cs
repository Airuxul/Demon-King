//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Network
{
    internal sealed partial class NetworkManager : GameFrameworkModule, INetworkManager
    {
        private sealed class HeartBeatState
        {
            private float _HeartBeatElapseSeconds;
            private int _MissHeartBeatCount;

            public HeartBeatState()
            {
                _HeartBeatElapseSeconds = 0f;
                _MissHeartBeatCount = 0;
            }

            public float HeartBeatElapseSeconds
            {
                get
                {
                    return _HeartBeatElapseSeconds;
                }
                set
                {
                    _HeartBeatElapseSeconds = value;
                }
            }

            public int MissHeartBeatCount
            {
                get
                {
                    return _MissHeartBeatCount;
                }
                set
                {
                    _MissHeartBeatCount = value;
                }
            }

            public void Reset(bool resetHeartBeatElapseSeconds)
            {
                if (resetHeartBeatElapseSeconds)
                {
                    _HeartBeatElapseSeconds = 0f;
                }

                _MissHeartBeatCount = 0;
            }
        }
    }
}
