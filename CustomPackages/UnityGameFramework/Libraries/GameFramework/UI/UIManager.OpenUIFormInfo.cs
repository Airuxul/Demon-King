//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.UI
{
    internal sealed partial class UIManager : GameFrameworkModule, IUIManager
    {
        private sealed class OpenUIFormInfo : IReference
        {
            private int _SerialId;
            private UIGroup _UIGroup;
            private bool _PauseCoveredUIForm;
            private object _UserData;

            public OpenUIFormInfo()
            {
                _SerialId = 0;
                _UIGroup = null;
                _PauseCoveredUIForm = false;
                _UserData = null;
            }

            public int SerialId
            {
                get
                {
                    return _SerialId;
                }
            }

            public UIGroup UIGroup
            {
                get
                {
                    return _UIGroup;
                }
            }

            public bool PauseCoveredUIForm
            {
                get
                {
                    return _PauseCoveredUIForm;
                }
            }

            public object UserData
            {
                get
                {
                    return _UserData;
                }
            }

            public static OpenUIFormInfo Create(int serialId, UIGroup uiGroup, bool pauseCoveredUIForm, object userData)
            {
                OpenUIFormInfo openUIFormInfo = ReferencePool.Acquire<OpenUIFormInfo>();
                openUIFormInfo._SerialId = serialId;
                openUIFormInfo._UIGroup = uiGroup;
                openUIFormInfo._PauseCoveredUIForm = pauseCoveredUIForm;
                openUIFormInfo._UserData = userData;
                return openUIFormInfo;
            }

            public void Clear()
            {
                _SerialId = 0;
                _UIGroup = null;
                _PauseCoveredUIForm = false;
                _UserData = null;
            }
        }
    }
}
