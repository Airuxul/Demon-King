//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Download;
using System;
#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#else
using UnityEngine.Experimental.Networking;
#endif

namespace UnityGameFramework.Runtime
{
    public partial class UnityWebRequestDownloadAgentHelper : DownloadAgentHelperBase, IDisposable
    {
        private sealed class DownloadHandler : DownloadHandlerScript
        {
            private readonly UnityWebRequestDownloadAgentHelper _owner;

            public DownloadHandler(UnityWebRequestDownloadAgentHelper owner)
                : base(owner._cachedBytes)
            {
                _owner = owner;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (_owner != null && _owner._unityWebRequest != null && dataLength > 0)
                {
                    DownloadAgentHelperUpdateBytesEventArgs downloadAgentHelperUpdateBytesEventArgs = DownloadAgentHelperUpdateBytesEventArgs.Create(data, 0, dataLength);
                    _owner._downloadAgentHelperUpdateBytesEventHandler(this, downloadAgentHelperUpdateBytesEventArgs);
                    ReferencePool.Release(downloadAgentHelperUpdateBytesEventArgs);

                    DownloadAgentHelperUpdateLengthEventArgs downloadAgentHelperUpdateLengthEventArgs = DownloadAgentHelperUpdateLengthEventArgs.Create(dataLength);
                    _owner._downloadAgentHelperUpdateLengthEventHandler(this, downloadAgentHelperUpdateLengthEventArgs);
                    ReferencePool.Release(downloadAgentHelperUpdateLengthEventArgs);
                }

                return base.ReceiveData(data, dataLength);
            }
        }
    }
}
