//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.IO;

namespace GameFramework.Download
{
    internal sealed partial class DownloadManager : GameFrameworkModule, IDownloadManager
    {
        /// <summary>
        /// 下载代理。
        /// </summary>
        private sealed class DownloadAgent : ITaskAgent<DownloadTask>, IDisposable
        {
            private readonly IDownloadAgentHelper _Helper;
            private DownloadTask _Task;
            private FileStream _FileStream;
            private int _WaitFlushSize;
            private float _WaitTime;
            private long _StartLength;
            private long _DownloadedLength;
            private long _SavedLength;
            private bool _Disposed;

            public GameFrameworkAction<DownloadAgent> DownloadAgentStart;
            public GameFrameworkAction<DownloadAgent, int> DownloadAgentUpdate;
            public GameFrameworkAction<DownloadAgent, long> DownloadAgentSuccess;
            public GameFrameworkAction<DownloadAgent, string> DownloadAgentFailure;

            /// <summary>
            /// 初始化下载代理的新实例。
            /// </summary>
            /// <param name="downloadAgentHelper">下载代理辅助器。</param>
            public DownloadAgent(IDownloadAgentHelper downloadAgentHelper)
            {
                if (downloadAgentHelper == null)
                {
                    throw new GameFrameworkException("Download agent helper is invalid.");
                }

                _Helper = downloadAgentHelper;
                _Task = null;
                _FileStream = null;
                _WaitFlushSize = 0;
                _WaitTime = 0f;
                _StartLength = 0L;
                _DownloadedLength = 0L;
                _SavedLength = 0L;
                _Disposed = false;

                DownloadAgentStart = null;
                DownloadAgentUpdate = null;
                DownloadAgentSuccess = null;
                DownloadAgentFailure = null;
            }

            /// <summary>
            /// 获取下载任务。
            /// </summary>
            public DownloadTask Task
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
            /// 获取开始下载时已经存在的大小。
            /// </summary>
            public long StartLength
            {
                get
                {
                    return _StartLength;
                }
            }

            /// <summary>
            /// 获取本次已经下载的大小。
            /// </summary>
            public long DownloadedLength
            {
                get
                {
                    return _DownloadedLength;
                }
            }

            /// <summary>
            /// 获取当前的大小。
            /// </summary>
            public long CurrentLength
            {
                get
                {
                    return _StartLength + _DownloadedLength;
                }
            }

            /// <summary>
            /// 获取已经存盘的大小。
            /// </summary>
            public long SavedLength
            {
                get
                {
                    return _SavedLength;
                }
            }

            /// <summary>
            /// 初始化下载代理。
            /// </summary>
            public void Initialize()
            {
                _Helper.DownloadAgentHelperUpdateBytes += OnDownloadAgentHelperUpdateBytes;
                _Helper.DownloadAgentHelperUpdateLength += OnDownloadAgentHelperUpdateLength;
                _Helper.DownloadAgentHelperComplete += OnDownloadAgentHelperComplete;
                _Helper.DownloadAgentHelperError += OnDownloadAgentHelperError;
            }

            /// <summary>
            /// 下载代理轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                if (_Task.Status == DownloadTaskStatus.Doing)
                {
                    _WaitTime += realElapseSeconds;
                    if (_WaitTime >= _Task.Timeout)
                    {
                        DownloadAgentHelperErrorEventArgs downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, "Timeout");
                        OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
                        ReferencePool.Release(downloadAgentHelperErrorEventArgs);
                    }
                }
            }

            /// <summary>
            /// 关闭并清理下载代理。
            /// </summary>
            public void Shutdown()
            {
                Dispose();

                _Helper.DownloadAgentHelperUpdateBytes -= OnDownloadAgentHelperUpdateBytes;
                _Helper.DownloadAgentHelperUpdateLength -= OnDownloadAgentHelperUpdateLength;
                _Helper.DownloadAgentHelperComplete -= OnDownloadAgentHelperComplete;
                _Helper.DownloadAgentHelperError -= OnDownloadAgentHelperError;
            }

