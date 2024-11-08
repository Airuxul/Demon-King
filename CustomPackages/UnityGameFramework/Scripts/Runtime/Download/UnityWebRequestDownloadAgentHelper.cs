//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Download;
using System;
#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#else
using UnityEngine.Experimental.Networking;
#endif
using Utility = GameFramework.Utility;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 使用 UnityWebRequest 实现的下载代理辅助器。
    /// </summary>
    public partial class UnityWebRequestDownloadAgentHelper : DownloadAgentHelperBase, IDisposable
    {
        private const int CachedBytesLength = 0x1000;
        private readonly byte[] _cachedBytes = new byte[CachedBytesLength];

        private UnityWebRequest _unityWebRequest = null;
        private bool _disposed = false;

        private EventHandler<DownloadAgentHelperUpdateBytesEventArgs> _downloadAgentHelperUpdateBytesEventHandler = null;
        private EventHandler<DownloadAgentHelperUpdateLengthEventArgs> _downloadAgentHelperUpdateLengthEventHandler = null;
        private EventHandler<DownloadAgentHelperCompleteEventArgs> _downloadAgentHelperCompleteEventHandler = null;
        private EventHandler<DownloadAgentHelperErrorEventArgs> _downloadAgentHelperErrorEventHandler = null;

        /// <summary>
        /// 下载代理辅助器更新数据流事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperUpdateBytesEventArgs> DownloadAgentHelperUpdateBytes
        {
            add => _downloadAgentHelperUpdateBytesEventHandler += value;
            remove => _downloadAgentHelperUpdateBytesEventHandler -= value;
        }

        /// <summary>
        /// 下载代理辅助器更新数据大小事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperUpdateLengthEventArgs> DownloadAgentHelperUpdateLength
        {
            add => _downloadAgentHelperUpdateLengthEventHandler += value;
            remove => _downloadAgentHelperUpdateLengthEventHandler -= value;
        }

        /// <summary>
        /// 下载代理辅助器完成事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperCompleteEventArgs> DownloadAgentHelperComplete
        {
            add => _downloadAgentHelperCompleteEventHandler += value;
            remove => _downloadAgentHelperCompleteEventHandler -= value;
        }

        /// <summary>
        /// 下载代理辅助器错误事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperErrorEventArgs> DownloadAgentHelperError
        {
            add => _downloadAgentHelperErrorEventHandler += value;
            remove => _downloadAgentHelperErrorEventHandler -= value;
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Download(string downloadUri, object userData)
        {
            if (_downloadAgentHelperUpdateBytesEventHandler == null || _downloadAgentHelperUpdateLengthEventHandler == null || _downloadAgentHelperCompleteEventHandler == null || _downloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            _unityWebRequest = new UnityWebRequest(downloadUri);
            _unityWebRequest.downloadHandler = new DownloadHandler(this);
#if UNITY_2017_2_OR_NEWER
            _unityWebRequest.SendWebRequest();
#else
            _UnityWebRequest.Send();
#endif
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        /// <param name="fromPosition">下载数据起始位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Download(string downloadUri, long fromPosition, object userData)
        {
            if (_downloadAgentHelperUpdateBytesEventHandler == null || _downloadAgentHelperUpdateLengthEventHandler == null || _downloadAgentHelperCompleteEventHandler == null || _downloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            _unityWebRequest = new UnityWebRequest(downloadUri);
            _unityWebRequest.SetRequestHeader("Range", Utility.Text.Format("bytes={0}-", fromPosition));
            _unityWebRequest.downloadHandler = new DownloadHandler(this);
#if UNITY_2017_2_OR_NEWER
            _unityWebRequest.SendWebRequest();
#else
            _UnityWebRequest.Send();
#endif
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        /// <param name="fromPosition">下载数据起始位置。</param>
        /// <param name="toPosition">下载数据结束位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Download(string downloadUri, long fromPosition, long toPosition, object userData)
        {
            if (_downloadAgentHelperUpdateBytesEventHandler == null || _downloadAgentHelperUpdateLengthEventHandler == null || _downloadAgentHelperCompleteEventHandler == null || _downloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            _unityWebRequest = new UnityWebRequest(downloadUri);
            _unityWebRequest.SetRequestHeader("Range", Utility.Text.Format("bytes={0}-{1}", fromPosition, toPosition));
            _unityWebRequest.downloadHandler = new DownloadHandler(this);
#if UNITY_2017_2_OR_NEWER
            _unityWebRequest.SendWebRequest();
#else
            _UnityWebRequest.Send();
#endif
        }

        /// <summary>
        /// 重置下载代理辅助器。
        /// </summary>
        public override void Reset()
        {
            if (_unityWebRequest != null)
            {
                _unityWebRequest.Abort();
                _unityWebRequest.Dispose();
                _unityWebRequest = null;
            }

            Array.Clear(_cachedBytes, 0, CachedBytesLength);
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
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_unityWebRequest != null)
                {
                    _unityWebRequest.Dispose();
                    _unityWebRequest = null;
                }
            }

            _disposed = true;
        }

        private void Update()
        {
            if (_unityWebRequest == null)
            {
                return;
            }

            if (!_unityWebRequest.isDone)
            {
                return;
            }

            bool isError = false;
#if UNITY_2020_2_OR_NEWER
            isError = _unityWebRequest.result != UnityWebRequest.Result.Success;
#elif UNITY_2017_1_OR_NEWER
            isError = _UnityWebRequest.isNetworkError || _UnityWebRequest.isHttpError;
#else
            isError = _UnityWebRequest.isError;
#endif
            if (isError)
            {
                DownloadAgentHelperErrorEventArgs downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(_unityWebRequest.responseCode == RangeNotSatisfiableErrorCode, _unityWebRequest.error);
                _downloadAgentHelperErrorEventHandler(this, downloadAgentHelperErrorEventArgs);
                ReferencePool.Release(downloadAgentHelperErrorEventArgs);
            }
            else
            {
                DownloadAgentHelperCompleteEventArgs downloadAgentHelperCompleteEventArgs = DownloadAgentHelperCompleteEventArgs.Create((long)_unityWebRequest.downloadedBytes);
                _downloadAgentHelperCompleteEventHandler(this, downloadAgentHelperCompleteEventArgs);
                ReferencePool.Release(downloadAgentHelperCompleteEventArgs);
            }
        }
    }
}
