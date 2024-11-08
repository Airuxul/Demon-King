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
        private sealed class ReceiveState : IDisposable
        {
            private const int DefaultBufferLength = 1024 * 64;
            private MemoryStream _Stream;
            private IPacketHeader _PacketHeader;
            private bool _Disposed;

            public ReceiveState()
            {
                _Stream = new MemoryStream(DefaultBufferLength);
                _PacketHeader = null;
                _Disposed = false;
            }

            public MemoryStream Stream
            {
                get
                {
                    return _Stream;
                }
            }

            public IPacketHeader PacketHeader
            {
                get
                {
                    return _PacketHeader;
                }
            }

            public void PrepareForPacketHeader(int packetHeaderLength)
            {
                Reset(packetHeaderLength, null);
            }

            public void PrepareForPacket(IPacketHeader packetHeader)
            {
                if (packetHeader == null)
                {
                    throw new GameFrameworkException("Packet header is invalid.");
                }

                Reset(packetHeader.PacketLength, packetHeader);
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

            private void Reset(int targetLength, IPacketHeader packetHeader)
            {
                if (targetLength < 0)
                {
                    throw new GameFrameworkException("Target length is invalid.");
                }

                _Stream.Position = 0L;
                _Stream.SetLength(targetLength);
                _PacketHeader = packetHeader;
            }
        }
    }
}
