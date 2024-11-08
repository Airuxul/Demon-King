//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Net.Sockets;

namespace GameFramework.Network
{
    internal sealed partial class NetworkManager : GameFrameworkModule, INetworkManager
    {
        private sealed class ConnectState
        {
            private readonly Socket _Socket;
            private readonly object _UserData;

            public ConnectState(Socket socket, object userData)
            {
                _Socket = socket;
                _UserData = userData;
            }

            public Socket Socket
            {
                get
                {
                    return _Socket;
                }
            }

            public object UserData
            {
                get
                {
                    return _UserData;
                }
            }
        }
    }
}
