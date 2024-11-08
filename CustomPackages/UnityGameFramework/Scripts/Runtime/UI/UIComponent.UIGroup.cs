//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityGameFramework.Runtime
{
    public sealed partial class UIComponent : GameFrameworkComponent
    {
        [Serializable]
        private sealed class UIGroup
        {
            [FormerlySerializedAs("_Name")] [SerializeField]
            private string name = null;

            [FormerlySerializedAs("_Depth")] [SerializeField]
            private int depth = 0;

            public string Name => name;

            public int Depth => depth;
        }
    }
}
