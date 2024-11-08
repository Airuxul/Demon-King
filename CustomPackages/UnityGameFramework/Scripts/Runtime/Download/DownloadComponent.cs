//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Download;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 下载组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Download")]
    public sealed class DownloadComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;
        private const int OneMegaBytes = 1024 * 1024;

        private IDownloadManager _downloadManager = null;
        private EventComponent _eventComponent = null;

        [FormerlySerializedAs("_InstanceRoot")] [SerializeField]
        private Transform instanceRoot = null;

        [FormerlySerializedAs("_DownloadAgentHelperTypeName")] [SerializeField]
        private string downloadAgentHelperTypeName = "UnityGameFramework.Runtime.UnityWebRequestDownloadAgentHelper";

        [FormerlySerializedAs("_CustomDownloadAgentHelper")] [SerializeField]
        private DownloadAgentHelperBase customDownloadAgentHelper = null;

        [FormerlySerializedAs("_DownloadAgentHelperCount")] [SerializeField]
        private int downloadAgentHelperCount = 3;

        [FormerlySerializedAs("_Timeout")] [SerializeField]
        private float timeout = 30f;

        [FormerlySerializedAs("_FlushSize")] [SerializeField]
        private int flushSize = OneMegaBytes;

        /// <summary>
        /// 获取或设置下载是否被暂停。
        /// </summary>
        public bool Paused
        {
            get => _downloadManager.Paused;
            set => _downloadManager.Paused = value;
        }

        /// <summary>
        /// 获取下载代理总数量。
        /// </summary>
        public int TotalAgentCount => _downloadManager.TotalAgentCount;

        /// <summary>
        /// 获取可用下载代理数量。
        /// </summary>
        public int FreeAgentCount => _downloadManager.FreeAgentCount;

        /// <summary>
        /// 获取工作中下载代理数量。
        /// </summary>
        public int WorkingAgentCount => _downloadManager.WorkingAgentCount;

        /// <summary>
        /// 获取等待下载任务数量。
        /// </summary>
        public int WaitingTaskCount => _downloadManager.WaitingTaskCount;

        /// <summary>
        /// 获取或设置下载超时时长，以秒为单位。
        /// </summary>
        public float Timeout
        {
            get => _downloadManager.Timeout;
            set => _downloadManager.Timeout = timeout = value;
        }

        /// <summary>
        /// 获取或设置将缓冲区写入磁盘的临界大小，仅当开启断点续传时有效。
        /// </summary>
        public int FlushSize
        {
            get => _downloadManager.FlushSize;
            set => _downloadManager.FlushSize = flushSize = value;
        }

        /// <summary>
        /// 获取当前下载速度。
        /// </summary>
        public float CurrentSpeed => _downloadManager.CurrentSpeed;

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _downloadManager = GameFrameworkEntry.GetModule<IDownloadManager>();
            if (_downloadManager == null)
            {
                Log.Fatal("Download manager is invalid.");
                return;
            }

            _downloadManager.DownloadStart += OnDownloadStart;
            _downloadManager.DownloadUpdate += OnDownloadUpdate;
            _downloadManager.DownloadSuccess += OnDownloadSuccess;
            _downloadManager.DownloadFailure += OnDownloadFailure;
            _downloadManager.FlushSize = flushSize;
            _downloadManager.Timeout = timeout;
        }

        private void Start()
        {
            _eventComponent = GameEntry.GetComponent<EventComponent>();
            if (_eventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            if (instanceRoot == null)
            {
                instanceRoot = new GameObject("Download Agent Instances").transform;
                instanceRoot.SetParent(gameObject.transform);
                instanceRoot.localScale = Vector3.one;
            }

            for (int i = 0; i < downloadAgentHelperCount; i++)
            {
                AddDownloadAgentHelper(i);
            }
        }

        /// <summary>
        /// 根据下载任务的序列编号获取下载任务的信息。
        /// </summary>
        /// <param name="serialId">要获取信息的下载任务的序列编号。</param>
        /// <returns>下载任务的信息。</returns>
        public TaskInfo GetDownloadInfo(int serialId)
        {
            return _downloadManager.GetDownloadInfo(serialId);
        }

        /// <summary>
        /// 根据下载任务的标签获取下载任务的信息。
        /// </summary>
        /// <param name="tag">要获取信息的下载任务的标签。</param>
        /// <returns>下载任务的信息。</returns>
        public TaskInfo[] GetDownloadInfos(string tag)
        {
            return _downloadManager.GetDownloadInfos(tag);
        }

        /// <summary>
        /// 根据下载任务的标签获取下载任务的信息。
        /// </summary>
        /// <param name="tag">要获取信息的下载任务的标签。</param>
        /// <param name="results">下载任务的信息。</param>
        public void GetDownloadInfos(string tag, List<TaskInfo> results)
        {
            _downloadManager.GetDownloadInfos(tag, results);
        }

        /// <summary>
        /// 获取所有下载任务的信息。
        /// </summary>
        /// <returns>所有下载任务的信息。</returns>
        public TaskInfo[] GetAllDownloadInfos()
        {
            return _downloadManager.GetAllDownloadInfos();
        }

        /// <summary>
        /// 获取所有下载任务的信息。
        /// </summary>
        /// <param name="results">所有下载任务的信息。</param>
        public void GetAllDownloadInfos(List<TaskInfo> results)
        {
            _downloadManager.GetAllDownloadInfos(results);
        }

        /// <summary>
        /// 增加下载任务。
        /// </summary>
        /// <param name="downloadPath">下载后存放路径。</param>
        /// <param name="downloadUri">原始下载地址。</param>
        /// <returns>新增下载任务的序列编号。</returns>
        public int AddDownload(string downloadPath, string downloadUri)
        {
            return AddDownload(downloadPath, downloadUri, null, DefaultPriority, null);
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
            return AddDownload(downloadPath, downloadUri, tag, DefaultPriority, null);
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
            return AddDownload(downloadPath, downloadUri, null, DefaultPriority, userData);
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
            return AddDownload(downloadPath, downloadUri, tag, DefaultPriority, userData);
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
            return _downloadManager.AddDownload(downloadPath, downloadUri, tag, priority, userData);
        }

        /// <summary>
        /// 根据下载任务的序列编号移除下载任务。
        /// </summary>
        /// <param name="serialId">要移除下载任务的序列编号。</param>
        /// <returns>是否移除下载任务成功。</returns>
        public bool RemoveDownload(int serialId)
        {
            return _downloadManager.RemoveDownload(serialId);
        }

        /// <summary>
        /// 根据下载任务的标签移除下载任务。
        /// </summary>
        /// <param name="tag">要移除下载任务的标签。</param>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveDownloads(string tag)
        {
            return _downloadManager.RemoveDownloads(tag);
        }

        /// <summary>
        /// 移除所有下载任务。
        /// </summary>
        /// <returns>移除下载任务的数量。</returns>
        public int RemoveAllDownloads()
        {
            return _downloadManager.RemoveAllDownloads();
        }

        /// <summary>
        /// 增加下载代理辅助器。
        /// </summary>
        /// <param name="index">下载代理辅助器索引。</param>
        private void AddDownloadAgentHelper(int index)
        {
            DownloadAgentHelperBase downloadAgentHelper = Helper.CreateHelper(downloadAgentHelperTypeName, customDownloadAgentHelper, index);
            if (downloadAgentHelper == null)
            {
                Log.Error("Can not create download agent helper.");
                return;
            }

            downloadAgentHelper.name = Utility.Text.Format("Download Agent Helper - {0}", index);
            Transform transform = downloadAgentHelper.transform;
            transform.SetParent(instanceRoot);
            transform.localScale = Vector3.one;

            _downloadManager.AddDownloadAgentHelper(downloadAgentHelper);
        }

        private void OnDownloadStart(object sender, GameFramework.Download.DownloadStartEventArgs e)
        {
            _eventComponent.Fire(this, DownloadStartEventArgs.Create(e));
        }

        private void OnDownloadUpdate(object sender, GameFramework.Download.DownloadUpdateEventArgs e)
        {
            _eventComponent.Fire(this, DownloadUpdateEventArgs.Create(e));
        }

        private void OnDownloadSuccess(object sender, GameFramework.Download.DownloadSuccessEventArgs e)
        {
            _eventComponent.Fire(this, DownloadSuccessEventArgs.Create(e));
        }

        private void OnDownloadFailure(object sender, GameFramework.Download.DownloadFailureEventArgs e)
        {
            Log.Warning("Download failure, download serial id '{0}', download path '{1}', download uri '{2}', error message '{3}'.", e.SerialId, e.DownloadPath, e.DownloadUri, e.ErrorMessage);
            _eventComponent.Fire(this, DownloadFailureEventArgs.Create(e));
        }
    }
}
