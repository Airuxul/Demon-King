//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceBuilderController
    {
        private sealed class ResourceCode
        {
            private readonly Platform _Platform;
            private readonly int _Length;
            private readonly int _HashCode;
            private readonly int _CompressedLength;
            private readonly int _CompressedHashCode;

            public ResourceCode(Platform platform, int length, int hashCode, int compressedLength, int compressedHashCode)
            {
                _Platform = platform;
                _Length = length;
                _HashCode = hashCode;
                _CompressedLength = compressedLength;
                _CompressedHashCode = compressedHashCode;
            }

            public Platform Platform
            {
                get
                {
                    return _Platform;
                }
            }

            public int Length
            {
                get
                {
                    return _Length;
                }
            }

            public int HashCode
            {
                get
                {
                    return _HashCode;
                }
            }

            public int CompressedLength
            {
                get
                {
                    return _CompressedLength;
                }
            }

            public int CompressedHashCode
            {
                get
                {
                    return _CompressedHashCode;
                }
            }
        }
    }
}
