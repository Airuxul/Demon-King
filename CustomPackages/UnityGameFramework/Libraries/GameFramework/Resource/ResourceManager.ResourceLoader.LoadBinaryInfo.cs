//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            private sealed class LoadBinaryInfo : IReference
            {
                private string _BinaryAssetName;
                private ResourceInfo _ResourceInfo;
                private LoadBinaryCallbacks _LoadBinaryCallbacks;
                private object _UserData;

                public LoadBinaryInfo()
                {
                    _BinaryAssetName = null;
                    _ResourceInfo = null;
                    _LoadBinaryCallbacks = null;
                    _UserData = null;
                }

                public string BinaryAssetName
                {
                    get
                    {
                        return _BinaryAssetName;
                    }
                }

                public ResourceInfo ResourceInfo
                {
                    get
                    {
                        return _ResourceInfo;
                    }
                }

                public LoadBinaryCallbacks LoadBinaryCallbacks
                {
                    get
                    {
                        return _LoadBinaryCallbacks;
                    }
                }

                public object UserData
                {
                    get
                    {
                        return _UserData;
                    }
                }

                public static LoadBinaryInfo Create(string binaryAssetName, ResourceInfo resourceInfo, LoadBinaryCallbacks loadBinaryCallbacks, object userData)
                {
                    LoadBinaryInfo loadBinaryInfo = ReferencePool.Acquire<LoadBinaryInfo>();
                    loadBinaryInfo._BinaryAssetName = binaryAssetName;
                    loadBinaryInfo._ResourceInfo = resourceInfo;
                    loadBinaryInfo._LoadBinaryCallbacks = loadBinaryCallbacks;
                    loadBinaryInfo._UserData = userData;
                    return loadBinaryInfo;
                }

                public void Clear()
                {
                    _BinaryAssetName = null;
                    _ResourceInfo = null;
                    _LoadBinaryCallbacks = null;
                    _UserData = null;
                }
            }
        }
    }
}
