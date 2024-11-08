//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;

namespace GameFramework.UI
{
    internal sealed partial class UIManager : GameFrameworkModule, IUIManager
    {
        /// <summary>
        /// 界面组。
        /// </summary>
        private sealed partial class UIGroup : IUIGroup
        {
            private readonly string _Name;
            private int _Depth;
            private bool _Pause;
            private readonly IUIGroupHelper _UIGroupHelper;
            private readonly GameFrameworkLinkedList<UIFormInfo> _UIFormInfos;
            private LinkedListNode<UIFormInfo> _CachedNode;

            /// <summary>
            /// 初始化界面组的新实例。
            /// </summary>
            /// <param name="name">界面组名称。</param>
            /// <param name="depth">界面组深度。</param>
            /// <param name="uiGroupHelper">界面组辅助器。</param>
            public UIGroup(string name, int depth, IUIGroupHelper uiGroupHelper)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new GameFrameworkException("UI group name is invalid.");
                }

                if (uiGroupHelper == null)
                {
                    throw new GameFrameworkException("UI group helper is invalid.");
                }

                _Name = name;
                _Pause = false;
                _UIGroupHelper = uiGroupHelper;
                _UIFormInfos = new GameFrameworkLinkedList<UIFormInfo>();
                _CachedNode = null;
                Depth = depth;
            }

            /// <summary>
            /// 获取界面组名称。
            /// </summary>
            public string Name
            {
                get
                {
                    return _Name;
                }
            }

            /// <summary>
            /// 获取或设置界面组深度。
            /// </summary>
            public int Depth
            {
                get
                {
                    return _Depth;
                }
                set
                {
                    if (_Depth == value)
                    {
                        return;
                    }

                    _Depth = value;
                    _UIGroupHelper.SetDepth(_Depth);
                    Refresh();
                }
            }

            /// <summary>
            /// 获取或设置界面组是否暂停。
            /// </summary>
            public bool Pause
            {
                get
                {
                    return _Pause;
                }
                set
                {
                    if (_Pause == value)
                    {
                        return;
                    }

                    _Pause = value;
                    Refresh();
                }
            }

            /// <summary>
            /// 获取界面组中界面数量。
            /// </summary>
            public int UIFormCount
            {
                get
                {
                    return _UIFormInfos.Count;
                }
            }

            /// <summary>
            /// 获取当前界面。
            /// </summary>
            public IUIForm CurrentUIForm
            {
                get
                {
                    return _UIFormInfos.First != null ? _UIFormInfos.First.Value.UIForm : null;
                }
            }

            /// <summary>
            /// 获取界面组辅助器。
            /// </summary>
            public IUIGroupHelper Helper
            {
                get
                {
                    return _UIGroupHelper;
                }
            }

