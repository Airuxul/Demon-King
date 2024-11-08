//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace GameFramework
{
    /// <summary>
    /// 任务信息。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct TaskInfo
    {
        private readonly bool _IsValid;
        private readonly int _SerialId;
        private readonly string _Tag;
        private readonly int _Priority;
        private readonly object _UserData;
        private readonly TaskStatus _Status;
        private readonly string _Description;

        /// <summary>
        /// 初始化任务信息的新实例。
        /// </summary>
        /// <param name="serialId">任务的序列编号。</param>
        /// <param name="tag">任务的标签。</param>
        /// <param name="priority">任务的优先级。</param>
        /// <param name="userData">任务的用户自定义数据。</param>
        /// <param name="status">任务状态。</param>
        /// <param name="description">任务描述。</param>
        public TaskInfo(int serialId, string tag, int priority, object userData, TaskStatus status, string description)
        {
            _IsValid = true;
            _SerialId = serialId;
            _Tag = tag;
            _Priority = priority;
            _UserData = userData;
            _Status = status;
            _Description = description;
        }

        /// <summary>
        /// 获取任务信息是否有效。
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _IsValid;
            }
        }

        /// <summary>
        /// 获取任务的序列编号。
        /// </summary>
        public int SerialId
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _SerialId;
            }
        }

        /// <summary>
        /// 获取任务的标签。
        /// </summary>
        public string Tag
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _Tag;
            }
        }

        /// <summary>
        /// 获取任务的优先级。
        /// </summary>
        public int Priority
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _Priority;
            }
        }

        /// <summary>
        /// 获取任务的用户自定义数据。
        /// </summary>
        public object UserData
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _UserData;
            }
        }

        /// <summary>
        /// 获取任务状态。
        /// </summary>
        public TaskStatus Status
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _Status;
            }
        }

        /// <summary>
        /// 获取任务描述。
        /// </summary>
        public string Description
        {
            get
            {
                if (!_IsValid)
                {
                    throw new GameFrameworkException("Data is invalid.");
                }

                return _Description;
            }
        }
    }
}
