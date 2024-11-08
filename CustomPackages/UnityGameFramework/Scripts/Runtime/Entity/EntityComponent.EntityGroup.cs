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
    public sealed partial class EntityComponent : GameFrameworkComponent
    {
        [Serializable]
        private sealed class EntityGroup
        {
            [FormerlySerializedAs("_Name")] [SerializeField]
            private string name = null;

            [FormerlySerializedAs("_InstanceAutoReleaseInterval")] [SerializeField]
            private float instanceAutoReleaseInterval = 60f;

            [FormerlySerializedAs("_InstanceCapacity")] [SerializeField]
            private int instanceCapacity = 16;

            [FormerlySerializedAs("_InstanceExpireTime")] [SerializeField]
            private float instanceExpireTime = 60f;

            [FormerlySerializedAs("_InstancePriority")] [SerializeField]
            private int instancePriority = 0;

            public string Name => name;

            public float InstanceAutoReleaseInterval => instanceAutoReleaseInterval;

            public int InstanceCapacity => instanceCapacity;

            public float InstanceExpireTime => instanceExpireTime;

            public int InstancePriority => instancePriority;
        }
    }
}
