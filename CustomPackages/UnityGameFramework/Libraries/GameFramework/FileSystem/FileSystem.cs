//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace GameFramework.FileSystem
{
    /// <summary>
    /// 文件系统。
    /// </summary>
    internal sealed partial class FileSystem : IFileSystem
    {
        private const int ClusterSize = 1024 * 4;
        private const int CachedBytesLength = 0x1000;

        private static readonly string[] EmptyStringArray = new string[] { };
        private static readonly byte[] s_CachedBytes = new byte[CachedBytesLength];

        private static readonly int HeaderDataSize = Marshal.SizeOf(typeof(HeaderData));
        private static readonly int BlockDataSize = Marshal.SizeOf(typeof(BlockData));
        private static readonly int StringDataSize = Marshal.SizeOf(typeof(StringData));

        private readonly string _FullPath;
        private readonly FileSystemAccess _Access;
        private readonly FileSystemStream _Stream;
        private readonly Dictionary<string, int> _FileDatas;
        private readonly List<BlockData> _BlockDatas;
        private readonly GameFrameworkMultiDictionary<int, int> _FreeBlockIndexes;
        private readonly SortedDictionary<int, StringData> _StringDatas;
        private readonly Queue<int> _FreeStringIndexes;
        private readonly Queue<StringData> _FreeStringDatas;

        private HeaderData _HeaderData;
        private int _BlockDataOffset;
        private int _StringDataOffset;
        private int _FileDataOffset;

        /// <summary>
        /// 初始化文件系统的新实例。
        /// </summary>
        /// <param name="fullPath">文件系统完整路径。</param>
        /// <param name="access">文件系统访问方式。</param>
        /// <param name="stream">文件系统流。</param>
        private FileSystem(string fullPath, FileSystemAccess access, FileSystemStream stream)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new GameFrameworkException("Full path is invalid.");
            }

            if (access == FileSystemAccess.Unspecified)
            {
                throw new GameFrameworkException("Access is invalid.");
            }

            if (stream == null)
            {
                throw new GameFrameworkException("Stream is invalid.");
            }

            _FullPath = fullPath;
            _Access = access;
            _Stream = stream;
            _FileDatas = new Dictionary<string, int>(StringComparer.Ordinal);
            _BlockDatas = new List<BlockData>();
            _FreeBlockIndexes = new GameFrameworkMultiDictionary<int, int>();
            _StringDatas = new SortedDictionary<int, StringData>();
            _FreeStringIndexes = new Queue<int>();
            _FreeStringDatas = new Queue<StringData>();

            _HeaderData = default(HeaderData);
            _BlockDataOffset = 0;
            _StringDataOffset = 0;
            _FileDataOffset = 0;

            Utility.Marshal.EnsureCachedHGlobalSize(CachedBytesLength);
        }

        /// <summary>
        /// 获取文件系统完整路径。
        /// </summary>
        public string FullPath
        {
            get
            {
                return _FullPath;
            }
        }

        /// <summary>
        /// 获取文件系统访问方式。
        /// </summary>
        public FileSystemAccess Access
        {
            get
            {
                return _Access;
            }
        }

        /// <summary>
        /// 获取文件数量。
        /// </summary>
        public int FileCount
        {
            get
            {
                return _FileDatas.Count;
            }
        }

        /// <summary>
        /// 获取最大文件数量。
        /// </summary>
        public int MaxFileCount
        {
            get
            {
                return _HeaderData.MaxFileCount;
            }
        }

        /// <summary>
        /// 创建文件系统。
        /// </summary>
        /// <param name="fullPath">要创建的文件系统的完整路径。</param>
        /// <param name="access">要创建的文件系统的访问方式。</param>
        /// <param name="stream">要创建的文件系统的文件系统流。</param>
        /// <param name="maxFileCount">要创建的文件系统的最大文件数量。</param>
        /// <param name="maxBlockCount">要创建的文件系统的最大块数据数量。</param>
        /// <returns>创建的文件系统。</returns>
        public static FileSystem Create(string fullPath, FileSystemAccess access, FileSystemStream stream, int maxFileCount, int maxBlockCount)
        {
            if (maxFileCount <= 0)
            {
                throw new GameFrameworkException("Max file count is invalid.");
            }

            if (maxBlockCount <= 0)
            {
                throw new GameFrameworkException("Max block count is invalid.");
            }

            if (maxFileCount > maxBlockCount)
            {
                throw new GameFrameworkException("Max file count can not larger than max block count.");
            }

            FileSystem fileSystem = new FileSystem(fullPath, access, stream);
            fileSystem._HeaderData = new HeaderData(maxFileCount, maxBlockCount);
            CalcOffsets(fileSystem);
            Utility.Marshal.StructureToBytes(fileSystem._HeaderData, HeaderDataSize, s_CachedBytes);

            try
            {
                stream.Write(s_CachedBytes, 0, HeaderDataSize);
                stream.SetLength(fileSystem._FileDataOffset);
                return fileSystem;
            }
            catch
            {
                fileSystem.Shutdown();
                return null;
            }
        }

        /// <summary>
        /// 加载文件系统。
        /// </summary>
        /// <param name="fullPath">要加载的文件系统的完整路径。</param>
        /// <param name="access">要加载的文件系统的访问方式。</param>
        /// <param name="stream">要加载的文件系统的文件系统流。</param>
        /// <returns>加载的文件系统。</returns>
        public static FileSystem Load(string fullPath, FileSystemAccess access, FileSystemStream stream)
        {
            FileSystem fileSystem = new FileSystem(fullPath, access, stream);

            stream.Read(s_CachedBytes, 0, HeaderDataSize);
            fileSystem._HeaderData = Utility.Marshal.BytesToStructure<HeaderData>(HeaderDataSize, s_CachedBytes);
            if (!fileSystem._HeaderData.IsValid)
            {
                return null;
            }

            CalcOffsets(fileSystem);

            if (fileSystem._BlockDatas.Capacity < fileSystem._HeaderData.BlockCount)
            {
                fileSystem._BlockDatas.Capacity = fileSystem._HeaderData.BlockCount;
            }

            for (int i = 0; i < fileSystem._HeaderData.BlockCount; i++)
            {
                stream.Read(s_CachedBytes, 0, BlockDataSize);
                BlockData blockData = Utility.Marshal.BytesToStructure<BlockData>(BlockDataSize, s_CachedBytes);
                fileSystem._BlockDatas.Add(blockData);
            }

            for (int i = 0; i < fileSystem._BlockDatas.Count; i++)
            {
                BlockData blockData = fileSystem._BlockDatas[i];
                if (blockData.Using)
                {
                    StringData stringData = fileSystem.ReadStringData(blockData.StringIndex);
                    fileSystem._StringDatas.Add(blockData.StringIndex, stringData);
                    fileSystem._FileDatas.Add(stringData.GetString(fileSystem._HeaderData.GetEncryptBytes()), i);
                }
                else
                {
                    fileSystem._FreeBlockIndexes.Add(blockData.Length, i);
                }
            }

            int index = 0;
            foreach (KeyValuePair<int, StringData> i in fileSystem._StringDatas)
            {
                while (index < i.Key)
                {
                    fileSystem._FreeStringIndexes.Enqueue(index++);
                }

                index++;
            }

            return fileSystem;
        }

        /// <summary>
        /// 关闭并清理文件系统。
        /// </summary>
        public void Shutdown()
        {
            _Stream.Close();

            _FileDatas.Clear();
            _BlockDatas.Clear();
            _FreeBlockIndexes.Clear();
            _StringDatas.Clear();
            _FreeStringIndexes.Clear();
            _FreeStringDatas.Clear();

            _BlockDataOffset = 0;
            _StringDataOffset = 0;
            _FileDataOffset = 0;
        }

        /// <summary>
        /// 获取文件信息。
        /// </summary>
        /// <param name="name">要获取文件信息的文件名称。</param>
        /// <returns>获取的文件信息。</returns>
        public FileInfo GetFileInfo(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            int blockIndex = 0;
            if (!_FileDatas.TryGetValue(name, out blockIndex))
            {
                return default(FileInfo);
            }

            BlockData blockData = _BlockDatas[blockIndex];
            return new FileInfo(name, GetClusterOffset(blockData.ClusterIndex), blockData.Length);
        }

        /// <summary>
        /// 获取所有文件信息。
        /// </summary>
        /// <returns>获取的所有文件信息。</returns>
        public FileInfo[] GetAllFileInfos()
        {
            int index = 0;
            FileInfo[] results = new FileInfo[_FileDatas.Count];
            foreach (KeyValuePair<string, int> fileData in _FileDatas)
            {
                BlockData blockData = _BlockDatas[fileData.Value];
                results[index++] = new FileInfo(fileData.Key, GetClusterOffset(blockData.ClusterIndex), blockData.Length);
            }

            return results;
        }

        /// <summary>
        /// 获取所有文件信息。
        /// </summary>
        /// <param name="results">获取的所有文件信息。</param>
        public void GetAllFileInfos(List<FileInfo> results)
        {
            if (results == null)
            {
                throw new GameFrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<string, int> fileData in _FileDatas)
            {
                BlockData blockData = _BlockDatas[fileData.Value];
                results.Add(new FileInfo(fileData.Key, GetClusterOffset(blockData.ClusterIndex), blockData.Length));
            }
        }

        /// <summary>
        /// 检查是否存在指定文件。
        /// </summary>
        /// <param name="name">要检查的文件名称。</param>
        /// <returns>是否存在指定文件。</returns>
        public bool HasFile(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            return _FileDatas.ContainsKey(name);
        }

        /// <summary>
        /// 读取指定文件。
        /// </summary>
        /// <param name="name">要读取的文件名称。</param>
        /// <returns>存储读取文件内容的二进制流。</returns>
        public byte[] ReadFile(string name)
        {
            if (_Access != FileSystemAccess.Read && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not readable.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            FileInfo fileInfo = GetFileInfo(name);
            if (!fileInfo.IsValid)
            {
                return null;
            }

            int length = fileInfo.Length;
            byte[] buffer = new byte[length];
            if (length > 0)
            {
                _Stream.Position = fileInfo.Offset;
                _Stream.Read(buffer, 0, length);
            }

            return buffer;
        }

        /// <summary>
        /// 读取指定文件。
        /// </summary>
        /// <param name="name">要读取的文件名称。</param>
        /// <param name="buffer">存储读取文件内容的二进制流。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFile(string name, byte[] buffer)
        {
            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return ReadFile(name, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 读取指定文件。
        /// </summary>
        /// <param name="name">要读取的文件名称。</param>
        /// <param name="buffer">存储读取文件内容的二进制流。</param>
        /// <param name="startIndex">存储读取文件内容的二进制流的起始位置。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFile(string name, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return ReadFile(name, buffer, startIndex, buffer.Length - startIndex);
        }

        /// <summary>
        /// 读取指定文件。
        /// </summary>
        /// <param name="name">要读取的文件名称。</param>
        /// <param name="buffer">存储读取文件内容的二进制流。</param>
        /// <param name="startIndex">存储读取文件内容的二进制流的起始位置。</param>
        /// <param name="length">存储读取文件内容的二进制流的长度。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFile(string name, byte[] buffer, int startIndex, int length)
        {
            if (_Access != FileSystemAccess.Read && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not readable.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            if (startIndex < 0 || length < 0 || startIndex + length > buffer.Length)
            {
                throw new GameFrameworkException("Start index or length is invalid.");
            }

            FileInfo fileInfo = GetFileInfo(name);
            if (!fileInfo.IsValid)
            {
                return 0;
            }

            _Stream.Position = fileInfo.Offset;
            if (length > fileInfo.Length)
            {
                length = fileInfo.Length;
            }

            if (length > 0)
            {
                return _Stream.Read(buffer, startIndex, length);
            }

            return 0;
        }

        /// <summary>
        /// 读取指定文件。
        /// </summary>
        /// <param name="name">要读取的文件名称。</param>
        /// <param name="stream">存储读取文件内容的二进制流。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFile(string name, Stream stream)
        {
            if (_Access != FileSystemAccess.Read && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not readable.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            if (stream == null)
            {
                throw new GameFrameworkException("Stream is invalid.");
            }

            if (!stream.CanWrite)
            {
                throw new GameFrameworkException("Stream is not writable.");
            }

            FileInfo fileInfo = GetFileInfo(name);
            if (!fileInfo.IsValid)
            {
                return 0;
            }

            int length = fileInfo.Length;
            if (length > 0)
            {
                _Stream.Position = fileInfo.Offset;
                return _Stream.Read(stream, length);
            }

            return 0;
        }

        /// <summary>
        /// 读取指定文件的指定片段。
        /// </summary>
        /// <param name="name">要读取片段的文件名称。</param>
        /// <param name="length">要读取片段的长度。</param>
        /// <returns>存储读取文件片段内容的二进制流。</returns>
        public byte[] ReadFileSegment(string name, int length)
        {
            return ReadFileSegment(name, 0, length);
        }

        /// <summary>
        /// 读取指定文件的指定片段。
        /// </summary>
        /// <param name="name">要读取片段的文件名称。</param>
        /// <param name="offset">要读取片段的偏移。</param>
        /// <param name="length">要读取片段的长度。</param>
        /// <returns>存储读取文件片段内容的二进制流。</returns>
        public byte[] ReadFileSegment(string name, int offset, int length)
        {
            if (_Access != FileSystemAccess.Read && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not readable.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            if (offset < 0)
            {
                throw new GameFrameworkException("Index is invalid.");
            }

            if (length < 0)
            {
                throw new GameFrameworkException("Length is invalid.");
            }

            FileInfo fileInfo = GetFileInfo(name);
            if (!fileInfo.IsValid)
            {
                return null;
            }

            if (offset > fileInfo.Length)
            {
                offset = fileInfo.Length;
            }

            int leftLength = fileInfo.Length - offset;
            if (length > leftLength)
            {
                length = leftLength;
            }

            byte[] buffer = new byte[length];
            if (length > 0)
            {
                _Stream.Position = fileInfo.Offset + offset;
                _Stream.Read(buffer, 0, length);
            }

            return buffer;
        }

        /// <summary>
        /// 读取指定文件的指定片段。
        /// </summary>
        /// <param name="name">要读取片段的文件名称。</param>
        /// <param name="buffer">存储读取文件片段内容的二进制流。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFileSegment(string name, byte[] buffer)
        {
            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return ReadFileSegment(name, 0, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 读取指定文件的指定片段。
        /// </summary>
        /// <param name="name">要读取片段的文件名称。</param>
        /// <param name="buffer">存储读取文件片段内容的二进制流。</param>
        /// <param name="length">要读取片段的长度。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFileSegment(string name, byte[] buffer, int length)
        {
            return ReadFileSegment(name, 0, buffer, 0, length);
        }

        /// <summary>
        /// 读取指定文件的指定片段。
        /// </summary>
        /// <param name="name">要读取片段的文件名称。</param>
        /// <param name="buffer">存储读取文件片段内容的二进制流。</param>
        /// <param name="startIndex">存储读取文件片段内容的二进制流的起始位置。</param>
        /// <param name="length">要读取片段的长度。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFileSegment(string name, byte[] buffer, int startIndex, int length)
        {
            return ReadFileSegment(name, 0, buffer, startIndex, length);
        }

        /// <summary>
        /// 读取指定文件的指定片段。
        /// </summary>
        /// <param name="name">要读取片段的文件名称。</param>
        /// <param name="offset">要读取片段的偏移。</param>
        /// <param name="buffer">存储读取文件片段内容的二进制流。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFileSegment(string name, int offset, byte[] buffer)
        {
            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return ReadFileSegment(name, offset, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 读取指定文件的指定片段。
        /// </summary>
        /// <param name="name">要读取片段的文件名称。</param>
        /// <param name="offset">要读取片段的偏移。</param>
        /// <param name="buffer">存储读取文件片段内容的二进制流。</param>
        /// <param name="length">要读取片段的长度。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFileSegment(string name, int offset, byte[] buffer, int length)
        {
            return ReadFileSegment(name, offset, buffer, 0, length);
        }

        /// <summary>
        /// 读取指定文件的指定片段。
        /// </summary>
        /// <param name="name">要读取片段的文件名称。</param>
        /// <param name="offset">要读取片段的偏移。</param>
        /// <param name="buffer">存储读取文件片段内容的二进制流。</param>
        /// <param name="startIndex">存储读取文件片段内容的二进制流的起始位置。</param>
        /// <param name="length">要读取片段的长度。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFileSegment(string name, int offset, byte[] buffer, int startIndex, int length)
        {
            if (_Access != FileSystemAccess.Read && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not readable.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            if (offset < 0)
            {
                throw new GameFrameworkException("Index is invalid.");
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            if (startIndex < 0 || length < 0 || startIndex + length > buffer.Length)
            {
                throw new GameFrameworkException("Start index or length is invalid.");
            }

            FileInfo fileInfo = GetFileInfo(name);
            if (!fileInfo.IsValid)
            {
                return 0;
            }

            if (offset > fileInfo.Length)
            {
                offset = fileInfo.Length;
            }

            int leftLength = fileInfo.Length - offset;
            if (length > leftLength)
            {
                length = leftLength;
            }

            if (length > 0)
            {
                _Stream.Position = fileInfo.Offset + offset;
                return _Stream.Read(buffer, startIndex, length);
            }

            return 0;
        }

        /// <summary>
        /// 读取指定文件的指定片段。
        /// </summary>
        /// <param name="name">要读取片段的文件名称。</param>
        /// <param name="stream">存储读取文件片段内容的二进制流。</param>
        /// <param name="length">要读取片段的长度。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFileSegment(string name, Stream stream, int length)
        {
            return ReadFileSegment(name, 0, stream, length);
        }

        /// <summary>
        /// 读取指定文件的指定片段。
        /// </summary>
        /// <param name="name">要读取片段的文件名称。</param>
        /// <param name="offset">要读取片段的偏移。</param>
        /// <param name="stream">存储读取文件片段内容的二进制流。</param>
        /// <param name="length">要读取片段的长度。</param>
        /// <returns>实际读取了多少字节。</returns>
        public int ReadFileSegment(string name, int offset, Stream stream, int length)
        {
            if (_Access != FileSystemAccess.Read && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not readable.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            if (offset < 0)
            {
                throw new GameFrameworkException("Index is invalid.");
            }

            if (stream == null)
            {
                throw new GameFrameworkException("Stream is invalid.");
            }

            if (!stream.CanWrite)
            {
                throw new GameFrameworkException("Stream is not writable.");
            }

            if (length < 0)
            {
                throw new GameFrameworkException("Length is invalid.");
            }

            FileInfo fileInfo = GetFileInfo(name);
            if (!fileInfo.IsValid)
            {
                return 0;
            }

            if (offset > fileInfo.Length)
            {
                offset = fileInfo.Length;
            }

            int leftLength = fileInfo.Length - offset;
            if (length > leftLength)
            {
                length = leftLength;
            }

            if (length > 0)
            {
                _Stream.Position = fileInfo.Offset + offset;
                return _Stream.Read(stream, length);
            }

            return 0;
        }

        /// <summary>
        /// 写入指定文件。
        /// </summary>
        /// <param name="name">要写入的文件名称。</param>
        /// <param name="buffer">存储写入文件内容的二进制流。</param>
        /// <returns>是否写入指定文件成功。</returns>
        public bool WriteFile(string name, byte[] buffer)
        {
            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return WriteFile(name, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 写入指定文件。
        /// </summary>
        /// <param name="name">要写入的文件名称。</param>
        /// <param name="buffer">存储写入文件内容的二进制流。</param>
        /// <param name="startIndex">存储写入文件内容的二进制流的起始位置。</param>
        /// <returns>是否写入指定文件成功。</returns>
        public bool WriteFile(string name, byte[] buffer, int startIndex)
        {
            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            return WriteFile(name, buffer, startIndex, buffer.Length - startIndex);
        }

        /// <summary>
        /// 写入指定文件。
        /// </summary>
        /// <param name="name">要写入的文件名称。</param>
        /// <param name="buffer">存储写入文件内容的二进制流。</param>
        /// <param name="startIndex">存储写入文件内容的二进制流的起始位置。</param>
        /// <param name="length">存储写入文件内容的二进制流的长度。</param>
        /// <returns>是否写入指定文件成功。</returns>
        public bool WriteFile(string name, byte[] buffer, int startIndex, int length)
        {
            if (_Access != FileSystemAccess.Write && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not writable.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            if (name.Length > byte.MaxValue)
            {
                throw new GameFrameworkException(Utility.Text.Format("Name '{0}' is too long.", name));
            }

            if (buffer == null)
            {
                throw new GameFrameworkException("Buffer is invalid.");
            }

            if (startIndex < 0 || length < 0 || startIndex + length > buffer.Length)
            {
                throw new GameFrameworkException("Start index or length is invalid.");
            }

            bool hasFile = false;
            int oldBlockIndex = -1;
            if (_FileDatas.TryGetValue(name, out oldBlockIndex))
            {
                hasFile = true;
            }

            if (!hasFile && _FileDatas.Count >= _HeaderData.MaxFileCount)
            {
                return false;
            }

            int blockIndex = AllocBlock(length);
            if (blockIndex < 0)
            {
                return false;
            }

            if (length > 0)
            {
                _Stream.Position = GetClusterOffset(_BlockDatas[blockIndex].ClusterIndex);
                _Stream.Write(buffer, startIndex, length);
            }

            ProcessWriteFile(name, hasFile, oldBlockIndex, blockIndex, length);
            _Stream.Flush();
            return true;
        }

        /// <summary>
        /// 写入指定文件。
        /// </summary>
        /// <param name="name">要写入的文件名称。</param>
        /// <param name="stream">存储写入文件内容的二进制流。</param>
        /// <returns>是否写入指定文件成功。</returns>
        public bool WriteFile(string name, Stream stream)
        {
            if (_Access != FileSystemAccess.Write && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not writable.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            if (name.Length > byte.MaxValue)
            {
                throw new GameFrameworkException(Utility.Text.Format("Name '{0}' is too long.", name));
            }

            if (stream == null)
            {
                throw new GameFrameworkException("Stream is invalid.");
            }

            if (!stream.CanRead)
            {
                throw new GameFrameworkException("Stream is not readable.");
            }

            bool hasFile = false;
            int oldBlockIndex = -1;
            if (_FileDatas.TryGetValue(name, out oldBlockIndex))
            {
                hasFile = true;
            }

            if (!hasFile && _FileDatas.Count >= _HeaderData.MaxFileCount)
            {
                return false;
            }

            int length = (int)(stream.Length - stream.Position);
            int blockIndex = AllocBlock(length);
            if (blockIndex < 0)
            {
                return false;
            }

            if (length > 0)
            {
                _Stream.Position = GetClusterOffset(_BlockDatas[blockIndex].ClusterIndex);
                _Stream.Write(stream, length);
            }

            ProcessWriteFile(name, hasFile, oldBlockIndex, blockIndex, length);
            _Stream.Flush();
            return true;
        }

        /// <summary>
        /// 写入指定文件。
        /// </summary>
        /// <param name="name">要写入的文件名称。</param>
        /// <param name="filePath">存储写入文件内容的文件路径。</param>
        /// <returns>是否写入指定文件成功。</returns>
        public bool WriteFile(string name, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new GameFrameworkException("File path is invalid");
            }

            if (!File.Exists(filePath))
            {
                return false;
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return WriteFile(name, fileStream);
            }
        }

        /// <summary>
        /// 将指定文件另存为物理文件。
        /// </summary>
        /// <param name="name">要另存为的文件名称。</param>
        /// <param name="filePath">存储写入文件内容的文件路径。</param>
        /// <returns>是否将指定文件另存为物理文件成功。</returns>
        public bool SaveAsFile(string name, string filePath)
        {
            if (_Access != FileSystemAccess.Read && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not readable.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new GameFrameworkException("File path is invalid");
            }

            FileInfo fileInfo = GetFileInfo(name);
            if (!fileInfo.IsValid)
            {
                return false;
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                int length = fileInfo.Length;
                if (length > 0)
                {
                    _Stream.Position = fileInfo.Offset;
                    return _Stream.Read(fileStream, length) == length;
                }

                return true;
            }
        }

        /// <summary>
        /// 重命名指定文件。
        /// </summary>
        /// <param name="oldName">要重命名的文件名称。</param>
        /// <param name="newName">重命名后的文件名称。</param>
        /// <returns>是否重命名指定文件成功。</returns>
        public bool RenameFile(string oldName, string newName)
        {
            if (_Access != FileSystemAccess.Write && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not writable.");
            }

            if (string.IsNullOrEmpty(oldName))
            {
                throw new GameFrameworkException("Old name is invalid.");
            }

            if (string.IsNullOrEmpty(newName))
            {
                throw new GameFrameworkException("New name is invalid.");
            }

            if (newName.Length > byte.MaxValue)
            {
                throw new GameFrameworkException(Utility.Text.Format("New name '{0}' is too long.", newName));
            }

            if (oldName == newName)
            {
                return true;
            }

            if (_FileDatas.ContainsKey(newName))
            {
                return false;
            }

            int blockIndex = 0;
            if (!_FileDatas.TryGetValue(oldName, out blockIndex))
            {
                return false;
            }

            int stringIndex = _BlockDatas[blockIndex].StringIndex;
            StringData stringData = _StringDatas[stringIndex].SetString(newName, _HeaderData.GetEncryptBytes());
            _StringDatas[stringIndex] = stringData;
            WriteStringData(stringIndex, stringData);
            _FileDatas.Add(newName, blockIndex);
            _FileDatas.Remove(oldName);
            _Stream.Flush();
            return true;
        }

        /// <summary>
        /// 删除指定文件。
        /// </summary>
        /// <param name="name">要删除的文件名称。</param>
        /// <returns>是否删除指定文件成功。</returns>
        public bool DeleteFile(string name)
        {
            if (_Access != FileSystemAccess.Write && _Access != FileSystemAccess.ReadWrite)
            {
                throw new GameFrameworkException("File system is not writable.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Name is invalid.");
            }

            int blockIndex = 0;
            if (!_FileDatas.TryGetValue(name, out blockIndex))
            {
                return false;
            }

            _FileDatas.Remove(name);

            BlockData blockData = _BlockDatas[blockIndex];
            int stringIndex = blockData.StringIndex;
            StringData stringData = _StringDatas[stringIndex].Clear();
            _FreeStringIndexes.Enqueue(stringIndex);
            _FreeStringDatas.Enqueue(stringData);
            _StringDatas.Remove(stringIndex);
            WriteStringData(stringIndex, stringData);

            blockData = blockData.Free();
            _BlockDatas[blockIndex] = blockData;
            if (!TryCombineFreeBlocks(blockIndex))
            {
                _FreeBlockIndexes.Add(blockData.Length, blockIndex);
                WriteBlockData(blockIndex);
            }

            _Stream.Flush();
            return true;
        }

        private void ProcessWriteFile(string name, bool hasFile, int oldBlockIndex, int blockIndex, int length)
        {
            BlockData blockData = _BlockDatas[blockIndex];
            if (hasFile)
            {
                BlockData oldBlockData = _BlockDatas[oldBlockIndex];
                blockData = new BlockData(oldBlockData.StringIndex, blockData.ClusterIndex, length);
                _BlockDatas[blockIndex] = blockData;
                WriteBlockData(blockIndex);

                oldBlockData = oldBlockData.Free();
                _BlockDatas[oldBlockIndex] = oldBlockData;
                if (!TryCombineFreeBlocks(oldBlockIndex))
                {
                    _FreeBlockIndexes.Add(oldBlockData.Length, oldBlockIndex);
                    WriteBlockData(oldBlockIndex);
                }
            }
            else
            {
                int stringIndex = AllocString(name);
                blockData = new BlockData(stringIndex, blockData.ClusterIndex, length);
                _BlockDatas[blockIndex] = blockData;
                WriteBlockData(blockIndex);
            }

            if (hasFile)
            {
                _FileDatas[name] = blockIndex;
            }
            else
            {
                _FileDatas.Add(name, blockIndex);
            }
        }

        private bool TryCombineFreeBlocks(int freeBlockIndex)
        {
            BlockData freeBlockData = _BlockDatas[freeBlockIndex];
            if (freeBlockData.Length <= 0)
            {
                return false;
            }

            int previousFreeBlockIndex = -1;
            int nextFreeBlockIndex = -1;
            int nextBlockDataClusterIndex = freeBlockData.ClusterIndex + GetUpBoundClusterCount(freeBlockData.Length);
            foreach (KeyValuePair<int, GameFrameworkLinkedListRange<int>> blockIndexes in _FreeBlockIndexes)
            {
                if (blockIndexes.Key <= 0)
                {
                    continue;
                }

                int blockDataClusterCount = GetUpBoundClusterCount(blockIndexes.Key);
                foreach (int blockIndex in blockIndexes.Value)
                {
                    BlockData blockData = _BlockDatas[blockIndex];
                    if (blockData.ClusterIndex + blockDataClusterCount == freeBlockData.ClusterIndex)
                    {
                        previousFreeBlockIndex = blockIndex;
                    }
                    else if (blockData.ClusterIndex == nextBlockDataClusterIndex)
                    {
                        nextFreeBlockIndex = blockIndex;
                    }
                }
            }

            if (previousFreeBlockIndex < 0 && nextFreeBlockIndex < 0)
            {
                return false;
            }

            _FreeBlockIndexes.Remove(freeBlockData.Length, freeBlockIndex);
            if (previousFreeBlockIndex >= 0)
            {
                BlockData previousFreeBlockData = _BlockDatas[previousFreeBlockIndex];
                _FreeBlockIndexes.Remove(previousFreeBlockData.Length, previousFreeBlockIndex);
                freeBlockData = new BlockData(previousFreeBlockData.ClusterIndex, previousFreeBlockData.Length + freeBlockData.Length);
                _BlockDatas[previousFreeBlockIndex] = BlockData.Empty;
                _FreeBlockIndexes.Add(0, previousFreeBlockIndex);
                WriteBlockData(previousFreeBlockIndex);
            }

            if (nextFreeBlockIndex >= 0)
            {
                BlockData nextFreeBlockData = _BlockDatas[nextFreeBlockIndex];
                _FreeBlockIndexes.Remove(nextFreeBlockData.Length, nextFreeBlockIndex);
                freeBlockData = new BlockData(freeBlockData.ClusterIndex, freeBlockData.Length + nextFreeBlockData.Length);
                _BlockDatas[nextFreeBlockIndex] = BlockData.Empty;
                _FreeBlockIndexes.Add(0, nextFreeBlockIndex);
                WriteBlockData(nextFreeBlockIndex);
            }

            _BlockDatas[freeBlockIndex] = freeBlockData;
            _FreeBlockIndexes.Add(freeBlockData.Length, freeBlockIndex);
            WriteBlockData(freeBlockIndex);
            return true;
        }

        private int GetEmptyBlockIndex()
        {
            GameFrameworkLinkedListRange<int> lengthRange = default(GameFrameworkLinkedListRange<int>);
            if (_FreeBlockIndexes.TryGetValue(0, out lengthRange))
            {
                int blockIndex = lengthRange.First.Value;
                _FreeBlockIndexes.Remove(0, blockIndex);
                return blockIndex;
            }

            if (_BlockDatas.Count < _HeaderData.MaxBlockCount)
            {
                int blockIndex = _BlockDatas.Count;
                _BlockDatas.Add(BlockData.Empty);
                WriteHeaderData();
                return blockIndex;
            }

            return -1;
        }

        private int AllocBlock(int length)
        {
            if (length <= 0)
            {
                return GetEmptyBlockIndex();
            }

            length = (int)GetUpBoundClusterOffset(length);

            int lengthFound = -1;
            GameFrameworkLinkedListRange<int> lengthRange = default(GameFrameworkLinkedListRange<int>);
            foreach (KeyValuePair<int, GameFrameworkLinkedListRange<int>> i in _FreeBlockIndexes)
            {
                if (i.Key < length)
                {
                    continue;
                }

                if (lengthFound >= 0 && lengthFound < i.Key)
                {
                    continue;
                }

                lengthFound = i.Key;
                lengthRange = i.Value;
            }

            if (lengthFound >= 0)
            {
                if (lengthFound > length && _BlockDatas.Count >= _HeaderData.MaxBlockCount)
                {
                    return -1;
                }

                int blockIndex = lengthRange.First.Value;
                _FreeBlockIndexes.Remove(lengthFound, blockIndex);
                if (lengthFound > length)
                {
                    BlockData blockData = _BlockDatas[blockIndex];
                    _BlockDatas[blockIndex] = new BlockData(blockData.ClusterIndex, length);
                    WriteBlockData(blockIndex);

                    int deltaLength = lengthFound - length;
                    int anotherBlockIndex = GetEmptyBlockIndex();
                    _BlockDatas[anotherBlockIndex] = new BlockData(blockData.ClusterIndex + GetUpBoundClusterCount(length), deltaLength);
                    _FreeBlockIndexes.Add(deltaLength, anotherBlockIndex);
                    WriteBlockData(anotherBlockIndex);
                }

                return blockIndex;
            }
            else
            {
                int blockIndex = GetEmptyBlockIndex();
                if (blockIndex < 0)
                {
                    return -1;
                }

                long fileLength = _Stream.Length;
                try
                {
                    _Stream.SetLength(fileLength + length);
                }
                catch
                {
                    return -1;
                }

                _BlockDatas[blockIndex] = new BlockData(GetUpBoundClusterCount(fileLength), length);
                WriteBlockData(blockIndex);
                return blockIndex;
            }
        }

        private int AllocString(string value)
        {
            int stringIndex = -1;
            StringData stringData = default(StringData);

            if (_FreeStringIndexes.Count > 0)
            {
                stringIndex = _FreeStringIndexes.Dequeue();
            }
            else
            {
                stringIndex = _StringDatas.Count;
            }

            if (_FreeStringDatas.Count > 0)
            {
                stringData = _FreeStringDatas.Dequeue();
            }
            else
            {
                byte[] bytes = new byte[byte.MaxValue];
                Utility.Random.GetRandomBytes(bytes);
                stringData = new StringData(0, bytes);
            }

            stringData = stringData.SetString(value, _HeaderData.GetEncryptBytes());
            _StringDatas.Add(stringIndex, stringData);
            WriteStringData(stringIndex, stringData);
            return stringIndex;
        }

        private void WriteHeaderData()
        {
            _HeaderData = _HeaderData.SetBlockCount(_BlockDatas.Count);
            Utility.Marshal.StructureToBytes(_HeaderData, HeaderDataSize, s_CachedBytes);
            _Stream.Position = 0L;
            _Stream.Write(s_CachedBytes, 0, HeaderDataSize);
        }

        private void WriteBlockData(int blockIndex)
        {
            Utility.Marshal.StructureToBytes(_BlockDatas[blockIndex], BlockDataSize, s_CachedBytes);
            _Stream.Position = _BlockDataOffset + BlockDataSize * blockIndex;
            _Stream.Write(s_CachedBytes, 0, BlockDataSize);
        }

        private StringData ReadStringData(int stringIndex)
        {
            _Stream.Position = _StringDataOffset + StringDataSize * stringIndex;
            _Stream.Read(s_CachedBytes, 0, StringDataSize);
            return Utility.Marshal.BytesToStructure<StringData>(StringDataSize, s_CachedBytes);
        }

        private void WriteStringData(int stringIndex, StringData stringData)
        {
            Utility.Marshal.StructureToBytes(stringData, StringDataSize, s_CachedBytes);
            _Stream.Position = _StringDataOffset + StringDataSize * stringIndex;
            _Stream.Write(s_CachedBytes, 0, StringDataSize);
        }

        private static void CalcOffsets(FileSystem fileSystem)
        {
            fileSystem._BlockDataOffset = HeaderDataSize;
            fileSystem._StringDataOffset = fileSystem._BlockDataOffset + BlockDataSize * fileSystem._HeaderData.MaxBlockCount;
            fileSystem._FileDataOffset = (int)GetUpBoundClusterOffset(fileSystem._StringDataOffset + StringDataSize * fileSystem._HeaderData.MaxFileCount);
        }

        private static long GetUpBoundClusterOffset(long offset)
        {
            return (offset - 1L + ClusterSize) / ClusterSize * ClusterSize;
        }

        private static int GetUpBoundClusterCount(long length)
        {
            return (int)((length - 1L + ClusterSize) / ClusterSize);
        }

        private static long GetClusterOffset(int clusterIndex)
        {
            return (long)ClusterSize * clusterIndex;
        }
    }
}
