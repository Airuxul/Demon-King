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
    public sealed partial class SoundComponent : GameFrameworkComponent
    {
        [Serializable]
        private sealed class SoundGroup
        {
            [FormerlySerializedAs("_Name")] [SerializeField]
            private string name = null;

            [FormerlySerializedAs("_AvoidBeingReplacedBySamePriority")] [SerializeField]
            private bool avoidBeingReplacedBySamePriority = false;

            [FormerlySerializedAs("_Mute")] [SerializeField]
            private bool mute = false;

            [FormerlySerializedAs("_Volume")] [SerializeField, Range(0f, 1f)]
            private float volume = 1f;

            [FormerlySerializedAs("_AgentHelperCount")] [SerializeField]
            private int agentHelperCount = 1;

            public string Name => name;

            public bool AvoidBeingReplacedBySamePriority => avoidBeingReplacedBySamePriority;

            public bool Mute => mute;

            public float Volume => volume;

            public int AgentHelperCount => agentHelperCount;
        }
    }
}
