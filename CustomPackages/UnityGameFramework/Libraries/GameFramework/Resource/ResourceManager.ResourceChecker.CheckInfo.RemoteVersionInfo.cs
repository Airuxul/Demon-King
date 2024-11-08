//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceChecker
        {
            private sealed partial class CheckInfo
            {
                /// <summary>
                /// 远程资源状态信息。
                /// </summary>
                [StructLayout(LayoutKind.Auto)]
                private struct RemoteVersionInfo
                {
                    private readonly bool _Exist;
                    private readonly string _FileSystemName;
                    private readonly LoadType _LoadType;
                    private readonly int _Length;
                    private readonly int _HashCode;
                    private readonly int _CompressedLength;
                    private readonly int _CompressedHashCode;

                    public RemoteVersionInfo(string fileSystemName, LoadType loadType, int length, int hashCode, int compressedLength, int compressedHashCode)
                    {
                        _Exist = true;
                        _FileSystemName = fileSystemName;
                        _LoadType = loadType;
                        _Length = length;
                        _HashCode = hashCode;
                        _CompressedLength = compressedLength;
                        _CompressedHashCode = compressedHashCode;
                    }

                    public bool Exist
                    {
                        get
                        {
                            return _Exist;
                        }
                    }

                    public bool UseFileSystem
                    {
                        get
                        {
                            return !string.IsNullOrEmpty(_FileSystemName);
                        }
                    }

                    public string FileSystemName
                    {
                        get
                        {
                            return _FileSystemName;
                        }
                    }

                    public LoadType LoadType
                    {
                        get
                        {
                            return _LoadType;
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
    }
}
