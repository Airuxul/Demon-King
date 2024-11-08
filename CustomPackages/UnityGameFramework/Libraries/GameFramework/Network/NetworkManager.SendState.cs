//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.IO;

namespace GameFramework.Network
{
    internal sealed partial class NetworkManager : GameFrameworkModule, INetworkManager
    {
        private sealed class SendState : IDisposable
        {
            private const int DefaultBufferLength = 1024 * 64;
            private MemoryStream _Stream;
            private bool _Disposed;

            public SendState()
            {
                _Stream = new MemoryStream(DefaultBufferLength);
                _Disposed = false;
            }

            public MemoryStream Stream
            {
                get
                {
                    return _Stream;
                }
            }

            public void Reset()
            {
                _Stream.Position = 0L;
                _Stream.SetLength(0L);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (_Disposed)
                {
                    return;
                }

                if (disposing)
                {
                    if (_Stream != null)
                    {
                        _Stream.Dispose();
                        _Stream = null;
                    }
                }

                _Disposed = true;
            }
        }
    }
}
