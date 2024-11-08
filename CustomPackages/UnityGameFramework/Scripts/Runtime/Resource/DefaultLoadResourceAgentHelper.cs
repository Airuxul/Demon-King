//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.FileSystem;
using GameFramework.Resource;
using System;
using UnityEngine;
#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#endif
using UnityEngine.SceneManagement;
using Utility = GameFramework.Utility;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 默认加载资源代理辅助器。
    /// </summary>
    public class DefaultLoadResourceAgentHelper : LoadResourceAgentHelperBase, IDisposable
    {
        private string _fileFullPath = null;
        private string _fileName = null;
        private string _bytesFullPath = null;
        private string _assetName = null;
        private float _lastProgress = 0f;
        private bool _disposed = false;
#if UNITY_5_4_OR_NEWER
        private UnityWebRequest _unityWebRequest = null;
#else
        private WWW _WWW = null;
#endif
        private AssetBundleCreateRequest _fileAssetBundleCreateRequest = null;
        private AssetBundleCreateRequest _bytesAssetBundleCreateRequest = null;
        private AssetBundleRequest _assetBundleRequest = null;
        private AsyncOperation _asyncOperation = null;

        private EventHandler<LoadResourceAgentHelperUpdateEventArgs> _loadResourceAgentHelperUpdateEventHandler = null;
        private EventHandler<LoadResourceAgentHelperReadFileCompleteEventArgs> _loadResourceAgentHelperReadFileCompleteEventHandler = null;
        private EventHandler<LoadResourceAgentHelperReadBytesCompleteEventArgs> _loadResourceAgentHelperReadBytesCompleteEventHandler = null;
        private EventHandler<LoadResourceAgentHelperParseBytesCompleteEventArgs> _loadResourceAgentHelperParseBytesCompleteEventHandler = null;
        private EventHandler<LoadResourceAgentHelperLoadCompleteEventArgs> _loadResourceAgentHelperLoadCompleteEventHandler = null;
        private EventHandler<LoadResourceAgentHelperErrorEventArgs> _loadResourceAgentHelperErrorEventHandler = null;

        /// <summary>
        /// 加载资源代理辅助器异步加载资源更新事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperUpdateEventArgs> LoadResourceAgentHelperUpdate
        {
            add => _loadResourceAgentHelperUpdateEventHandler += value;
            remove => _loadResourceAgentHelperUpdateEventHandler -= value;
        }

        /// <summary>
        /// 加载资源代理辅助器异步读取资源文件完成事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperReadFileCompleteEventArgs> LoadResourceAgentHelperReadFileComplete
        {
            add => _loadResourceAgentHelperReadFileCompleteEventHandler += value;
            remove => _loadResourceAgentHelperReadFileCompleteEventHandler -= value;
        }

        /// <summary>
        /// 加载资源代理辅助器异步读取资源二进制流完成事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperReadBytesCompleteEventArgs> LoadResourceAgentHelperReadBytesComplete
        {
            add => _loadResourceAgentHelperReadBytesCompleteEventHandler += value;
            remove => _loadResourceAgentHelperReadBytesCompleteEventHandler -= value;
        }

        /// <summary>
        /// 加载资源代理辅助器异步将资源二进制流转换为加载对象完成事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperParseBytesCompleteEventArgs> LoadResourceAgentHelperParseBytesComplete
        {
            add => _loadResourceAgentHelperParseBytesCompleteEventHandler += value;
            remove => _loadResourceAgentHelperParseBytesCompleteEventHandler -= value;
        }

        /// <summary>
        /// 加载资源代理辅助器异步加载资源完成事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperLoadCompleteEventArgs> LoadResourceAgentHelperLoadComplete
        {
            add => _loadResourceAgentHelperLoadCompleteEventHandler += value;
            remove => _loadResourceAgentHelperLoadCompleteEventHandler -= value;
        }

        /// <summary>
        /// 加载资源代理辅助器错误事件。
        /// </summary>
        public override event EventHandler<LoadResourceAgentHelperErrorEventArgs> LoadResourceAgentHelperError
        {
            add => _loadResourceAgentHelperErrorEventHandler += value;
            remove => _loadResourceAgentHelperErrorEventHandler -= value;
        }

        /// <summary>
        /// 通过加载资源代理辅助器开始异步读取资源文件。
        /// </summary>
        /// <param name="fullPath">要加载资源的完整路径名。</param>
        public override void ReadFile(string fullPath)
        {
            if (_loadResourceAgentHelperReadFileCompleteEventHandler == null || _loadResourceAgentHelperUpdateEventHandler == null || _loadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            _fileFullPath = fullPath;
            _fileAssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(fullPath);
        }

        /// <summary>
        /// 通过加载资源代理辅助器开始异步读取资源文件。
        /// </summary>
        /// <param name="fileSystem">要加载资源的文件系统。</param>
        /// <param name="name">要加载资源的名称。</param>
        public override void ReadFile(IFileSystem fileSystem, string name)
        {
#if UNITY_5_3_5 || UNITY_5_3_6 || UNITY_5_3_7 || UNITY_5_3_8 || UNITY_5_4_OR_NEWER
            if (_loadResourceAgentHelperReadFileCompleteEventHandler == null || _loadResourceAgentHelperUpdateEventHandler == null || _loadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            FileInfo fileInfo = fileSystem.GetFileInfo(name);
            _fileFullPath = fileSystem.FullPath;
            _fileName = name;
            _fileAssetBundleCreateRequest = AssetBundle.LoadFromFileAsync(fileSystem.FullPath, 0u, (ulong)fileInfo.Offset);
#else
            Log.Fatal("Load from file async with offset is not supported, use Unity 5.3.5f1 or above.");
#endif
        }

        /// <summary>
        /// 通过加载资源代理辅助器开始异步读取资源二进制流。
        /// </summary>
        /// <param name="fullPath">要加载资源的完整路径名。</param>
        public override void ReadBytes(string fullPath)
        {
            if (_loadResourceAgentHelperReadBytesCompleteEventHandler == null || _loadResourceAgentHelperUpdateEventHandler == null || _loadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            _bytesFullPath = fullPath;
#if UNITY_5_4_OR_NEWER
            _unityWebRequest = UnityWebRequest.Get(Utility.Path.GetRemotePath(fullPath));
#if UNITY_2017_2_OR_NEWER
            _unityWebRequest.SendWebRequest();
#else
            _UnityWebRequest.Send();
#endif
#else
            _WWW = new WWW(Utility.Path.GetRemotePath(fullPath));
#endif
        }

        /// <summary>
        /// 通过加载资源代理辅助器开始异步读取资源二进制流。
        /// </summary>
        /// <param name="fileSystem">要加载资源的文件系统。</param>
        /// <param name="name">要加载资源的名称。</param>
        public override void ReadBytes(IFileSystem fileSystem, string name)
        {
            if (_loadResourceAgentHelperReadBytesCompleteEventHandler == null || _loadResourceAgentHelperUpdateEventHandler == null || _loadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            byte[] bytes = fileSystem.ReadFile(name);
            LoadResourceAgentHelperReadBytesCompleteEventArgs loadResourceAgentHelperReadBytesCompleteEventArgs = LoadResourceAgentHelperReadBytesCompleteEventArgs.Create(bytes);
            _loadResourceAgentHelperReadBytesCompleteEventHandler(this, loadResourceAgentHelperReadBytesCompleteEventArgs);
            ReferencePool.Release(loadResourceAgentHelperReadBytesCompleteEventArgs);
        }

        /// <summary>
        /// 通过加载资源代理辅助器开始异步将资源二进制流转换为加载对象。
        /// </summary>
        /// <param name="bytes">要加载资源的二进制流。</param>
        public override void ParseBytes(byte[] bytes)
        {
            if (_loadResourceAgentHelperParseBytesCompleteEventHandler == null || _loadResourceAgentHelperUpdateEventHandler == null || _loadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            _bytesAssetBundleCreateRequest = AssetBundle.LoadFromMemoryAsync(bytes);
        }

        /// <summary>
        /// 通过加载资源代理辅助器开始异步加载资源。
        /// </summary>
        /// <param name="resource">资源。</param>
        /// <param name="assetName">要加载的资源名称。</param>
        /// <param name="assetType">要加载资源的类型。</param>
        /// <param name="isScene">要加载的资源是否是场景。</param>
        public override void LoadAsset(object resource, string assetName, Type assetType, bool isScene)
        {
            if (_loadResourceAgentHelperLoadCompleteEventHandler == null || _loadResourceAgentHelperUpdateEventHandler == null || _loadResourceAgentHelperErrorEventHandler == null)
            {
                Log.Fatal("Load resource agent helper handler is invalid.");
                return;
            }

            AssetBundle assetBundle = resource as AssetBundle;
            if (assetBundle == null)
            {
                LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.TypeError, "Can not load asset bundle from loaded resource which is not an asset bundle.");
                _loadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                return;
            }

            if (string.IsNullOrEmpty(assetName))
            {
                LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError, "Can not load asset from asset bundle which child name is invalid.");
                _loadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                return;
            }

            _assetName = assetName;
            if (isScene)
            {
                int sceneNamePositionStart = assetName.LastIndexOf('/');
                int sceneNamePositionEnd = assetName.LastIndexOf('.');
                if (sceneNamePositionStart <= 0 || sceneNamePositionEnd <= 0 || sceneNamePositionStart > sceneNamePositionEnd)
                {
                    LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError, Utility.Text.Format("Scene name '{0}' is invalid.", assetName));
                    _loadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    return;
                }

                string sceneName = assetName.Substring(sceneNamePositionStart + 1, sceneNamePositionEnd - sceneNamePositionStart - 1);
                _asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
            else
            {
                if (assetType != null)
                {
                    _assetBundleRequest = assetBundle.LoadAssetAsync(assetName, assetType);
                }
                else
                {
                    _assetBundleRequest = assetBundle.LoadAssetAsync(assetName);
                }
            }
        }

        /// <summary>
        /// 重置加载资源代理辅助器。
        /// </summary>
        public override void Reset()
        {
            _fileFullPath = null;
            _fileName = null;
            _bytesFullPath = null;
            _assetName = null;
            _lastProgress = 0f;

#if UNITY_5_4_OR_NEWER
            if (_unityWebRequest != null)
            {
                _unityWebRequest.Dispose();
                _unityWebRequest = null;
            }
#else
            if (_WWW != null)
            {
                _WWW.Dispose();
                _WWW = null;
            }
#endif

            _fileAssetBundleCreateRequest = null;
            _bytesAssetBundleCreateRequest = null;
            _assetBundleRequest = null;
            _asyncOperation = null;
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
#if UNITY_5_4_OR_NEWER
                if (_unityWebRequest != null)
                {
                    _unityWebRequest.Dispose();
                    _unityWebRequest = null;
                }
#else
                if (_WWW != null)
                {
                    _WWW.Dispose();
                    _WWW = null;
                }
#endif
            }

            _disposed = true;
        }

        private void Update()
        {
#if UNITY_5_4_OR_NEWER
            UpdateUnityWebRequest();
#else
            UpdateWWW();
#endif
            UpdateFileAssetBundleCreateRequest();
            UpdateBytesAssetBundleCreateRequest();
            UpdateAssetBundleRequest();
            UpdateAsyncOperation();
        }

#if UNITY_5_4_OR_NEWER
        private void UpdateUnityWebRequest()
        {
            if (_unityWebRequest != null)
            {
                if (_unityWebRequest.isDone)
                {
                    if (string.IsNullOrEmpty(_unityWebRequest.error))
                    {
                        LoadResourceAgentHelperReadBytesCompleteEventArgs loadResourceAgentHelperReadBytesCompleteEventArgs = LoadResourceAgentHelperReadBytesCompleteEventArgs.Create(_unityWebRequest.downloadHandler.data);
                        _loadResourceAgentHelperReadBytesCompleteEventHandler(this, loadResourceAgentHelperReadBytesCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperReadBytesCompleteEventArgs);
                        _unityWebRequest.Dispose();
                        _unityWebRequest = null;
                        _bytesFullPath = null;
                        _lastProgress = 0f;
                    }
                    else
                    {
                        bool isError = false;
#if UNITY_2020_2_OR_NEWER
                        isError = _unityWebRequest.result != UnityWebRequest.Result.Success;
#elif UNITY_2017_1_OR_NEWER
                        isError = _UnityWebRequest.isNetworkError || _UnityWebRequest.isHttpError;
#else
                        isError = _UnityWebRequest.isError;
#endif
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.NotExist, Utility.Text.Format("Can not load asset bundle '{0}' with error message '{1}'.", _bytesFullPath, isError ? _unityWebRequest.error : null));
                        _loadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (_unityWebRequest.downloadProgress != _lastProgress)
                {
                    _lastProgress = _unityWebRequest.downloadProgress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.ReadResource, _unityWebRequest.downloadProgress);
                    _loadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }
#else
        private void UpdateWWW()
        {
            if (_WWW != null)
            {
                if (_WWW.isDone)
                {
                    if (string.IsNullOrEmpty(_WWW.error))
                    {
                        LoadResourceAgentHelperReadBytesCompleteEventArgs loadResourceAgentHelperReadBytesCompleteEventArgs = LoadResourceAgentHelperReadBytesCompleteEventArgs.Create(_WWW.bytes);
                        _LoadResourceAgentHelperReadBytesCompleteEventHandler(this, loadResourceAgentHelperReadBytesCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperReadBytesCompleteEventArgs);
                        _WWW.Dispose();
                        _WWW = null;
                        _BytesFullPath = null;
                        _LastProgress = 0f;
                    }
                    else
                    {
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.NotExist, Utility.Text.Format("Can not load asset bundle '{0}' with error message '{1}'.", _BytesFullPath, _WWW.error));
                        _LoadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (_WWW.progress != _LastProgress)
                {
                    _LastProgress = _WWW.progress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.ReadResource, _WWW.progress);
                    _LoadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }
#endif

        private void UpdateFileAssetBundleCreateRequest()
        {
            if (_fileAssetBundleCreateRequest != null)
            {
                if (_fileAssetBundleCreateRequest.isDone)
                {
                    AssetBundle assetBundle = _fileAssetBundleCreateRequest.assetBundle;
                    if (assetBundle != null)
                    {
                        AssetBundleCreateRequest oldFileAssetBundleCreateRequest = _fileAssetBundleCreateRequest;
                        LoadResourceAgentHelperReadFileCompleteEventArgs loadResourceAgentHelperReadFileCompleteEventArgs = LoadResourceAgentHelperReadFileCompleteEventArgs.Create(assetBundle);
                        _loadResourceAgentHelperReadFileCompleteEventHandler(this, loadResourceAgentHelperReadFileCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperReadFileCompleteEventArgs);
                        if (_fileAssetBundleCreateRequest == oldFileAssetBundleCreateRequest)
                        {
                            _fileAssetBundleCreateRequest = null;
                            _lastProgress = 0f;
                        }
                    }
                    else
                    {
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.NotExist, Utility.Text.Format("Can not load asset bundle from file '{0}' which is not a valid asset bundle.", _fileName == null ? _fileFullPath : Utility.Text.Format("{0} | {1}", _fileFullPath, _fileName)));
                        _loadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (_fileAssetBundleCreateRequest.progress != _lastProgress)
                {
                    _lastProgress = _fileAssetBundleCreateRequest.progress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadResource, _fileAssetBundleCreateRequest.progress);
                    _loadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }

        private void UpdateBytesAssetBundleCreateRequest()
        {
            if (_bytesAssetBundleCreateRequest != null)
            {
                if (_bytesAssetBundleCreateRequest.isDone)
                {
                    AssetBundle assetBundle = _bytesAssetBundleCreateRequest.assetBundle;
                    if (assetBundle != null)
                    {
                        AssetBundleCreateRequest oldBytesAssetBundleCreateRequest = _bytesAssetBundleCreateRequest;
                        LoadResourceAgentHelperParseBytesCompleteEventArgs loadResourceAgentHelperParseBytesCompleteEventArgs = LoadResourceAgentHelperParseBytesCompleteEventArgs.Create(assetBundle);
                        _loadResourceAgentHelperParseBytesCompleteEventHandler(this, loadResourceAgentHelperParseBytesCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperParseBytesCompleteEventArgs);
                        if (_bytesAssetBundleCreateRequest == oldBytesAssetBundleCreateRequest)
                        {
                            _bytesAssetBundleCreateRequest = null;
                            _lastProgress = 0f;
                        }
                    }
                    else
                    {
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.NotExist, "Can not load asset bundle from memory which is not a valid asset bundle.");
                        _loadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (_bytesAssetBundleCreateRequest.progress != _lastProgress)
                {
                    _lastProgress = _bytesAssetBundleCreateRequest.progress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadResource, _bytesAssetBundleCreateRequest.progress);
                    _loadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }

        private void UpdateAssetBundleRequest()
        {
            if (_assetBundleRequest != null)
            {
                if (_assetBundleRequest.isDone)
                {
                    if (_assetBundleRequest.asset != null)
                    {
                        LoadResourceAgentHelperLoadCompleteEventArgs loadResourceAgentHelperLoadCompleteEventArgs = LoadResourceAgentHelperLoadCompleteEventArgs.Create(_assetBundleRequest.asset);
                        _loadResourceAgentHelperLoadCompleteEventHandler(this, loadResourceAgentHelperLoadCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperLoadCompleteEventArgs);
                        _assetName = null;
                        _lastProgress = 0f;
                        _assetBundleRequest = null;
                    }
                    else
                    {
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError, Utility.Text.Format("Can not load asset '{0}' from asset bundle which is not exist.", _assetName));
                        _loadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (_assetBundleRequest.progress != _lastProgress)
                {
                    _lastProgress = _assetBundleRequest.progress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadAsset, _assetBundleRequest.progress);
                    _loadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }

        private void UpdateAsyncOperation()
        {
            if (_asyncOperation != null)
            {
                if (_asyncOperation.isDone)
                {
                    if (_asyncOperation.allowSceneActivation)
                    {
                        SceneAsset sceneAsset = new SceneAsset();
                        LoadResourceAgentHelperLoadCompleteEventArgs loadResourceAgentHelperLoadCompleteEventArgs = LoadResourceAgentHelperLoadCompleteEventArgs.Create(sceneAsset);
                        _loadResourceAgentHelperLoadCompleteEventHandler(this, loadResourceAgentHelperLoadCompleteEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperLoadCompleteEventArgs);
                        _assetName = null;
                        _lastProgress = 0f;
                        _asyncOperation = null;
                    }
                    else
                    {
                        LoadResourceAgentHelperErrorEventArgs loadResourceAgentHelperErrorEventArgs = LoadResourceAgentHelperErrorEventArgs.Create(LoadResourceStatus.AssetError, Utility.Text.Format("Can not load scene asset '{0}' from asset bundle.", _assetName));
                        _loadResourceAgentHelperErrorEventHandler(this, loadResourceAgentHelperErrorEventArgs);
                        ReferencePool.Release(loadResourceAgentHelperErrorEventArgs);
                    }
                }
                else if (_asyncOperation.progress != _lastProgress)
                {
                    _lastProgress = _asyncOperation.progress;
                    LoadResourceAgentHelperUpdateEventArgs loadResourceAgentHelperUpdateEventArgs = LoadResourceAgentHelperUpdateEventArgs.Create(LoadResourceProgress.LoadScene, _asyncOperation.progress);
                    _loadResourceAgentHelperUpdateEventHandler(this, loadResourceAgentHelperUpdateEventArgs);
                    ReferencePool.Release(loadResourceAgentHelperUpdateEventArgs);
                }
            }
        }
    }
}
