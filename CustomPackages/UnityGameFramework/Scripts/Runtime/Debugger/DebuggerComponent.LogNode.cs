//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 日志记录结点。
        /// </summary>
        public sealed class LogNode : IReference
        {
            private DateTime _logTime;
            private int _logFrameCount;
            private LogType _logType;
            private string _logMessage;
            private string _stackTrack;

            /// <summary>
            /// 初始化日志记录结点的新实例。
            /// </summary>
            public LogNode()
            {
                _logTime = default(DateTime);
                _logFrameCount = 0;
                _logType = LogType.Error;
                _logMessage = null;
                _stackTrack = null;
            }

            /// <summary>
            /// 获取日志时间。
            /// </summary>
            public DateTime LogTime => _logTime;

            /// <summary>
            /// 获取日志帧计数。
            /// </summary>
            public int LogFrameCount => _logFrameCount;

            /// <summary>
            /// 获取日志类型。
            /// </summary>
            public LogType LogType => _logType;

            /// <summary>
            /// 获取日志内容。
            /// </summary>
            public string LogMessage => _logMessage;

            /// <summary>
            /// 获取日志堆栈信息。
            /// </summary>
            public string StackTrack => _stackTrack;

            /// <summary>
            /// 创建日志记录结点。
            /// </summary>
            /// <param name="logType">日志类型。</param>
            /// <param name="logMessage">日志内容。</param>
            /// <param name="stackTrack">日志堆栈信息。</param>
            /// <returns>创建的日志记录结点。</returns>
            public static LogNode Create(LogType logType, string logMessage, string stackTrack)
            {
                LogNode logNode = ReferencePool.Acquire<LogNode>();
                logNode._logTime = DateTime.UtcNow;
                logNode._logFrameCount = Time.frameCount;
                logNode._logType = logType;
                logNode._logMessage = logMessage;
                logNode._stackTrack = stackTrack;
                return logNode;
            }

            /// <summary>
            /// 清理日志记录结点。
            /// </summary>
            public void Clear()
            {
                _logTime = default(DateTime);
                _logFrameCount = 0;
                _logType = LogType.Error;
                _logMessage = null;
                _stackTrack = null;
            }
        }
    }
}
