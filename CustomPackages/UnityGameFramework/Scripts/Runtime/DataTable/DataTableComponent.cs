//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.DataTable;
using GameFramework.Resource;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 数据表组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Data Table")]
    public sealed class DataTableComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;

        private IDataTableManager _dataTableManager = null;
        private EventComponent _eventComponent = null;

        [FormerlySerializedAs("_EnableLoadDataTableUpdateEvent")] [SerializeField]
        private bool enableLoadDataTableUpdateEvent = false;

        [FormerlySerializedAs("_EnableLoadDataTableDependencyAssetEvent")] [SerializeField]
        private bool enableLoadDataTableDependencyAssetEvent = false;

        [FormerlySerializedAs("_DataTableHelperTypeName")] [SerializeField]
        private string dataTableHelperTypeName = "UnityGameFramework.Runtime.DefaultDataTableHelper";

        [FormerlySerializedAs("_CustomDataTableHelper")] [SerializeField]
        private DataTableHelperBase customDataTableHelper = null;

        [FormerlySerializedAs("_CachedBytesSize")] [SerializeField]
        private int cachedBytesSize = 0;

        /// <summary>
        /// 获取数据表数量。
        /// </summary>
        public int Count => _dataTableManager.Count;

        /// <summary>
        /// 获取缓冲二进制流的大小。
        /// </summary>
        public int CachedBytesSize => _dataTableManager.CachedBytesSize;

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _dataTableManager = GameFrameworkEntry.GetModule<IDataTableManager>();
            if (_dataTableManager == null)
            {
                Log.Fatal("Data table manager is invalid.");
                return;
            }
        }

        private void Start()
        {
            BaseComponent baseComponent = GameEntry.GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Fatal("Base component is invalid.");
                return;
            }

            _eventComponent = GameEntry.GetComponent<EventComponent>();
            if (_eventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            if (baseComponent.EditorResourceMode)
            {
                _dataTableManager.SetResourceManager(baseComponent.EditorResourceHelper);
            }
            else
            {
                _dataTableManager.SetResourceManager(GameFrameworkEntry.GetModule<IResourceManager>());
            }

            DataTableHelperBase dataTableHelper = Helper.CreateHelper(dataTableHelperTypeName, customDataTableHelper);
            if (dataTableHelper == null)
            {
                Log.Error("Can not create data table helper.");
                return;
            }

            dataTableHelper.name = "Data Table Helper";
            Transform transform = dataTableHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;

            _dataTableManager.SetDataProviderHelper(dataTableHelper);
            _dataTableManager.SetDataTableHelper(dataTableHelper);
            if (cachedBytesSize > 0)
            {
                EnsureCachedBytesSize(cachedBytesSize);
            }
        }

        /// <summary>
        /// 确保二进制流缓存分配足够大小的内存并缓存。
        /// </summary>
        /// <param name="ensureSize">要确保二进制流缓存分配内存的大小。</param>
        public void EnsureCachedBytesSize(int ensureSize)
        {
            _dataTableManager.EnsureCachedBytesSize(ensureSize);
        }

        /// <summary>
        /// 释放缓存的二进制流。
        /// </summary>
        public void FreeCachedBytes()
        {
            _dataTableManager.FreeCachedBytes();
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable<T>() where T : IDataRow
        {
            return _dataTableManager.HasDataTable<T>();
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable(Type dataRowType)
        {
            return _dataTableManager.HasDataTable(dataRowType);
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable<T>(string name) where T : IDataRow
        {
            return _dataTableManager.HasDataTable<T>(name);
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable(Type dataRowType, string name)
        {
            return _dataTableManager.HasDataTable(dataRowType, name);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>要获取的数据表。</returns>
        public IDataTable<T> GetDataTable<T>() where T : IDataRow
        {
            return _dataTableManager.GetDataTable<T>();
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>要获取的数据表。</returns>
        public DataTableBase GetDataTable(Type dataRowType)
        {
            return _dataTableManager.GetDataTable(dataRowType);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>要获取的数据表。</returns>
        public IDataTable<T> GetDataTable<T>(string name) where T : IDataRow
        {
            return _dataTableManager.GetDataTable<T>(name);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>要获取的数据表。</returns>
        public DataTableBase GetDataTable(Type dataRowType, string name)
        {
            return _dataTableManager.GetDataTable(dataRowType, name);
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        public DataTableBase[] GetAllDataTables()
        {
            return _dataTableManager.GetAllDataTables();
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <param name="results">所有数据表。</param>
        public void GetAllDataTables(List<DataTableBase> results)
        {
            _dataTableManager.GetAllDataTables(results);
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>要创建的数据表。</returns>
        public IDataTable<T> CreateDataTable<T>() where T : class, IDataRow, new()
        {
            return CreateDataTable<T>(null);
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>要创建的数据表。</returns>
        public DataTableBase CreateDataTable(Type dataRowType)
        {
            return CreateDataTable(dataRowType, null);
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>要创建的数据表。</returns>
        public IDataTable<T> CreateDataTable<T>(string name) where T : class, IDataRow, new()
        {
            IDataTable<T> dataTable = _dataTableManager.CreateDataTable<T>(name);
            DataTableBase dataTableBase = (DataTableBase)dataTable;
            dataTableBase.ReadDataSuccess += OnReadDataSuccess;
            dataTableBase.ReadDataFailure += OnReadDataFailure;

            if (enableLoadDataTableUpdateEvent)
            {
                dataTableBase.ReadDataUpdate += OnReadDataUpdate;
            }

            if (enableLoadDataTableDependencyAssetEvent)
            {
                dataTableBase.ReadDataDependencyAsset += OnReadDataDependencyAsset;
            }

            return dataTable;
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>要创建的数据表。</returns>
        public DataTableBase CreateDataTable(Type dataRowType, string name)
        {
            DataTableBase dataTable = _dataTableManager.CreateDataTable(dataRowType, name);
            dataTable.ReadDataSuccess += OnReadDataSuccess;
            dataTable.ReadDataFailure += OnReadDataFailure;

            if (enableLoadDataTableUpdateEvent)
            {
                dataTable.ReadDataUpdate += OnReadDataUpdate;
            }

            if (enableLoadDataTableDependencyAssetEvent)
            {
                dataTable.ReadDataDependencyAsset += OnReadDataDependencyAsset;
            }

            return dataTable;
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable<T>() where T : IDataRow, new()
        {
            return _dataTableManager.DestroyDataTable<T>();
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable(Type dataRowType)
        {
            return _dataTableManager.DestroyDataTable(dataRowType);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable<T>(string name) where T : IDataRow
        {
            return _dataTableManager.DestroyDataTable<T>(name);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable(Type dataRowType, string name)
        {
            return _dataTableManager.DestroyDataTable(dataRowType, name);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="dataTable">要销毁的数据表。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable<T>(IDataTable<T> dataTable) where T : IDataRow
        {
            return _dataTableManager.DestroyDataTable(dataTable);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataTable">要销毁的数据表。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable(DataTableBase dataTable)
        {
            return _dataTableManager.DestroyDataTable(dataTable);
        }

        private void OnReadDataSuccess(object sender, ReadDataSuccessEventArgs e)
        {
            _eventComponent.Fire(this, LoadDataTableSuccessEventArgs.Create(e));
        }

        private void OnReadDataFailure(object sender, ReadDataFailureEventArgs e)
        {
            Log.Warning("Load data table failure, asset name '{0}', error message '{1}'.", e.DataAssetName, e.ErrorMessage);
            _eventComponent.Fire(this, LoadDataTableFailureEventArgs.Create(e));
        }

        private void OnReadDataUpdate(object sender, ReadDataUpdateEventArgs e)
        {
            _eventComponent.Fire(this, LoadDataTableUpdateEventArgs.Create(e));
        }

        private void OnReadDataDependencyAsset(object sender, ReadDataDependencyAssetEventArgs e)
        {
            _eventComponent.Fire(this, LoadDataTableDependencyAssetEventArgs.Create(e));
        }
    }
}
