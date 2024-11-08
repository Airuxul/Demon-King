//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace GameFramework.FileSystem
{
    internal sealed partial class FileSystem : IFileSystem
    {
        /// <summary>
        /// 字符串数据。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct StringData
        {
            private static readonly byte[] s_CachedBytes = new byte[byte.MaxValue + 1];

            private readonly byte _Length;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = byte.MaxValue)]
            private readonly byte[] _Bytes;

            public StringData(byte length, byte[] bytes)
            {
                _Length = length;
                _Bytes = bytes;
            }

            public string GetString(byte[] encryptBytes)
            {
                if (_Length <= 0)
                {
                    return null;
                }

                Array.Copy(_Bytes, 0, s_CachedBytes, 0, _Length);
                Utility.Encryption.GetSelfXorBytes(s_CachedBytes, 0, _Length, encryptBytes);
                return Utility.Converter.GetString(s_CachedBytes, 0, _Length);
            }

            public StringData SetString(string value, byte[] encryptBytes)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return Clear();
                }

                int length = Utility.Converter.GetBytes(value, s_CachedBytes);
                if (length > byte.MaxValue)
                {
                    throw new GameFrameworkException(Utility.Text.Format("String '{0}' is too long.", value));
                }

                Utility.Encryption.GetSelfXorBytes(s_CachedBytes, encryptBytes);
                Array.Copy(s_CachedBytes, 0, _Bytes, 0, length);
                return new StringData((byte)length, _Bytes);
            }

            public StringData Clear()
            {
                return new StringData(0, _Bytes);
            }
        }
    }
}
