//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Resource;
using System;

namespace GameFramework
{
    /// <summary>
    /// 数据提供者。
    /// </summary>
    /// <typeparam name="T">数据提供者的持有者的类型。</typeparam>
    internal sealed class DataProvider<T> : IDataProvider<T>
    {
        private const int BlockSize = 1024 * 4;
        private static byte[] s_CachedBytes = null;

        private readonly T _Owner;
        private readonly LoadAssetCallbacks _LoadAssetCallbacks;
        private readonly LoadBinaryCallbacks _LoadBinaryCallbacks;
        private IResourceManager _ResourceManager;
        private IDataProviderHelper<T> _DataProviderHelper;
        private EventHandler<ReadDataSuccessEventArgs> _ReadDataSuccessEventHandler;
        private EventHandler<ReadDataFailureEventArgs> _ReadDataFailureEventHandler;
        private EventHandler<ReadDataUpdateEventArgs> _ReadDataUpdateEventHandler;
        private EventHandler<ReadDataDependencyAssetEventArgs> _ReadDataDependencyAssetEventHandler;

        /// <summary>
        /// 初始化数据提供者的新实例。
        /// </summary>
        /// <param name="owner">数据提供者的持有者。</param>
        public DataProvider(T owner)
        {
            _Owner = owner;
            _LoadAssetCallbacks = new LoadAssetCallbacks(LoadAssetSuccessCallback, LoadAssetOrBinaryFailureCallback, LoadAssetUpdateCallback, LoadAssetDependencyAssetCallback);
            _LoadBinaryCallbacks = new LoadBinaryCallbacks(LoadBinarySuccessCallback, LoadAssetOrBinaryFailureCallback);
            _ResourceManager = null;
            _DataProviderHelper = null;
            _ReadDataSuccessEventHandler = null;
            _ReadDataFailureEventHandler = null;
            _ReadDataUpdateEventHandler = null;
            _ReadDataDependencyAssetEventHandler = null;
        }

        /// <summary>
        /// 获取缓冲二进制流的大小。
        /// </summary>
        public static int CachedBytesSize
        {
            get
            {
                return s_CachedBytes != null ? s_CachedBytes.Length : 0;
            }
        }

        /// <summary>
        /// 读取数据成功事件。
        /// </summary>
        public event EventHandler<ReadDataSuccessEventArgs> ReadDataSuccess
        {
            add
            {
                _ReadDataSuccessEventHandler += value;
            }
            remove
            {
                _ReadDataSuccessEventHandler -= value;
            }
        }

        /// <summary>
        /// 读取数据失败事件。
        /// </summary>
        public event EventHandler<ReadDataFailureEventArgs> ReadDataFailure
        {
            add
            {
                _ReadDataFailureEventHandler += value;
            }
            remove
            {
                _ReadDataFailureEventHandler -= value;
            }
        }

        /// <summary>
        /// 读取数据更新事件。
        /// </summary>
        public event EventHandler<ReadDataUpdateEventArgs> ReadDataUpdate
        {
            add
            {
                _ReadDataUpdateEventHandler += value;
            }
            remove
            {
                _ReadDataUpdateEventHandler -= value;
            }
        }

        /// <summary>
        /// 读取数据时加载依赖资源事件。
        /// </summary>
        public event EventHandler<ReadDataDependencyAssetEventArgs> ReadDataDependencyAsset
        {
            add
            {
                _ReadDataDependencyAssetEventHandler += value;
            }
            remove
            {
                _ReadDataDependencyAssetEventHandler -= value;
            }
        }

        /// <summary>
        /// 确保二进制流缓存分配足够大小的内存并缓存。
        /// </summary>
        /// <param name="ensureSize">要确保二进制流缓存分配内存的大小。</param>
        public static void EnsureCachedBytesSize(int ensureSize)
        {
            if (ensureSize < 0)
            {
                throw new GameFrameworkException("Ensure size is invalid.");
            }

            if (s_CachedBytes == null || s_CachedBytes.Length < ensureSize)
            {
                FreeCachedBytes();
                int size = (ensureSize - 1 + BlockSize) / BlockSize * BlockSize;
                s_CachedBytes = new byte[size];
            }
        }

