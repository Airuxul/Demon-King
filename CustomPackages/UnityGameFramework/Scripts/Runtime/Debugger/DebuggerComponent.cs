//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Debugger;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 调试器组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Debugger")]
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 默认调试器漂浮框大小。
        /// </summary>
        internal static readonly Rect DefaultIconRect = new Rect(10f, 10f, 60f, 60f);

        /// <summary>
        /// 默认调试器窗口大小。
        /// </summary>
        internal static readonly Rect DefaultWindowRect = new Rect(10f, 10f, 640f, 480f);

        /// <summary>
        /// 默认调试器窗口缩放比例。
        /// </summary>
        internal static readonly float DefaultWindowScale = 1f;

        private static readonly TextEditor STextEditor = new TextEditor();
        private IDebuggerManager _debuggerManager = null;
        private Rect _dragRect = new Rect(0f, 0f, float.MaxValue, 25f);
        private Rect _iconRect = DefaultIconRect;
        private Rect _windowRect = DefaultWindowRect;
        private float _windowScale = DefaultWindowScale;

        [FormerlySerializedAs("_Skin")] [SerializeField]
        private GUISkin skin = null;

        [FormerlySerializedAs("_ActiveWindow")] [SerializeField]
        private DebuggerActiveWindowType activeWindow = DebuggerActiveWindowType.AlwaysOpen;

        [FormerlySerializedAs("_ShowFullWindow")] [SerializeField]
        private bool showFullWindow = false;

        [FormerlySerializedAs("_ConsoleWindow")] [SerializeField]
        private ConsoleWindow consoleWindow = new ConsoleWindow();

        private SystemInformationWindow _systemInformationWindow = new SystemInformationWindow();
        private EnvironmentInformationWindow _environmentInformationWindow = new EnvironmentInformationWindow();
        private ScreenInformationWindow _screenInformationWindow = new ScreenInformationWindow();
        private GraphicsInformationWindow _graphicsInformationWindow = new GraphicsInformationWindow();
        private InputSummaryInformationWindow _inputSummaryInformationWindow = new InputSummaryInformationWindow();
        private InputTouchInformationWindow _inputTouchInformationWindow = new InputTouchInformationWindow();
        private InputLocationInformationWindow _inputLocationInformationWindow = new InputLocationInformationWindow();
        private InputAccelerationInformationWindow _inputAccelerationInformationWindow = new InputAccelerationInformationWindow();
        private InputGyroscopeInformationWindow _inputGyroscopeInformationWindow = new InputGyroscopeInformationWindow();
        private InputCompassInformationWindow _inputCompassInformationWindow = new InputCompassInformationWindow();
        private PathInformationWindow _pathInformationWindow = new PathInformationWindow();
        private SceneInformationWindow _sceneInformationWindow = new SceneInformationWindow();
        private TimeInformationWindow _timeInformationWindow = new TimeInformationWindow();
        private QualityInformationWindow _qualityInformationWindow = new QualityInformationWindow();
        private ProfilerInformationWindow _profilerInformationWindow = new ProfilerInformationWindow();
        private WebPlayerInformationWindow _webPlayerInformationWindow = new WebPlayerInformationWindow();
        private RuntimeMemorySummaryWindow _runtimeMemorySummaryWindow = new RuntimeMemorySummaryWindow();
        private RuntimeMemoryInformationWindow<Object> _runtimeMemoryAllInformationWindow = new RuntimeMemoryInformationWindow<Object>();
        private RuntimeMemoryInformationWindow<Texture> _runtimeMemoryTextureInformationWindow = new RuntimeMemoryInformationWindow<Texture>();
        private RuntimeMemoryInformationWindow<Mesh> _runtimeMemoryMeshInformationWindow = new RuntimeMemoryInformationWindow<Mesh>();
        private RuntimeMemoryInformationWindow<Material> _runtimeMemoryMaterialInformationWindow = new RuntimeMemoryInformationWindow<Material>();
        private RuntimeMemoryInformationWindow<Shader> _runtimeMemoryShaderInformationWindow = new RuntimeMemoryInformationWindow<Shader>();
        private RuntimeMemoryInformationWindow<AnimationClip> _runtimeMemoryAnimationClipInformationWindow = new RuntimeMemoryInformationWindow<AnimationClip>();
        private RuntimeMemoryInformationWindow<AudioClip> _runtimeMemoryAudioClipInformationWindow = new RuntimeMemoryInformationWindow<AudioClip>();
        private RuntimeMemoryInformationWindow<Font> _runtimeMemoryFontInformationWindow = new RuntimeMemoryInformationWindow<Font>();
        private RuntimeMemoryInformationWindow<TextAsset> _runtimeMemoryTextAssetInformationWindow = new RuntimeMemoryInformationWindow<TextAsset>();
        private RuntimeMemoryInformationWindow<ScriptableObject> _runtimeMemoryScriptableObjectInformationWindow = new RuntimeMemoryInformationWindow<ScriptableObject>();
        private ObjectPoolInformationWindow _objectPoolInformationWindow = new ObjectPoolInformationWindow();
        private ReferencePoolInformationWindow _referencePoolInformationWindow = new ReferencePoolInformationWindow();
        private NetworkInformationWindow _networkInformationWindow = new NetworkInformationWindow();
        private SettingsWindow _settingsWindow = new SettingsWindow();
        private OperationsWindow _operationsWindow = new OperationsWindow();

        private FpsCounter _fpsCounter = null;

        /// <summary>
        /// 获取或设置调试器窗口是否激活。
        /// </summary>
        public bool ActiveWindow
        {
            get => _debuggerManager.ActiveWindow;
            set
            {
                _debuggerManager.ActiveWindow = value;
                enabled = value;
            }
        }

        /// <summary>
        /// 获取或设置是否显示完整调试器界面。
        /// </summary>
        public bool ShowFullWindow
        {
            get => showFullWindow;
            set => showFullWindow = value;
        }

        /// <summary>
        /// 获取或设置调试器漂浮框大小。
        /// </summary>
        public Rect IconRect
        {
            get => _iconRect;
            set => _iconRect = value;
        }

        /// <summary>
        /// 获取或设置调试器窗口大小。
        /// </summary>
        public Rect WindowRect
        {
            get => _windowRect;
            set => _windowRect = value;
        }

        /// <summary>
        /// 获取或设置调试器窗口缩放比例。
        /// </summary>
        public float WindowScale
        {
            get => _windowScale;
            set => _windowScale = value;
        }

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _debuggerManager = GameFrameworkEntry.GetModule<IDebuggerManager>();
            if (_debuggerManager == null)
            {
                Log.Fatal("Debugger manager is invalid.");
                return;
            }

            _fpsCounter = new FpsCounter(0.5f);
        }

        private void Start()
        {
            RegisterDebuggerWindow("Console", consoleWindow);
            RegisterDebuggerWindow("Information/System", _systemInformationWindow);
            RegisterDebuggerWindow("Information/Environment", _environmentInformationWindow);
            RegisterDebuggerWindow("Information/Screen", _screenInformationWindow);
            RegisterDebuggerWindow("Information/Graphics", _graphicsInformationWindow);
            RegisterDebuggerWindow("Information/Input/Summary", _inputSummaryInformationWindow);
            RegisterDebuggerWindow("Information/Input/Touch", _inputTouchInformationWindow);
            RegisterDebuggerWindow("Information/Input/Location", _inputLocationInformationWindow);
            RegisterDebuggerWindow("Information/Input/Acceleration", _inputAccelerationInformationWindow);
            RegisterDebuggerWindow("Information/Input/Gyroscope", _inputGyroscopeInformationWindow);
            RegisterDebuggerWindow("Information/Input/Compass", _inputCompassInformationWindow);
            RegisterDebuggerWindow("Information/Other/Scene", _sceneInformationWindow);
            RegisterDebuggerWindow("Information/Other/Path", _pathInformationWindow);
            RegisterDebuggerWindow("Information/Other/Time", _timeInformationWindow);
            RegisterDebuggerWindow("Information/Other/Quality", _qualityInformationWindow);
            RegisterDebuggerWindow("Information/Other/Web Player", _webPlayerInformationWindow);
            RegisterDebuggerWindow("Profiler/Summary", _profilerInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Summary", _runtimeMemorySummaryWindow);
            RegisterDebuggerWindow("Profiler/Memory/All", _runtimeMemoryAllInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Texture", _runtimeMemoryTextureInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Mesh", _runtimeMemoryMeshInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Material", _runtimeMemoryMaterialInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Shader", _runtimeMemoryShaderInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/AnimationClip", _runtimeMemoryAnimationClipInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/AudioClip", _runtimeMemoryAudioClipInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/Font", _runtimeMemoryFontInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/TextAsset", _runtimeMemoryTextAssetInformationWindow);
            RegisterDebuggerWindow("Profiler/Memory/ScriptableObject", _runtimeMemoryScriptableObjectInformationWindow);
            RegisterDebuggerWindow("Profiler/Object Pool", _objectPoolInformationWindow);
            RegisterDebuggerWindow("Profiler/Reference Pool", _referencePoolInformationWindow);
            RegisterDebuggerWindow("Profiler/Network", _networkInformationWindow);
            RegisterDebuggerWindow("Other/Settings", _settingsWindow);
            RegisterDebuggerWindow("Other/Operations", _operationsWindow);

            switch (activeWindow)
            {
                case DebuggerActiveWindowType.AlwaysOpen:
                    ActiveWindow = true;
                    break;

                case DebuggerActiveWindowType.OnlyOpenWhenDevelopment:
                    ActiveWindow = Debug.isDebugBuild;
                    break;

                case DebuggerActiveWindowType.OnlyOpenInEditor:
                    ActiveWindow = Application.isEditor;
                    break;

                default:
                    ActiveWindow = false;
                    break;
            }
        }

        private void Update()
        {
            _fpsCounter.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (_debuggerManager == null || !_debuggerManager.ActiveWindow)
            {
                return;
            }

            GUISkin cachedGuiSkin = GUI.skin;
            Matrix4x4 cachedMatrix = GUI.matrix;

            GUI.skin = skin;
            GUI.matrix = Matrix4x4.Scale(new Vector3(_windowScale, _windowScale, 1f));

            if (showFullWindow)
            {
                _windowRect = GUILayout.Window(0, _windowRect, DrawWindow, "<b>GAME FRAMEWORK DEBUGGER</b>");
            }
            else
            {
                _iconRect = GUILayout.Window(0, _iconRect, DrawDebuggerWindowIcon, "<b>DEBUGGER</b>");
            }

            GUI.matrix = cachedMatrix;
            GUI.skin = cachedGuiSkin;
        }

        /// <summary>
        /// 注册调试器窗口。
        /// </summary>
        /// <param name="path">调试器窗口路径。</param>
        /// <param name="debuggerWindow">要注册的调试器窗口。</param>
        /// <param name="args">初始化调试器窗口参数。</param>
        public void RegisterDebuggerWindow(string path, IDebuggerWindow debuggerWindow, params object[] args)
        {
            _debuggerManager.RegisterDebuggerWindow(path, debuggerWindow, args);
        }

        /// <summary>
        /// 解除注册调试器窗口。
        /// </summary>
        /// <param name="path">调试器窗口路径。</param>
        /// <returns>是否解除注册调试器窗口成功。</returns>
        public bool UnregisterDebuggerWindow(string path)
        {
            return _debuggerManager.UnregisterDebuggerWindow(path);
        }

        /// <summary>
        /// 获取调试器窗口。
        /// </summary>
        /// <param name="path">调试器窗口路径。</param>
        /// <returns>要获取的调试器窗口。</returns>
        public IDebuggerWindow GetDebuggerWindow(string path)
        {
            return _debuggerManager.GetDebuggerWindow(path);
        }

        /// <summary>
        /// 选中调试器窗口。
        /// </summary>
        /// <param name="path">调试器窗口路径。</param>
        /// <returns>是否成功选中调试器窗口。</returns>
        public bool SelectDebuggerWindow(string path)
        {
            return _debuggerManager.SelectDebuggerWindow(path);
        }

        /// <summary>
        /// 还原调试器窗口布局。
        /// </summary>
        public void ResetLayout()
        {
            IconRect = DefaultIconRect;
            WindowRect = DefaultWindowRect;
            WindowScale = DefaultWindowScale;
        }

        /// <summary>
        /// 获取记录的所有日志。
        /// </summary>
        /// <param name="results">要获取的日志。</param>
        public void GetRecentLogs(List<LogNode> results)
        {
            consoleWindow.GetRecentLogs(results);
        }

        /// <summary>
        /// 获取记录的最近日志。
        /// </summary>
        /// <param name="results">要获取的日志。</param>
        /// <param name="count">要获取最近日志的数量。</param>
        public void GetRecentLogs(List<LogNode> results, int count)
        {
            consoleWindow.GetRecentLogs(results, count);
        }

        private void DrawWindow(int windowId)
        {
            GUI.DragWindow(_dragRect);
            DrawDebuggerWindowGroup(_debuggerManager.DebuggerWindowRoot);
        }

        private void DrawDebuggerWindowGroup(IDebuggerWindowGroup debuggerWindowGroup)
        {
            if (debuggerWindowGroup == null)
            {
                return;
            }

            List<string> names = new List<string>();
            string[] debuggerWindowNames = debuggerWindowGroup.GetDebuggerWindowNames();
            for (int i = 0; i < debuggerWindowNames.Length; i++)
            {
                names.Add(Utility.Text.Format("<b>{0}</b>", debuggerWindowNames[i]));
            }

            if (debuggerWindowGroup == _debuggerManager.DebuggerWindowRoot)
            {
                names.Add("<b>Close</b>");
            }

            int toolbarIndex = GUILayout.Toolbar(debuggerWindowGroup.SelectedIndex, names.ToArray(), GUILayout.Height(30f), GUILayout.MaxWidth(Screen.width));
            if (toolbarIndex >= debuggerWindowGroup.DebuggerWindowCount)
            {
                showFullWindow = false;
                return;
            }

            if (debuggerWindowGroup.SelectedWindow == null)
            {
                return;
            }

            if (debuggerWindowGroup.SelectedIndex != toolbarIndex)
            {
                debuggerWindowGroup.SelectedWindow.OnLeave();
                debuggerWindowGroup.SelectedIndex = toolbarIndex;
                debuggerWindowGroup.SelectedWindow.OnEnter();
            }

            IDebuggerWindowGroup subDebuggerWindowGroup = debuggerWindowGroup.SelectedWindow as IDebuggerWindowGroup;
            if (subDebuggerWindowGroup != null)
            {
                DrawDebuggerWindowGroup(subDebuggerWindowGroup);
            }

            debuggerWindowGroup.SelectedWindow.OnDraw();
        }

        private void DrawDebuggerWindowIcon(int windowId)
        {
            GUI.DragWindow(_dragRect);
            GUILayout.Space(5);
            Color32 color = Color.white;
            consoleWindow.RefreshCount();
            if (consoleWindow.FatalCount > 0)
            {
                color = consoleWindow.GetLogStringColor(LogType.Exception);
            }
            else if (consoleWindow.ErrorCount > 0)
            {
                color = consoleWindow.GetLogStringColor(LogType.Error);
            }
            else if (consoleWindow.WarningCount > 0)
            {
                color = consoleWindow.GetLogStringColor(LogType.Warning);
            }
            else
            {
                color = consoleWindow.GetLogStringColor(LogType.Log);
            }

            string title = Utility.Text.Format("<color=#{0:x2}{1:x2}{2:x2}{3:x2}><b>FPS: {4:F2}</b></color>", color.r, color.g, color.b, color.a, _fpsCounter.CurrentFps);
            if (GUILayout.Button(title, GUILayout.Width(100f), GUILayout.Height(40f)))
            {
                showFullWindow = true;
            }
        }

        private static void CopyToClipboard(string content)
        {
            STextEditor.text = content;
            STextEditor.OnFocus();
            STextEditor.Copy();
            STextEditor.text = string.Empty;
        }
    }
}
