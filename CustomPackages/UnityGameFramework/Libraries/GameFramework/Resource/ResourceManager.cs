//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Download;
using GameFramework.FileSystem;
using GameFramework.ObjectPool;
using System;
using System.Collections.Generic;
using System.IO;

namespace GameFramework.Resource
{
    /// <summary>
    /// 资源管理器。
    /// </summary>
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private const string RemoteVersionListFileName = "GameFrameworkVersion.dat";
        private const string LocalVersionListFileName = "GameFrameworkList.dat";
        private const string DefaultExtension = "dat";
        private const string TempExtension = "tmp";
        private const int FileSystemMaxFileCount = 1024 * 16;
        private const int FileSystemMaxBlockCount = 1024 * 256;

        private Dictionary<string, AssetInfo> _AssetInfos;
        private Dictionary<ResourceName, ResourceInfo> _ResourceInfos;
        private SortedDictionary<ResourceName, ReadWriteResourceInfo> _ReadWriteResourceInfos;
        private readonly Dictionary<string, IFileSystem> _ReadOnlyFileSystems;
        private readonly Dictionary<string, IFileSystem> _ReadWriteFileSystems;
        private readonly Dictionary<string, ResourceGroup> _ResourceGroups;

        private PackageVersionListSerializer _PackageVersionListSerializer;
        private UpdatableVersionListSerializer _UpdatableVersionListSerializer;
        private ReadOnlyVersionListSerializer _ReadOnlyVersionListSerializer;
        private ReadWriteVersionListSerializer _ReadWriteVersionListSerializer;
        private ResourcePackVersionListSerializer _ResourcePackVersionListSerializer;

        private IFileSystemManager _FileSystemManager;
        private ResourceIniter _ResourceIniter;
        private VersionListProcessor _VersionListProcessor;
        private ResourceVerifier _ResourceVerifier;
        private ResourceChecker _ResourceChecker;
        private ResourceUpdater _ResourceUpdater;
        private ResourceLoader _ResourceLoader;
        private IResourceHelper _ResourceHelper;

        private string _ReadOnlyPath;
        private string _ReadWritePath;
        private ResourceMode _ResourceMode;
        private bool _RefuseSetFlag;
        private string _CurrentVariant;
        private string _UpdatePrefixUri;
        private string _ApplicableGameVersion;
        private int _InternalResourceVersion;
        private MemoryStream _CachedStream;
        private DecryptResourceCallback _DecryptResourceCallback;
        private InitResourcesCompleteCallback _InitResourcesCompleteCallback;
        private UpdateVersionListCallbacks _UpdateVersionListCallbacks;
        private VerifyResourcesCompleteCallback _VerifyResourcesCompleteCallback;
        private CheckResourcesCompleteCallback _CheckResourcesCompleteCallback;
        private ApplyResourcesCompleteCallback _ApplyResourcesCompleteCallback;
        private UpdateResourcesCompleteCallback _UpdateResourcesCompleteCallback;
        private EventHandler<ResourceVerifyStartEventArgs> _ResourceVerifyStartEventHandler;
        private EventHandler<ResourceVerifySuccessEventArgs> _ResourceVerifySuccessEventHandler;
        private EventHandler<ResourceVerifyFailureEventArgs> _ResourceVerifyFailureEventHandler;
        private EventHandler<ResourceApplyStartEventArgs> _ResourceApplyStartEventHandler;
        private EventHandler<ResourceApplySuccessEventArgs> _ResourceApplySuccessEventHandler;
        private EventHandler<ResourceApplyFailureEventArgs> _ResourceApplyFailureEventHandler;
        private EventHandler<ResourceUpdateStartEventArgs> _ResourceUpdateStartEventHandler;
        private EventHandler<ResourceUpdateChangedEventArgs> _ResourceUpdateChangedEventHandler;
        private EventHandler<ResourceUpdateSuccessEventArgs> _ResourceUpdateSuccessEventHandler;
        private EventHandler<ResourceUpdateFailureEventArgs> _ResourceUpdateFailureEventHandler;
        private EventHandler<ResourceUpdateAllCompleteEventArgs> _ResourceUpdateAllCompleteEventHandler;

        /// <summary>
        /// 初始化资源管理器的新实例。
        /// </summary>
        public ResourceManager()
        {
            _AssetInfos = null;
            _ResourceInfos = null;
            _ReadWriteResourceInfos = null;
            _ReadOnlyFileSystems = new Dictionary<string, IFileSystem>(StringComparer.Ordinal);
            _ReadWriteFileSystems = new Dictionary<string, IFileSystem>(StringComparer.Ordinal);
            _ResourceGroups = new Dictionary<string, ResourceGroup>(StringComparer.Ordinal);

            _PackageVersionListSerializer = null;
            _UpdatableVersionListSerializer = null;
            _ReadOnlyVersionListSerializer = null;
            _ReadWriteVersionListSerializer = null;
            _ResourcePackVersionListSerializer = null;

            _ResourceIniter = null;
            _VersionListProcessor = null;
            _ResourceVerifier = null;
            _ResourceChecker = null;
            _ResourceUpdater = null;
            _ResourceLoader = new ResourceLoader(this);

            _ResourceHelper = null;
            _ReadOnlyPath = null;
            _ReadWritePath = null;
            _ResourceMode = ResourceMode.Unspecified;
            _RefuseSetFlag = false;
            _CurrentVariant = null;
            _UpdatePrefixUri = null;
            _ApplicableGameVersion = null;
            _InternalResourceVersion = 0;
            _CachedStream = null;
            _DecryptResourceCallback = null;
            _InitResourcesCompleteCallback = null;
            _UpdateVersionListCallbacks = null;
            _VerifyResourcesCompleteCallback = null;
            _CheckResourcesCompleteCallback = null;
            _ApplyResourcesCompleteCallback = null;
            _UpdateResourcesCompleteCallback = null;
            _ResourceVerifySuccessEventHandler = null;
            _ResourceVerifyFailureEventHandler = null;
            _ResourceApplySuccessEventHandler = null;
            _ResourceApplyFailureEventHandler = null;
            _ResourceUpdateStartEventHandler = null;
            _ResourceUpdateChangedEventHandler = null;
            _ResourceUpdateSuccessEventHandler = null;
            _ResourceUpdateFailureEventHandler = null;
            _ResourceUpdateAllCompleteEventHandler = null;
        }

        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        internal override int Priority
        {
            get
            {
                return 3;
            }
        }

        /// <summary>
        /// 获取资源只读区路径。
        /// </summary>
        public string ReadOnlyPath
        {
            get
            {
                return _ReadOnlyPath;
            }
        }

        /// <summary>
        /// 获取资源读写区路径。
        /// </summary>
        public string ReadWritePath
        {
            get
            {
                return _ReadWritePath;
            }
        }

        /// <summary>
        /// 获取资源模式。
        /// </summary>
        public ResourceMode ResourceMode
        {
            get
            {
                return _ResourceMode;
            }
        }

        /// <summary>
        /// 获取当前变体。
        /// </summary>
        public string CurrentVariant
        {
            get
            {
                return _CurrentVariant;
            }
        }

        /// <summary>
        /// 获取单机模式版本资源列表序列化器。
        /// </summary>
        public PackageVersionListSerializer PackageVersionListSerializer
        {
            get
            {
                return _PackageVersionListSerializer;
            }
        }

        /// <summary>
        /// 获取可更新模式版本资源列表序列化器。
        /// </summary>
        public UpdatableVersionListSerializer UpdatableVersionListSerializer
        {
            get
            {
                return _UpdatableVersionListSerializer;
            }
        }

        /// <summary>
        /// 获取本地只读区版本资源列表序列化器。
        /// </summary>
        public ReadOnlyVersionListSerializer ReadOnlyVersionListSerializer
        {
            get
            {
                return _ReadOnlyVersionListSerializer;
            }
        }

        /// <summary>
        /// 获取本地读写区版本资源列表序列化器。
        /// </summary>
        public ReadWriteVersionListSerializer ReadWriteVersionListSerializer
        {
            get
            {
                return _ReadWriteVersionListSerializer;
            }
        }

        /// <summary>
        /// 获取资源包版本资源列表序列化器。
        /// </summary>
        public ResourcePackVersionListSerializer ResourcePackVersionListSerializer
        {
            get
            {
                return _ResourcePackVersionListSerializer;
            }
        }

        /// <summary>
        /// 获取当前资源适用的游戏版本号。
        /// </summary>
        public string ApplicableGameVersion
        {
            get
            {
                return _ApplicableGameVersion;
            }
        }

        /// <summary>
        /// 获取当前内部资源版本号。
        /// </summary>
        public int InternalResourceVersion
        {
            get
            {
                return _InternalResourceVersion;
            }
        }

        /// <summary>
        /// 获取资源数量。
        /// </summary>
        public int AssetCount
        {
            get
            {
                return _AssetInfos != null ? _AssetInfos.Count : 0;
            }
        }

        /// <summary>
        /// 获取资源数量。
        /// </summary>
        public int ResourceCount
        {
            get
            {
                return _ResourceInfos != null ? _ResourceInfos.Count : 0;
            }
        }

