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
        /// Web 请求代理。
        /// </summary>
        private sealed class WebRequestAgent : ITaskAgent<WebRequestTask>
        {
            private readonly IWebRequestAgentHelper _Helper;
            private WebRequestTask _Task;
            private float _WaitTime;

            public GameFrameworkAction<WebRequestAgent> WebRequestAgentStart;
            public GameFrameworkAction<WebRequestAgent, byte[]> WebRequestAgentSuccess;
            public GameFrameworkAction<WebRequestAgent, string> WebRequestAgentFailure;

            /// <summary>
            /// 初始化 Web 请求代理的新实例。
            /// </summary>
            /// <param name="webRequestAgentHelper">Web 请求代理辅助器。</param>
            public WebRequestAgent(IWebRequestAgentHelper webRequestAgentHelper)
            {
                if (webRequestAgentHelper == null)
                {
                    throw new GameFrameworkException("Web request agent helper is invalid.");
                }

                _Helper = webRequestAgentHelper;
                _Task = null;
                _WaitTime = 0f;

                WebRequestAgentStart = null;
                WebRequestAgentSuccess = null;
                WebRequestAgentFailure = null;
            }

            /// <summary>
            /// 获取 Web 请求任务。
            /// </summary>
            public WebRequestTask Task
            {
                get
                {
                    return _Task;
                }
            }

            /// <summary>
            /// 获取已经等待时间。
            /// </summary>
            public float WaitTime
            {
                get
                {
                    return _WaitTime;
                }
            }

            /// <summary>
            /// 初始化 Web 请求代理。
            /// </summary>
            public void Initialize()
            {
                _Helper.WebRequestAgentHelperComplete += OnWebRequestAgentHelperComplete;
                _Helper.WebRequestAgentHelperError += OnWebRequestAgentHelperError;
            }

            /// <summary>
            /// Web 请求代理轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (_Task.Status == WebRequestTaskStatus.Doing)
                {
                    _WaitTime += realElapseSeconds;
                    if (_WaitTime >= _Task.Timeout)
                    {
                        WebRequestAgentHelperErrorEventArgs webRequestAgentHelperErrorEventArgs = WebRequestAgentHelperErrorEventArgs.Create("Timeout");
                        OnWebRequestAgentHelperError(this, webRequestAgentHelperErrorEventArgs);
                        ReferencePool.Release(webRequestAgentHelperErrorEventArgs);
                    }
                }
            }

            /// <summary>
            /// 关闭并清理 Web 请求代理。
            /// </summary>
            public void Shutdown()
            {
                Reset();
                _Helper.WebRequestAgentHelperComplete -= OnWebRequestAgentHelperComplete;
                _Helper.WebRequestAgentHelperError -= OnWebRequestAgentHelperError;
            }

            /// <summary>
            /// 开始处理 Web 请求任务。
            /// </summary>
            /// <param name="task">要处理的 Web 请求任务。</param>
            /// <returns>开始处理任务的状态。</returns>
            public StartTaskStatus Start(WebRequestTask task)
            {
                if (task == null)
                {
                    throw new GameFrameworkException("Task is invalid.");
                }

                _Task = task;
                _Task.Status = WebRequestTaskStatus.Doing;

                if (WebRequestAgentStart != null)
                {
                    WebRequestAgentStart(this);
                }

                byte[] postData = _Task.GetPostData();
                if (postData == null)
                {
                    _Helper.Request(_Task.WebRequestUri, _Task.UserData);
                }
                else
                {
                    _Helper.Request(_Task.WebRequestUri, postData, _Task.UserData);
                }

                _WaitTime = 0f;
                return StartTaskStatus.CanResume;
            }

            /// <summary>
            /// 重置 Web 请求代理。
            /// </summary>
            public void Reset()
            {
                _Helper.Reset();
                _Task = null;
                _WaitTime = 0f;
            }

            private void OnWebRequestAgentHelperComplete(object sender, WebRequestAgentHelperCompleteEventArgs e)
            {
                _Helper.Reset();
                _Task.Status = WebRequestTaskStatus.Done;

                if (WebRequestAgentSuccess != null)
                {
                    WebRequestAgentSuccess(this, e.GetWebResponseBytes());
                }

                _Task.Done = true;
            }

            private void OnWebRequestAgentHelperError(object sender, WebRequestAgentHelperErrorEventArgs e)
            {
                _Helper.Reset();
                _Task.Status = WebRequestTaskStatus.Error;

                if (WebRequestAgentFailure != null)
                {
                    WebRequestAgentFailure(this, e.ErrorMessage);
                }

                _Task.Done = true;
            }
        }
    }
}
