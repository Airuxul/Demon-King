//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.DataNode;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 数据结点组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Data Node")]
    public sealed class DataNodeComponent : GameFrameworkComponent
    {
        private IDataNodeManager _dataNodeManager = null;

        /// <summary>
        /// 获取根数据结点。
        /// </summary>
        public IDataNode Root => _dataNodeManager.Root;

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _dataNodeManager = GameFrameworkEntry.GetModule<IDataNodeManager>();
            if (_dataNodeManager == null)
            {
                Log.Fatal("Data node manager is invalid.");
                return;
            }
        }

        private void Start()
        {
        }

        /// <summary>
        /// 根据类型获取数据结点的数据。
        /// </summary>
        /// <typeparam name="T">要获取的数据类型。</typeparam>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <returns>指定类型的数据。</returns>
        public T GetData<T>(string path) where T : Variable
        {
            return _dataNodeManager.GetData<T>(path);
        }

        /// <summary>
        /// 获取数据结点的数据。
        /// </summary>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <returns>数据结点的数据。</returns>
        public Variable GetData(string path)
        {
            return _dataNodeManager.GetData(path);
        }

        /// <summary>
        /// 根据类型获取数据结点的数据。
        /// </summary>
        /// <typeparam name="T">要获取的数据类型。</typeparam>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <param name="node">查找起始结点。</param>
        /// <returns>指定类型的数据。</returns>
        public T GetData<T>(string path, IDataNode node) where T : Variable
        {
            return _dataNodeManager.GetData<T>(path, node);
        }

        /// <summary>
        /// 获取数据结点的数据。
        /// </summary>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <param name="node">查找起始结点。</param>
        /// <returns>数据结点的数据。</returns>
        public Variable GetData(string path, IDataNode node)
        {
            return _dataNodeManager.GetData(path, node);
        }

        /// <summary>
        /// 设置数据结点的数据。
        /// </summary>
        /// <typeparam name="T">要设置的数据类型。</typeparam>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <param name="data">要设置的数据。</param>
        public void SetData<T>(string path, T data) where T : Variable
        {
            _dataNodeManager.SetData(path, data);
        }

        /// <summary>
        /// 设置数据结点的数据。
        /// </summary>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <param name="data">要设置的数据。</param>
        public void SetData(string path, Variable data)
        {
            _dataNodeManager.SetData(path, data);
        }

        /// <summary>
        /// 设置数据结点的数据。
        /// </summary>
        /// <typeparam name="T">要设置的数据类型。</typeparam>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <param name="data">要设置的数据。</param>
        /// <param name="node">查找起始结点。</param>
        public void SetData<T>(string path, T data, IDataNode node) where T : Variable
        {
            _dataNodeManager.SetData(path, data, node);
        }

        /// <summary>
        /// 设置数据结点的数据。
        /// </summary>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <param name="data">要设置的数据。</param>
        /// <param name="node">查找起始结点。</param>
        public void SetData(string path, Variable data, IDataNode node)
        {
            _dataNodeManager.SetData(path, data, node);
        }

        /// <summary>
        /// 获取数据结点。
        /// </summary>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <returns>指定位置的数据结点，如果没有找到，则返回空。</returns>
        public IDataNode GetNode(string path)
        {
            return _dataNodeManager.GetNode(path);
        }

        /// <summary>
        /// 获取数据结点。
        /// </summary>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <param name="node">查找起始结点。</param>
        /// <returns>指定位置的数据结点，如果没有找到，则返回空。</returns>
        public IDataNode GetNode(string path, IDataNode node)
        {
            return _dataNodeManager.GetNode(path, node);
        }

        /// <summary>
        /// 获取或增加数据结点。
        /// </summary>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <returns>指定位置的数据结点，如果没有找到，则增加相应的数据结点。</returns>
        public IDataNode GetOrAddNode(string path)
        {
            return _dataNodeManager.GetOrAddNode(path);
        }

        /// <summary>
        /// 获取或增加数据结点。
        /// </summary>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <param name="node">查找起始结点。</param>
        /// <returns>指定位置的数据结点，如果没有找到，则增加相应的数据结点。</returns>
        public IDataNode GetOrAddNode(string path, IDataNode node)
        {
            return _dataNodeManager.GetOrAddNode(path, node);
        }

        /// <summary>
        /// 移除数据结点。
        /// </summary>
        /// <param name="path">相对于 node 的查找路径。</param>
        public void RemoveNode(string path)
        {
            _dataNodeManager.RemoveNode(path);
        }

        /// <summary>
        /// 移除数据结点。
        /// </summary>
        /// <param name="path">相对于 node 的查找路径。</param>
        /// <param name="node">查找起始结点。</param>
        public void RemoveNode(string path, IDataNode node)
        {
            _dataNodeManager.RemoveNode(path, node);
        }

        /// <summary>
        /// 移除所有数据结点。
        /// </summary>
        public void Clear()
        {
            _dataNodeManager.Clear();
        }
    }
}
