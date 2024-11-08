//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    internal sealed class AttachEntityInfo : IReference
    {
        private Transform _parentTransform;
        private object _userData;

        public AttachEntityInfo()
        {
            _parentTransform = null;
            _userData = null;
        }

        public Transform ParentTransform => _parentTransform;

        public object UserData => _userData;

        public static AttachEntityInfo Create(Transform parentTransform, object userData)
        {
            AttachEntityInfo attachEntityInfo = ReferencePool.Acquire<AttachEntityInfo>();
            attachEntityInfo._parentTransform = parentTransform;
            attachEntityInfo._userData = userData;
            return attachEntityInfo;
        }

        public void Clear()
        {
            _parentTransform = null;
            _userData = null;
        }
    }
}
