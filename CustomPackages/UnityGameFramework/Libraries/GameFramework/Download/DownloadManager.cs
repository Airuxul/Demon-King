//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace GameFramework.Download
{
    /// <summary>
    /// 下载管理器。
    /// </summary>
    internal sealed partial class DownloadManager : GameFrameworkModule, IDownloadManager
    {
        private const int OneMegaBytes = 1024 * 1024;

        private readonly TaskPool<DownloadTask> _TaskPool;
        private readonly DownloadCounter _DownloadCounter;
        private int _FlushSize;
        private float _Timeout;
        private EventHandler<DownloadStartEventArgs> _DownloadStartEventHandler;
        private EventHandler<DownloadUpdateEventArgs> _DownloadUpdateEventHandler;
        private EventHandler<DownloadSuccessEventArgs> _DownloadSuccessEventHandler;
        private EventHandler<DownloadFailureEventArgs> _DownloadFailureEventHandler;

        /// <summary>
        /// 初始化下载管理器的新实例。
        /// </summary>
        public DownloadManager()
        {
            _TaskPool = new TaskPool<DownloadTask>();
            _DownloadCounter = new DownloadCounter(1f, 10f);
            _FlushSize = OneMegaBytes;
            _Timeout = 30f;
            _DownloadStartEventHandler = null;
            _DownloadUpdateEventHandler = null;
            _DownloadSuccessEventHandler = null;
            _DownloadFailureEventHandler = null;
        }

        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        internal override int Priority
        {
            get
            {
                return 5;
            }
        }

        /// <summary>
        /// 获取或设置下载是否被暂停。
        /// </summary>
        public bool Paused
        {
            get
            {
                return _TaskPool.Paused;
            }
            set
            {
                _TaskPool.Paused = value;
            }
        }

        /// <summary>
        /// 获取下载代理总数量。
        /// </summary>
        public int TotalAgentCount
        {
            get
            {
                return _TaskPool.TotalAgentCount;
            }
        }

        /// <summary>
        /// 获取可用下载代理数量。
        /// </summary>
        public int FreeAgentCount
        {
            get
            {
                return _TaskPool.FreeAgentCount;
            }
        }

        /// <summary>
        /// 获取工作中下载代理数量。
        /// </summary>
        public int WorkingAgentCount
        {
            get
            {
                return _TaskPool.WorkingAgentCount;
            }
        }

        /// <summary>
        /// 获取等待下载任务数量。
        /// </summary>
        public int WaitingTaskCount
        {
            get
            {
                return _TaskPool.WaitingTaskCount;
            }
        }

        /// <summary>
        /// 获取或设置将缓冲区写入磁盘的临界大小。
        /// </summary>
        public int FlushSize
        {
            get
            {
                return _FlushSize;
            }
            set
            {
                _FlushSize = value;
            }
        }

        /// <summary>
        /// 获取或设置下载超时时长，以秒为单位。
        /// </summary>
        public float Timeout
        {
            get
            {
                return _Timeout;
            }
            set
            {
                _Timeout = value;
            }
        }

        /// <summary>
        /// 获取当前下载速度。
        /// </summary>
        public float CurrentSpeed
        {
            get
            {
                return _DownloadCounter.CurrentSpeed;
            }
        }

        /// <summary>
        /// 下载开始事件。
        /// </summary>
        public event EventHandler<DownloadStartEventArgs> DownloadStart
        {
            add
            {
                _DownloadStartEventHandler += value;
            }
            remove
            {
                _DownloadStartEventHandler -= value;
            }
        }

        /// <summary>
        /// 下载更新事件。
        /// </summary>
        public event EventHandler<DownloadUpdateEventArgs> DownloadUpdate
        {
            add
            {
                _DownloadUpdateEventHandler += value;
            }
            remove
            {
                _DownloadUpdateEventHandler -= value;
            }
        }

        /// <summary>
        /// 下载成功事件。
        /// </summary>
        public event EventHandler<DownloadSuccessEventArgs> DownloadSuccess
        {
            add
            {
                _DownloadSuccessEventHandler += value;
            }
            remove
            {
                _DownloadSuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// 下载失败事件。
        /// </summary>
        public event EventHandler<DownloadFailureEventArgs> DownloadFailure
        {
            add
            {
                _DownloadFailureEventHandler += value;
            }
            remove
            {
                _DownloadFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// 下载管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            _TaskPool.Update(elapseSeconds, realElapseSeconds);
            _DownloadCounter.Update(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 关闭并清理下载管理器。
        /// </summary>
        internal override void Shutdown()
        {
            _TaskPool.Shutdown();
            _DownloadCounter.Shutdown();
        }

        /// <summary>
        /// 增加下载代理辅助器。
        /// </summary>
        /// <param name="downloadAgentHelper">要增加的下载代理辅助器。</param>
        public void AddDownloadAgentHelper(IDownloadAgentHelper downloadAgentHelper)
        {
            DownloadAgent agent = new DownloadAgent(downloadAgentHelper);
            agent.DownloadAgentStart += OnDownloadAgentStart;
            agent.DownloadAgentUpdate += OnDownloadAgentUpdate;
            agent.DownloadAgentSuccess += OnDownloadAgentSuccess;
            agent.DownloadAgentFailure += OnDownloadAgentFailure;

            _TaskPool.AddAgent(agent);
        }

        /// <summary>
        /// 根据下载任务的序列编号获取下载任务的信息。
        /// </summary>
        /// <param name="serialId">要获取信息的下载任务的序列编号。</param>
        /// <returns>下载任务的信息。</returns>
        public TaskInfo GetDownloadInfo(int serialId)
        {
            return _TaskPool.GetTaskInfo(serialId);
        }

        /// <summary>
        /// 根据下载任务的标签获取下载任务的信息。
        /// </summary>
        /// <param name="tag">要获取信息的下载任务的标签。</param>
        /// <returns>下载任务的信息。</returns>
        public TaskInfo[] GetDownloadInfos(string tag)
        {
            return _TaskPool.GetTaskInfos(tag);
        }

        /// <summary>
        /// 根据下载任务的标签获取下载任务的信息。
        /// </summary>
        /// <param name="tag">要获取信息的下载任务的标签。</param>
        /// <param name="results">下载任务的信息。</param>
        public void GetDownloadInfos(string tag, List<TaskInfo> results)
        {
            _TaskPool.GetTaskInfos(tag, results);
        }

        /// <summary>
        /// 获取所有下载任务的信息。
        /// </summary>
        /// <returns>所有下载任务的信息。</returns>
        public TaskInfo[] GetAllDownloadInfos()
        {
            return _TaskPool.GetAllTaskInfos();
        }

        /// <summary>
        /// 获取所有下载任务的信息。
        /// </summary>
        /// <param name="results">所有下载任务的信息。</param>
        public void GetAllDownloadInfos(List<TaskInfo> results)
        {
            _TaskPool.GetAllTaskInfos(results);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri)
        {
            return AddDownload(downloadPath, downloadUri, null, Constant.DefaultPriority, null);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="tag">下载任务的标签。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag)
        {
            return AddDownload(downloadPath, downloadUri, tag, Constant.DefaultPriority, null);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, int priority)
        {
            return AddDownload(downloadPath, downloadUri, null, priority, null);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, object userData)
        {
            return AddDownload(downloadPath, downloadUri, null, Constant.DefaultPriority, userData);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="tag">下载任务的标签。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag, int priority)
        {
            return AddDownload(downloadPath, downloadUri, tag, priority, null);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="tag">下载任务的标签。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag, object userData)
        {
            return AddDownload(downloadPath, downloadUri, tag, Constant.DefaultPriority, userData);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, int priority, object userData)
        {
            return AddDownload(downloadPath, downloadUri, null, priority, userData);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <param name="tag">下载任务的标签。</param>
        /// <param name="priority">下载任务的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri, string tag, int priority, object userData)
        {
            if (string.IsNullOrEmpty(downloadPath))
            {
                throw new GameFrameworkException("Download path is invalid.");
            }

            if (string.IsNullOrEmpty(downloadUri))
            {
                throw new GameFrameworkException("Download uri is invalid.");
            }

            if (TotalAgentCount <= 0)
            {
                throw new GameFrameworkException("You must add download agent first.");
            }

            DownloadTask downloadTask = DownloadTask.Create(downloadPath, downloadUri, tag, priority, _FlushSize, _Timeout, userData);
            _TaskPool.AddTask(downloadTask);
            return downloadTask.SerialId;
        }

        /// <summary>
        /// 根据下载任务的序列编号移除下载任务。
        /// </summary>
        /// <param name="serialId">要移除下载任务的序列编号。</param>
        /// <returns>是否移除下载任务成功。</returns>
        public bool RemoveDownload(int serialId)
        {
            return _TaskPool.RemoveTask(serialId);
        }

        /// <summary>
        /// 根据下载任务的标签移除下载任务。
        /// </summary>
        /// <param name="tag">要移除下载任务的标签。</param>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveDownloads(string tag)
        {
            return _TaskPool.RemoveTasks(tag);
        }

        /// <summary>
        /// 移除所有下载任务。
        /// </summary>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveAllDownloads()
        {
            return _TaskPool.RemoveAllTasks();
        }

        private void OnDownloadAgentStart(DownloadAgent sender)
        {
            if (_DownloadStartEventHandler != null)
            {
                DownloadStartEventArgs downloadStartEventArgs = DownloadStartEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
                _DownloadStartEventHandler(this, downloadStartEventArgs);
                ReferencePool.Release(downloadStartEventArgs);
            }
        }

        private void OnDownloadAgentUpdate(DownloadAgent sender, int deltaLength)
        {
            _DownloadCounter.RecordDeltaLength(deltaLength);
            if (_DownloadUpdateEventHandler != null)
            {
                DownloadUpdateEventArgs downloadUpdateEventArgs = DownloadUpdateEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
                _DownloadUpdateEventHandler(this, downloadUpdateEventArgs);
                ReferencePool.Release(downloadUpdateEventArgs);
            }
        }

        private void OnDownloadAgentSuccess(DownloadAgent sender, long length)
        {
            if (_DownloadSuccessEventHandler != null)
            {
                DownloadSuccessEventArgs downloadSuccessEventArgs = DownloadSuccessEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadPath, sender.Task.DownloadUri, sender.CurrentLength, sender.Task.UserData);
                _DownloadSuccessEventHandler(this, downloadSuccessEventArgs);
                ReferencePool.Release(downloadSuccessEventArgs);
            }
        }

        private void OnDownloadAgentFailure(DownloadAgent sender, string errorMessage)
        {
            if (_DownloadFailureEventHandler != null)
            {
                DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(sender.Task.SerialId, sender.Task.DownloadPath, sender.Task.DownloadUri, errorMessage, sender.Task.UserData);
                _DownloadFailureEventHandler(this, downloadFailureEventArgs);
                ReferencePool.Release(downloadFailureEventArgs);
            }
        }
    }
}
