//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;
using UnityEngine.Serialization;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 界面字符型主键。
    /// </summary>
    public sealed class UIStringKey : MonoBehaviour
    {
        [FormerlySerializedAs("_Key")] [SerializeField]
        private string key = null;

        /// <summary>
        /// 获取或设置主键。
        /// </summary>
        public string Key
        {
            get => key ?? string.Empty;
            set => key = value;
        }
    }
}
