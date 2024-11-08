//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;

namespace UnityGameFramework.Runtime
{
    internal sealed class ShowEntityInfo : IReference
    {
        private Type _entityLogicType;
        private object _userData;

        public ShowEntityInfo()
        {
            _entityLogicType = null;
            _userData = null;
        }

        public Type EntityLogicType => _entityLogicType;

        public object UserData => _userData;

        public static ShowEntityInfo Create(Type entityLogicType, object userData)
        {
            ShowEntityInfo showEntityInfo = ReferencePool.Acquire<ShowEntityInfo>();
            showEntityInfo._entityLogicType = entityLogicType;
            showEntityInfo._userData = userData;
            return showEntityInfo;
        }

        public void Clear()
        {
            _entityLogicType = null;
            _userData = null;
        }
    }
}
