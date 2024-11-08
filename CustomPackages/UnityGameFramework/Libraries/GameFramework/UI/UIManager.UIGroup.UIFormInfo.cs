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
        private sealed partial class UIGroup : IUIGroup
        {
            /// <summary>
            /// 界面组界面信息。
            /// </summary>
            private sealed class UIFormInfo : IReference
            {
                private IUIForm _UIForm;
                private bool _Paused;
                private bool _Covered;

                public UIFormInfo()
                {
                    _UIForm = null;
                    _Paused = false;
                    _Covered = false;
                }

                public IUIForm UIForm
                {
                    get
                    {
                        return _UIForm;
                    }
                }

                public bool Paused
                {
                    get
                    {
                        return _Paused;
                    }
                    set
                    {
                        _Paused = value;
                    }
                }

                public bool Covered
                {
                    get
                    {
                        return _Covered;
                    }
                    set
                    {
                        _Covered = value;
                    }
                }

                public static UIFormInfo Create(IUIForm uiForm)
                {
                    if (uiForm == null)
                    {
                        throw new GameFrameworkException("UI form is invalid.");
                    }

                    UIFormInfo uiFormInfo = ReferencePool.Acquire<UIFormInfo>();
                    uiFormInfo._UIForm = uiForm;
                    uiFormInfo._Paused = true;
                    uiFormInfo._Covered = true;
                    return uiFormInfo;
                }

                public void Clear()
                {
                    _UIForm = null;
                    _Paused = false;
                    _Covered = false;
                }
            }
        }
    }
}
