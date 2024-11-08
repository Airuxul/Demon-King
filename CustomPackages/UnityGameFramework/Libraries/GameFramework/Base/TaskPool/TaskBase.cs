//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework
{
    /// <summary>
    /// 任务基类。
    /// </summary>
    internal abstract class TaskBase : IReference
    {
        /// <summary>
        /// 任务默认优先级。
        /// </summary>
        public const int DefaultPriority = 0;

        private int _SerialId;
        private string _Tag;
        private int _Priority;
        private object _UserData;

        private bool _Done;

        /// <summary>
        /// 初始化任务基类的新实例。
        /// </summary>
        public TaskBase()
        {
            _SerialId = 0;
            _Tag = null;
            _Priority = DefaultPriority;
            _Done = false;
            _UserData = null;
        }

        /// <summary>
        /// 获取任务的序列编号。
        /// </summary>
        public int SerialId
        {
            get
            {
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
                return _UserData;
            }
        }

        /// <summary>
        /// 获取或设置任务是否完成。
        /// </summary>
        public bool Done
        {
            get
            {
                return _Done;
            }
            set
            {
                _Done = value;
            }
        }

        /// <summary>
        /// 获取任务描述。
        /// </summary>
        public virtual string Description
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// 初始化任务基类。
        /// </summary>
        /// <param name="serialId">任务的序列编号。</param>
        /// <param name="tag">任务的标签。</param>
        /// <param name="priority">任务的优先级。</param>
        /// <param name="userData">任务的用户自定义数据。</param>
        internal void Initialize(int serialId, string tag, int priority, object userData)
        {
            _SerialId = serialId;
            _Tag = tag;
            _Priority = priority;
            _UserData = userData;
            _Done = false;
        }

        /// <summary>
        /// 清理任务基类。
        /// </summary>
        public virtual void Clear()
        {
            _SerialId = 0;
            _Tag = null;
            _Priority = DefaultPriority;
            _UserData = null;
            _Done = false;
        }
    }
}