        /// <summary>
        /// 释放缓存的二进制流。
        /// </summary>
        public static void FreeCachedBytes()
        {
            s_CachedBytes = null;
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <param name="dataAssetName">内容资源名称。</param>
        public void ReadData(string dataAssetName)
        {
            ReadData(dataAssetName, Constant.DefaultPriority, null);
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <param name="dataAssetName">内容资源名称。</param>
        /// <param name="priority">加载数据资源的优先级。</param>
        public void ReadData(string dataAssetName, int priority)
        {
            ReadData(dataAssetName, priority, null);
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <param name="dataAssetName">内容资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void ReadData(string dataAssetName, object userData)
        {
            ReadData(dataAssetName, Constant.DefaultPriority, userData);
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <param name="dataAssetName">内容资源名称。</param>
        /// <param name="priority">加载数据资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void ReadData(string dataAssetName, int priority, object userData)
        {
            if (_ResourceManager == null)
            {
                throw new GameFrameworkException("You must set resource manager first.");
            }

            if (_DataProviderHelper == null)
            {
                throw new GameFrameworkException("You must set data provider helper first.");
            }

            HasAssetResult result = _ResourceManager.HasAsset(dataAssetName);
            switch (result)
            {
                case HasAssetResult.AssetOnDisk:
                case HasAssetResult.AssetOnFileSystem:
                    _ResourceManager.LoadAsset(dataAssetName, priority, _LoadAssetCallbacks, userData);
                    break;

                case HasAssetResult.BinaryOnDisk:
                    _ResourceManager.LoadBinary(dataAssetName, _LoadBinaryCallbacks, userData);
                    break;

                case HasAssetResult.BinaryOnFileSystem:
                    int dataLength = _ResourceManager.GetBinaryLength(dataAssetName);
                    EnsureCachedBytesSize(dataLength);
                    if (dataLength != _ResourceManager.LoadBinaryFromFileSystem(dataAssetName, s_CachedBytes))
                    {
                        throw new GameFrameworkException(Utility.Text.Format("Load binary '{0}' from file system with internal error.", dataAssetName));
                    }

                    try
                    {
                        if (!_DataProviderHelper.ReadData(_Owner, dataAssetName, s_CachedBytes, 0, dataLength, userData))
                        {
                            throw new GameFrameworkException(Utility.Text.Format("Load data failure in data provider helper, data asset name '{0}'.", dataAssetName));
                        }

                        if (_ReadDataSuccessEventHandler != null)
                        {
                            ReadDataSuccessEventArgs loadDataSuccessEventArgs = ReadDataSuccessEventArgs.Create(dataAssetName, 0f, userData);
                            _ReadDataSuccessEventHandler(this, loadDataSuccessEventArgs);
                            ReferencePool.Release(loadDataSuccessEventArgs);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (_ReadDataFailureEventHandler != null)
                        {
                            ReadDataFailureEventArgs loadDataFailureEventArgs = ReadDataFailureEventArgs.Create(dataAssetName, exception.ToString(), userData);
                            _ReadDataFailureEventHandler(this, loadDataFailureEventArgs);
                            ReferencePool.Release(loadDataFailureEventArgs);
                            return;
                        }

                        throw;
                    }

                    break;

                default:
                    throw new GameFrameworkException(Utility.Text.Format("Data asset '{0}' is '{1}'.", dataAssetName, result));
            }
        }

        /// <summary>
        /// 解析内容。
        /// </summary>
        /// <param name="dataString">要解析的内容字符串。</param>
        /// <returns>是否解析内容成功。</returns>
        public bool ParseData(string dataString)
        {
            return ParseData(dataString, null);
        }

        /// <summary>
        /// 解析内容。
        /// </summary>
        /// <param name="dataString">要解析的内容字符串。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否解析内容成功。</returns>
        public bool ParseData(string dataString, object userData)
        {
            if (_DataProviderHelper == null)
            {
                throw new GameFrameworkException("You must set data helper first.");
            }

            if (dataString == null)
            {
                throw new GameFrameworkException("Data string is invalid.");
            }

            try
            {
                return _DataProviderHelper.ParseData(_Owner, dataString, userData);
            }
            catch (Exception exception)
            {
                if (exception is GameFrameworkException)
                {
                    throw;
                }

                throw new GameFrameworkException(Utility.Text.Format("Can not parse data string with exception '{0}'.", exception), exception);
            }
        }

        /// <summary>
        /// 解析内容。
        /// </summary>
        /// <param name="dataBytes">要解析的内容二进制流。</param>
        /// <returns>是否解析内容成功。</returns>
        public bool ParseData(byte[] dataBytes)
        {
            if (dataBytes == null)
            {
                throw new GameFrameworkException("Data bytes is invalid.");
            }

            return ParseData(dataBytes, 0, dataBytes.Length, null);
        }

        /// <summary>
        /// 解析内容。
        /// </summary>
        /// <param name="dataBytes">要解析的内容二进制流。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否解析内容成功。</returns>
        public bool ParseData(byte[] dataBytes, object userData)
        {
            if (dataBytes == null)
            {
                throw new GameFrameworkException("Data bytes is invalid.");
            }

            return ParseData(dataBytes, 0, dataBytes.Length, userData);
        }

        /// <summary>
        /// 解析内容。
        /// </summary>
        /// <param name="dataBytes">要解析的内容二进制流。</param>
        /// <param name="startIndex">内容二进制流的起始位置。</param>
        /// <param name="length">内容二进制流的长度。</param>
        /// <returns>是否解析内容成功。</returns>
        public bool ParseData(byte[] dataBytes, int startIndex, int length)
        {
            return ParseData(dataBytes, startIndex, length, null);
        }

        /// <summary>
        /// 解析内容。
        /// </summary>
        /// <param name="dataBytes">要解析的内容二进制流。</param>
        /// <param name="startIndex">内容二进制流的起始位置。</param>
        /// <param name="length">内容二进制流的长度。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否解析内容成功。</returns>
        public bool ParseData(byte[] dataBytes, int startIndex, int length, object userData)
        {
            if (_DataProviderHelper == null)
            {
                throw new GameFrameworkException("You must set data helper first.");
            }

            if (dataBytes == null)
            {
                throw new GameFrameworkException("Data bytes is invalid.");
            }

            if (startIndex < 0 || length < 0 || startIndex + length > dataBytes.Length)
            {
                throw new GameFrameworkException("Start index or length is invalid.");
            }

            try
            {
                return _DataProviderHelper.ParseData(_Owner, dataBytes, startIndex, length, userData);
            }
            catch (Exception exception)
            {
                if (exception is GameFrameworkException)
                {
                    throw;
                }

                throw new GameFrameworkException(Utility.Text.Format("Can not parse data bytes with exception '{0}'.", exception), exception);
            }
        }

        /// <summary>
        /// 设置资源管理器。
        /// </summary>
        /// <param name="resourceManager">资源管理器。</param>
        internal void SetResourceManager(IResourceManager resourceManager)
        {
            if (resourceManager == null)
            {
                throw new GameFrameworkException("Resource manager is invalid.");
            }

            _ResourceManager = resourceManager;
        }

        /// <summary>
        /// 设置数据提供者辅助器。
        /// </summary>
        /// <param name="dataProviderHelper">数据提供者辅助器。</param>
        internal void SetDataProviderHelper(IDataProviderHelper<T> dataProviderHelper)
        {
            if (dataProviderHelper == null)
            {
                throw new GameFrameworkException("Data provider helper is invalid.");
            }

            _DataProviderHelper = dataProviderHelper;
        }

        private void LoadAssetSuccessCallback(string dataAssetName, object dataAsset, float duration, object userData)
        {
            try
            {
                if (!_DataProviderHelper.ReadData(_Owner, dataAssetName, dataAsset, userData))
                {
                    throw new GameFrameworkException(Utility.Text.Format("Load data failure in data provider helper, data asset name '{0}'.", dataAssetName));
                }

                if (_ReadDataSuccessEventHandler != null)
                {
                    ReadDataSuccessEventArgs loadDataSuccessEventArgs = ReadDataSuccessEventArgs.Create(dataAssetName, duration, userData);
                    _ReadDataSuccessEventHandler(this, loadDataSuccessEventArgs);
                    ReferencePool.Release(loadDataSuccessEventArgs);
                }
            }
            catch (Exception exception)
            {
                if (_ReadDataFailureEventHandler != null)
                {
                    ReadDataFailureEventArgs loadDataFailureEventArgs = ReadDataFailureEventArgs.Create(dataAssetName, exception.ToString(), userData);
                    _ReadDataFailureEventHandler(this, loadDataFailureEventArgs);
                    ReferencePool.Release(loadDataFailureEventArgs);
                    return;
                }

                throw;
            }
            finally
            {
                _DataProviderHelper.ReleaseDataAsset(_Owner, dataAsset);
            }
        }

        private void LoadAssetOrBinaryFailureCallback(string dataAssetName, LoadResourceStatus status, string errorMessage, object userData)
        {
            string appendErrorMessage = Utility.Text.Format("Load data failure, data asset name '{0}', status '{1}', error message '{2}'.", dataAssetName, status, errorMessage);
            if (_ReadDataFailureEventHandler != null)
            {
                ReadDataFailureEventArgs loadDataFailureEventArgs = ReadDataFailureEventArgs.Create(dataAssetName, appendErrorMessage, userData);
                _ReadDataFailureEventHandler(this, loadDataFailureEventArgs);
                ReferencePool.Release(loadDataFailureEventArgs);
                return;
            }

            throw new GameFrameworkException(appendErrorMessage);
        }

        private void LoadAssetUpdateCallback(string dataAssetName, float progress, object userData)
        {
            if (_ReadDataUpdateEventHandler != null)
            {
                ReadDataUpdateEventArgs loadDataUpdateEventArgs = ReadDataUpdateEventArgs.Create(dataAssetName, progress, userData);
                _ReadDataUpdateEventHandler(this, loadDataUpdateEventArgs);
                ReferencePool.Release(loadDataUpdateEventArgs);
            }
        }

        private void LoadAssetDependencyAssetCallback(string dataAssetName, string dependencyAssetName, int loadedCount, int totalCount, object userData)
        {
            if (_ReadDataDependencyAssetEventHandler != null)
            {
                ReadDataDependencyAssetEventArgs loadDataDependencyAssetEventArgs = ReadDataDependencyAssetEventArgs.Create(dataAssetName, dependencyAssetName, loadedCount, totalCount, userData);
                _ReadDataDependencyAssetEventHandler(this, loadDataDependencyAssetEventArgs);
                ReferencePool.Release(loadDataDependencyAssetEventArgs);
            }
        }

        private void LoadBinarySuccessCallback(string dataAssetName, byte[] dataBytes, float duration, object userData)
        {
            try
            {
                if (!_DataProviderHelper.ReadData(_Owner, dataAssetName, dataBytes, 0, dataBytes.Length, userData))
                {
                    throw new GameFrameworkException(Utility.Text.Format("Load data failure in data provider helper, data asset name '{0}'.", dataAssetName));
                }

                if (_ReadDataSuccessEventHandler != null)
                {
                    ReadDataSuccessEventArgs loadDataSuccessEventArgs = ReadDataSuccessEventArgs.Create(dataAssetName, duration, userData);
                    _ReadDataSuccessEventHandler(this, loadDataSuccessEventArgs);
                    ReferencePool.Release(loadDataSuccessEventArgs);
                }
            }
            catch (Exception exception)
            {
                if (_ReadDataFailureEventHandler != null)
                {
                    ReadDataFailureEventArgs loadDataFailureEventArgs = ReadDataFailureEventArgs.Create(dataAssetName, exception.ToString(), userData);
                    _ReadDataFailureEventHandler(this, loadDataFailureEventArgs);
                    ReferencePool.Release(loadDataFailureEventArgs);
                    return;
                }

                throw;
            }
        }
    }
}