        /// <summary>
        /// 获取资源组数量。
        /// </summary>
        public int ResourceGroupCount
        {
            get
            {
                return _ResourceGroups.Count;
            }
        }

        /// <summary>
        /// 获取或设置资源更新下载地址前缀。
        /// </summary>
        public string UpdatePrefixUri
        {
            get
            {
                return _UpdatePrefixUri;
            }
            set
            {
                _UpdatePrefixUri = value;
            }
        }

        /// <summary>
        /// 获取或设置每更新多少字节的资源，重新生成一次版本资源列表。
        /// </summary>
        public int GenerateReadWriteVersionListLength
        {
            get
            {
                return _ResourceUpdater != null ? _ResourceUpdater.GenerateReadWriteVersionListLength : 0;
            }
            set
            {
                if (_ResourceUpdater == null)
                {
                    throw new GameFrameworkException("You can not use GenerateReadWriteVersionListLength at this time.");
                }

                _ResourceUpdater.GenerateReadWriteVersionListLength = value;
            }
        }

        /// <summary>
        /// 获取正在应用的资源包路径。
        /// </summary>
        public string ApplyingResourcePackPath
        {
            get
            {
                return _ResourceUpdater != null ? _ResourceUpdater.ApplyingResourcePackPath : null;
            }
        }

        /// <summary>
        /// 获取等待应用资源数量。
        /// </summary>
        public int ApplyWaitingCount
        {
            get
            {
                return _ResourceUpdater != null ? _ResourceUpdater.ApplyWaitingCount : 0;
            }
        }

        /// <summary>
        /// 获取或设置资源更新重试次数。
        /// </summary>
        public int UpdateRetryCount
        {
            get
            {
                return _ResourceUpdater != null ? _ResourceUpdater.UpdateRetryCount : 0;
            }
            set
            {
                if (_ResourceUpdater == null)
                {
                    throw new GameFrameworkException("You can not use UpdateRetryCount at this time.");
                }

                _ResourceUpdater.UpdateRetryCount = value;
            }
        }

        /// <summary>
        /// 获取正在更新的资源组。
        /// </summary>
        public IResourceGroup UpdatingResourceGroup
        {
            get
            {
                return _ResourceUpdater != null ? _ResourceUpdater.UpdatingResourceGroup : null;
            }
        }

        /// <summary>
        /// 获取等待更新资源数量。
        /// </summary>
        public int UpdateWaitingCount
        {
            get
            {
                return _ResourceUpdater != null ? _ResourceUpdater.UpdateWaitingCount : 0;
            }
        }

        /// <summary>
        /// 获取使用时下载的等待更新资源数量。
        /// </summary>
        public int UpdateWaitingWhilePlayingCount
        {
            get
            {
                return _ResourceUpdater != null ? _ResourceUpdater.UpdateWaitingWhilePlayingCount : 0;
            }
        }

        /// <summary>
        /// 获取候选更新资源数量。
        /// </summary>
        public int UpdateCandidateCount
        {
            get
            {
                return _ResourceUpdater != null ? _ResourceUpdater.UpdateCandidateCount : 0;
            }
        }

        /// <summary>
        /// 获取加载资源代理总数量。
        /// </summary>
        public int LoadTotalAgentCount
        {
            get
            {
                return _ResourceLoader.TotalAgentCount;
            }
        }

        /// <summary>
        /// 获取可用加载资源代理数量。
        /// </summary>
        public int LoadFreeAgentCount
        {
            get
            {
                return _ResourceLoader.FreeAgentCount;
            }
        }

        /// <summary>
        /// 获取工作中加载资源代理数量。
        /// </summary>
        public int LoadWorkingAgentCount
        {
            get
            {
                return _ResourceLoader.WorkingAgentCount;
            }
        }

