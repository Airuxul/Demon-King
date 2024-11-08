//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceBuilderController
    {
        private sealed class ResourceData
        {
            private readonly string _Name;
            private readonly string _Variant;
            private readonly string _FileSystem;
            private readonly LoadType _LoadType;
            private readonly bool _Packed;
            private readonly string[] _ResourceGroups;
            private readonly List<AssetData> _AssetDatas;
            private readonly List<ResourceCode> _Codes;

            public ResourceData(string name, string variant, string fileSystem, LoadType loadType, bool packed, string[] resourceGroups)
            {
                _Name = name;
                _Variant = variant;
                _FileSystem = fileSystem;
                _LoadType = loadType;
                _Packed = packed;
                _ResourceGroups = resourceGroups;
                _AssetDatas = new List<AssetData>();
                _Codes = new List<ResourceCode>();
            }

            public string Name
            {
                get
                {
                    return _Name;
                }
            }

            public string Variant
            {
                get
                {
                    return _Variant;
                }
            }

            public string FileSystem
            {
                get
                {
                    return _FileSystem;
                }
            }

            public bool IsLoadFromBinary
            {
                get
                {
                    return _LoadType == LoadType.LoadFromBinary || _LoadType == LoadType.LoadFromBinaryAndQuickDecrypt || _LoadType == LoadType.LoadFromBinaryAndDecrypt;
                }
            }

            public LoadType LoadType
            {
                get
                {
                    return _LoadType;
                }
            }

            public bool Packed
            {
                get
                {
                    return _Packed;
                }
            }

            public int AssetCount
            {
                get
                {
                    return _AssetDatas.Count;
                }
            }

            public string[] GetResourceGroups()
            {
                return _ResourceGroups;
            }

            public string[] GetAssetGuids()
            {
                string[] assetGuids = new string[_AssetDatas.Count];
                for (int i = 0; i < _AssetDatas.Count; i++)
                {
                    assetGuids[i] = _AssetDatas[i].Guid;
                }

                return assetGuids;
            }

            public string[] GetAssetNames()
            {
                string[] assetNames = new string[_AssetDatas.Count];
                for (int i = 0; i < _AssetDatas.Count; i++)
                {
                    assetNames[i] = _AssetDatas[i].Name;
                }

                return assetNames;
            }

            public AssetData[] GetAssetDatas()
            {
                return _AssetDatas.ToArray();
            }

            public AssetData GetAssetData(string assetName)
            {
                foreach (AssetData assetData in _AssetDatas)
                {
                    if (assetData.Name == assetName)
                    {
                        return assetData;
                    }
                }

                return null;
            }

            public void AddAssetData(string guid, string name, int length, int hashCode, string[] dependencyAssetNames)
            {
                _AssetDatas.Add(new AssetData(guid, name, length, hashCode, dependencyAssetNames));
            }

            public ResourceCode GetCode(Platform platform)
            {
                foreach (ResourceCode code in _Codes)
                {
                    if (code.Platform == platform)
                    {
                        return code;
                    }
                }

                return null;
            }

            public ResourceCode[] GetCodes()
            {
                return _Codes.ToArray();
            }

            public void AddCode(Platform platform, int length, int hashCode, int compressedLength, int compressedHashCode)
            {
                _Codes.Add(new ResourceCode(platform, length, hashCode, compressedLength, compressedHashCode));
            }
        }
    }
}
