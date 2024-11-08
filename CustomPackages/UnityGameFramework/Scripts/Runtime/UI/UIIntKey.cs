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
    /// 界面整型主键。
    /// </summary>
    public sealed class UIIntKey : MonoBehaviour
    {
        [FormerlySerializedAs("_Key")] [SerializeField]
        private int key = 0;

        /// <summary>
        /// 获取或设置主键。
        /// </summary>
        public int Key
        {
            get => key;
            set => key = value;
        }
    }
}
