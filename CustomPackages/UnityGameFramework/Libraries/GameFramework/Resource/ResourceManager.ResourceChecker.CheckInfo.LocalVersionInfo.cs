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
                /// 本地资源状态信息。
                /// </summary>
                [StructLayout(LayoutKind.Auto)]
                private struct LocalVersionInfo
                {
                    private readonly bool _Exist;
                    private readonly string _FileSystemName;
                    private readonly LoadType _LoadType;
                    private readonly int _Length;
                    private readonly int _HashCode;

                    public LocalVersionInfo(string fileSystemName, LoadType loadType, int length, int hashCode)
                    {
                        _Exist = true;
                        _FileSystemName = fileSystemName;
                        _LoadType = loadType;
                        _Length = length;
                        _HashCode = hashCode;
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
                }
            }
        }
    }
}
