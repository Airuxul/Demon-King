//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.FileSystem;
using System;
using System.IO;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 安卓文件系统流。
    /// </summary>
    public sealed class AndroidFileSystemStream : FileSystemStream
    {
        private static readonly string SplitFlag = "!/assets/";
        private static readonly int SplitFlagLength = SplitFlag.Length;
        private static readonly AndroidJavaObject SAssetManager = null;
        private static readonly IntPtr SInternalReadMethodId = IntPtr.Zero;
        private static readonly jvalue[] SInternalReadArgs = null;

        private readonly AndroidJavaObject _fileStream;
        private readonly IntPtr _fileStreamRawObject;

        static AndroidFileSystemStream()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            if (unityPlayer == null)
            {
                throw new GameFrameworkException("Unity player is invalid.");
            }

            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            if (currentActivity == null)
            {
                throw new GameFrameworkException("Current activity is invalid.");
            }

            AndroidJavaObject assetManager = currentActivity.Call<AndroidJavaObject>("getAssets");
            if (assetManager == null)
            {
                throw new GameFrameworkException("Asset manager is invalid.");
            }

            SAssetManager = assetManager;

            IntPtr inputStreamClassPtr = AndroidJNI.FindClass("java/io/InputStream");
            SInternalReadMethodId = AndroidJNIHelper.GetMethodID(inputStreamClassPtr, "read", "([BII)I");
            SInternalReadArgs = new jvalue[3];

            AndroidJNI.DeleteLocalRef(inputStreamClassPtr);
            currentActivity.Dispose();
            unityPlayer.Dispose();
        }

        /// <summary>
        /// 初始化安卓文件系统流的新实例。
        /// </summary>
        /// <param name="fullPath">要加载的文件系统的完整路径。</param>
        /// <param name="access">要加载的文件系统的访问方式。</param>
        /// <param name="createNew">是否创建新的文件系统流。</param>
        public AndroidFileSystemStream(string fullPath, FileSystemAccess access, bool createNew)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new GameFrameworkException("Full path is invalid.");
            }

            if (access != FileSystemAccess.Read)
            {
                throw new GameFrameworkException(Utility.Text.Format("'{0}' is not supported in AndroidFileSystemStream.", access));
            }

            if (createNew)
            {
                throw new GameFrameworkException("Create new is not supported in AndroidFileSystemStream.");
            }

            int position = fullPath.LastIndexOf(SplitFlag, StringComparison.Ordinal);
            if (position < 0)
            {
                throw new GameFrameworkException("Can not find split flag in full path.");
            }

            string fileName = fullPath.Substring(position + SplitFlagLength);
            _fileStream = InternalOpen(fileName);
            if (_fileStream == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Open file '{0}' from Android asset manager failure.", fullPath));
            }

            _fileStreamRawObject = _fileStream.GetRawObject();
        }

        /// <summary>
        /// 获取或设置文件系统流位置。
        /// </summary>
        protected override long Position
        {
            get => throw new GameFrameworkException("Get position is not supported in AndroidFileSystemStream.");
            set => Seek(value, SeekOrigin.Begin);
        }

        /// <summary>
        /// 获取文件系统流长度。
        /// </summary>
        protected override long Length => InternalAvailable();

        /// <summary>
        /// 设置文件系统流长度。
        /// </summary>
        /// <param name="length">要设置的文件系统流的长度。</param>
        protected override void SetLength(long length)
        {
            throw new GameFrameworkException("SetLength is not supported in AndroidFileSystemStream.");
        }

        /// <summary>
        /// 定位文件系统流位置。
        /// </summary>
        /// <param name="offset">要定位的文件系统流位置的偏移。</param>
        /// <param name="origin">要定位的文件系统流位置的方式。</param>
        protected override void Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.End)
            {
                Seek(Length + offset, SeekOrigin.Begin);
                return;
            }

            if (origin == SeekOrigin.Begin)
            {
                InternalReset();
            }

            while (offset > 0)
            {
                long skip = InternalSkip(offset);
                if (skip < 0)
                {
                    return;
                }

                offset -= skip;
            }
        }

        /// <summary>
        /// 从文件系统流中读取一个字节。
        /// </summary>
        /// <returns>读取的字节，若已经到达文件结尾，则返回 -1。</returns>
        protected override int ReadByte()
        {
            return InternalRead();
        }

        /// <summary>
        /// 从文件系统流中读取二进制流。
        /// </summary>
        /// <param name="buffer">存储读取文件内容的二进制流。</param>
        /// <param name="startIndex">存储读取文件内容的二进制流的起始位置。</param>
        /// <param name="length">存储读取文件内容的二进制流的长度。</param>
        /// <returns>实际读取了多少字节。</returns>
        protected override int Read(byte[] buffer, int startIndex, int length)
        {
            byte[] result = null;
            int bytesRead = InternalRead(length, out result);
            Array.Copy(result, 0, buffer, startIndex, bytesRead);
            return bytesRead;
        }

        /// <summary>
        /// 向文件系统流中写入一个字节。
        /// </summary>
        /// <param name="value">要写入的字节。</param>
        protected override void WriteByte(byte value)
        {
            throw new GameFrameworkException("WriteByte is not supported in AndroidFileSystemStream.");
        }

        /// <summary>
        /// 向文件系统流中写入二进制流。
        /// </summary>
        /// <param name="buffer">存储写入文件内容的二进制流。</param>
        /// <param name="startIndex">存储写入文件内容的二进制流的起始位置。</param>
        /// <param name="length">存储写入文件内容的二进制流的长度。</param>
        protected override void Write(byte[] buffer, int startIndex, int length)
        {
            throw new GameFrameworkException("Write is not supported in AndroidFileSystemStream.");
        }

        /// <summary>
        /// 将文件系统流立刻更新到存储介质中。
        /// </summary>
        protected override void Flush()
        {
            throw new GameFrameworkException("Flush is not supported in AndroidFileSystemStream.");
        }

        /// <summary>
        /// 关闭文件系统流。
        /// </summary>
        protected override void Close()
        {
            InternalClose();
            _fileStream.Dispose();
        }

        private AndroidJavaObject InternalOpen(string fileName)
        {
            return SAssetManager.Call<AndroidJavaObject>("open", fileName);
        }

        private int InternalAvailable()
        {
            return _fileStream.Call<int>("available");
        }

        private void InternalClose()
        {
            _fileStream.Call("close");
        }

        private int InternalRead()
        {
            return _fileStream.Call<int>("read");
        }

        private int InternalRead(int length, out byte[] result)
        {
#if UNITY_2019_2_OR_NEWER
#pragma warning disable CS0618
#endif
            IntPtr resultPtr = AndroidJNI.NewByteArray(length);
#if UNITY_2019_2_OR_NEWER
#pragma warning restore CS0618
#endif
            int offset = 0;
            int bytesLeft = length;
            while (bytesLeft > 0)
            {
                SInternalReadArgs[0] = new jvalue() { l = resultPtr };
                SInternalReadArgs[1] = new jvalue() { i = offset };
                SInternalReadArgs[2] = new jvalue() { i = bytesLeft };
                int bytesRead = AndroidJNI.CallIntMethod(_fileStreamRawObject, SInternalReadMethodId, SInternalReadArgs);
                if (bytesRead <= 0)
                {
                    break;
                }

                offset += bytesRead;
                bytesLeft -= bytesRead;
            }

#if UNITY_2019_2_OR_NEWER
#pragma warning disable CS0618
#endif
            result = AndroidJNI.FromByteArray(resultPtr);
#if UNITY_2019_2_OR_NEWER
#pragma warning restore CS0618
#endif
            AndroidJNI.DeleteLocalRef(resultPtr);
            return offset;
        }

        private void InternalReset()
        {
            _fileStream.Call("reset");
        }

        private long InternalSkip(long offset)
        {
            return _fileStream.Call<long>("skip", offset);
        }
    }
}
