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
        private sealed class AssetData
        {
            private readonly string _Guid;
            private readonly string _Name;
            private readonly int _Length;
            private readonly int _HashCode;
            private readonly string[] _DependencyAssetNames;

            public AssetData(string guid, string name, int length, int hashCode, string[] dependencyAssetNames)
            {
                _Guid = guid;
                _Name = name;
                _Length = length;
                _HashCode = hashCode;
                _DependencyAssetNames = dependencyAssetNames;
            }

            public string Guid
            {
                get
                {
                    return _Guid;
                }
            }

            public string Name
            {
                get
                {
                    return _Name;
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

            public string[] GetDependencyAssetNames()
            {
                return _DependencyAssetNames;
            }
        }
    }
}
