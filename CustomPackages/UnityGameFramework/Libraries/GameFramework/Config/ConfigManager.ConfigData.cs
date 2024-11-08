//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace GameFramework.Config
{
    internal sealed partial class ConfigManager : GameFrameworkModule, IConfigManager
    {
        [StructLayout(LayoutKind.Auto)]
        private struct ConfigData
        {
            private readonly bool _BoolValue;
            private readonly int _IntValue;
            private readonly float _FloatValue;
            private readonly string _StringValue;

            public ConfigData(bool boolValue, int intValue, float floatValue, string stringValue)
            {
                _BoolValue = boolValue;
                _IntValue = intValue;
                _FloatValue = floatValue;
                _StringValue = stringValue;
            }

            public bool BoolValue
            {
                get
                {
                    return _BoolValue;
                }
            }

            public int IntValue
            {
                get
                {
                    return _IntValue;
                }
            }

            public float FloatValue
            {
                get
                {
                    return _FloatValue;
                }
            }

            public string StringValue
            {
                get
                {
                    return _StringValue;
                }
            }
        }
    }
}