        /// <summary>
        /// 获取等待加载资源任务数量。
        /// </summary>
        public int LoadWaitingTaskCount
        {
            get
            {
                return _ResourceLoader.WaitingTaskCount;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        public float AssetAutoReleaseInterval
        {
            get
            {
                return _ResourceLoader.AssetAutoReleaseInterval;
            }
            set
            {
                _ResourceLoader.AssetAutoReleaseInterval = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池的容量。
        /// </summary>
        public int AssetCapacity
        {
            get
            {
                return _ResourceLoader.AssetCapacity;
            }
            set
            {
                _ResourceLoader.AssetCapacity = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池对象过期秒数。
        /// </summary>
        public float AssetExpireTime
        {
            get
            {
                return _ResourceLoader.AssetExpireTime;
            }
            set
            {
                _ResourceLoader.AssetExpireTime = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池的优先级。
        /// </summary>
        public int AssetPriority
        {
            get
            {
                return _ResourceLoader.AssetPriority;
            }
            set
            {
                _ResourceLoader.AssetPriority = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        public float ResourceAutoReleaseInterval
        {
            get
            {
                return _ResourceLoader.ResourceAutoReleaseInterval;
            }
            set
            {
                _ResourceLoader.ResourceAutoReleaseInterval = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池的容量。
        /// </summary>
        public int ResourceCapacity
        {
            get
            {
                return _ResourceLoader.ResourceCapacity;
            }
            set
            {
                _ResourceLoader.ResourceCapacity = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池对象过期秒数。
        /// </summary>
        public float ResourceExpireTime
        {
            get
            {
                return _ResourceLoader.ResourceExpireTime;
            }
            set
            {
                _ResourceLoader.ResourceExpireTime = value;
            }
        }

        /// <summary>
        /// 获取或设置资源对象池的优先级。
        /// </summary>
        public int ResourcePriority
        {
            get
            {
                return _ResourceLoader.ResourcePriority;
            }
            set
            {
                _ResourceLoader.ResourcePriority = value;
            }
        }

        /// <summary>
        /// 资源校验开始事件。
        /// </summary>
        public event EventHandler<ResourceVerifyStartEventArgs> ResourceVerifyStart
        {
            add
            {
                _ResourceVerifyStartEventHandler += value;
            }
            remove
            {
                _ResourceVerifyStartEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源校验成功事件。
        /// </summary>
        public event EventHandler<ResourceVerifySuccessEventArgs> ResourceVerifySuccess
        {
            add
            {
                _ResourceVerifySuccessEventHandler += value;
            }
            remove
            {
                _ResourceVerifySuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源校验失败事件。
        /// </summary>
        public event EventHandler<ResourceVerifyFailureEventArgs> ResourceVerifyFailure
        {
            add
            {
                _ResourceVerifyFailureEventHandler += value;
            }
            remove
            {
                _ResourceVerifyFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源应用开始事件。
        /// </summary>
        public event EventHandler<ResourceApplyStartEventArgs> ResourceApplyStart
        {
            add
            {
                _ResourceApplyStartEventHandler += value;
            }
            remove
            {
                _ResourceApplyStartEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源应用成功事件。
        /// </summary>
        public event EventHandler<ResourceApplySuccessEventArgs> ResourceApplySuccess
        {
            add
            {
                _ResourceApplySuccessEventHandler += value;
            }
            remove
            {
                _ResourceApplySuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源应用失败事件。
        /// </summary>
        public event EventHandler<ResourceApplyFailureEventArgs> ResourceApplyFailure
        {
            add
            {
                _ResourceApplyFailureEventHandler += value;
            }
            remove
            {
                _ResourceApplyFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源更新开始事件。
        /// </summary>
        public event EventHandler<ResourceUpdateStartEventArgs> ResourceUpdateStart
        {
            add
            {
                _ResourceUpdateStartEventHandler += value;
            }
            remove
            {
                _ResourceUpdateStartEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源更新改变事件。
        /// </summary>
        public event EventHandler<ResourceUpdateChangedEventArgs> ResourceUpdateChanged
        {
            add
            {
                _ResourceUpdateChangedEventHandler += value;
            }
            remove
            {
                _ResourceUpdateChangedEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源更新成功事件。
        /// </summary>
        public event EventHandler<ResourceUpdateSuccessEventArgs> ResourceUpdateSuccess
        {
            add
            {
                _ResourceUpdateSuccessEventHandler += value;
            }
            remove
            {
                _ResourceUpdateSuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源更新失败事件。
        /// </summary>
        public event EventHandler<ResourceUpdateFailureEventArgs> ResourceUpdateFailure
        {
            add
            {
                _ResourceUpdateFailureEventHandler += value;
            }
            remove
            {
                _ResourceUpdateFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源更新全部完成事件。
        /// </summary>
        public event EventHandler<ResourceUpdateAllCompleteEventArgs> ResourceUpdateAllComplete
        {
            add
            {
                _ResourceUpdateAllCompleteEventHandler += value;
            }
            remove
            {
                _ResourceUpdateAllCompleteEventHandler -= value;
            }
        }

        /// <summary>
        /// 资源管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (_ResourceVerifier != null)
            {
                _ResourceVerifier.Update(elapseSeconds, realElapseSeconds);
                return;
            }

            if (_ResourceUpdater != null)
            {
                _ResourceUpdater.Update(elapseSeconds, realElapseSeconds);
            }

            _ResourceLoader.Update(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 关闭并清理资源管理器。
        /// </summary>
        internal override void Shutdown()
        {
            if (_ResourceIniter != null)
            {
                _ResourceIniter.Shutdown();
                _ResourceIniter = null;
            }

            if (_VersionListProcessor != null)
            {
                _VersionListProcessor.VersionListUpdateSuccess -= OnVersionListProcessorUpdateSuccess;
                _VersionListProcessor.VersionListUpdateFailure -= OnVersionListProcessorUpdateFailure;
                _VersionListProcessor.Shutdown();
                _VersionListProcessor = null;
            }

            if (_ResourceVerifier != null)
            {
                _ResourceVerifier.ResourceVerifyStart -= OnVerifierResourceVerifyStart;
                _ResourceVerifier.ResourceVerifySuccess -= OnVerifierResourceVerifySuccess;
                _ResourceVerifier.ResourceVerifyFailure -= OnVerifierResourceVerifyFailure;
                _ResourceVerifier.ResourceVerifyComplete -= OnVerifierResourceVerifyComplete;
                _ResourceVerifier.Shutdown();
                _ResourceVerifier = null;
            }

            if (_ResourceChecker != null)
            {
                _ResourceChecker.ResourceNeedUpdate -= OnCheckerResourceNeedUpdate;
                _ResourceChecker.ResourceCheckComplete -= OnCheckerResourceCheckComplete;
                _ResourceChecker.Shutdown();
                _ResourceChecker = null;
            }

            if (_ResourceUpdater != null)
            {
                _ResourceUpdater.ResourceApplyStart -= OnUpdaterResourceApplyStart;
                _ResourceUpdater.ResourceApplySuccess -= OnUpdaterResourceApplySuccess;
                _ResourceUpdater.ResourceApplyFailure -= OnUpdaterResourceApplyFailure;
                _ResourceUpdater.ResourceApplyComplete -= OnUpdaterResourceApplyComplete;
                _ResourceUpdater.ResourceUpdateStart -= OnUpdaterResourceUpdateStart;
                _ResourceUpdater.ResourceUpdateChanged -= OnUpdaterResourceUpdateChanged;
                _ResourceUpdater.ResourceUpdateSuccess -= OnUpdaterResourceUpdateSuccess;
                _ResourceUpdater.ResourceUpdateFailure -= OnUpdaterResourceUpdateFailure;
                _ResourceUpdater.ResourceUpdateComplete -= OnUpdaterResourceUpdateComplete;
                _ResourceUpdater.ResourceUpdateAllComplete -= OnUpdaterResourceUpdateAllComplete;
                _ResourceUpdater.Shutdown();
                _ResourceUpdater = null;

                if (_ReadWriteResourceInfos != null)
                {
                    _ReadWriteResourceInfos.Clear();
                    _ReadWriteResourceInfos = null;
                }

                FreeCachedStream();
            }

            if (_ResourceLoader != null)
            {
                _ResourceLoader.Shutdown();
                _ResourceLoader = null;
            }

            if (_AssetInfos != null)
            {
                _AssetInfos.Clear();
                _AssetInfos = null;
            }

            if (_ResourceInfos != null)
            {
                _ResourceInfos.Clear();
                _ResourceInfos = null;
            }

            _ReadOnlyFileSystems.Clear();
            _ReadWriteFileSystems.Clear();
            _ResourceGroups.Clear();
        }

        /// <summary>
        /// 设置资源只读区路径。
        /// </summary>
        /// <param name="readOnlyPath">资源只读区路径。</param>
        public void SetReadOnlyPath(string readOnlyPath)
        {
            if (string.IsNullOrEmpty(readOnlyPath))
            {
                throw new GameFrameworkException("Read-only path is invalid.");
            }

            if (_RefuseSetFlag)
            {
                throw new GameFrameworkException("You can not set read-only path at this time.");
            }

            if (_ResourceLoader.TotalAgentCount > 0)
            {
                throw new GameFrameworkException("You must set read-only path before add load resource agent helper.");
            }

            _ReadOnlyPath = readOnlyPath;
        }

        /// <summary>
        /// 设置资源读写区路径。
        /// </summary>
        /// <param name="readWritePath">资源读写区路径。</param>
        public void SetReadWritePath(string readWritePath)
        {
            if (string.IsNullOrEmpty(readWritePath))
            {
                throw new GameFrameworkException("Read-write path is invalid.");
            }

            if (_RefuseSetFlag)
            {
                throw new GameFrameworkException("You can not set read-write path at this time.");
            }

            if (_ResourceLoader.TotalAgentCount > 0)
            {
                throw new GameFrameworkException("You must set read-write path before add load resource agent helper.");
            }

            _ReadWritePath = readWritePath;
        }

        /// <summary>
        /// 设置资源模式。
        /// </summary>
        /// <param name="resourceMode">资源模式。</param>
        public void SetResourceMode(ResourceMode resourceMode)
        {
            if (resourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("Resource mode is invalid.");
            }

            if (_RefuseSetFlag)
            {
                throw new GameFrameworkException("You can not set resource mode at this time.");
            }

            if (_ResourceMode == ResourceMode.Unspecified)
            {
                _ResourceMode = resourceMode;

                if (_ResourceMode == ResourceMode.Package)
                {
                    _PackageVersionListSerializer = new PackageVersionListSerializer();

                    _ResourceIniter = new ResourceIniter(this);
                    _ResourceIniter.ResourceInitComplete += OnIniterResourceInitComplete;
                }
                else if (_ResourceMode == ResourceMode.Updatable || _ResourceMode == ResourceMode.UpdatableWhilePlaying)
                {
                    _UpdatableVersionListSerializer = new UpdatableVersionListSerializer();
                    _ReadOnlyVersionListSerializer = new ReadOnlyVersionListSerializer();
                    _ReadWriteVersionListSerializer = new ReadWriteVersionListSerializer();
                    _ResourcePackVersionListSerializer = new ResourcePackVersionListSerializer();

                    _VersionListProcessor = new VersionListProcessor(this);
                    _VersionListProcessor.VersionListUpdateSuccess += OnVersionListProcessorUpdateSuccess;
                    _VersionListProcessor.VersionListUpdateFailure += OnVersionListProcessorUpdateFailure;

                    _ResourceChecker = new ResourceChecker(this);
                    _ResourceChecker.ResourceNeedUpdate += OnCheckerResourceNeedUpdate;
                    _ResourceChecker.ResourceCheckComplete += OnCheckerResourceCheckComplete;

                    _ResourceUpdater = new ResourceUpdater(this);
                    _ResourceUpdater.ResourceApplyStart += OnUpdaterResourceApplyStart;
                    _ResourceUpdater.ResourceApplySuccess += OnUpdaterResourceApplySuccess;
                    _ResourceUpdater.ResourceApplyFailure += OnUpdaterResourceApplyFailure;
                    _ResourceUpdater.ResourceApplyComplete += OnUpdaterResourceApplyComplete;
                    _ResourceUpdater.ResourceUpdateStart += OnUpdaterResourceUpdateStart;
                    _ResourceUpdater.ResourceUpdateChanged += OnUpdaterResourceUpdateChanged;
                    _ResourceUpdater.ResourceUpdateSuccess += OnUpdaterResourceUpdateSuccess;
                    _ResourceUpdater.ResourceUpdateFailure += OnUpdaterResourceUpdateFailure;
                    _ResourceUpdater.ResourceUpdateComplete += OnUpdaterResourceUpdateComplete;
                    _ResourceUpdater.ResourceUpdateAllComplete += OnUpdaterResourceUpdateAllComplete;
                }
            }
            else if (_ResourceMode != resourceMode)
            {
                throw new GameFrameworkException("You can not change resource mode at this time.");
            }
        }

        /// <summary>
        /// 设置当前变体。
        /// </summary>
        /// <param name="currentVariant">当前变体。</param>
        public void SetCurrentVariant(string currentVariant)
        {
            if (_RefuseSetFlag)
            {
                throw new GameFrameworkException("You can not set current variant at this time.");
            }

            _CurrentVariant = currentVariant;
        }

        /// <summary>
        /// 设置对象池管理器。
        /// </summary>
        /// <param name="objectPoolManager">对象池管理器。</param>
        public void SetObjectPoolManager(IObjectPoolManager objectPoolManager)
        {
            if (objectPoolManager == null)
            {
                throw new GameFrameworkException("Object pool manager is invalid.");
            }

            _ResourceLoader.SetObjectPoolManager(objectPoolManager);
        }

        /// <summary>
        /// 设置文件系统管理器。
        /// </summary>
        /// <param name="fileSystemManager">文件系统管理器。</param>
        public void SetFileSystemManager(IFileSystemManager fileSystemManager)
        {
            if (fileSystemManager == null)
            {
                throw new GameFrameworkException("File system manager is invalid.");
            }

            _FileSystemManager = fileSystemManager;
        }

        /// <summary>
        /// 设置下载管理器。
        /// </summary>
        /// <param name="downloadManager">下载管理器。</param>
        public void SetDownloadManager(IDownloadManager downloadManager)
        {
            if (downloadManager == null)
            {
                throw new GameFrameworkException("Download manager is invalid.");
            }

            if (_VersionListProcessor != null)
            {
                _VersionListProcessor.SetDownloadManager(downloadManager);
            }

            if (_ResourceUpdater != null)
            {
                _ResourceUpdater.SetDownloadManager(downloadManager);
            }
        }

        /// <summary>
        /// 设置解密资源回调函数。
        /// </summary>
        /// <param name="decryptResourceCallback">要设置的解密资源回调函数。</param>
        /// <remarks>如果不设置，将使用默认的解密资源回调函数。</remarks>
        public void SetDecryptResourceCallback(DecryptResourceCallback decryptResourceCallback)
        {
            if (_ResourceLoader.TotalAgentCount > 0)
            {
                throw new GameFrameworkException("You must set decrypt resource callback before add load resource agent helper.");
            }

            _DecryptResourceCallback = decryptResourceCallback;
        }

        /// <summary>
        /// 设置资源辅助器。
        /// </summary>
        /// <param name="resourceHelper">资源辅助器。</param>
        public void SetResourceHelper(IResourceHelper resourceHelper)
        {
            if (resourceHelper == null)
            {
                throw new GameFrameworkException("Resource helper is invalid.");
            }

            if (_ResourceLoader.TotalAgentCount > 0)
            {
                throw new GameFrameworkException("You must set resource helper before add load resource agent helper.");
            }

            _ResourceHelper = resourceHelper;
        }

        /// <summary>
        /// 增加加载资源代理辅助器。
        /// </summary>
        /// <param name="loadResourceAgentHelper">要增加的加载资源代理辅助器。</param>
        public void AddLoadResourceAgentHelper(ILoadResourceAgentHelper loadResourceAgentHelper)
        {
            if (_ResourceHelper == null)
            {
                throw new GameFrameworkException("Resource helper is invalid.");
            }

            if (string.IsNullOrEmpty(_ReadOnlyPath))
            {
                throw new GameFrameworkException("Read-only path is invalid.");
            }

            if (string.IsNullOrEmpty(_ReadWritePath))
            {
                throw new GameFrameworkException("Read-write path is invalid.");
            }

            _ResourceLoader.AddLoadResourceAgentHelper(loadResourceAgentHelper, _ResourceHelper, _ReadOnlyPath, _ReadWritePath, _DecryptResourceCallback);
        }

        /// <summary>
        /// 使用单机模式并初始化资源。
        /// </summary>
        /// <param name="initResourcesCompleteCallback">使用单机模式并初始化资源完成时的回调函数。</param>
        public void InitResources(InitResourcesCompleteCallback initResourcesCompleteCallback)
        {
            if (initResourcesCompleteCallback == null)
            {
                throw new GameFrameworkException("Init resources complete callback is invalid.");
            }

            if (_ResourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("You must set resource mode first.");
            }

            if (_ResourceMode != ResourceMode.Package)
            {
                throw new GameFrameworkException("You can not use InitResources without package resource mode.");
            }

            if (_ResourceIniter == null)
            {
                throw new GameFrameworkException("You can not use InitResources at this time.");
            }

            _RefuseSetFlag = true;
            _InitResourcesCompleteCallback = initResourcesCompleteCallback;
            _ResourceIniter.InitResources(_CurrentVariant);
        }

        /// <summary>
        /// 使用可更新模式并检查版本资源列表。
        /// </summary>
        /// <param name="latestInternalResourceVersion">最新的内部资源版本号。</param>
        /// <returns>检查版本资源列表结果。</returns>
        public CheckVersionListResult CheckVersionList(int latestInternalResourceVersion)
        {
            if (_ResourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("You must set resource mode first.");
            }

            if (_ResourceMode != ResourceMode.Updatable && _ResourceMode != ResourceMode.UpdatableWhilePlaying)
            {
                throw new GameFrameworkException("You can not use CheckVersionList without updatable resource mode.");
            }

            if (_VersionListProcessor == null)
            {
                throw new GameFrameworkException("You can not use CheckVersionList at this time.");
            }

            return _VersionListProcessor.CheckVersionList(latestInternalResourceVersion);
        }

        /// <summary>
        /// 使用可更新模式并更新版本资源列表。
        /// </summary>
        /// <param name="versionListLength">版本资源列表大小。</param>
        /// <param name="versionListHashCode">版本资源列表哈希值。</param>
        /// <param name="versionListCompressedLength">版本资源列表压缩后大小。</param>
        /// <param name="versionListCompressedHashCode">版本资源列表压缩后哈希值。</param>
        /// <param name="updateVersionListCallbacks">版本资源列表更新回调函数集。</param>
        public void UpdateVersionList(int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode, UpdateVersionListCallbacks updateVersionListCallbacks)
        {
            if (updateVersionListCallbacks == null)
            {
                throw new GameFrameworkException("Update version list callbacks is invalid.");
            }

            if (_ResourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("You must set resource mode first.");
            }

            if (_ResourceMode != ResourceMode.Updatable && _ResourceMode != ResourceMode.UpdatableWhilePlaying)
            {
                throw new GameFrameworkException("You can not use UpdateVersionList without updatable resource mode.");
            }

            if (_VersionListProcessor == null)
            {
                throw new GameFrameworkException("You can not use UpdateVersionList at this time.");
            }

            _UpdateVersionListCallbacks = updateVersionListCallbacks;
            _VersionListProcessor.UpdateVersionList(versionListLength, versionListHashCode, versionListCompressedLength, versionListCompressedHashCode);
        }

        /// <summary>
        /// 使用可更新模式并校验资源。
        /// </summary>
        /// <param name="verifyResourceLengthPerFrame">每帧至少校验资源的大小，以字节为单位。</param>
        /// <param name="verifyResourcesCompleteCallback">使用可更新模式并校验资源完成时的回调函数。</param>
        public void VerifyResources(int verifyResourceLengthPerFrame, VerifyResourcesCompleteCallback verifyResourcesCompleteCallback)
        {
            if (verifyResourcesCompleteCallback == null)
            {
                throw new GameFrameworkException("Verify resources complete callback is invalid.");
            }

            if (_ResourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("You must set resource mode first.");
            }

            if (_ResourceMode != ResourceMode.Updatable && _ResourceMode != ResourceMode.UpdatableWhilePlaying)
            {
                throw new GameFrameworkException("You can not use VerifyResources without updatable resource mode.");
            }

            if (_RefuseSetFlag)
            {
                throw new GameFrameworkException("You can not verify resources at this time.");
            }

            _ResourceVerifier = new ResourceVerifier(this);
            _ResourceVerifier.ResourceVerifyStart += OnVerifierResourceVerifyStart;
            _ResourceVerifier.ResourceVerifySuccess += OnVerifierResourceVerifySuccess;
            _ResourceVerifier.ResourceVerifyFailure += OnVerifierResourceVerifyFailure;
            _ResourceVerifier.ResourceVerifyComplete += OnVerifierResourceVerifyComplete;
            _VerifyResourcesCompleteCallback = verifyResourcesCompleteCallback;
            _ResourceVerifier.VerifyResources(verifyResourceLengthPerFrame);
        }

        /// <summary>
        /// 使用可更新模式并检查资源。
        /// </summary>
        /// <param name="ignoreOtherVariant">是否忽略处理其它变体的资源，若不忽略，将会移除其它变体的资源。</param>
        /// <param name="checkResourcesCompleteCallback">使用可更新模式并检查资源完成时的回调函数。</param>
        public void CheckResources(bool ignoreOtherVariant, CheckResourcesCompleteCallback checkResourcesCompleteCallback)
        {
            if (checkResourcesCompleteCallback == null)
            {
                throw new GameFrameworkException("Check resources complete callback is invalid.");
            }

            if (_ResourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("You must set resource mode first.");
            }

            if (_ResourceMode != ResourceMode.Updatable && _ResourceMode != ResourceMode.UpdatableWhilePlaying)
            {
                throw new GameFrameworkException("You can not use CheckResources without updatable resource mode.");
            }

            if (_ResourceChecker == null)
            {
                throw new GameFrameworkException("You can not use CheckResources at this time.");
            }

            _RefuseSetFlag = true;
            _CheckResourcesCompleteCallback = checkResourcesCompleteCallback;
            _ResourceChecker.CheckResources(_CurrentVariant, ignoreOtherVariant);
        }

        /// <summary>
        /// 使用可更新模式并应用资源包资源。
        /// </summary>
        /// <param name="resourcePackPath">要应用的资源包路径。</param>
        /// <param name="applyResourcesCompleteCallback">使用可更新模式并应用资源包资源完成时的回调函数。</param>
        public void ApplyResources(string resourcePackPath, ApplyResourcesCompleteCallback applyResourcesCompleteCallback)
        {
            if (string.IsNullOrEmpty(resourcePackPath))
            {
                throw new GameFrameworkException("Resource pack path is invalid.");
            }

            if (!File.Exists(resourcePackPath))
            {
                throw new GameFrameworkException(Utility.Text.Format("Resource pack '{0}' is not exist.", resourcePackPath));
            }

            if (applyResourcesCompleteCallback == null)
            {
                throw new GameFrameworkException("Apply resources complete callback is invalid.");
            }

            if (_ResourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("You must set resource mode first.");
            }

            if (_ResourceMode != ResourceMode.Updatable && _ResourceMode != ResourceMode.UpdatableWhilePlaying)
            {
                throw new GameFrameworkException("You can not use ApplyResources without updatable resource mode.");
            }

            if (_ResourceUpdater == null)
            {
                throw new GameFrameworkException("You can not use ApplyResources at this time.");
            }

            _ApplyResourcesCompleteCallback = applyResourcesCompleteCallback;
            _ResourceUpdater.ApplyResources(resourcePackPath);
        }

        /// <summary>
        /// 使用可更新模式并更新所有资源。
        /// </summary>
        /// <param name="updateResourcesCompleteCallback">使用可更新模式并更新默认资源组完成时的回调函数。</param>
        public void UpdateResources(UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            UpdateResources(string.Empty, updateResourcesCompleteCallback);
        }

        /// <summary>
        /// 使用可更新模式并更新指定资源组的资源。
        /// </summary>
        /// <param name="resourceGroupName">要更新的资源组名称。</param>
        /// <param name="updateResourcesCompleteCallback">使用可更新模式并更新指定资源组完成时的回调函数。</param>
        public void UpdateResources(string resourceGroupName, UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            if (updateResourcesCompleteCallback == null)
            {
                throw new GameFrameworkException("Update resources complete callback is invalid.");
            }

            if (_ResourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("You must set resource mode first.");
            }

            if (_ResourceMode != ResourceMode.Updatable && _ResourceMode != ResourceMode.UpdatableWhilePlaying)
            {
                throw new GameFrameworkException("You can not use UpdateResources without updatable resource mode.");
            }

            if (_ResourceUpdater == null)
            {
                throw new GameFrameworkException("You can not use UpdateResources at this time.");
            }

            ResourceGroup resourceGroup = (ResourceGroup)GetResourceGroup(resourceGroupName);
            if (resourceGroup == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Can not find resource group '{0}'.", resourceGroupName));
            }

            _UpdateResourcesCompleteCallback = updateResourcesCompleteCallback;
            _ResourceUpdater.UpdateResources(resourceGroup);
        }

        /// <summary>
        /// 停止更新资源。
        /// </summary>
        public void StopUpdateResources()
        {
            if (_ResourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("You must set resource mode first.");
            }

            if (_ResourceMode != ResourceMode.Updatable && _ResourceMode != ResourceMode.UpdatableWhilePlaying)
            {
                throw new GameFrameworkException("You can not use StopUpdateResources without updatable resource mode.");
            }

            if (_ResourceUpdater == null)
            {
                throw new GameFrameworkException("You can not use StopUpdateResources at this time.");
            }

            _ResourceUpdater.StopUpdateResources();
            _UpdateResourcesCompleteCallback = null;
        }

        /// <summary>
        /// 校验资源包。
        /// </summary>
        /// <param name="resourcePackPath">要校验的资源包路径。</param>
        /// <returns>是否校验资源包成功。</returns>
        public bool VerifyResourcePack(string resourcePackPath)
        {
            if (string.IsNullOrEmpty(resourcePackPath))
            {
                throw new GameFrameworkException("Resource pack path is invalid.");
            }

            if (!File.Exists(resourcePackPath))
            {
                throw new GameFrameworkException(Utility.Text.Format("Resource pack '{0}' is not exist.", resourcePackPath));
            }

            if (_ResourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("You must set resource mode first.");
            }

            if (_ResourceMode != ResourceMode.Updatable && _ResourceMode != ResourceMode.UpdatableWhilePlaying)
            {
                throw new GameFrameworkException("You can not use VerifyResourcePack without updatable resource mode.");
            }

            if (_ResourcePackVersionListSerializer == null)
            {
                throw new GameFrameworkException("You can not use VerifyResourcePack at this time.");
            }

            try
            {
                long length = 0L;
                ResourcePackVersionList versionList = default(ResourcePackVersionList);
                using (FileStream fileStream = new FileStream(resourcePackPath, FileMode.Open, FileAccess.Read))
                {
                    length = fileStream.Length;
                    versionList = _ResourcePackVersionListSerializer.Deserialize(fileStream);
                }

                if (!versionList.IsValid)
                {
                    return false;
                }

                if (versionList.Offset + versionList.Length != length)
                {
                    return false;
                }

                int hashCode = 0;
                using (FileStream fileStream = new FileStream(resourcePackPath, FileMode.Open, FileAccess.Read))
                {
                    fileStream.Position = versionList.Offset;
                    hashCode = Utility.Verifier.GetCrc32(fileStream);
                }

                if (versionList.HashCode != hashCode)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取所有加载资源任务的信息。
        /// </summary>
        /// <returns>所有加载资源任务的信息。</returns>
        public TaskInfo[] GetAllLoadAssetInfos()
        {
            return _ResourceLoader.GetAllLoadAssetInfos();
        }

        /// <summary>
        /// 获取所有加载资源任务的信息。
        /// </summary>
        /// <param name="results">所有加载资源任务的信息。</param>
        public void GetAllLoadAssetInfos(List<TaskInfo> results)
        {
            _ResourceLoader.GetAllLoadAssetInfos(results);
        }

        /// <summary>
        /// 检查资源是否存在。
        /// </summary>
        /// <param name="assetName">要检查资源的名称。</param>
        /// <returns>检查资源是否存在的结果。</returns>
        public HasAssetResult HasAsset(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            return _ResourceLoader.HasAsset(assetName);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        public void LoadAsset(string assetName, LoadAssetCallbacks loadAssetCallbacks)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _ResourceLoader.LoadAsset(assetName, null, Constant.DefaultPriority, loadAssetCallbacks, null);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="assetType">要加载资源的类型。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks loadAssetCallbacks)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _ResourceLoader.LoadAsset(assetName, assetType, Constant.DefaultPriority, loadAssetCallbacks, null);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        public void LoadAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _ResourceLoader.LoadAsset(assetName, null, priority, loadAssetCallbacks, null);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadAsset(string assetName, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _ResourceLoader.LoadAsset(assetName, null, Constant.DefaultPriority, loadAssetCallbacks, userData);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="assetType">要加载资源的类型。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _ResourceLoader.LoadAsset(assetName, assetType, priority, loadAssetCallbacks, null);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="assetType">要加载资源的类型。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _ResourceLoader.LoadAsset(assetName, assetType, Constant.DefaultPriority, loadAssetCallbacks, userData);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _ResourceLoader.LoadAsset(assetName, null, priority, loadAssetCallbacks, userData);
        }

        /// <summary>
        /// 异步加载资源。
        /// </summary>
        /// <param name="assetName">要加载资源的名称。</param>
        /// <param name="assetType">要加载资源的类型。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <param name="loadAssetCallbacks">加载资源回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _ResourceLoader.LoadAsset(assetName, assetType, priority, loadAssetCallbacks, userData);
        }

        /// <summary>
        /// 卸载资源。
        /// </summary>
        /// <param name="asset">要卸载的资源。</param>
        public void UnloadAsset(object asset)
        {
            if (asset == null)
            {
                throw new GameFrameworkException("Asset is invalid.");
            }

            if (_ResourceLoader == null)
            {
                return;
            }

            _ResourceLoader.UnloadAsset(asset);
        }

        /// <summary>
        /// 异步加载场景。
        /// </summary>
        /// <param name="sceneAssetName">要加载场景资源的名称。</param>
        /// <param name="loadSceneCallbacks">加载场景回调函数集。</param>
        public void LoadScene(string sceneAssetName, LoadSceneCallbacks loadSceneCallbacks)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (loadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Load scene callbacks is invalid.");
            }

            _ResourceLoader.LoadScene(sceneAssetName, Constant.DefaultPriority, loadSceneCallbacks, null);
        }

        /// <summary>
        /// 异步加载场景。
        /// </summary>
        /// <param name="sceneAssetName">要加载场景资源的名称。</param>
        /// <param name="priority">加载场景资源的优先级。</param>
        /// <param name="loadSceneCallbacks">加载场景回调函数集。</param>
        public void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks loadSceneCallbacks)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (loadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Load scene callbacks is invalid.");
            }

            _ResourceLoader.LoadScene(sceneAssetName, priority, loadSceneCallbacks, null);
        }

        /// <summary>
        /// 异步加载场景。
        /// </summary>
        /// <param name="sceneAssetName">要加载场景资源的名称。</param>
        /// <param name="loadSceneCallbacks">加载场景回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadScene(string sceneAssetName, LoadSceneCallbacks loadSceneCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (loadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Load scene callbacks is invalid.");
            }

            _ResourceLoader.LoadScene(sceneAssetName, Constant.DefaultPriority, loadSceneCallbacks, userData);
        }

        /// <summary>
        /// 异步加载场景。
        /// </summary>
        /// <param name="sceneAssetName">要加载场景资源的名称。</param>
        /// <param name="priority">加载场景资源的优先级。</param>
        /// <param name="loadSceneCallbacks">加载场景回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks loadSceneCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (loadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Load scene callbacks is invalid.");
            }

            _ResourceLoader.LoadScene(sceneAssetName, priority, loadSceneCallbacks, userData);
        }

        /// <summary>
        /// 异步卸载场景。
        /// </summary>
        /// <param name="sceneAssetName">要卸载场景资源的名称。</param>
        /// <param name="unloadSceneCallbacks">卸载场景回调函数集。</param>
        public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (unloadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Unload scene callbacks is invalid.");
            }

            _ResourceLoader.UnloadScene(sceneAssetName, unloadSceneCallbacks, null);
        }

        /// <summary>
        /// 异步卸载场景。
        /// </summary>
        /// <param name="sceneAssetName">要卸载场景资源的名称。</param>
        /// <param name="unloadSceneCallbacks">卸载场景回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (unloadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Unload scene callbacks is invalid.");
            }

            _ResourceLoader.UnloadScene(sceneAssetName, unloadSceneCallbacks, userData);
        }

        /// <summary>
        /// 获取二进制资源的实际路径。
        /// </summary>
        /// <param name="binaryAssetName">要获取实际路径的二进制资源的名称。</param>
        /// <returns>二进制资源的实际路径。</returns>
        /// <remarks>此方法仅适用于二进制资源存储在磁盘（而非文件系统）中的情况。若二进制资源存储在文件系统中时，返回值将始终为空。</remarks>
        public string GetBinaryPath(string binaryAssetName)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            return _ResourceLoader.GetBinaryPath(binaryAssetName);
        }

        /// <summary>
        /// 获取二进制资源的实际路径。
        /// </summary>
        /// <param name="binaryAssetName">要获取实际路径的二进制资源的名称。</param>
        /// <param name="storageInReadOnly">二进制资源是否存储在只读区中。</param>
        /// <param name="storageInFileSystem">二进制资源是否存储在文件系统中。</param>
        /// <param name="relativePath">二进制资源或存储二进制资源的文件系统，相对于只读区或者读写区的相对路径。</param>
        /// <param name="fileName">若二进制资源存储在文件系统中，则指示二进制资源在文件系统中的名称，否则此参数返回空。</param>
        /// <returns>是否获取二进制资源的实际路径成功。</returns>
        public bool GetBinaryPath(string binaryAssetName, out bool storageInReadOnly, out bool storageInFileSystem, out string relativePath, out string fileName)
        {
            return _ResourceLoader.GetBinaryPath(binaryAssetName, out storageInReadOnly, out storageInFileSystem, out relativePath, out fileName);
        }

        /// <summary>
        /// 获取二进制资源的长度。
        /// </summary>
        /// <param name="binaryAssetName">要获取长度的二进制资源的名称。</param>
        /// <returns>二进制资源的长度。</returns>
        public int GetBinaryLength(string binaryAssetName)
        {
            return _ResourceLoader.GetBinaryLength(binaryAssetName);
        }

        /// <summary>
        /// 异步加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <param name="loadBinaryCallbacks">加载二进制资源回调函数集。</param>
        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (loadBinaryCallbacks == null)
            {
                throw new GameFrameworkException("Load binary callbacks is invalid.");
            }

            _ResourceLoader.LoadBinary(binaryAssetName, loadBinaryCallbacks, null);
        }

        /// <summary>
        /// 异步加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <param name="loadBinaryCallbacks">加载二进制资源回调函数集。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (loadBinaryCallbacks == null)
            {
                throw new GameFrameworkException("Load binary callbacks is invalid.");
            }

            _ResourceLoader.LoadBinary(binaryAssetName, loadBinaryCallbacks, userData);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <returns>存储加载二进制资源的二进制流。</returns>
        public byte[] LoadBinaryFromFileSystem(string binaryAssetName)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            return _ResourceLoader.LoadBinaryFromFileSystem(binaryAssetName);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源的二进制流。</param>
        /// <returns>实际加载了多少字节。</returns>
        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return _ResourceLoader.LoadBinaryFromFileSystem(binaryAssetName, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源的二进制流。</param>
        /// <param name="startIndex">存储加载二进制资源的二进制流的起始位置。</param>
        /// <returns>实际加载了多少字节。</returns>
        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return _ResourceLoader.LoadBinaryFromFileSystem(binaryAssetName, buffer, startIndex, buffer.Length - startIndex);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源。
        /// </summary>
        /// <param name="binaryAssetName">要加载二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源的二进制流。</param>
        /// <param name="startIndex">存储加载二进制资源的二进制流的起始位置。</param>
        /// <param name="length">存储加载二进制资源的二进制流的长度。</param>
        /// <returns>实际加载了多少字节。</returns>
        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return _ResourceLoader.LoadBinaryFromFileSystem(binaryAssetName, buffer, startIndex, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>存储加载二进制资源片段内容的二进制流。</returns>
        public byte[] LoadBinarySegmentFromFileSystem(string binaryAssetName, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            return _ResourceLoader.LoadBinarySegmentFromFileSystem(binaryAssetName, 0, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="offset">要加载片段的偏移。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>存储加载二进制资源片段内容的二进制流。</returns>
        public byte[] LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            return _ResourceLoader.LoadBinarySegmentFromFileSystem(binaryAssetName, offset, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <returns>实际加载了多少字节。</returns>
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return _ResourceLoader.LoadBinarySegmentFromFileSystem(binaryAssetName, 0, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>实际加载了多少字节。</returns>
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return _ResourceLoader.LoadBinarySegmentFromFileSystem(binaryAssetName, 0, buffer, 0, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <param name="startIndex">存储加载二进制资源片段内容的二进制流的起始位置。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>实际加载了多少字节。</returns>
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return _ResourceLoader.LoadBinarySegmentFromFileSystem(binaryAssetName, 0, buffer, startIndex, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="offset">要加载片段的偏移。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <returns>实际加载了多少字节。</returns>
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return _ResourceLoader.LoadBinarySegmentFromFileSystem(binaryAssetName, offset, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="offset">要加载片段的偏移。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>实际加载了多少字节。</returns>
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return _ResourceLoader.LoadBinarySegmentFromFileSystem(binaryAssetName, offset, buffer, 0, length);
        }

        /// <summary>
        /// 从文件系统中加载二进制资源的片段。
        /// </summary>
        /// <param name="binaryAssetName">要加载片段的二进制资源的名称。</param>
        /// <param name="offset">要加载片段的偏移。</param>
        /// <param name="buffer">存储加载二进制资源片段内容的二进制流。</param>
        /// <param name="startIndex">存储加载二进制资源片段内容的二进制流的起始位置。</param>
        /// <param name="length">要加载片段的长度。</param>
        /// <returns>实际加载了多少字节。</returns>
        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return _ResourceLoader.LoadBinarySegmentFromFileSystem(binaryAssetName, offset, buffer, startIndex, length);
        }

        /// <summary>
        /// 检查资源组是否存在。
        /// </summary>
        /// <param name="resourceGroupName">要检查资源组的名称。</param>
        /// <returns>资源组是否存在。</returns>
        public bool HasResourceGroup(string resourceGroupName)
        {
            return _ResourceGroups.ContainsKey(resourceGroupName ?? string.Empty);
        }

        /// <summary>
        /// 获取默认资源组。
        /// </summary>
        /// <returns>默认资源组。</returns>
        public IResourceGroup GetResourceGroup()
        {
            return GetResourceGroup(string.Empty);
        }

        /// <summary>
        /// 获取资源组。
        /// </summary>
        /// <param name="resourceGroupName">要获取的资源组名称。</param>
        /// <returns>要获取的资源组。</returns>
        public IResourceGroup GetResourceGroup(string resourceGroupName)
        {
            ResourceGroup resourceGroup = null;
            if (_ResourceGroups.TryGetValue(resourceGroupName ?? string.Empty, out resourceGroup))
            {
                return resourceGroup;
            }

            return null;
        }

        /// <summary>
        /// 获取所有资源组。
        /// </summary>
        /// <returns>所有资源组。</returns>
        public IResourceGroup[] GetAllResourceGroups()
        {
            int index = 0;
            IResourceGroup[] results = new IResourceGroup[_ResourceGroups.Count];
            foreach (KeyValuePair<string, ResourceGroup> resourceGroup in _ResourceGroups)
            {
                results[index++] = resourceGroup.Value;
            }

            return results;
        }

        /// <summary>
        /// 获取所有资源组。
        /// </summary>
        /// <param name="results">所有资源组。</param>
        public void GetAllResourceGroups(List<IResourceGroup> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<string, ResourceGroup> resourceGroup in _ResourceGroups)
            {
                results.Add(resourceGroup.Value);
            }
        }

        /// <summary>
        /// 获取资源组集合。
        /// </summary>
        /// <param name="resourceGroupNames">要获取的资源组名称的集合。</param>
        /// <returns>要获取的资源组集合。</returns>
        public IResourceGroupCollection GetResourceGroupCollection(params string[] resourceGroupNames)
        {
            if (resourceGroupNames == null || resourceGroupNames.Length < 1)
            {
                throw new GameFrameworkException("Resource group names is invalid.");
            }

            ResourceGroup[] resourceGroups = new ResourceGroup[resourceGroupNames.Length];
            for (int i = 0; i < resourceGroupNames.Length; i++)
            {
                if (string.IsNullOrEmpty(resourceGroupNames[i]))
                {
                    throw new GameFrameworkException("Resource group name is invalid.");
                }

                resourceGroups[i] = (ResourceGroup)GetResourceGroup(resourceGroupNames[i]);
                if (resourceGroups[i] == null)
                {
                    throw new GameFrameworkException(Utility.Text.Format("Resource group '{0}' is not exist.", resourceGroupNames[i]));
                }
            }

            return new ResourceGroupCollection(resourceGroups, _ResourceInfos);
        }

        /// <summary>
        /// 获取资源组集合。
        /// </summary>
        /// <param name="resourceGroupNames">要获取的资源组名称的集合。</param>
        /// <returns>要获取的资源组集合。</returns>
        public IResourceGroupCollection GetResourceGroupCollection(List<string> resourceGroupNames)
        {
            if (resourceGroupNames == null || resourceGroupNames.Count < 1)
            {
                throw new GameFrameworkException("Resource group names is invalid.");
            }

            ResourceGroup[] resourceGroups = new ResourceGroup[resourceGroupNames.Count];
            for (int i = 0; i < resourceGroupNames.Count; i++)
            {
                if (string.IsNullOrEmpty(resourceGroupNames[i]))
                {
                    throw new GameFrameworkException("Resource group name is invalid.");
                }

                resourceGroups[i] = (ResourceGroup)GetResourceGroup(resourceGroupNames[i]);
                if (resourceGroups[i] == null)
                {
                    throw new GameFrameworkException(Utility.Text.Format("Resource group '{0}' is not exist.", resourceGroupNames[i]));
                }
            }

            return new ResourceGroupCollection(resourceGroups, _ResourceInfos);
        }

        private void UpdateResource(ResourceName resourceName)
        {
            _ResourceUpdater.UpdateResource(resourceName);
        }

        private ResourceGroup GetOrAddResourceGroup(string resourceGroupName)
        {
            if (resourceGroupName == null)
            {
                resourceGroupName = string.Empty;
            }

            ResourceGroup resourceGroup = null;
            if (!_ResourceGroups.TryGetValue(resourceGroupName, out resourceGroup))
            {
                resourceGroup = new ResourceGroup(resourceGroupName, _ResourceInfos);
                _ResourceGroups.Add(resourceGroupName, resourceGroup);
            }

            return resourceGroup;
        }

        private AssetInfo GetAssetInfo(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (_AssetInfos == null)
            {
                return null;
            }

            AssetInfo assetInfo = null;
            if (_AssetInfos.TryGetValue(assetName, out assetInfo))
            {
                return assetInfo;
            }

            return null;
        }

        private ResourceInfo GetResourceInfo(ResourceName resourceName)
        {
            if (_ResourceInfos == null)
            {
                return null;
            }

            ResourceInfo resourceInfo = null;
            if (_ResourceInfos.TryGetValue(resourceName, out resourceInfo))
            {
                return resourceInfo;
            }

            return null;
        }

        private IFileSystem GetFileSystem(string fileSystemName, bool storageInReadOnly)
        {
            if (string.IsNullOrEmpty(fileSystemName))
            {
                throw new GameFrameworkException("File system name is invalid.");
            }

            IFileSystem fileSystem = null;
            if (storageInReadOnly)
            {
                if (!_ReadOnlyFileSystems.TryGetValue(fileSystemName, out fileSystem))
                {
                    string fullPath = Utility.Path.GetRegularPath(Path.Combine(_ReadOnlyPath, Utility.Text.Format("{0}.{1}", fileSystemName, DefaultExtension)));
                    fileSystem = _FileSystemManager.GetFileSystem(fullPath);
                    if (fileSystem == null)
                    {
                        fileSystem = _FileSystemManager.LoadFileSystem(fullPath, FileSystemAccess.Read);
                        _ReadOnlyFileSystems.Add(fileSystemName, fileSystem);
                    }
                }
            }
            else
            {
                if (!_ReadWriteFileSystems.TryGetValue(fileSystemName, out fileSystem))
                {
                    string fullPath = Utility.Path.GetRegularPath(Path.Combine(_ReadWritePath, Utility.Text.Format("{0}.{1}", fileSystemName, DefaultExtension)));
                    fileSystem = _FileSystemManager.GetFileSystem(fullPath);
                    if (fileSystem == null)
                    {
                        if (File.Exists(fullPath))
                        {
                            fileSystem = _FileSystemManager.LoadFileSystem(fullPath, FileSystemAccess.ReadWrite);
                        }
                        else
                        {
                            string directory = Path.GetDirectoryName(fullPath);
                            if (!Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }

                            fileSystem = _FileSystemManager.CreateFileSystem(fullPath, FileSystemAccess.ReadWrite, FileSystemMaxFileCount, FileSystemMaxBlockCount);
                        }

                        _ReadWriteFileSystems.Add(fileSystemName, fileSystem);
                    }
                }
            }

            return fileSystem;
        }

        private void PrepareCachedStream()
        {
            if (_CachedStream == null)
            {
                _CachedStream = new MemoryStream();
            }

            _CachedStream.Position = 0L;
            _CachedStream.SetLength(0L);
        }

        private void FreeCachedStream()
        {
            if (_CachedStream != null)
            {
                _CachedStream.Dispose();
                _CachedStream = null;
            }
        }

        private void OnIniterResourceInitComplete()
        {
            _ResourceIniter.ResourceInitComplete -= OnIniterResourceInitComplete;
            _ResourceIniter.Shutdown();
            _ResourceIniter = null;

            _InitResourcesCompleteCallback();
            _InitResourcesCompleteCallback = null;
        }

        private void OnVersionListProcessorUpdateSuccess(string downloadPath, string downloadUri)
        {
            _UpdateVersionListCallbacks.UpdateVersionListSuccessCallback(downloadPath, downloadUri);
        }

        private void OnVersionListProcessorUpdateFailure(string downloadUri, string errorMessage)
        {
            if (_UpdateVersionListCallbacks.UpdateVersionListFailureCallback != null)
            {
                _UpdateVersionListCallbacks.UpdateVersionListFailureCallback(downloadUri, errorMessage);
            }
        }

        private void OnVerifierResourceVerifyStart(int count, long totalLength)
        {
            if (_ResourceVerifyStartEventHandler != null)
            {
                ResourceVerifyStartEventArgs resourceVerifyStartEventArgs = ResourceVerifyStartEventArgs.Create(count, totalLength);
                _ResourceVerifyStartEventHandler(this, resourceVerifyStartEventArgs);
                ReferencePool.Release(resourceVerifyStartEventArgs);
            }
        }

        private void OnVerifierResourceVerifySuccess(ResourceName resourceName, int length)
        {
            if (_ResourceVerifySuccessEventHandler != null)
            {
                ResourceVerifySuccessEventArgs resourceVerifySuccessEventArgs = ResourceVerifySuccessEventArgs.Create(resourceName.FullName, length);
                _ResourceVerifySuccessEventHandler(this, resourceVerifySuccessEventArgs);
                ReferencePool.Release(resourceVerifySuccessEventArgs);
            }
        }

        private void OnVerifierResourceVerifyFailure(ResourceName resourceName)
        {
            if (_ResourceVerifyFailureEventHandler != null)
            {
                ResourceVerifyFailureEventArgs resourceVerifyFailureEventArgs = ResourceVerifyFailureEventArgs.Create(resourceName.FullName);
                _ResourceVerifyFailureEventHandler(this, resourceVerifyFailureEventArgs);
                ReferencePool.Release(resourceVerifyFailureEventArgs);
            }
        }

        private void OnVerifierResourceVerifyComplete(bool result)
        {
            _VerifyResourcesCompleteCallback(result);
            _ResourceVerifier.ResourceVerifyStart -= OnVerifierResourceVerifyStart;
            _ResourceVerifier.ResourceVerifySuccess -= OnVerifierResourceVerifySuccess;
            _ResourceVerifier.ResourceVerifyFailure -= OnVerifierResourceVerifyFailure;
            _ResourceVerifier.ResourceVerifyComplete -= OnVerifierResourceVerifyComplete;
            _ResourceVerifier.Shutdown();
            _ResourceVerifier = null;
        }

        private void OnCheckerResourceNeedUpdate(ResourceName resourceName, string fileSystemName, LoadType loadType, int length, int hashCode, int compressedLength, int compressedHashCode)
        {
            _ResourceUpdater.AddResourceUpdate(resourceName, fileSystemName, loadType, length, hashCode, compressedLength, compressedHashCode, Utility.Path.GetRegularPath(Path.Combine(_ReadWritePath, resourceName.FullName)));
        }

        private void OnCheckerResourceCheckComplete(int movedCount, int removedCount, int updateCount, long updateTotalLength, long updateTotalCompressedLength)
        {
            _VersionListProcessor.VersionListUpdateSuccess -= OnVersionListProcessorUpdateSuccess;
            _VersionListProcessor.VersionListUpdateFailure -= OnVersionListProcessorUpdateFailure;
            _VersionListProcessor.Shutdown();
            _VersionListProcessor = null;
            _UpdateVersionListCallbacks = null;

            _ResourceChecker.ResourceNeedUpdate -= OnCheckerResourceNeedUpdate;
            _ResourceChecker.ResourceCheckComplete -= OnCheckerResourceCheckComplete;
            _ResourceChecker.Shutdown();
            _ResourceChecker = null;

            _ResourceUpdater.CheckResourceComplete(movedCount > 0 || removedCount > 0);

            if (updateCount <= 0)
            {
                _ResourceUpdater.ResourceApplyStart -= OnUpdaterResourceApplyStart;
                _ResourceUpdater.ResourceApplySuccess -= OnUpdaterResourceApplySuccess;
                _ResourceUpdater.ResourceApplyFailure -= OnUpdaterResourceApplyFailure;
                _ResourceUpdater.ResourceApplyComplete -= OnUpdaterResourceApplyComplete;
                _ResourceUpdater.ResourceUpdateStart -= OnUpdaterResourceUpdateStart;
                _ResourceUpdater.ResourceUpdateChanged -= OnUpdaterResourceUpdateChanged;
                _ResourceUpdater.ResourceUpdateSuccess -= OnUpdaterResourceUpdateSuccess;
                _ResourceUpdater.ResourceUpdateFailure -= OnUpdaterResourceUpdateFailure;
                _ResourceUpdater.ResourceUpdateComplete -= OnUpdaterResourceUpdateComplete;
                _ResourceUpdater.ResourceUpdateAllComplete -= OnUpdaterResourceUpdateAllComplete;
                _ResourceUpdater.Shutdown();
                _ResourceUpdater = null;

                _ReadWriteResourceInfos.Clear();
                _ReadWriteResourceInfos = null;

                FreeCachedStream();
            }

            _CheckResourcesCompleteCallback(movedCount, removedCount, updateCount, updateTotalLength, updateTotalCompressedLength);
            _CheckResourcesCompleteCallback = null;
        }

        private void OnUpdaterResourceApplyStart(string resourcePackPath, int count, long totalLength)
        {
            if (_ResourceApplyStartEventHandler != null)
            {
                ResourceApplyStartEventArgs resourceApplyStartEventArgs = ResourceApplyStartEventArgs.Create(resourcePackPath, count, totalLength);
                _ResourceApplyStartEventHandler(this, resourceApplyStartEventArgs);
                ReferencePool.Release(resourceApplyStartEventArgs);
            }
        }

        private void OnUpdaterResourceApplySuccess(ResourceName resourceName, string applyPath, string resourcePackPath, int length, int compressedLength)
        {
            if (_ResourceApplySuccessEventHandler != null)
            {
                ResourceApplySuccessEventArgs resourceApplySuccessEventArgs = ResourceApplySuccessEventArgs.Create(resourceName.FullName, applyPath, resourcePackPath, length, compressedLength);
                _ResourceApplySuccessEventHandler(this, resourceApplySuccessEventArgs);
                ReferencePool.Release(resourceApplySuccessEventArgs);
            }
        }

        private void OnUpdaterResourceApplyFailure(ResourceName resourceName, string resourcePackPath, string errorMessage)
        {
            if (_ResourceApplyFailureEventHandler != null)
            {
                ResourceApplyFailureEventArgs resourceApplyFailureEventArgs = ResourceApplyFailureEventArgs.Create(resourceName.FullName, resourcePackPath, errorMessage);
                _ResourceApplyFailureEventHandler(this, resourceApplyFailureEventArgs);
                ReferencePool.Release(resourceApplyFailureEventArgs);
            }
        }

        private void OnUpdaterResourceApplyComplete(string resourcePackPath, bool result)
        {
            ApplyResourcesCompleteCallback applyResourcesCompleteCallback = _ApplyResourcesCompleteCallback;
            _ApplyResourcesCompleteCallback = null;
            applyResourcesCompleteCallback(resourcePackPath, result);
        }

        private void OnUpdaterResourceUpdateStart(ResourceName resourceName, string downloadPath, string downloadUri, int currentLength, int compressedLength, int retryCount)
        {
            if (_ResourceUpdateStartEventHandler != null)
            {
                ResourceUpdateStartEventArgs resourceUpdateStartEventArgs = ResourceUpdateStartEventArgs.Create(resourceName.FullName, downloadPath, downloadUri, currentLength, compressedLength, retryCount);
                _ResourceUpdateStartEventHandler(this, resourceUpdateStartEventArgs);
                ReferencePool.Release(resourceUpdateStartEventArgs);
            }
        }

        private void OnUpdaterResourceUpdateChanged(ResourceName resourceName, string downloadPath, string downloadUri, int currentLength, int compressedLength)
        {
            if (_ResourceUpdateChangedEventHandler != null)
            {
                ResourceUpdateChangedEventArgs resourceUpdateChangedEventArgs = ResourceUpdateChangedEventArgs.Create(resourceName.FullName, downloadPath, downloadUri, currentLength, compressedLength);
                _ResourceUpdateChangedEventHandler(this, resourceUpdateChangedEventArgs);
                ReferencePool.Release(resourceUpdateChangedEventArgs);
            }
        }

        private void OnUpdaterResourceUpdateSuccess(ResourceName resourceName, string downloadPath, string downloadUri, int length, int compressedLength)
        {
            if (_ResourceUpdateSuccessEventHandler != null)
            {
                ResourceUpdateSuccessEventArgs resourceUpdateSuccessEventArgs = ResourceUpdateSuccessEventArgs.Create(resourceName.FullName, downloadPath, downloadUri, length, compressedLength);
                _ResourceUpdateSuccessEventHandler(this, resourceUpdateSuccessEventArgs);
                ReferencePool.Release(resourceUpdateSuccessEventArgs);
            }
        }

        private void OnUpdaterResourceUpdateFailure(ResourceName resourceName, string downloadUri, int retryCount, int totalRetryCount, string errorMessage)
        {
            if (_ResourceUpdateFailureEventHandler != null)
            {
                ResourceUpdateFailureEventArgs resourceUpdateFailureEventArgs = ResourceUpdateFailureEventArgs.Create(resourceName.FullName, downloadUri, retryCount, totalRetryCount, errorMessage);
                _ResourceUpdateFailureEventHandler(this, resourceUpdateFailureEventArgs);
                ReferencePool.Release(resourceUpdateFailureEventArgs);
            }
        }

        private void OnUpdaterResourceUpdateComplete(ResourceGroup resourceGroup, bool result)
        {
            Utility.Path.RemoveEmptyDirectory(_ReadWritePath);
            UpdateResourcesCompleteCallback updateResourcesCompleteCallback = _UpdateResourcesCompleteCallback;
            _UpdateResourcesCompleteCallback = null;
            updateResourcesCompleteCallback(resourceGroup, result);
        }

        private void OnUpdaterResourceUpdateAllComplete()
        {
            _ResourceUpdater.ResourceApplyStart -= OnUpdaterResourceApplyStart;
            _ResourceUpdater.ResourceApplySuccess -= OnUpdaterResourceApplySuccess;
            _ResourceUpdater.ResourceApplyFailure -= OnUpdaterResourceApplyFailure;
            _ResourceUpdater.ResourceApplyComplete -= OnUpdaterResourceApplyComplete;
            _ResourceUpdater.ResourceUpdateStart -= OnUpdaterResourceUpdateStart;
            _ResourceUpdater.ResourceUpdateChanged -= OnUpdaterResourceUpdateChanged;
            _ResourceUpdater.ResourceUpdateSuccess -= OnUpdaterResourceUpdateSuccess;
            _ResourceUpdater.ResourceUpdateFailure -= OnUpdaterResourceUpdateFailure;
            _ResourceUpdater.ResourceUpdateComplete -= OnUpdaterResourceUpdateComplete;
            _ResourceUpdater.ResourceUpdateAllComplete -= OnUpdaterResourceUpdateAllComplete;
            _ResourceUpdater.Shutdown();
            _ResourceUpdater = null;

            _ReadWriteResourceInfos.Clear();
            _ReadWriteResourceInfos = null;

            FreeCachedStream();
            Utility.Path.RemoveEmptyDirectory(_ReadWritePath);

            if (_ResourceUpdateAllCompleteEventHandler != null)
            {
                ResourceUpdateAllCompleteEventArgs resourceUpdateAllCompleteEventArgs = ResourceUpdateAllCompleteEventArgs.Create();
                _ResourceUpdateAllCompleteEventHandler(this, resourceUpdateAllCompleteEventArgs);
                ReferencePool.Release(resourceUpdateAllCompleteEventArgs);
            }
        }
    }
}