            /// <summary>
            /// 界面组轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                LinkedListNode<UIFormInfo> current = _UIFormInfos.First;
                while (current != null)
                {
                    if (current.Value.Paused)
                    {
                        break;
                    }

                    _CachedNode = current.Next;
                    current.Value.UIForm.OnUpdate(elapseSeconds, realElapseSeconds);
                    current = _CachedNode;
                    _CachedNode = null;
                }
            }

            /// <summary>
            /// 界面组中是否存在界面。
            /// </summary>
            /// <param name="serialId">界面序列编号。</param>
            /// <returns>界面组中是否存在界面。</returns>
            public bool HasUIForm(int serialId)
            {
                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    if (uiFormInfo.UIForm.SerialId == serialId)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// 界面组中是否存在界面。
            /// </summary>
            /// <param name="uiFormAssetName">界面资源名称。</param>
            /// <returns>界面组中是否存在界面。</returns>
            public bool HasUIForm(string uiFormAssetName)
            {
                if (string.IsNullOrEmpty(uiFormAssetName))
                {
                    throw new GameFrameworkException("UI form asset name is invalid.");
                }

                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    if (uiFormInfo.UIForm.UIFormAssetName == uiFormAssetName)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// 从界面组中获取界面。
            /// </summary>
            /// <param name="serialId">界面序列编号。</param>
            /// <returns>要获取的界面。</returns>
            public IUIForm GetUIForm(int serialId)
            {
                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    if (uiFormInfo.UIForm.SerialId == serialId)
                    {
                        return uiFormInfo.UIForm;
                    }
                }

                return null;
            }

            /// <summary>
            /// 从界面组中获取界面。
            /// </summary>
            /// <param name="uiFormAssetName">界面资源名称。</param>
            /// <returns>要获取的界面。</returns>
            public IUIForm GetUIForm(string uiFormAssetName)
            {
                if (string.IsNullOrEmpty(uiFormAssetName))
                {
                    throw new GameFrameworkException("UI form asset name is invalid.");
                }

                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    if (uiFormInfo.UIForm.UIFormAssetName == uiFormAssetName)
                    {
                        return uiFormInfo.UIForm;
                    }
                }

                return null;
            }

            /// <summary>
            /// 从界面组中获取界面。
            /// </summary>
            /// <param name="uiFormAssetName">界面资源名称。</param>
            /// <returns>要获取的界面。</returns>
            public IUIForm[] GetUIForms(string uiFormAssetName)
            {
                if (string.IsNullOrEmpty(uiFormAssetName))
                {
                    throw new GameFrameworkException("UI form asset name is invalid.");
                }

                List<IUIForm> results = new List<IUIForm>();
                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    if (uiFormInfo.UIForm.UIFormAssetName == uiFormAssetName)
                    {
                        results.Add(uiFormInfo.UIForm);
                    }
                }

                return results.ToArray();
            }

            /// <summary>
            /// 从界面组中获取界面。
            /// </summary>
            /// <param name="uiFormAssetName">界面资源名称。</param>
            /// <param name="results">要获取的界面。</param>
            public void GetUIForms(string uiFormAssetName, List<IUIForm> results)
            {
                if (string.IsNullOrEmpty(uiFormAssetName))
                {
                    throw new GameFrameworkException("UI form asset name is invalid.");
                }

                if (results == null)
                {
                    throw new GameFrameworkException("Results is invalid.");
                }

                results.Clear();
                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    if (uiFormInfo.UIForm.UIFormAssetName == uiFormAssetName)
                    {
                        results.Add(uiFormInfo.UIForm);
                    }
                }
            }

            /// <summary>
            /// 从界面组中获取所有界面。
            /// </summary>
            /// <returns>界面组中的所有界面。</returns>
            public IUIForm[] GetAllUIForms()
            {
                List<IUIForm> results = new List<IUIForm>();
                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    results.Add(uiFormInfo.UIForm);
                }

                return results.ToArray();
            }

            /// <summary>
            /// 从界面组中获取所有界面。
            /// </summary>
            /// <param name="results">界面组中的所有界面。</param>
            public void GetAllUIForms(List<IUIForm> results)
            {
                if (results == null)
                {
                    throw new GameFrameworkException("Results is invalid.");
                }

                results.Clear();
                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    results.Add(uiFormInfo.UIForm);
                }
            }

            /// <summary>
            /// 往界面组增加界面。
            /// </summary>
            /// <param name="uiForm">要增加的界面。</param>
            public void AddUIForm(IUIForm uiForm)
            {
                _UIFormInfos.AddFirst(UIFormInfo.Create(uiForm));
            }

            /// <summary>
            /// 从界面组移除界面。
            /// </summary>
            /// <param name="uiForm">要移除的界面。</param>
            public void RemoveUIForm(IUIForm uiForm)
            {
                UIFormInfo uiFormInfo = GetUIFormInfo(uiForm);
                if (uiFormInfo == null)
                {
                    throw new GameFrameworkException(Utility.Text.Format("Can not find UI form info for serial id '{0}', UI form asset name is '{1}'.", uiForm.SerialId, uiForm.UIFormAssetName));
                }

                if (!uiFormInfo.Covered)
                {
                    uiFormInfo.Covered = true;
                    uiForm.OnCover();
                }

                if (!uiFormInfo.Paused)
                {
                    uiFormInfo.Paused = true;
                    uiForm.OnPause();
                }

                if (_CachedNode != null && _CachedNode.Value.UIForm == uiForm)
                {
                    _CachedNode = _CachedNode.Next;
                }

                if (!_UIFormInfos.Remove(uiFormInfo))
                {
                    throw new GameFrameworkException(Utility.Text.Format("UI group '{0}' not exists specified UI form '[{1}]{2}'.", _Name, uiForm.SerialId, uiForm.UIFormAssetName));
                }

                ReferencePool.Release(uiFormInfo);
            }

            /// <summary>
            /// 激活界面。
            /// </summary>
            /// <param name="uiForm">要激活的界面。</param>
            /// <param name="userData">用户自定义数据。</param>
            public void RefocusUIForm(IUIForm uiForm, object userData)
            {
                UIFormInfo uiFormInfo = GetUIFormInfo(uiForm);
                if (uiFormInfo == null)
                {
                    throw new GameFrameworkException("Can not find UI form info.");
                }

                _UIFormInfos.Remove(uiFormInfo);
                _UIFormInfos.AddFirst(uiFormInfo);
            }

            /// <summary>
            /// 刷新界面组。
            /// </summary>
            public void Refresh()
            {
                LinkedListNode<UIFormInfo> current = _UIFormInfos.First;
                bool pause = _Pause;
                bool cover = false;
                int depth = UIFormCount;
                while (current != null && current.Value != null)
                {
                    LinkedListNode<UIFormInfo> next = current.Next;
                    current.Value.UIForm.OnDepthChanged(Depth, depth--);
                    if (current.Value == null)
                    {
                        return;
                    }

                    if (pause)
                    {
                        if (!current.Value.Covered)
                        {
                            current.Value.Covered = true;
                            current.Value.UIForm.OnCover();
                            if (current.Value == null)
                            {
                                return;
                            }
                        }

                        if (!current.Value.Paused)
                        {
                            current.Value.Paused = true;
                            current.Value.UIForm.OnPause();
                            if (current.Value == null)
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (current.Value.Paused)
                        {
                            current.Value.Paused = false;
                            current.Value.UIForm.OnResume();
                            if (current.Value == null)
                            {
                                return;
                            }
                        }

                        if (current.Value.UIForm.PauseCoveredUIForm)
                        {
                            pause = true;
                        }

                        if (cover)
                        {
                            if (!current.Value.Covered)
                            {
                                current.Value.Covered = true;
                                current.Value.UIForm.OnCover();
                                if (current.Value == null)
                                {
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if (current.Value.Covered)
                            {
                                current.Value.Covered = false;
                                current.Value.UIForm.OnReveal();
                                if (current.Value == null)
                                {
                                    return;
                                }
                            }

                            cover = true;
                        }
                    }

                    current = next;
                }
            }

            internal void InternalGetUIForms(string uiFormAssetName, List<IUIForm> results)
            {
                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    if (uiFormInfo.UIForm.UIFormAssetName == uiFormAssetName)
                    {
                        results.Add(uiFormInfo.UIForm);
                    }
                }
            }

            internal void InternalGetAllUIForms(List<IUIForm> results)
            {
                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    results.Add(uiFormInfo.UIForm);
                }
            }

            private UIFormInfo GetUIFormInfo(IUIForm uiForm)
            {
                if (uiForm == null)
                {
                    throw new GameFrameworkException("UI form is invalid.");
                }

                foreach (UIFormInfo uiFormInfo in _UIFormInfos)
                {
                    if (uiFormInfo.UIForm == uiForm)
                    {
                        return uiFormInfo;
                    }
                }

                return null;
            }
        }
    }
}
