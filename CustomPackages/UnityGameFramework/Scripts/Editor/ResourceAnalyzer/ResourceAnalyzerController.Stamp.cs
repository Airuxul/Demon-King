//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceAnalyzerController
    {
        [StructLayout(LayoutKind.Auto)]
        private struct Stamp
        {
            private readonly string _HostAssetName;
            private readonly string _DependencyAssetName;

            public Stamp(string hostAssetName, string dependencyAssetName)
            {
                _HostAssetName = hostAssetName;
                _DependencyAssetName = dependencyAssetName;
            }

            public string HostAssetName
            {
                get
                {
                    return _HostAssetName;
                }
            }

            public string DependencyAssetName
            {
                get
                {
                    return _DependencyAssetName;
                }
            }
        }
    }
}
