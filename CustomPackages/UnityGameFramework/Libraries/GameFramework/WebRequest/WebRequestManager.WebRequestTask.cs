//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.WebRequest
{
    internal sealed partial class WebRequestManager : GameFrameworkModule, IWebRequestManager
    {
        /// <summary>
        /// Web 请求任务。
        /// </summary>
        private sealed class WebRequestTask : TaskBase
        {
            private static int s_Serial = 0;

            private WebRequestTaskStatus _Status;
            private string _WebRequestUri;
            private byte[] _PostData;
            private float _Timeout;

            public WebRequestTask()
            {
                _Status = WebRequestTaskStatus.Todo;
                _WebRequestUri = null;
                _PostData = null;
                _Timeout = 0f;
            }

            /// <summary>
            /// 获取或设置 Web 请求任务的状态。
            /// </summary>
            public WebRequestTaskStatus Status
            {
                get
                {
                    return _Status;
                }
                set
                {
                    _Status = value;
                }
            }

            /// <summary>
            /// 获取要发送的远程地址。
            /// </summary>
            public string WebRequestUri
            {
                get
                {
                    return _WebRequestUri;
                }
            }

            /// <summary>
            /// 获取 Web 请求超时时长，以秒为单位。
            /// </summary>
            public float Timeout
            {
                get
                {
                    return _Timeout;
                }
            }

            /// <summary>
            /// 获取 Web 请求任务的描述。
            /// </summary>
            public override string Description
            {
                get
                {
                    return _WebRequestUri;
                }
            }

            /// <summary>
            /// 创建 Web 请求任务。
            /// </summary>
            /// <param name="webRequestUri">要发送的远程地址。</param>
            /// <param name="postData">要发送的数据流。</param>
            /// <param name="tag">Web 请求任务的标签。</param>
            /// <param name="priority">Web 请求任务的优先级。</param>
            /// <param name="timeout">下载超时时长，以秒为单位。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <returns>创建的 Web 请求任务。</returns>
            public static WebRequestTask Create(string webRequestUri, byte[] postData, string tag, int priority, float timeout, object userData)
            {
                WebRequestTask webRequestTask = ReferencePool.Acquire<WebRequestTask>();
                webRequestTask.Initialize(++s_Serial, tag, priority, userData);
                webRequestTask._WebRequestUri = webRequestUri;
                webRequestTask._PostData = postData;
                webRequestTask._Timeout = timeout;
                return webRequestTask;
            }

            /// <summary>
            /// 清理 Web 请求任务。
            /// </summary>
            public override void Clear()
            {
                base.Clear();
                _Status = WebRequestTaskStatus.Todo;
                _WebRequestUri = null;
                _PostData = null;
                _Timeout = 0f;
            }

            /// <summary>
            /// 获取要发送的数据流。
            /// </summary>
            public byte[] GetPostData()
            {
                return _PostData;
            }
        }
    }
}
