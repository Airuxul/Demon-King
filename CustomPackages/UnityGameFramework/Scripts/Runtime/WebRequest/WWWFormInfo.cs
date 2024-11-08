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
    internal sealed class WWWFormInfo : IReference
    {
        private WWWForm _wwwForm;
        private object _userData;

        public WWWFormInfo()
        {
            _wwwForm = null;
            _userData = null;
        }

        public WWWForm WWWForm => _wwwForm;

        public object UserData => _userData;

        public static WWWFormInfo Create(WWWForm wwwForm, object userData)
        {
            WWWFormInfo wwwFormInfo = ReferencePool.Acquire<WWWFormInfo>();
            wwwFormInfo._wwwForm = wwwForm;
            wwwFormInfo._userData = userData;
            return wwwFormInfo;
        }

        public void Clear()
        {
            _wwwForm = null;
            _userData = null;
        }
    }
}
