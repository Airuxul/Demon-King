//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// 任务池。
    /// </summary>
    /// <typeparam name="T">任务类型。</typeparam>
    internal sealed class TaskPool<T> where T : TaskBase
    {
        private readonly Stack<ITaskAgent<T>> _FreeAgents;
        private readonly GameFrameworkLinkedList<ITaskAgent<T>> _WorkingAgents;
        private readonly GameFrameworkLinkedList<T> _WaitingTasks;
        private bool _Paused;

        /// <summary>
        /// 初始化任务池的新实例。
        /// </summary>
        public TaskPool()
        {
            _FreeAgents = new Stack<ITaskAgent<T>>();
            _WorkingAgents = new GameFrameworkLinkedList<ITaskAgent<T>>();
            _WaitingTasks = new GameFrameworkLinkedList<T>();
            _Paused = false;
        }

        /// <summary>
        /// 获取或设置任务池是否被暂停。
        /// </summary>
        public bool Paused
        {
            get
            {
                return _Paused;
            }
            set
            {
                _Paused = value;
            }
        }

        /// <summary>
        /// 获取任务代理总数量。
        /// </summary>
        public int TotalAgentCount
        {
            get
            {
                return FreeAgentCount + WorkingAgentCount;
            }
        }

        /// <summary>
        /// 获取可用任务代理数量。
        /// </summary>
        public int FreeAgentCount
        {
            get
            {
                return _FreeAgents.Count;
            }
        }

        /// <summary>
        /// 获取工作中任务代理数量。
        /// </summary>
        public int WorkingAgentCount
        {
            get
            {
                return _WorkingAgents.Count;
            }
        }

        /// <summary>
        /// 获取等待任务数量。
        /// </summary>
        public int WaitingTaskCount
        {
            get
            {
                return _WaitingTasks.Count;
            }
        }

        /// <summary>
        /// 任务池轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (_Paused)
            {
                return;
            }

            ProcessRunningTasks(elapseSeconds, realElapseSeconds);
            ProcessWaitingTasks(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 关闭并清理任务池。
        /// </summary>
        public void Shutdown()
        {
            RemoveAllTasks();

            while (FreeAgentCount > 0)
            {
                _FreeAgents.Pop().Shutdown();
            }
        }

        /// <summary>
        /// 增加任务代理。
        /// </summary>
        /// <param name="agent">要增加的任务代理。</param>
        public void AddAgent(ITaskAgent<T> agent)
        {
            if (agent == null)
            {
                throw new GameFrameworkException("Task agent is invalid.");
            }

            agent.Initialize();
            _FreeAgents.Push(agent);
        }

        /// <summary>
        /// 根据任务的序列编号获取任务的信息。
        /// </summary>
        /// <param name="serialId">要获取信息的任务的序列编号。</param>
        /// <returns>任务的信息。</returns>
        public TaskInfo GetTaskInfo(int serialId)
        {
            foreach (ITaskAgent<T> workingAgent in _WorkingAgents)
            {
                T workingTask = workingAgent.Task;
                if (workingTask.SerialId == serialId)
                {
                    return new TaskInfo(workingTask.SerialId, workingTask.Tag, workingTask.Priority, workingTask.UserData, workingTask.Done ? TaskStatus.Done : TaskStatus.Doing, workingTask.Description);
                }
            }

            foreach (T waitingTask in _WaitingTasks)
            {
                if (waitingTask.SerialId == serialId)
                {
                    return new TaskInfo(waitingTask.SerialId, waitingTask.Tag, waitingTask.Priority, waitingTask.UserData, TaskStatus.Todo, waitingTask.Description);
                }
            }

            return default(TaskInfo);
        }

        /// <summary>
        /// 根据任务的标签获取任务的信息。
        /// </summary>
        /// <param name="tag">要获取信息的任务的标签。</param>
        /// <returns>任务的信息。</returns>
        public TaskInfo[] GetTaskInfos(string tag)
        {
            List<TaskInfo> results = new List<TaskInfo>();
            GetTaskInfos(tag, results);
            return results.ToArray();
        }

        /// <summary>
        /// 根据任务的标签获取任务的信息。
        /// </summary>
        /// <param name="tag">要获取信息的任务的标签。</param>
        /// <param name="results">任务的信息。</param>
        public void GetTaskInfos(string tag, List<TaskInfo> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (ITaskAgent<T> workingAgent in _WorkingAgents)
            {
                T workingTask = workingAgent.Task;
                if (workingTask.Tag == tag)
                {
                    results.Add(new TaskInfo(workingTask.SerialId, workingTask.Tag, workingTask.Priority, workingTask.UserData, workingTask.Done ? TaskStatus.Done : TaskStatus.Doing, workingTask.Description));
                }
            }

            foreach (T waitingTask in _WaitingTasks)
            {
                if (waitingTask.Tag == tag)
                {
                    results.Add(new TaskInfo(waitingTask.SerialId, waitingTask.Tag, waitingTask.Priority, waitingTask.UserData, TaskStatus.Todo, waitingTask.Description));
                }
            }
        }

        /// <summary>
        /// 获取所有任务的信息。
        /// </summary>
        /// <returns>所有任务的信息。</returns>
        public TaskInfo[] GetAllTaskInfos()
        {
            int index = 0;
            TaskInfo[] results = new TaskInfo[_WorkingAgents.Count + _WaitingTasks.Count];
            foreach (ITaskAgent<T> workingAgent in _WorkingAgents)
            {
                T workingTask = workingAgent.Task;
                results[index++] = new TaskInfo(workingTask.SerialId, workingTask.Tag, workingTask.Priority, workingTask.UserData, workingTask.Done ? TaskStatus.Done : TaskStatus.Doing, workingTask.Description);
            }

            foreach (T waitingTask in _WaitingTasks)
            {
                results[index++] = new TaskInfo(waitingTask.SerialId, waitingTask.Tag, waitingTask.Priority, waitingTask.UserData, TaskStatus.Todo, waitingTask.Description);
            }

            return results;
        }

        /// <summary>
        /// 获取所有任务的信息。
        /// </summary>
        /// <param name="results">所有任务的信息。</param>
        public void GetAllTaskInfos(List<TaskInfo> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (ITaskAgent<T> workingAgent in _WorkingAgents)
            {
                T workingTask = workingAgent.Task;
                results.Add(new TaskInfo(workingTask.SerialId, workingTask.Tag, workingTask.Priority, workingTask.UserData, workingTask.Done ? TaskStatus.Done : TaskStatus.Doing, workingTask.Description));
            }

            foreach (T waitingTask in _WaitingTasks)
            {
                results.Add(new TaskInfo(waitingTask.SerialId, waitingTask.Tag, waitingTask.Priority, waitingTask.UserData, TaskStatus.Todo, waitingTask.Description));
            }
        }

        /// <summary>
        /// 增加任务。
        /// </summary>
        /// <param name="task">要增加的任务。</param>
        public void AddTask(T task)
        {
            LinkedListNode<T> current = _WaitingTasks.Last;
            while (current != null)
            {
                if (task.Priority <= current.Value.Priority)
                {
                    break;
                }

                current = current.Previous;
            }

            if (current != null)
            {
                _WaitingTasks.AddAfter(current, task);
            }
            else
            {
                _WaitingTasks.AddFirst(task);
            }
        }

        /// <summary>
        /// 根据任务的序列编号移除任务。
        /// </summary>
        /// <param name="serialId">要移除任务的序列编号。</param>
        /// <returns>是否移除任务成功。</returns>
        public bool RemoveTask(int serialId)
        {
            foreach (T task in _WaitingTasks)
            {
                if (task.SerialId == serialId)
                {
                    _WaitingTasks.Remove(task);
                    ReferencePool.Release(task);
                    return true;
                }
            }

            LinkedListNode<ITaskAgent<T>> currentWorkingAgent = _WorkingAgents.First;
            while (currentWorkingAgent != null)
            {
                LinkedListNode<ITaskAgent<T>> next = currentWorkingAgent.Next;
                ITaskAgent<T> workingAgent = currentWorkingAgent.Value;
                T task = workingAgent.Task;
                if (task.SerialId == serialId)
                {
                    workingAgent.Reset();
                    _FreeAgents.Push(workingAgent);
                    _WorkingAgents.Remove(currentWorkingAgent);
                    ReferencePool.Release(task);
                    return true;
                }

                currentWorkingAgent = next;
            }

            return false;
        }

        /// <summary>
        /// 根据任务的标签移除任务。
        /// </summary>
        /// <param name="tag">要移除任务的标签。</param>
        /// <returns>移除任务的数量。</returns>
        public int RemoveTasks(string tag)
        {
            int count = 0;

            LinkedListNode<T> currentWaitingTask = _WaitingTasks.First;
            while (currentWaitingTask != null)
            {
                LinkedListNode<T> next = currentWaitingTask.Next;
                T task = currentWaitingTask.Value;
                if (task.Tag == tag)
                {
                    _WaitingTasks.Remove(currentWaitingTask);
                    ReferencePool.Release(task);
                    count++;
                }

                currentWaitingTask = next;
            }

            LinkedListNode<ITaskAgent<T>> currentWorkingAgent = _WorkingAgents.First;
            while (currentWorkingAgent != null)
            {
                LinkedListNode<ITaskAgent<T>> next = currentWorkingAgent.Next;
                ITaskAgent<T> workingAgent = currentWorkingAgent.Value;
                T task = workingAgent.Task;
                if (task.Tag == tag)
                {
                    workingAgent.Reset();
                    _FreeAgents.Push(workingAgent);
                    _WorkingAgents.Remove(currentWorkingAgent);
                    ReferencePool.Release(task);
                    count++;
                }

                currentWorkingAgent = next;
            }

            return count;
        }

        /// <summary>
        /// 移除所有任务。
        /// </summary>
        /// <returns>移除任务的数量。</returns>
        public int RemoveAllTasks()
        {
            int count = _WaitingTasks.Count + _WorkingAgents.Count;

            foreach (T task in _WaitingTasks)
            {
                ReferencePool.Release(task);
            }

            _WaitingTasks.Clear();

            foreach (ITaskAgent<T> workingAgent in _WorkingAgents)
            {
                T task = workingAgent.Task;
                workingAgent.Reset();
                _FreeAgents.Push(workingAgent);
                ReferencePool.Release(task);
            }

            _WorkingAgents.Clear();

            return count;
        }

        private void ProcessRunningTasks(float elapseSeconds, float realElapseSeconds)
        {
            LinkedListNode<ITaskAgent<T>> current = _WorkingAgents.First;
            while (current != null)
            {
                T task = current.Value.Task;
                if (!task.Done)
                {
                    current.Value.Update(elapseSeconds, realElapseSeconds);
                    current = current.Next;
                    continue;
                }

                LinkedListNode<ITaskAgent<T>> next = current.Next;
                current.Value.Reset();
                _FreeAgents.Push(current.Value);
                _WorkingAgents.Remove(current);
                ReferencePool.Release(task);
                current = next;
            }
        }

        private void ProcessWaitingTasks(float elapseSeconds, float realElapseSeconds)
        {
            LinkedListNode<T> current = _WaitingTasks.First;
            while (current != null && FreeAgentCount > 0)
            {
                ITaskAgent<T> agent = _FreeAgents.Pop();
                LinkedListNode<ITaskAgent<T>> agentNode = _WorkingAgents.AddLast(agent);
                T task = current.Value;
                LinkedListNode<T> next = current.Next;
                StartTaskStatus status = agent.Start(task);
                if (status == StartTaskStatus.Done || status == StartTaskStatus.HasToWait || status == StartTaskStatus.UnknownError)
                {
                    agent.Reset();
                    _FreeAgents.Push(agent);
                    _WorkingAgents.Remove(agentNode);
                }

                if (status == StartTaskStatus.Done || status == StartTaskStatus.CanResume || status == StartTaskStatus.UnknownError)
                {
                    _WaitingTasks.Remove(current);
                }

                if (status == StartTaskStatus.Done || status == StartTaskStatus.UnknownError)
                {
                    ReferencePool.Release(task);
                }

                current = next;
            }
        }
    }
}