            /// <summary>
            /// 开始处理下载任务。
            /// </summary>
            /// <param name="task">要处理的下载任务。</param>
            /// <returns>开始处理任务的状态。</returns>
            public StartTaskStatus Start(DownloadTask task)
            {
                if (task == null)
                {
                    throw new GameFrameworkException("Task is invalid.");
                }

                _Task = task;

                _Task.Status = DownloadTaskStatus.Doing;
                string downloadFile = Utility.Text.Format("{0}.download", _Task.DownloadPath);

                try
                {
                    if (File.Exists(downloadFile))
                    {
                        _FileStream = File.OpenWrite(downloadFile);
                        _FileStream.Seek(0L, SeekOrigin.End);
                        _StartLength = _SavedLength = _FileStream.Length;
                        _DownloadedLength = 0L;
                    }
                    else
                    {
                        string directory = Path.GetDirectoryName(_Task.DownloadPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        _FileStream = new FileStream(downloadFile, FileMode.Create, FileAccess.Write);
                        _StartLength = _SavedLength = _DownloadedLength = 0L;
                    }

                    if (DownloadAgentStart != null)
                    {
                        DownloadAgentStart(this);
                    }

                    if (_StartLength > 0L)
                    {
                        _Helper.Download(_Task.DownloadUri, _StartLength, _Task.UserData);
                    }
                    else
                    {
                        _Helper.Download(_Task.DownloadUri, _Task.UserData);
                    }

                    return StartTaskStatus.CanResume;
                }
                catch (Exception exception)
                {
                    DownloadAgentHelperErrorEventArgs downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, exception.ToString());
                    OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
                    ReferencePool.Release(downloadAgentHelperErrorEventArgs);
                    return StartTaskStatus.UnknownError;
                }
            }

            /// <summary>
            /// 重置下载代理。
            /// </summary>
            public void Reset()
            {
                _Helper.Reset();

                if (_FileStream != null)
                {
                    _FileStream.Close();
                    _FileStream = null;
                }

                _Task = null;
                _WaitFlushSize = 0;
                _WaitTime = 0f;
                _StartLength = 0L;
                _DownloadedLength = 0L;
                _SavedLength = 0L;
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
                    if (_FileStream != null)
                    {
                        _FileStream.Dispose();
                        _FileStream = null;
                    }
                }

                _Disposed = true;
            }

            private void OnDownloadAgentHelperUpdateBytes(object sender, DownloadAgentHelperUpdateBytesEventArgs e)
            {
                _WaitTime = 0f;
                try
                {
                    _FileStream.Write(e.GetBytes(), e.Offset, e.Length);
                    _WaitFlushSize += e.Length;
                    _SavedLength += e.Length;

                    if (_WaitFlushSize >= _Task.FlushSize)
                    {
                        _FileStream.Flush();
                        _WaitFlushSize = 0;
                    }
                }
                catch (Exception exception)
                {
                    DownloadAgentHelperErrorEventArgs downloadAgentHelperErrorEventArgs = DownloadAgentHelperErrorEventArgs.Create(false, exception.ToString());
                    OnDownloadAgentHelperError(this, downloadAgentHelperErrorEventArgs);
                    ReferencePool.Release(downloadAgentHelperErrorEventArgs);
                }
            }

            private void OnDownloadAgentHelperUpdateLength(object sender, DownloadAgentHelperUpdateLengthEventArgs e)
            {
                _WaitTime = 0f;
                _DownloadedLength += e.DeltaLength;
                if (DownloadAgentUpdate != null)
                {
                    DownloadAgentUpdate(this, e.DeltaLength);
                }
            }

            private void OnDownloadAgentHelperComplete(object sender, DownloadAgentHelperCompleteEventArgs e)
            {
                _WaitTime = 0f;
                _DownloadedLength = e.Length;
                if (_SavedLength != CurrentLength)
                {
                    throw new GameFrameworkException("Internal download error.");
                }

                _Helper.Reset();
                _FileStream.Close();
                _FileStream = null;

                if (File.Exists(_Task.DownloadPath))
                {
                    File.Delete(_Task.DownloadPath);
                }

                File.Move(Utility.Text.Format("{0}.download", _Task.DownloadPath), _Task.DownloadPath);

                _Task.Status = DownloadTaskStatus.Done;

                if (DownloadAgentSuccess != null)
                {
                    DownloadAgentSuccess(this, e.Length);
                }

                _Task.Done = true;
            }

            private void OnDownloadAgentHelperError(object sender, DownloadAgentHelperErrorEventArgs e)
            {
                _Helper.Reset();
                if (_FileStream != null)
                {
                    _FileStream.Close();
                    _FileStream = null;
                }

                if (e.DeleteDownloading)
                {
                    File.Delete(Utility.Text.Format("{0}.download", _Task.DownloadPath));
                }

                _Task.Status = DownloadTaskStatus.Error;

                if (DownloadAgentFailure != null)
                {
                    DownloadAgentFailure(this, e.ErrorMessage);
                }

                _Task.Done = true;
            }
        }
    }
}
