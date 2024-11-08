//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace GameFramework.Network
{
    internal sealed partial class NetworkManager : GameFrameworkModule, INetworkManager
    {
        /// <summary>
        /// 网络频道基类。
        /// </summary>
        private abstract class NetworkChannelBase : INetworkChannel, IDisposable
        {
            private const float DefaultHeartBeatInterval = 30f;

            private readonly string _Name;
            protected readonly Queue<Packet> _SendPacketPool;
            protected readonly EventPool<Packet> _ReceivePacketPool;
            protected readonly INetworkChannelHelper _NetworkChannelHelper;
            protected AddressFamily _AddressFamily;
            protected bool _ResetHeartBeatElapseSecondsWhenReceivePacket;
            protected float _HeartBeatInterval;
            protected Socket _Socket;
            protected readonly SendState _SendState;
            protected readonly ReceiveState _ReceiveState;
            protected readonly HeartBeatState _HeartBeatState;
            protected int _SentPacketCount;
            protected int _ReceivedPacketCount;
            protected bool _Active;
            private bool _Disposed;

            public GameFrameworkAction<NetworkChannelBase, object> NetworkChannelConnected;
            public GameFrameworkAction<NetworkChannelBase> NetworkChannelClosed;
            public GameFrameworkAction<NetworkChannelBase, int> NetworkChannelMissHeartBeat;
            public GameFrameworkAction<NetworkChannelBase, NetworkErrorCode, SocketError, string> NetworkChannelError;
            public GameFrameworkAction<NetworkChannelBase, object> NetworkChannelCustomError;

            /// <summary>
            /// 初始化网络频道基类的新实例。
            /// </summary>
            /// <param name="name">网络频道名称。</param>
            /// <param name="networkChannelHelper">网络频道辅助器。</param>
            public NetworkChannelBase(string name, INetworkChannelHelper networkChannelHelper)
            {
                _Name = name ?? string.Empty;
                _SendPacketPool = new Queue<Packet>();
                _ReceivePacketPool = new EventPool<Packet>(EventPoolMode.Default);
                _NetworkChannelHelper = networkChannelHelper;
                _AddressFamily = AddressFamily.Unknown;
                _ResetHeartBeatElapseSecondsWhenReceivePacket = false;
                _HeartBeatInterval = DefaultHeartBeatInterval;
                _Socket = null;
                _SendState = new SendState();
                _ReceiveState = new ReceiveState();
                _HeartBeatState = new HeartBeatState();
                _SentPacketCount = 0;
                _ReceivedPacketCount = 0;
                _Active = false;
                _Disposed = false;

                NetworkChannelConnected = null;
                NetworkChannelClosed = null;
                NetworkChannelMissHeartBeat = null;
                NetworkChannelError = null;
                NetworkChannelCustomError = null;

                networkChannelHelper.Initialize(this);
            }

            /// <summary>
            /// 获取网络频道名称。
            /// </summary>
            public string Name
            {
                get
                {
                    return _Name;
                }
            }

            /// <summary>
            /// 获取网络频道所使用的 Socket。
            /// </summary>
            public Socket Socket
            {
                get
                {
                    return _Socket;
                }
            }

            /// <summary>
            /// 获取是否已连接。
            /// </summary>
            public bool Connected
            {
                get
                {
                    if (_Socket != null)
                    {
                        return _Socket.Connected;
                    }

                    return false;
                }
            }

            /// <summary>
            /// 获取网络服务类型。
            /// </summary>
            public abstract ServiceType ServiceType
            {
                get;
            }

            /// <summary>
            /// 获取网络地址类型。
            /// </summary>
            public AddressFamily AddressFamily
            {
                get
                {
                    return _AddressFamily;
                }
            }

            /// <summary>
            /// 获取要发送的消息包数量。
            /// </summary>
            public int SendPacketCount
            {
                get
                {
                    return _SendPacketPool.Count;
                }
            }

            /// <summary>
            /// 获取累计发送的消息包数量。
            /// </summary>
            public int SentPacketCount
            {
                get
                {
                    return _SentPacketCount;
                }
            }

            /// <summary>
            /// 获取已接收未处理的消息包数量。
            /// </summary>
            public int ReceivePacketCount
            {
                get
                {
                    return _ReceivePacketPool.EventCount;
                }
            }

            /// <summary>
            /// 获取累计已接收的消息包数量。
            /// </summary>
            public int ReceivedPacketCount
            {
                get
                {
                    return _ReceivedPacketCount;
                }
            }

            /// <summary>
            /// 获取或设置当收到消息包时是否重置心跳流逝时间。
            /// </summary>
            public bool ResetHeartBeatElapseSecondsWhenReceivePacket
            {
                get
                {
                    return _ResetHeartBeatElapseSecondsWhenReceivePacket;
                }
                set
                {
                    _ResetHeartBeatElapseSecondsWhenReceivePacket = value;
                }
            }

            /// <summary>
            /// 获取丢失心跳的次数。
            /// </summary>
            public int MissHeartBeatCount
            {
                get
                {
                    return _HeartBeatState.MissHeartBeatCount;
                }
            }

            /// <summary>
            /// 获取或设置心跳间隔时长，以秒为单位。
            /// </summary>
            public float HeartBeatInterval
            {
                get
                {
                    return _HeartBeatInterval;
                }
                set
                {
                    _HeartBeatInterval = value;
                }
            }

            /// <summary>
            /// 获取心跳等待时长，以秒为单位。
            /// </summary>
            public float HeartBeatElapseSeconds
            {
                get
                {
                    return _HeartBeatState.HeartBeatElapseSeconds;
                }
            }

            /// <summary>
            /// 网络频道轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public virtual void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (_Socket == null || !_Active)
                {
                    return;
                }

                ProcessSend();
                ProcessReceive();
                if (_Socket == null || !_Active)
                {
                    return;
                }

                _ReceivePacketPool.Update(elapseSeconds, realElapseSeconds);

                if (_HeartBeatInterval > 0f)
                {
                    bool sendHeartBeat = false;
                    int missHeartBeatCount = 0;
                    lock (_HeartBeatState)
                    {
                        if (_Socket == null || !_Active)
                        {
                            return;
                        }

                        _HeartBeatState.HeartBeatElapseSeconds += realElapseSeconds;
                        if (_HeartBeatState.HeartBeatElapseSeconds >= _HeartBeatInterval)
                        {
                            sendHeartBeat = true;
                            missHeartBeatCount = _HeartBeatState.MissHeartBeatCount;
                            _HeartBeatState.HeartBeatElapseSeconds = 0f;
                            _HeartBeatState.MissHeartBeatCount++;
                        }
                    }

                    if (sendHeartBeat && _NetworkChannelHelper.SendHeartBeat())
                    {
                        if (missHeartBeatCount > 0 && NetworkChannelMissHeartBeat != null)
                        {
                            NetworkChannelMissHeartBeat(this, missHeartBeatCount);
                        }
                    }
                }
            }

            /// <summary>
            /// 关闭网络频道。
            /// </summary>
            public virtual void Shutdown()
            {
                Close();
                _ReceivePacketPool.Shutdown();
                _NetworkChannelHelper.Shutdown();
            }

            /// <summary>
            /// 注册网络消息包处理函数。
            /// </summary>
            /// <param name="handler">要注册的网络消息包处理函数。</param>
            public void RegisterHandler(IPacketHandler handler)
            {
                if (handler == null)
                {
                    throw new GameFrameworkException("Packet handler is invalid.");
                }

                _ReceivePacketPool.Subscribe(handler.Id, handler.Handle);
            }

            /// <summary>
            /// 设置默认事件处理函数。
            /// </summary>
            /// <param name="handler">要设置的默认事件处理函数。</param>
            public void SetDefaultHandler(EventHandler<Packet> handler)
            {
                _ReceivePacketPool.SetDefaultHandler(handler);
            }

            /// <summary>
            /// 连接到远程主机。
            /// </summary>
            /// <param name="ipAddress">远程主机的 IP 地址。</param>
            /// <param name="port">远程主机的端口号。</param>
            public void Connect(IPAddress ipAddress, int port)
            {
                Connect(ipAddress, port, null);
            }

            /// <summary>
            /// 连接到远程主机。
            /// </summary>
            /// <param name="ipAddress">远程主机的 IP 地址。</param>
            /// <param name="port">远程主机的端口号。</param>
            /// <param name="userData">用户自定义数据。</param>
            public virtual void Connect(IPAddress ipAddress, int port, object userData)
            {
                if (_Socket != null)
                {
                    Close();
                    _Socket = null;
                }

                switch (ipAddress.AddressFamily)
                {
                    case System.Net.Sockets.AddressFamily.InterNetwork:
                        _AddressFamily = AddressFamily.IPv4;
                        break;

                    case System.Net.Sockets.AddressFamily.InterNetworkV6:
                        _AddressFamily = AddressFamily.IPv6;
                        break;

                    default:
                        string errorMessage = Utility.Text.Format("Not supported address family '{0}'.", ipAddress.AddressFamily);
                        if (NetworkChannelError != null)
                        {
                            NetworkChannelError(this, NetworkErrorCode.AddressFamilyError, SocketError.Success, errorMessage);
                            return;
                        }

                        throw new GameFrameworkException(errorMessage);
                }

                _SendState.Reset();
                _ReceiveState.PrepareForPacketHeader(_NetworkChannelHelper.PacketHeaderLength);
            }

            /// <summary>
            /// 关闭连接并释放所有相关资源。
            /// </summary>
            public void Close()
            {
                lock (this)
                {
                    if (_Socket == null)
                    {
                        return;
                    }

                    _Active = false;

                    try
                    {
                        _Socket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        _Socket.Close();
                        _Socket = null;

                        if (NetworkChannelClosed != null)
                        {
                            NetworkChannelClosed(this);
                        }
                    }

                    _SentPacketCount = 0;
                    _ReceivedPacketCount = 0;

                    lock (_SendPacketPool)
                    {
                        _SendPacketPool.Clear();
                    }

                    _ReceivePacketPool.Clear();

                    lock (_HeartBeatState)
                    {
                        _HeartBeatState.Reset(true);
                    }
                }
            }

            /// <summary>
            /// 向远程主机发送消息包。
            /// </summary>
            /// <typeparam name="T">消息包类型。</typeparam>
            /// <param name="packet">要发送的消息包。</param>
            public void Send<T>(T packet) where T : Packet
            {
                if (_Socket == null)
                {
                    string errorMessage = "You must connect first.";
                    if (NetworkChannelError != null)
                    {
                        NetworkChannelError(this, NetworkErrorCode.SendError, SocketError.Success, errorMessage);
                        return;
                    }

                    throw new GameFrameworkException(errorMessage);
                }

                if (!_Active)
                {
                    string errorMessage = "Socket is not active.";
                    if (NetworkChannelError != null)
                    {
                        NetworkChannelError(this, NetworkErrorCode.SendError, SocketError.Success, errorMessage);
                        return;
                    }

                    throw new GameFrameworkException(errorMessage);
                }

                if (packet == null)
                {
                    string errorMessage = "Packet is invalid.";
                    if (NetworkChannelError != null)
                    {
                        NetworkChannelError(this, NetworkErrorCode.SendError, SocketError.Success, errorMessage);
                        return;
                    }

                    throw new GameFrameworkException(errorMessage);
                }

                lock (_SendPacketPool)
                {
                    _SendPacketPool.Enqueue(packet);
                }
            }

            /// <summary>
            /// 释放资源。
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// 释放资源。
            /// </summary>
            /// <param name="disposing">释放资源标记。</param>
            private void Dispose(bool disposing)
            {
                if (_Disposed)
                {
                    return;
                }

                if (disposing)
                {
                    Close();
                    _SendState.Dispose();
                    _ReceiveState.Dispose();
                }

                _Disposed = true;
            }

            protected virtual bool ProcessSend()
            {
                if (_SendState.Stream.Length > 0 || _SendPacketPool.Count <= 0)
                {
                    return false;
                }

                while (_SendPacketPool.Count > 0)
                {
                    Packet packet = null;
                    lock (_SendPacketPool)
                    {
                        packet = _SendPacketPool.Dequeue();
                    }

                    bool serializeResult = false;
                    try
                    {
                        serializeResult = _NetworkChannelHelper.Serialize(packet, _SendState.Stream);
                    }
                    catch (Exception exception)
                    {
                        _Active = false;
                        if (NetworkChannelError != null)
                        {
                            SocketException socketException = exception as SocketException;
                            NetworkChannelError(this, NetworkErrorCode.SerializeError, socketException != null ? socketException.SocketErrorCode : SocketError.Success, exception.ToString());
                            return false;
                        }

                        throw;
                    }

                    if (!serializeResult)
                    {
                        string errorMessage = "Serialized packet failure.";
                        if (NetworkChannelError != null)
                        {
                            NetworkChannelError(this, NetworkErrorCode.SerializeError, SocketError.Success, errorMessage);
                            return false;
                        }

                        throw new GameFrameworkException(errorMessage);
                    }
                }

                _SendState.Stream.Position = 0L;
                return true;
            }

            protected virtual void ProcessReceive()
            {
            }

            protected virtual bool ProcessPacketHeader()
            {
                try
                {
                    object customErrorData = null;
                    IPacketHeader packetHeader = _NetworkChannelHelper.DeserializePacketHeader(_ReceiveState.Stream, out customErrorData);

                    if (customErrorData != null && NetworkChannelCustomError != null)
                    {
                        NetworkChannelCustomError(this, customErrorData);
                    }

                    if (packetHeader == null)
                    {
                        string errorMessage = "Packet header is invalid.";
                        if (NetworkChannelError != null)
                        {
                            NetworkChannelError(this, NetworkErrorCode.DeserializePacketHeaderError, SocketError.Success, errorMessage);
                            return false;
                        }

                        throw new GameFrameworkException(errorMessage);
                    }

                    _ReceiveState.PrepareForPacket(packetHeader);
                    if (packetHeader.PacketLength <= 0)
                    {
                        bool processSuccess = ProcessPacket();
                        _ReceivedPacketCount++;
                        return processSuccess;
                    }
                }
                catch (Exception exception)
                {
                    _Active = false;
                    if (NetworkChannelError != null)
                    {
                        SocketException socketException = exception as SocketException;
                        NetworkChannelError(this, NetworkErrorCode.DeserializePacketHeaderError, socketException != null ? socketException.SocketErrorCode : SocketError.Success, exception.ToString());
                        return false;
                    }

                    throw;
                }

                return true;
            }

            protected virtual bool ProcessPacket()
            {
                lock (_HeartBeatState)
                {
                    _HeartBeatState.Reset(_ResetHeartBeatElapseSecondsWhenReceivePacket);
                }

                try
                {
                    object customErrorData = null;
                    Packet packet = _NetworkChannelHelper.DeserializePacket(_ReceiveState.PacketHeader, _ReceiveState.Stream, out customErrorData);

                    if (customErrorData != null && NetworkChannelCustomError != null)
                    {
                        NetworkChannelCustomError(this, customErrorData);
                    }

                    if (packet != null)
                    {
                        _ReceivePacketPool.Fire(this, packet);
                    }

                    _ReceiveState.PrepareForPacketHeader(_NetworkChannelHelper.PacketHeaderLength);
                }
                catch (Exception exception)
                {
                    _Active = false;
                    if (NetworkChannelError != null)
                    {
                        SocketException socketException = exception as SocketException;
                        NetworkChannelError(this, NetworkErrorCode.DeserializePacketError, socketException != null ? socketException.SocketErrorCode : SocketError.Success, exception.ToString());
                        return false;
                    }

                    throw;
                }

                return true;
            }
        }
    }
}
