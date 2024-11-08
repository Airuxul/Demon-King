//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Localization;
using GameFramework.Resource;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 基础组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Base")]
    public sealed class BaseComponent : GameFrameworkComponent
    {
        private const int DefaultDpi = 96;  // default windows dpi

        private float _mGameSpeedBeforePause = 1f;

        [FormerlySerializedAs("m_EditorResourceMode")] [SerializeField]
        private bool mEditorResourceMode = true;

        [FormerlySerializedAs("m_EditorLanguage")] [SerializeField]
        private Language mEditorLanguage = Language.Unspecified;

        [FormerlySerializedAs("m_TextHelperTypeName")] [SerializeField]
        private string mTextHelperTypeName = "UnityGameFramework.Runtime.DefaultTextHelper";

        [FormerlySerializedAs("m_VersionHelperTypeName")] [SerializeField]
        private string mVersionHelperTypeName = "UnityGameFramework.Runtime.DefaultVersionHelper";

        [FormerlySerializedAs("m_LogHelperTypeName")] [SerializeField]
        private string mLogHelperTypeName = "UnityGameFramework.Runtime.DefaultLogHelper";

        [FormerlySerializedAs("m_CompressionHelperTypeName")] [SerializeField]
        private string mCompressionHelperTypeName = "UnityGameFramework.Runtime.DefaultCompressionHelper";

        [FormerlySerializedAs("m_JsonHelperTypeName")] [SerializeField]
        private string mJsonHelperTypeName = "UnityGameFramework.Runtime.DefaultJsonHelper";

        [FormerlySerializedAs("m_FrameRate")] [SerializeField]
        private int mFrameRate = 30;

        [FormerlySerializedAs("m_GameSpeed")] [SerializeField]
        private float mGameSpeed = 1f;

        [FormerlySerializedAs("m_RunInBackground")] [SerializeField]
        private bool mRunInBackground = true;

        [FormerlySerializedAs("m_NeverSleep")] [SerializeField]
        private bool mNeverSleep = true;

        /// <summary>
        /// 获取或设置是否使用编辑器资源模式（仅编辑器内有效）。
        /// </summary>
        public bool EditorResourceMode
        {
            get => mEditorResourceMode;
            set => mEditorResourceMode = value;
        }

        /// <summary>
        /// 获取或设置编辑器语言（仅编辑器内有效）。
        /// </summary>
        public Language EditorLanguage
        {
            get => mEditorLanguage;
            set => mEditorLanguage = value;
        }

        /// <summary>
        /// 获取或设置编辑器资源辅助器。
        /// </summary>
        public IResourceManager EditorResourceHelper
        {
            get;
            set;
        }

        /// <summary>
        /// 获取或设置游戏帧率。
        /// </summary>
        public int FrameRate
        {
            get => mFrameRate;
            set => Application.targetFrameRate = mFrameRate = value;
        }

        /// <summary>
        /// 获取或设置游戏速度。
        /// </summary>
        public float GameSpeed
        {
            get => mGameSpeed;
            set => Time.timeScale = mGameSpeed = value >= 0f ? value : 0f;
        }

        /// <summary>
        /// 获取游戏是否暂停。
        /// </summary>
        public bool IsGamePaused => mGameSpeed <= 0f;

        /// <summary>
        /// 获取是否正常游戏速度。
        /// </summary>
        public bool IsNormalGameSpeed => mGameSpeed == 1f;

        /// <summary>
        /// 获取或设置是否允许后台运行。
        /// </summary>
        public bool RunInBackground
        {
            get => mRunInBackground;
            set => Application.runInBackground = mRunInBackground = value;
        }

        /// <summary>
        /// 获取或设置是否禁止休眠。
        /// </summary>
        public bool NeverSleep
        {
            get => mNeverSleep;
            set
            {
                mNeverSleep = value;
                Screen.sleepTimeout = value ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
            }
        }

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            InitTextHelper();
            InitVersionHelper();
            InitLogHelper();
            Log.Info("Game Framework Version: {0}", GameFramework.Version.GameFrameworkVersion);
            Log.Info("Game Version: {0} ({1})", GameFramework.Version.GameVersion, GameFramework.Version.InternalGameVersion);
            Log.Info("Unity Version: {0}", Application.unityVersion);

#if UNITY_5_3_OR_NEWER || UNITY_5_3
            InitCompressionHelper();
            InitJsonHelper();

            Utility.Converter.ScreenDpi = Screen.dpi;
            if (Utility.Converter.ScreenDpi <= 0)
            {
                Utility.Converter.ScreenDpi = DefaultDpi;
            }

            mEditorResourceMode &= Application.isEditor;
            if (mEditorResourceMode)
            {
                Log.Info("During this run, Game Framework will use editor resource files, which you should validate first.");
            }

            Application.targetFrameRate = mFrameRate;
            Time.timeScale = mGameSpeed;
            Application.runInBackground = mRunInBackground;
            Screen.sleepTimeout = mNeverSleep ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
#else
            Log.Error("Game Framework only applies with Unity 5.3 and above, but current Unity version is {0}.", Application.unityVersion);
            GameEntry.Shutdown(ShutdownType.Quit);
#endif
#if UNITY_5_6_OR_NEWER
            Application.lowMemory += OnLowMemory;
#endif
        }

        private void Start()
        {
        }

        private void Update()
        {
            GameFrameworkEntry.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnApplicationQuit()
        {
#if UNITY_5_6_OR_NEWER
            Application.lowMemory -= OnLowMemory;
#endif
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            GameFrameworkEntry.Shutdown();
        }

        /// <summary>
        /// 暂停游戏。
        /// </summary>
        public void PauseGame()
        {
            if (IsGamePaused)
            {
                return;
            }

            _mGameSpeedBeforePause = GameSpeed;
            GameSpeed = 0f;
        }

        /// <summary>
        /// 恢复游戏。
        /// </summary>
        public void ResumeGame()
        {
            if (!IsGamePaused)
            {
                return;
            }

            GameSpeed = _mGameSpeedBeforePause;
        }

        /// <summary>
        /// 重置为正常游戏速度。
        /// </summary>
        public void ResetNormalGameSpeed()
        {
            if (IsNormalGameSpeed)
            {
                return;
            }

            GameSpeed = 1f;
        }

        internal void Shutdown()
        {
            Destroy(gameObject);
        }

        private void InitTextHelper()
        {
            if (string.IsNullOrEmpty(mTextHelperTypeName))
            {
                return;
            }

            Type textHelperType = Utility.Assembly.GetType(mTextHelperTypeName);
            if (textHelperType == null)
            {
                Log.Error("Can not find text helper type '{0}'.", mTextHelperTypeName);
                return;
            }

            Utility.Text.ITextHelper textHelper = (Utility.Text.ITextHelper)Activator.CreateInstance(textHelperType);
            if (textHelper == null)
            {
                Log.Error("Can not create text helper instance '{0}'.", mTextHelperTypeName);
                return;
            }

            Utility.Text.SetTextHelper(textHelper);
        }

        private void InitVersionHelper()
        {
            if (string.IsNullOrEmpty(mVersionHelperTypeName))
            {
                return;
            }

            Type versionHelperType = Utility.Assembly.GetType(mVersionHelperTypeName);
            if (versionHelperType == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Can not find version helper type '{0}'.", mVersionHelperTypeName));
            }

            GameFramework.Version.IVersionHelper versionHelper = (GameFramework.Version.IVersionHelper)Activator.CreateInstance(versionHelperType);
            if (versionHelper == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Can not create version helper instance '{0}'.", mVersionHelperTypeName));
            }

            GameFramework.Version.SetVersionHelper(versionHelper);
        }

        private void InitLogHelper()
        {
            if (string.IsNullOrEmpty(mLogHelperTypeName))
            {
                return;
            }

            Type logHelperType = Utility.Assembly.GetType(mLogHelperTypeName);
            if (logHelperType == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Can not find log helper type '{0}'.", mLogHelperTypeName));
            }

            GameFrameworkLog.ILogHelper logHelper = (GameFrameworkLog.ILogHelper)Activator.CreateInstance(logHelperType);
            if (logHelper == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Can not create log helper instance '{0}'.", mLogHelperTypeName));
            }

            GameFrameworkLog.SetLogHelper(logHelper);
        }

        private void InitCompressionHelper()
        {
            if (string.IsNullOrEmpty(mCompressionHelperTypeName))
            {
                return;
            }

            Type compressionHelperType = Utility.Assembly.GetType(mCompressionHelperTypeName);
            if (compressionHelperType == null)
            {
                Log.Error("Can not find compression helper type '{0}'.", mCompressionHelperTypeName);
                return;
            }

            Utility.Compression.ICompressionHelper compressionHelper = (Utility.Compression.ICompressionHelper)Activator.CreateInstance(compressionHelperType);
            if (compressionHelper == null)
            {
                Log.Error("Can not create compression helper instance '{0}'.", mCompressionHelperTypeName);
                return;
            }

            Utility.Compression.SetCompressionHelper(compressionHelper);
        }

        private void InitJsonHelper()
        {
            if (string.IsNullOrEmpty(mJsonHelperTypeName))
            {
                return;
            }

            Type jsonHelperType = Utility.Assembly.GetType(mJsonHelperTypeName);
            if (jsonHelperType == null)
            {
                Log.Error("Can not find JSON helper type '{0}'.", mJsonHelperTypeName);
                return;
            }

            Utility.Json.IJsonHelper jsonHelper = (Utility.Json.IJsonHelper)Activator.CreateInstance(jsonHelperType);
            if (jsonHelper == null)
            {
                Log.Error("Can not create JSON helper instance '{0}'.", mJsonHelperTypeName);
                return;
            }

            Utility.Json.SetJsonHelper(jsonHelper);
        }

        private void OnLowMemory()
        {
            Log.Info("Low memory reported...");

            ObjectPoolComponent objectPoolComponent = GameEntry.GetComponent<ObjectPoolComponent>();
            if (objectPoolComponent != null)
            {
                objectPoolComponent.ReleaseAllUnused();
            }

            ResourceComponent resourceCompoent = GameEntry.GetComponent<ResourceComponent>();
            if (resourceCompoent != null)
            {
                resourceCompoent.ForceUnloadUnusedAssets(true);
            }
        }
    }
}
