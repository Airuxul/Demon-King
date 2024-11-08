//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace GameFramework.FileSystem
{
    internal sealed partial class FileSystem : IFileSystem
    {
        /// <summary>
        /// 头数据。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct HeaderData
        {
            private const int HeaderLength = 3;
            private const int FileSystemVersion = 0;
            private const int EncryptBytesLength = 4;
            private static readonly byte[] Header = new byte[HeaderLength] { (byte)'G', (byte)'F', (byte)'F' };

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = HeaderLength)]
            private readonly byte[] _Header;

            private readonly byte _Version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = EncryptBytesLength)]
            private readonly byte[] _EncryptBytes;

            private readonly int _MaxFileCount;
            private readonly int _MaxBlockCount;
            private readonly int _BlockCount;

            public HeaderData(int maxFileCount, int maxBlockCount)
                : this(FileSystemVersion, new byte[EncryptBytesLength], maxFileCount, maxBlockCount, 0)
            {
                Utility.Random.GetRandomBytes(_EncryptBytes);
            }

            public HeaderData(byte version, byte[] encryptBytes, int maxFileCount, int maxBlockCount, int blockCount)
            {
                _Header = Header;
                _Version = version;
                _EncryptBytes = encryptBytes;
                _MaxFileCount = maxFileCount;
                _MaxBlockCount = maxBlockCount;
                _BlockCount = blockCount;
            }

            public bool IsValid
            {
                get
                {
                    return _Header.Length == HeaderLength && _Header[0] == Header[0] && _Header[1] == Header[1] && _Header[2] == Header[2] && _Version == FileSystemVersion && _EncryptBytes.Length == EncryptBytesLength
                        && _MaxFileCount > 0 && _MaxBlockCount > 0 && _MaxFileCount <= _MaxBlockCount && _BlockCount > 0 && _BlockCount <= _MaxBlockCount;
                }
            }

            public byte Version
            {
                get
                {
                    return _Version;
                }
            }

            public int MaxFileCount
            {
                get
                {
                    return _MaxFileCount;
                }
            }

            public int MaxBlockCount
            {
                get
                {
                    return _MaxBlockCount;
                }
            }

            public int BlockCount
            {
                get
                {
                    return _BlockCount;
                }
            }

            public byte[] GetEncryptBytes()
            {
                return _EncryptBytes;
            }

            public HeaderData SetBlockCount(int blockCount)
            {
                return new HeaderData(_Version, _EncryptBytes, _MaxFileCount, _MaxBlockCount, blockCount);
            }
        }
    }
}
