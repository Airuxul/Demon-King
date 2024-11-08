//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

#if !UNITY_2018_3_OR_NEWER

using GameFramework;
using GameFramework.WebRequest;
using System;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// WWW Web 请求代理辅助器。
    /// </summary>
    public class WWWWebRequestAgentHelper : WebRequestAgentHelperBase, IDisposable
    {
        private WWW _WWW = null;
        private bool _Disposed = false;

        private EventHandler<WebRequestAgentHelperCompleteEventArgs> _WebRequestAgentHelperCompleteEventHandler = null;
        private EventHandler<WebRequestAgentHelperErrorEventArgs> _WebRequestAgentHelperErrorEventHandler = null;

        /// <summary>
        /// Web 请求代理辅助器完成事件。
        /// </summary>
        public override event EventHandler<WebRequestAgentHelperCompleteEventArgs> WebRequestAgentHelperComplete
        {
            add
            {
                _WebRequestAgentHelperCompleteEventHandler += value;
            }
            remove
            {
                _WebRequestAgentHelperCompleteEventHandler -= value;
            }
        }

        /// <summary>
        /// Web 请求代理辅助器错误事件。
        /// </summary>
        public override event EventHandler<WebRequestAgentHelperErrorEventArgs> WebRequestAgentHelperError
        {
            add
            {
                _WebRequestAgentHelperErrorEventHandler += value;
            }
            remove
            {
                _WebRequestAgentHelperErrorEventHandler -= value;
            }
        }

        /// <summary>
        /// 通过 Web 请求代理辅助器发送请求。
        /// </summary>
        /// <param name="webRequestUri">要发送的远程地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Request(string webRequestUri, object userData)
        {
            if (_WebRequestAgentHelperCompleteEventHandler == null || _WebRequestAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Web request agent helper handler is invalid.");
                return;
            }

            WWWFormInfo wwwFormInfo = (WWWFormInfo)userData;
            if (wwwFormInfo.WWWForm == null)
            {
                _WWW = new WWW(webRequestUri);
            }
            else
            {
                _WWW = new WWW(webRequestUri, wwwFormInfo.WWWForm);
            }
        }

        /// <summary>
        /// 通过 Web 请求代理辅助器发送请求。
        /// </summary>
        /// <param name="webRequestUri">要发送的远程地址。</param>
        /// <param name="postData">要发送的数据流。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Request(string webRequestUri, byte[] postData, object userData)
        {
            if (_WebRequestAgentHelperCompleteEventHandler == null || _WebRequestAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Web request agent helper handler is invalid.");
                return;
            }

            _WWW = new WWW(webRequestUri, postData);
        }

        /// <summary>
        /// 重置 Web 请求代理辅助器。
        /// </summary>
        public override void Reset()
        {
            if (_WWW != null)
            {
                _WWW.Dispose();
                _WWW = null;
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
            if (_WWW == null || !_WWW.isDone)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_WWW.error))
            {
                WebRequestAgentHelperErrorEventArgs webRequestAgentHelperErrorEventArgs = WebRequestAgentHelperErrorEventArgs.Create(_WWW.error);
                _WebRequestAgentHelperErrorEventHandler(this, webRequestAgentHelperErrorEventArgs);
                ReferencePool.Release(webRequestAgentHelperErrorEventArgs);
            }
            else
            {
                WebRequestAgentHelperCompleteEventArgs webRequestAgentHelperCompleteEventArgs = WebRequestAgentHelperCompleteEventArgs.Create(_WWW.bytes);
                _WebRequestAgentHelperCompleteEventHandler(this, webRequestAgentHelperCompleteEventArgs);
                ReferencePool.Release(webRequestAgentHelperCompleteEventArgs);
            }
        }
    }
}

#endif
