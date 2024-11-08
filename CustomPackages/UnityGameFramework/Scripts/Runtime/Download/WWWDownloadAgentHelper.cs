//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

#if !UNITY_2018_3_OR_NEWER

using GameFramework;
using GameFramework.Download;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// WWW 下载代理辅助器。
    /// </summary>
    public class WWWDownloadAgentHelper : DownloadAgentHelperBase, IDisposable
    {
        private WWW _WWW = null;
        private int _LastDownloadedSize = 0;
        private bool _Disposed = false;

        private EventHandler<DownloadAgentHelperUpdateBytesEventArgs> _DownloadAgentHelperUpdateBytesEventHandler = null;
        private EventHandler<DownloadAgentHelperUpdateLengthEventArgs> _DownloadAgentHelperUpdateLengthEventHandler = null;
        private EventHandler<DownloadAgentHelperCompleteEventArgs> _DownloadAgentHelperCompleteEventHandler = null;
        private EventHandler<DownloadAgentHelperErrorEventArgs> _DownloadAgentHelperErrorEventHandler = null;

        /// <summary>
        /// 下载代理辅助器更新数据流事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperUpdateBytesEventArgs> DownloadAgentHelperUpdateBytes
        {
            add
            {
                _DownloadAgentHelperUpdateBytesEventHandler += value;
            }
            remove
            {
                _DownloadAgentHelperUpdateBytesEventHandler -= value;
            }
        }

        /// <summary>
        /// 下载代理辅助器更新数据大小事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperUpdateLengthEventArgs> DownloadAgentHelperUpdateLength
        {
            add
            {
                _DownloadAgentHelperUpdateLengthEventHandler += value;
            }
            remove
            {
                _DownloadAgentHelperUpdateLengthEventHandler -= value;
            }
        }

        /// <summary>
        /// 下载代理辅助器完成事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperCompleteEventArgs> DownloadAgentHelperComplete
        {
            add
            {
                _DownloadAgentHelperCompleteEventHandler += value;
            }
            remove
            {
                _DownloadAgentHelperCompleteEventHandler -= value;
            }
        }

        /// <summary>
        /// 下载代理辅助器错误事件。
        /// </summary>
        public override event EventHandler<DownloadAgentHelperErrorEventArgs> DownloadAgentHelperError
        {
            add
            {
                _DownloadAgentHelperErrorEventHandler += value;
            }
            remove
            {
                _DownloadAgentHelperErrorEventHandler -= value;
            }
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Download(string downloadUri, object userData)
        {
            if (_DownloadAgentHelperUpdateBytesEventHandler == null || _DownloadAgentHelperUpdateLengthEventHandler == null || _DownloadAgentHelperCompleteEventHandler == null || _DownloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            _WWW = new WWW(downloadUri);
        }

        /// <summary>
        /// 通过下载代理辅助器下载指定地址的数据。
        /// </summary>
        /// <param name="downloadUri">下载地址。</param>
        /// <param name="fromPosition">下载数据起始位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Download(string downloadUri, long fromPosition, object userData)
        {
            if (_DownloadAgentHelperUpdateBytesEventHandler == null || _DownloadAgentHelperUpdateLengthEventHandler == null || _DownloadAgentHelperCompleteEventHandler == null || _DownloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            Dictionary<string, string> header = new Dictionary<string, string>
            {
                { "Range", Utility.Text.Format("bytes={0}-", fromPosition) }
            };

            _WWW = new WWW(downloadUri, null, header);
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
            if (_DownloadAgentHelperUpdateBytesEventHandler == null || _DownloadAgentHelperUpdateLengthEventHandler == null || _DownloadAgentHelperCompleteEventHandler == null || _DownloadAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Download agent helper handler is invalid.");
                return;
            }

            Dictionary<string, string> header = new Dictionary<string, string>
            {
                { "Range", Utility.Text.Format("bytes={0}-{1}", fromPosition, toPosition) }
            };

            _WWW = new WWW(downloadUri, null, header);
        }

        /// <summary>
        /// 重置下载代理辅助器。
        /// </summary>
        public override void Reset()
        {
            if (_WWW != null)
            {
                _WWW.Dispose();
                _WWW = null;
            }

            _LastDownloadedSize = 0;
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
            if (_Disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_WWW != null)
                {
                    _WWW.Dispose();
                    _WWW = null;
                }
            }

            _Disposed = true;
        }

        private void Update()
        {
            if (_WWW == null)
            {
                return;
            }

            int deltaLength = _WWW.bytesDownloaded - _LastDownloadedSize;
            if (deltaLength > 0)
            {
                _LastDownloadedSize = _WWW.bytesDownloaded;
                DownloadAgentHelperUpdateLengthEventArgs downloadAgentHelperUpdateLengthEventArgs = DownloadAgentHelperUpdateLengthEventArgs.Create(deltaLength);
                _DownloadAgentHelperUpdateLengthEventHandler(this, downloadAgentHelperUpdateLengthEventArgs);
                ReferencePool.Release(downloadAgentHelperUpdateLengthEventArgs);
            }

            if (_WWW == null)
            {
                return;
            }

            if (!_WWW.isDone)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_WWW.error))
            {
                DownloadAgentHelperErrorEventArgs dodwnloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(_WWW.error.StartsWith(RangeNotSatisfiableErrorCode.ToString(), StringComparison.Ordinal), _WWW.error);
                _DownloadAgentHelperErrorEventHandler(this, dodwnloadAgentHelperErrorEventArgs);
                ReferencePool.Release(dodwnloadAgentHelperErrorEventArgs);
            }
            else
            {
                byte[] bytes = _WWW.bytes;
                DownloadAgentHelperUpdateBytesEventArgs downloadAgentHelperUpdateBytesEventArgs = DownloadAgentHelperUpdateBytesEventArgs.Create(bytes, 0, bytes.Length);
                _DownloadAgentHelperUpdateBytesEventHandler(this, downloadAgentHelperUpdateBytesEventArgs);
                ReferencePool.Release(downloadAgentHelperUpdateBytesEventArgs);

                DownloadAgentHelperCompleteEventArgs downloadAgentHelperCompleteEventArgs = DownloadAgentHelperCompleteEventArgs.Create(bytes.LongLength);
                _DownloadAgentHelperCompleteEventHandler(this, downloadAgentHelperCompleteEventArgs);
                ReferencePool.Release(downloadAgentHelperCompleteEventArgs);
            }
        }
    }
}

#endif
