//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace GameFramework.Network
{
    /// <summary>
    /// 网络管理器。
    /// </summary>
    internal sealed partial class NetworkManager : GameFrameworkModule, INetworkManager
    {
        private readonly Dictionary<string, NetworkChannelBase> _NetworkChannels;

        private EventHandler<NetworkConnectedEventArgs> _NetworkConnectedEventHandler;
        private EventHandler<NetworkClosedEventArgs> _NetworkClosedEventHandler;
        private EventHandler<NetworkMissHeartBeatEventArgs> _NetworkMissHeartBeatEventHandler;
        private EventHandler<NetworkErrorEventArgs> _NetworkErrorEventHandler;
        private EventHandler<NetworkCustomErrorEventArgs> _NetworkCustomErrorEventHandler;

        /// <summary>
        /// 初始化网络管理器的新实例。
        /// </summary>
        public NetworkManager()
        {
            _NetworkChannels = new Dictionary<string, NetworkChannelBase>(StringComparer.Ordinal);
            _NetworkConnectedEventHandler = null;
            _NetworkClosedEventHandler = null;
            _NetworkMissHeartBeatEventHandler = null;
            _NetworkErrorEventHandler = null;
            _NetworkCustomErrorEventHandler = null;
        }

        /// <summary>
        /// 获取网络频道数量。
        /// </summary>
        public int NetworkChannelCount
        {
            get
            {
                return _NetworkChannels.Count;
            }
        }

        /// <summary>
        /// 网络连接成功事件。
        /// </summary>
        public event EventHandler<NetworkConnectedEventArgs> NetworkConnected
        {
            add
            {
                _NetworkConnectedEventHandler += value;
            }
            remove
            {
                _NetworkConnectedEventHandler -= value;
            }
        }

        /// <summary>
        /// 网络连接关闭事件。
        /// </summary>
        public event EventHandler<NetworkClosedEventArgs> NetworkClosed
        {
            add
            {
                _NetworkClosedEventHandler += value;
            }
            remove
            {
                _NetworkClosedEventHandler -= value;
            }
        }

        /// <summary>
        /// 网络心跳包丢失事件。
        /// </summary>
        public event EventHandler<NetworkMissHeartBeatEventArgs> NetworkMissHeartBeat
        {
            add
            {
                _NetworkMissHeartBeatEventHandler += value;
            }
            remove
            {
                _NetworkMissHeartBeatEventHandler -= value;
            }
        }

        /// <summary>
        /// 网络错误事件。
        /// </summary>
        public event EventHandler<NetworkErrorEventArgs> NetworkError
        {
            add
            {
                _NetworkErrorEventHandler += value;
            }
            remove
            {
                _NetworkErrorEventHandler -= value;
            }
        }

        /// <summary>
        /// 用户自定义网络错误事件。
        /// </summary>
        public event EventHandler<NetworkCustomErrorEventArgs> NetworkCustomError
        {
            add
            {
                _NetworkCustomErrorEventHandler += value;
            }
            remove
            {
                _NetworkCustomErrorEventHandler -= value;
            }
        }

        /// <summary>
        /// 网络管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (KeyValuePair<string, NetworkChannelBase> networkChannel in _NetworkChannels)
            {
                networkChannel.Value.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理网络管理器。
        /// </summary>
        internal override void Shutdown()
        {
            foreach (KeyValuePair<string, NetworkChannelBase> networkChannel in _NetworkChannels)
            {
                NetworkChannelBase networkChannelBase = networkChannel.Value;
                networkChannelBase.NetworkChannelConnected -= OnNetworkChannelConnected;
                networkChannelBase.NetworkChannelClosed -= OnNetworkChannelClosed;
                networkChannelBase.NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
                networkChannelBase.NetworkChannelError -= OnNetworkChannelError;
                networkChannelBase.NetworkChannelCustomError -= OnNetworkChannelCustomError;
                networkChannelBase.Shutdown();
            }

            _NetworkChannels.Clear();
        }

        /// <summary>
        /// 检查是否存在网络频道。
        /// </summary>
        /// <param name="name">网络频道名称。</param>
        /// <returns>是否存在网络频道。</returns>
        public bool HasNetworkChannel(string name)
        {
            return _NetworkChannels.ContainsKey(name ?? string.Empty);
        }

        /// <summary>
        /// 获取网络频道。
        /// </summary>
        /// <param name="name">网络频道名称。</param>
        /// <returns>要获取的网络频道。</returns>
        public INetworkChannel GetNetworkChannel(string name)
        {
            NetworkChannelBase networkChannel = null;
            if (_NetworkChannels.TryGetValue(name ?? string.Empty, out networkChannel))
            {
                return networkChannel;
            }

            return null;
        }

        /// <summary>
        /// 获取所有网络频道。
        /// </summary>
        /// <returns>所有网络频道。</returns>
        public INetworkChannel[] GetAllNetworkChannels()
        {
            int index = 0;
            INetworkChannel[] results = new INetworkChannel[_NetworkChannels.Count];
            foreach (KeyValuePair<string, NetworkChannelBase> networkChannel in _NetworkChannels)
            {
                results[index++] = networkChannel.Value;
            }

            return results;
        }

        /// <summary>
        /// 获取所有网络频道。
        /// </summary>
        /// <param name="results">所有网络频道。</param>
        public void GetAllNetworkChannels(List<INetworkChannel> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<string, NetworkChannelBase> networkChannel in _NetworkChannels)
            {
                results.Add(networkChannel.Value);
            }
        }

        /// <summary>
        /// 创建网络频道。
        /// </summary>
        /// <param name="name">网络频道名称。</param>
        /// <param name="serviceType">网络服务类型。</param>
        /// <param name="networkChannelHelper">网络频道辅助器。</param>
        /// <returns>要创建的网络频道。</returns>
        public INetworkChannel CreateNetworkChannel(string name, ServiceType serviceType, INetworkChannelHelper networkChannelHelper)
        {
            if (networkChannelHelper == null)
            {
                throw new GameFrameworkException("Network channel helper is invalid.");
            }

            if (networkChannelHelper.PacketHeaderLength < 0)
            {
                throw new GameFrameworkException("Packet header length is invalid.");
            }

            if (HasNetworkChannel(name))
            {
                throw new GameFrameworkException(Utility.Text.Format("Already exist network channel '{0}'.", name ?? string.Empty));
            }

            NetworkChannelBase networkChannel = null;
            switch (serviceType)
            {
                case ServiceType.Tcp:
                    networkChannel = new TcpNetworkChannel(name, networkChannelHelper);
                    break;

                case ServiceType.TcpWithSyncReceive:
                    networkChannel = new TcpWithSyncReceiveNetworkChannel(name, networkChannelHelper);
                    break;

                default:
                    throw new GameFrameworkException(Utility.Text.Format("Not supported service type '{0}'.", serviceType));
            }

            networkChannel.NetworkChannelConnected += OnNetworkChannelConnected;
            networkChannel.NetworkChannelClosed += OnNetworkChannelClosed;
            networkChannel.NetworkChannelMissHeartBeat += OnNetworkChannelMissHeartBeat;
            networkChannel.NetworkChannelError += OnNetworkChannelError;
            networkChannel.NetworkChannelCustomError += OnNetworkChannelCustomError;
            _NetworkChannels.Add(name, networkChannel);
            return networkChannel;
        }

        /// <summary>
        /// 销毁网络频道。
        /// </summary>
        /// <param name="name">网络频道名称。</param>
        /// <returns>是否销毁网络频道成功。</returns>
        public bool DestroyNetworkChannel(string name)
        {
            NetworkChannelBase networkChannel = null;
            if (_NetworkChannels.TryGetValue(name ?? string.Empty, out networkChannel))
            {
                networkChannel.NetworkChannelConnected -= OnNetworkChannelConnected;
                networkChannel.NetworkChannelClosed -= OnNetworkChannelClosed;
                networkChannel.NetworkChannelMissHeartBeat -= OnNetworkChannelMissHeartBeat;
                networkChannel.NetworkChannelError -= OnNetworkChannelError;
                networkChannel.NetworkChannelCustomError -= OnNetworkChannelCustomError;
                networkChannel.Shutdown();
                return _NetworkChannels.Remove(name);
            }

            return false;
        }

        private void OnNetworkChannelConnected(NetworkChannelBase networkChannel, object userData)
        {
            if (_NetworkConnectedEventHandler != null)
            {
                lock (_NetworkConnectedEventHandler)
                {
                    NetworkConnectedEventArgs networkConnectedEventArgs = NetworkConnectedEventArgs.Create(networkChannel, userData);
                    _NetworkConnectedEventHandler(this, networkConnectedEventArgs);
                    ReferencePool.Release(networkConnectedEventArgs);
                }
            }
        }

        private void OnNetworkChannelClosed(NetworkChannelBase networkChannel)
        {
            if (_NetworkClosedEventHandler != null)
            {
                lock (_NetworkClosedEventHandler)
                {
                    NetworkClosedEventArgs networkClosedEventArgs = NetworkClosedEventArgs.Create(networkChannel);
                    _NetworkClosedEventHandler(this, networkClosedEventArgs);
                    ReferencePool.Release(networkClosedEventArgs);
                }
            }
        }

        private void OnNetworkChannelMissHeartBeat(NetworkChannelBase networkChannel, int missHeartBeatCount)
        {
            if (_NetworkMissHeartBeatEventHandler != null)
            {
                lock (_NetworkMissHeartBeatEventHandler)
                {
                    NetworkMissHeartBeatEventArgs networkMissHeartBeatEventArgs = NetworkMissHeartBeatEventArgs.Create(networkChannel, missHeartBeatCount);
                    _NetworkMissHeartBeatEventHandler(this, networkMissHeartBeatEventArgs);
                    ReferencePool.Release(networkMissHeartBeatEventArgs);
                }
            }
        }

        private void OnNetworkChannelError(NetworkChannelBase networkChannel, NetworkErrorCode errorCode, SocketError socketErrorCode, string errorMessage)
        {
            if (_NetworkErrorEventHandler != null)
            {
                lock (_NetworkErrorEventHandler)
                {
                    NetworkErrorEventArgs networkErrorEventArgs = NetworkErrorEventArgs.Create(networkChannel, errorCode, socketErrorCode, errorMessage);
                    _NetworkErrorEventHandler(this, networkErrorEventArgs);
                    ReferencePool.Release(networkErrorEventArgs);
                }
            }
        }

        private void OnNetworkChannelCustomError(NetworkChannelBase networkChannel, object customErrorData)
        {
            if (_NetworkCustomErrorEventHandler != null)
            {
                lock (_NetworkCustomErrorEventHandler)
                {
                    NetworkCustomErrorEventArgs networkCustomErrorEventArgs = NetworkCustomErrorEventArgs.Create(networkChannel, customErrorData);
                    _NetworkCustomErrorEventHandler(this, networkCustomErrorEventArgs);
                    ReferencePool.Release(networkCustomErrorEventArgs);
                }
            }
        }
    }
}
