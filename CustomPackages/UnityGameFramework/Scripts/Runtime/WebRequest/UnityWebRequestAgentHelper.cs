//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#else
using UnityEngine.Experimental.Networking;
#endif
using System;
using GameFramework;
using GameFramework.WebRequest;
using Utility = GameFramework.Utility;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 使用 UnityWebRequest 实现的 Web 请求代理辅助器。
    /// </summary>
    public class UnityWebRequestAgentHelper : WebRequestAgentHelperBase, IDisposable
    {
        private UnityWebRequest _unityWebRequest;
        private bool _disposed;

        private EventHandler<WebRequestAgentHelperCompleteEventArgs> _webRequestAgentHelperCompleteEventHandler;
        private EventHandler<WebRequestAgentHelperErrorEventArgs> _webRequestAgentHelperErrorEventHandler;

        /// <summary>
        /// Web 请求代理辅助器完成事件。
        /// </summary>
        public override event EventHandler<WebRequestAgentHelperCompleteEventArgs> WebRequestAgentHelperComplete
        {
            add => _webRequestAgentHelperCompleteEventHandler += value;
            remove => _webRequestAgentHelperCompleteEventHandler -= value;
        }

        /// <summary>
        /// Web 请求代理辅助器错误事件。
        /// </summary>
        public override event EventHandler<WebRequestAgentHelperErrorEventArgs> WebRequestAgentHelperError
        {
            add => _webRequestAgentHelperErrorEventHandler += value;
            remove => _webRequestAgentHelperErrorEventHandler -= value;
        }

        /// <summary>
        /// 通过 Web 请求代理辅助器发送请求。
        /// </summary>
        /// <param name="webRequestUri">要发送的远程地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Request(string webRequestUri, object userData)
        {
            if (_webRequestAgentHelperCompleteEventHandler == null || _webRequestAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Web request agent helper handler is invalid.");
                return;
            }

            WWWFormInfo wwwFormInfo = (WWWFormInfo)userData;
            if (wwwFormInfo.WWWForm == null)
            {
                _unityWebRequest = UnityWebRequest.Get(webRequestUri);
            }
            else
            {
                _unityWebRequest = UnityWebRequest.Post(webRequestUri, wwwFormInfo.WWWForm);
            }

#if UNITY_2017_2_OR_NEWER
            _unityWebRequest.SendWebRequest();
#else
            _UnityWebRequest.Send();
#endif
        }

        /// <summary>
        /// 通过 Web 请求代理辅助器发送请求。
        /// </summary>
        /// <param name="webRequestUri">要发送的远程地址。</param>
        /// <param name="postData">要发送的数据流。</param>
        /// <param name="userData">用户自定义数据。</param>
        public override void Request(string webRequestUri, byte[] postData, object userData)
        {
            if (_webRequestAgentHelperCompleteEventHandler == null || _webRequestAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Web request agent helper handler is invalid.");
                return;
            }

            _unityWebRequest = UnityWebRequest.PostWwwForm(webRequestUri, Utility.Converter.GetString(postData));
#if UNITY_2017_2_OR_NEWER
            _unityWebRequest.SendWebRequest();
#else
            _UnityWebRequest.Send();
#endif
        }

        /// <summary>
        /// 重置 Web 请求代理辅助器。
        /// </summary>
        public override void Reset()
        {
            if (_unityWebRequest != null)
            {
                _unityWebRequest.Dispose();
                _unityWebRequest = null;
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
            if (_unityWebRequest == null || !_unityWebRequest.isDone)
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
                WebRequestAgentHelperErrorEventArgs webRequestAgentHelperErrorEventArgs = WebRequestAgentHelperErrorEventArgs.Create(_unityWebRequest.error);
                _webRequestAgentHelperErrorEventHandler(this, webRequestAgentHelperErrorEventArgs);
                ReferencePool.Release(webRequestAgentHelperErrorEventArgs);
            }
            else if (_unityWebRequest.downloadHandler.isDone)
            {
                WebRequestAgentHelperCompleteEventArgs webRequestAgentHelperCompleteEventArgs = WebRequestAgentHelperCompleteEventArgs.Create(_unityWebRequest.downloadHandler.data);
                _webRequestAgentHelperCompleteEventHandler(this, webRequestAgentHelperCompleteEventArgs);
                ReferencePool.Release(webRequestAgentHelperCompleteEventArgs);
            }
        }
    }
}
