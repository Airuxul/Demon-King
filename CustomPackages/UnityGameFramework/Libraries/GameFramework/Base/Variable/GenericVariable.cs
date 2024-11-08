//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;

namespace GameFramework
{
    /// <summary>
    /// 变量。
    /// </summary>
    /// <typeparam name="T">变量类型。</typeparam>
    public abstract class Variable<T> : Variable
    {
        private T _Value;

        /// <summary>
        /// 初始化变量的新实例。
        /// </summary>
        public Variable()
        {
            _Value = default(T);
        }

        /// <summary>
        /// 获取变量类型。
        /// </summary>
        public override Type Type
        {
            get
            {
                return typeof(T);
            }
        }

        /// <summary>
        /// 获取或设置变量值。
        /// </summary>
        public T Value
        {
            get
            {
                return _Value;
            }
            set
            {
                _Value = value;
            }
        }

        /// <summary>
        /// 获取变量值。
        /// </summary>
        /// <returns>变量值。</returns>
        public override object GetValue()
        {
            return _Value;
        }

        /// <summary>
        /// 设置变量值。
        /// </summary>
        /// <param name="value">变量值。</param>
        public override void SetValue(object value)
        {
            _Value = (T)value;
        }

        /// <summary>
        /// 清理变量值。
        /// </summary>
        public override void Clear()
        {
            _Value = default(T);
        }

        /// <summary>
        /// 获取变量字符串。
        /// </summary>
        /// <returns>变量字符串。</returns>
        public override string ToString()
        {
            return (_Value != null) ? _Value.ToString() : "<Null>";
        }
    }
}
