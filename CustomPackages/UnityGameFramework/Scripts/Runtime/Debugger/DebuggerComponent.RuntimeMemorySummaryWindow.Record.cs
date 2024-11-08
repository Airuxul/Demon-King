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
        private sealed partial class RuntimeMemorySummaryWindow : ScrollableDebuggerWindowBase
        {
            private sealed class Record
            {
                private readonly string _name;
                private int _count;
                private long _size;

                public Record(string name)
                {
                    _name = name;
                    _count = 0;
                    _size = 0L;
                }

                public string Name => _name;

                public int Count
                {
                    get => _count;
                    set => _count = value;
                }

                public long Size
                {
                    get => _size;
                    set => _size = value;
                }
            }
        }
    }
}
