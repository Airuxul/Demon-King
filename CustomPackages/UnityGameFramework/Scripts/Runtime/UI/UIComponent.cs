//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.ObjectPool;
using GameFramework.Resource;
using GameFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 界面组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/UI")]
    public sealed partial class UIComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;

        private IUIManager _uiManager = null;
        private EventComponent _eventComponent = null;

        private readonly List<IUIForm> _internalUIFormResults = new List<IUIForm>();

        [FormerlySerializedAs("_EnableOpenUIFormSuccessEvent")] [SerializeField]
        private bool enableOpenUIFormSuccessEvent = true;

        [FormerlySerializedAs("_EnableOpenUIFormFailureEvent")] [SerializeField]
        private bool enableOpenUIFormFailureEvent = true;

        [FormerlySerializedAs("_EnableOpenUIFormUpdateEvent")] [SerializeField]
        private bool enableOpenUIFormUpdateEvent = false;

        [FormerlySerializedAs("_EnableOpenUIFormDependencyAssetEvent")] [SerializeField]
        private bool enableOpenUIFormDependencyAssetEvent = false;

        [FormerlySerializedAs("_EnableCloseUIFormCompleteEvent")] [SerializeField]
        private bool enableCloseUIFormCompleteEvent = true;

        [FormerlySerializedAs("_InstanceAutoReleaseInterval")] [SerializeField]
        private float instanceAutoReleaseInterval = 60f;

        [FormerlySerializedAs("_InstanceCapacity")] [SerializeField]
        private int instanceCapacity = 16;

        [FormerlySerializedAs("_InstanceExpireTime")] [SerializeField]
        private float instanceExpireTime = 60f;

        [FormerlySerializedAs("_InstancePriority")] [SerializeField]
        private int instancePriority = 0;

        [FormerlySerializedAs("_InstanceRoot")] [SerializeField]
        private Transform instanceRoot = null;

        [FormerlySerializedAs("_UIFormHelperTypeName")] [SerializeField]
        private string uiFormHelperTypeName = "UnityGameFramework.Runtime.DefaultUIFormHelper";

        [FormerlySerializedAs("_CustomUIFormHelper")] [SerializeField]
        private UIFormHelperBase customUIFormHelper = null;

        [FormerlySerializedAs("_UIGroupHelperTypeName")] [SerializeField]
        private string uiGroupHelperTypeName = "UnityGameFramework.Runtime.DefaultUIGroupHelper";

        [FormerlySerializedAs("_CustomUIGroupHelper")] [SerializeField]
        private UIGroupHelperBase customUIGroupHelper = null;

        [FormerlySerializedAs("_UIGroups")] [SerializeField]
        private UIGroup[] uiGroups = null;

        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        public int UIGroupCount => _uiManager.UIGroupCount;

        /// <summary>
        /// 获取或设置界面实例对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        public float InstanceAutoReleaseInterval
        {
            get => _uiManager.InstanceAutoReleaseInterval;
            set => _uiManager.InstanceAutoReleaseInterval = instanceAutoReleaseInterval = value;
        }

        /// <summary>
        /// 获取或设置界面实例对象池的容量。
        /// </summary>
        public int InstanceCapacity
        {
            get => _uiManager.InstanceCapacity;
            set => _uiManager.InstanceCapacity = instanceCapacity = value;
        }

        /// <summary>
        /// 获取或设置界面实例对象池对象过期秒数。
        /// </summary>
        public float InstanceExpireTime
        {
            get => _uiManager.InstanceExpireTime;
            set => _uiManager.InstanceExpireTime = instanceExpireTime = value;
        }

        /// <summary>
        /// 获取或设置界面实例对象池的优先级。
        /// </summary>
        public int InstancePriority
        {
            get => _uiManager.InstancePriority;
            set => _uiManager.InstancePriority = instancePriority = value;
        }

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _uiManager = GameFrameworkEntry.GetModule<IUIManager>();
            if (_uiManager == null)
            {
                Log.Fatal("UI manager is invalid.");
                return;
            }

            if (enableOpenUIFormSuccessEvent)
            {
                _uiManager.OpenUIFormSuccess += OnOpenUIFormSuccess;
            }

            _uiManager.OpenUIFormFailure += OnOpenUIFormFailure;

            if (enableOpenUIFormUpdateEvent)
            {
                _uiManager.OpenUIFormUpdate += OnOpenUIFormUpdate;
            }

            if (enableOpenUIFormDependencyAssetEvent)
            {
                _uiManager.OpenUIFormDependencyAsset += OnOpenUIFormDependencyAsset;
            }

            if (enableCloseUIFormCompleteEvent)
            {
                _uiManager.CloseUIFormComplete += OnCloseUIFormComplete;
            }
        }

        private void Start()
        {
            BaseComponent baseComponent = GameEntry.GetComponent<BaseComponent>();
            if (baseComponent == null)
            {
                Log.Fatal("Base component is invalid.");
                return;
            }

            _eventComponent = GameEntry.GetComponent<EventComponent>();
            if (_eventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            if (baseComponent.EditorResourceMode)
            {
                _uiManager.SetResourceManager(baseComponent.EditorResourceHelper);
            }
            else
            {
                _uiManager.SetResourceManager(GameFrameworkEntry.GetModule<IResourceManager>());
            }

            _uiManager.SetObjectPoolManager(GameFrameworkEntry.GetModule<IObjectPoolManager>());
            _uiManager.InstanceAutoReleaseInterval = instanceAutoReleaseInterval;
            _uiManager.InstanceCapacity = instanceCapacity;
            _uiManager.InstanceExpireTime = instanceExpireTime;
            _uiManager.InstancePriority = instancePriority;

            UIFormHelperBase uiFormHelper = Helper.CreateHelper(uiFormHelperTypeName, customUIFormHelper);
            if (uiFormHelper == null)
            {
                Log.Error("Can not create UI form helper.");
                return;
            }

            uiFormHelper.name = "UI Form Helper";
            Transform transform = uiFormHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;

            _uiManager.SetUIFormHelper(uiFormHelper);

            if (instanceRoot == null)
            {
                instanceRoot = new GameObject("UI Form Instances").transform;
                instanceRoot.SetParent(gameObject.transform);
                instanceRoot.localScale = Vector3.one;
            }

            instanceRoot.gameObject.layer = LayerMask.NameToLayer("UI");

            for (int i = 0; i < uiGroups.Length; i++)
            {
                if (!AddUIGroup(uiGroups[i].Name, uiGroups[i].Depth))
                {
                    Log.Warning("Add UI group '{0}' failure.", uiGroups[i].Name);
                    continue;
                }
            }
        }

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否存在界面组。</returns>
        public bool HasUIGroup(string uiGroupName)
        {
            return _uiManager.HasUIGroup(uiGroupName);
        }

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>要获取的界面组。</returns>
        public IUIGroup GetUIGroup(string uiGroupName)
        {
            return _uiManager.GetUIGroup(uiGroupName);
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        public IUIGroup[] GetAllUIGroups()
        {
            return _uiManager.GetAllUIGroups();
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <param name="results">所有界面组。</param>
        public void GetAllUIGroups(List<IUIGroup> results)
        {
            _uiManager.GetAllUIGroups(results);
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(string uiGroupName)
        {
            return AddUIGroup(uiGroupName, 0);
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="depth">界面组深度。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(string uiGroupName, int depth)
        {
            if (_uiManager.HasUIGroup(uiGroupName))
            {
                return false;
            }

            UIGroupHelperBase uiGroupHelper = Helper.CreateHelper(uiGroupHelperTypeName, customUIGroupHelper, UIGroupCount);
            if (uiGroupHelper == null)
            {
                Log.Error("Can not create UI group helper.");
                return false;
            }

            uiGroupHelper.name = Utility.Text.Format("UI Group - {0}", uiGroupName);
            uiGroupHelper.gameObject.layer = LayerMask.NameToLayer("UI");
            Transform transform = uiGroupHelper.transform;
            transform.SetParent(instanceRoot);
            transform.localScale = Vector3.one;

            return _uiManager.AddUIGroup(uiGroupName, depth, uiGroupHelper);
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUIForm(int serialId)
        {
            return _uiManager.HasUIForm(serialId);
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUIForm(string uiFormAssetName)
        {
            return _uiManager.HasUIForm(uiFormAssetName);
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public UIForm GetUIForm(int serialId)
        {
            return (UIForm)_uiManager.GetUIForm(serialId);
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public UIForm GetUIForm(string uiFormAssetName)
        {
            return (UIForm)_uiManager.GetUIForm(uiFormAssetName);
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public UIForm[] GetUIForms(string uiFormAssetName)
        {
            IUIForm[] uiForms = _uiManager.GetUIForms(uiFormAssetName);
            UIForm[] uiFormImpls = new UIForm[uiForms.Length];
            for (int i = 0; i < uiForms.Length; i++)
            {
                uiFormImpls[i] = (UIForm)uiForms[i];
            }

            return uiFormImpls;
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        public void GetUIForms(string uiFormAssetName, List<UIForm> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            _uiManager.GetUIForms(uiFormAssetName, _internalUIFormResults);
            foreach (IUIForm uiForm in _internalUIFormResults)
            {
                results.Add((UIForm)uiForm);
            }
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <returns>所有已加载的界面。</returns>
        public UIForm[] GetAllLoadedUIForms()
        {
            IUIForm[] uiForms = _uiManager.GetAllLoadedUIForms();
            UIForm[] uiFormImpls = new UIForm[uiForms.Length];
            for (int i = 0; i < uiForms.Length; i++)
            {
                uiFormImpls[i] = (UIForm)uiForms[i];
            }

            return uiFormImpls;
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <param name="results">所有已加载的界面。</param>
        public void GetAllLoadedUIForms(List<UIForm> results)
        {
            if (results == null)
            {
                Log.Error("Results is invalid.");
                return;
            }

            results.Clear();
            _uiManager.GetAllLoadedUIForms(_internalUIFormResults);
            foreach (IUIForm uiForm in _internalUIFormResults)
            {
                results.Add((UIForm)uiForm);
            }
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <returns>所有正在加载界面的序列编号。</returns>
        public int[] GetAllLoadingUIFormSerialIds()
        {
            return _uiManager.GetAllLoadingUIFormSerialIds();
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <param name="results">所有正在加载界面的序列编号。</param>
        public void GetAllLoadingUIFormSerialIds(List<int> results)
        {
            _uiManager.GetAllLoadingUIFormSerialIds(results);
        }

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUIForm(int serialId)
        {
            return _uiManager.IsLoadingUIForm(serialId);
        }

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUIForm(string uiFormAssetName)
        {
            return _uiManager.IsLoadingUIForm(uiFormAssetName);
        }

        /// <summary>
        /// 是否是合法的界面。
        /// </summary>
        /// <param name="uiForm">界面。</param>
        /// <returns>界面是否合法。</returns>
        public bool IsValidUIForm(UIForm uiForm)
        {
            return _uiManager.IsValidUIForm(uiForm);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>界面的序列编号。</returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, DefaultPriority, false, null);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="priority">加载界面资源的优先级。</param>
        /// <returns>界面的序列编号。</returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, int priority)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, priority, false, null);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <returns>界面的序列编号。</returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, bool pauseCoveredUIForm)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, DefaultPriority, pauseCoveredUIForm, null);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面的序列编号。</returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, object userData)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, DefaultPriority, false, userData);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="priority">加载界面资源的优先级。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <returns>界面的序列编号。</returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, int priority, bool pauseCoveredUIForm)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, priority, pauseCoveredUIForm, null);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="priority">加载界面资源的优先级。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面的序列编号。</returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, int priority, object userData)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, priority, false, userData);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面的序列编号。</returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, bool pauseCoveredUIForm, object userData)
        {
            return OpenUIForm(uiFormAssetName, uiGroupName, DefaultPriority, pauseCoveredUIForm, userData);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="priority">加载界面资源的优先级。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面的序列编号。</returns>
        public int OpenUIForm(string uiFormAssetName, string uiGroupName, int priority, bool pauseCoveredUIForm, object userData)
        {
            return _uiManager.OpenUIForm(uiFormAssetName, uiGroupName, priority, pauseCoveredUIForm, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUIForm(int serialId)
        {
            _uiManager.CloseUIForm(serialId);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIForm(int serialId, object userData)
        {
            _uiManager.CloseUIForm(serialId, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        public void CloseUIForm(UIForm uiForm)
        {
            _uiManager.CloseUIForm(uiForm);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIForm(UIForm uiForm, object userData)
        {
            _uiManager.CloseUIForm(uiForm, userData);
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        public void CloseAllLoadedUIForms()
        {
            _uiManager.CloseAllLoadedUIForms();
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseAllLoadedUIForms(object userData)
        {
            _uiManager.CloseAllLoadedUIForms(userData);
        }

        /// <summary>
        /// 关闭所有正在加载的界面。
        /// </summary>
        public void CloseAllLoadingUIForms()
        {
            _uiManager.CloseAllLoadingUIForms();
        }

        /// <summary>
        /// 激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        public void RefocusUIForm(UIForm uiForm)
        {
            _uiManager.RefocusUIForm(uiForm);
        }

        /// <summary>
        /// 激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void RefocusUIForm(UIForm uiForm, object userData)
        {
            _uiManager.RefocusUIForm(uiForm, userData);
        }

        /// <summary>
        /// 设置界面是否被加锁。
        /// </summary>
        /// <param name="uiForm">要设置是否被加锁的界面。</param>
        /// <param name="locked">界面是否被加锁。</param>
        public void SetUIFormInstanceLocked(UIForm uiForm, bool locked)
        {
            if (uiForm == null)
            {
                Log.Warning("UI form is invalid.");
                return;
            }

            _uiManager.SetUIFormInstanceLocked(uiForm.gameObject, locked);
        }

        /// <summary>
        /// 设置界面的优先级。
        /// </summary>
        /// <param name="uiForm">要设置优先级的界面。</param>
        /// <param name="priority">界面优先级。</param>
        public void SetUIFormInstancePriority(UIForm uiForm, int priority)
        {
            if (uiForm == null)
            {
                Log.Warning("UI form is invalid.");
                return;
            }

            _uiManager.SetUIFormInstancePriority(uiForm.gameObject, priority);
        }

        private void OnOpenUIFormSuccess(object sender, GameFramework.UI.OpenUIFormSuccessEventArgs e)
        {
            _eventComponent.Fire(this, OpenUIFormSuccessEventArgs.Create(e));
        }

        private void OnOpenUIFormFailure(object sender, GameFramework.UI.OpenUIFormFailureEventArgs e)
        {
            Log.Warning("Open UI form failure, asset name '{0}', UI group name '{1}', pause covered UI form '{2}', error message '{3}'.", e.UIFormAssetName, e.UIGroupName, e.PauseCoveredUIForm, e.ErrorMessage);
            if (enableOpenUIFormFailureEvent)
            {
                _eventComponent.Fire(this, OpenUIFormFailureEventArgs.Create(e));
            }
        }

        private void OnOpenUIFormUpdate(object sender, GameFramework.UI.OpenUIFormUpdateEventArgs e)
        {
            _eventComponent.Fire(this, OpenUIFormUpdateEventArgs.Create(e));
        }

        private void OnOpenUIFormDependencyAsset(object sender, GameFramework.UI.OpenUIFormDependencyAssetEventArgs e)
        {
            _eventComponent.Fire(this, OpenUIFormDependencyAssetEventArgs.Create(e));
        }

        private void OnCloseUIFormComplete(object sender, GameFramework.UI.CloseUIFormCompleteEventArgs e)
        {
            _eventComponent.Fire(this, CloseUIFormCompleteEventArgs.Create(e));
        }
    }
}
