//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace UnityGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        private sealed partial class RuntimeMemoryInformationWindow<T> : ScrollableDebuggerWindowBase where T : UnityEngine.Object
        {
            private sealed class Sample
            {
                private readonly string _name;
                private readonly string _type;
                private readonly long _size;
                private bool _highlight;

                public Sample(string name, string type, long size)
                {
                    _name = name;
                    _type = type;
                    _size = size;
                    _highlight = false;
                }

                public string Name => _name;

                public string Type => _type;

                public long Size => _size;

                public bool Highlight
                {
                    get => _highlight;
                    set => _highlight = value;
                }
            }
        }
    }
}
