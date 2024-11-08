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
        [StructLayout(LayoutKind.Auto)]
        private struct ReadWriteResourceInfo
        {
            private readonly string _FileSystemName;
            private readonly LoadType _LoadType;
            private readonly int _Length;
            private readonly int _HashCode;

            public ReadWriteResourceInfo(string fileSystemName, LoadType loadType, int length, int hashCode)
            {
                _FileSystemName = fileSystemName;
                _LoadType = loadType;
                _Length = length;
                _HashCode = hashCode;
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
