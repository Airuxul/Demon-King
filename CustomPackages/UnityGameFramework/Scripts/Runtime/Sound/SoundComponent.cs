//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Resource;
#if UNITY_5_3
using GameFramework.Scene;
#endif
using GameFramework.Sound;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 声音组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Sound")]
    public sealed partial class SoundComponent : GameFrameworkComponent
    {
        private const int DefaultPriority = 0;

        private ISoundManager _soundManager = null;
        private EventComponent _eventComponent = null;
        private AudioListener _audioListener = null;

        [FormerlySerializedAs("_EnablePlaySoundUpdateEvent")] [SerializeField]
        private bool enablePlaySoundUpdateEvent = false;

        [FormerlySerializedAs("_EnablePlaySoundDependencyAssetEvent")] [SerializeField]
        private bool enablePlaySoundDependencyAssetEvent = false;

        [FormerlySerializedAs("_InstanceRoot")] [SerializeField]
        private Transform instanceRoot = null;

        [FormerlySerializedAs("_AudioMixer")] [SerializeField]
        private AudioMixer audioMixer = null;

        [FormerlySerializedAs("_SoundHelperTypeName")] [SerializeField]
        private string soundHelperTypeName = "UnityGameFramework.Runtime.DefaultSoundHelper";

        [FormerlySerializedAs("_CustomSoundHelper")] [SerializeField]
        private SoundHelperBase customSoundHelper = null;

        [FormerlySerializedAs("_SoundGroupHelperTypeName")] [SerializeField]
        private string soundGroupHelperTypeName = "UnityGameFramework.Runtime.DefaultSoundGroupHelper";

        [FormerlySerializedAs("_CustomSoundGroupHelper")] [SerializeField]
        private SoundGroupHelperBase customSoundGroupHelper = null;

        [FormerlySerializedAs("_SoundAgentHelperTypeName")] [SerializeField]
        private string soundAgentHelperTypeName = "UnityGameFramework.Runtime.DefaultSoundAgentHelper";

        [FormerlySerializedAs("_CustomSoundAgentHelper")] [SerializeField]
        private SoundAgentHelperBase customSoundAgentHelper = null;

        [FormerlySerializedAs("_SoundGroups")] [SerializeField]
        private SoundGroup[] soundGroups = null;

        /// <summary>
        /// 获取声音组数量。
        /// </summary>
        public int SoundGroupCount => _soundManager.SoundGroupCount;

        /// <summary>
        /// 获取声音混响器。
        /// </summary>
        public AudioMixer AudioMixer => audioMixer;

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _soundManager = GameFrameworkEntry.GetModule<ISoundManager>();
            if (_soundManager == null)
            {
                Log.Fatal("Sound manager is invalid.");
                return;
            }

            _soundManager.PlaySoundSuccess += OnPlaySoundSuccess;
            _soundManager.PlaySoundFailure += OnPlaySoundFailure;

            if (enablePlaySoundUpdateEvent)
            {
                _soundManager.PlaySoundUpdate += OnPlaySoundUpdate;
            }

            if (enablePlaySoundDependencyAssetEvent)
            {
                _soundManager.PlaySoundDependencyAsset += OnPlaySoundDependencyAsset;
            }

            _audioListener = gameObject.GetOrAddComponent<AudioListener>();

#if UNITY_5_4_OR_NEWER
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
#else
            ISceneManager sceneManager = GameFrameworkEntry.GetModule<ISceneManager>();
            if (sceneManager == null)
            {
                Log.Fatal("Scene manager is invalid.");
                return;
            }

            sceneManager.LoadSceneSuccess += OnLoadSceneSuccess;
            sceneManager.LoadSceneFailure += OnLoadSceneFailure;
            sceneManager.UnloadSceneSuccess += OnUnloadSceneSuccess;
            sceneManager.UnloadSceneFailure += OnUnloadSceneFailure;
#endif
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
                _soundManager.SetResourceManager(baseComponent.EditorResourceHelper);
            }
            else
            {
                _soundManager.SetResourceManager(GameFrameworkEntry.GetModule<IResourceManager>());
            }

            SoundHelperBase soundHelper = Helper.CreateHelper(soundHelperTypeName, customSoundHelper);
            if (soundHelper == null)
            {
                Log.Error("Can not create sound helper.");
                return;
            }

            soundHelper.name = "Sound Helper";
            Transform transform = soundHelper.transform;
            transform.SetParent(this.transform);
            transform.localScale = Vector3.one;

            _soundManager.SetSoundHelper(soundHelper);

            if (instanceRoot == null)
            {
                instanceRoot = new GameObject("Sound Instances").transform;
                instanceRoot.SetParent(gameObject.transform);
                instanceRoot.localScale = Vector3.one;
            }

            for (int i = 0; i < soundGroups.Length; i++)
            {
                if (!AddSoundGroup(soundGroups[i].Name, soundGroups[i].AvoidBeingReplacedBySamePriority, soundGroups[i].Mute, soundGroups[i].Volume, soundGroups[i].AgentHelperCount))
                {
                    Log.Warning("Add sound group '{0}' failure.", soundGroups[i].Name);
                    continue;
                }
            }
        }

        private void OnDestroy()
        {
#if UNITY_5_4_OR_NEWER
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
#endif
        }

        /// <summary>
        /// 是否存在指定声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>指定声音组是否存在。</returns>
        public bool HasSoundGroup(string soundGroupName)
        {
            return _soundManager.HasSoundGroup(soundGroupName);
        }

        /// <summary>
        /// 获取指定声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>要获取的声音组。</returns>
        public ISoundGroup GetSoundGroup(string soundGroupName)
        {
            return _soundManager.GetSoundGroup(soundGroupName);
        }

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <returns>所有声音组。</returns>
        public ISoundGroup[] GetAllSoundGroups()
        {
            return _soundManager.GetAllSoundGroups();
        }

        /// <summary>
        /// 获取所有声音组。
        /// </summary>
        /// <param name="results">所有声音组。</param>
        public void GetAllSoundGroups(List<ISoundGroup> results)
        {
            _soundManager.GetAllSoundGroups(results);
        }

        /// <summary>
        /// 增加声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="soundAgentHelperCount">声音代理辅助器数量。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(string soundGroupName, int soundAgentHelperCount)
        {
            return AddSoundGroup(soundGroupName, false, false, 1f, soundAgentHelperCount);
        }

        /// <summary>
        /// 增加声音组。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="soundGroupAvoidBeingReplacedBySamePriority">声音组中的声音是否避免被同优先级声音替换。</param>
        /// <param name="soundGroupMute">声音组是否静音。</param>
        /// <param name="soundGroupVolume">声音组音量。</param>
        /// <param name="soundAgentHelperCount">声音代理辅助器数量。</param>
        /// <returns>是否增加声音组成功。</returns>
        public bool AddSoundGroup(string soundGroupName, bool soundGroupAvoidBeingReplacedBySamePriority, bool soundGroupMute, float soundGroupVolume, int soundAgentHelperCount)
        {
            if (_soundManager.HasSoundGroup(soundGroupName))
            {
                return false;
            }

            SoundGroupHelperBase soundGroupHelper = Helper.CreateHelper(soundGroupHelperTypeName, customSoundGroupHelper, SoundGroupCount);
            if (soundGroupHelper == null)
            {
                Log.Error("Can not create sound group helper.");
                return false;
            }

            soundGroupHelper.name = Utility.Text.Format("Sound Group - {0}", soundGroupName);
            Transform transform = soundGroupHelper.transform;
            transform.SetParent(instanceRoot);
            transform.localScale = Vector3.one;

            if (audioMixer != null)
            {
                AudioMixerGroup[] audioMixerGroups = audioMixer.FindMatchingGroups(Utility.Text.Format("Master/{0}", soundGroupName));
                if (audioMixerGroups.Length > 0)
                {
                    soundGroupHelper.AudioMixerGroup = audioMixerGroups[0];
                }
                else
                {
                    soundGroupHelper.AudioMixerGroup = audioMixer.FindMatchingGroups("Master")[0];
                }
            }

            if (!_soundManager.AddSoundGroup(soundGroupName, soundGroupAvoidBeingReplacedBySamePriority, soundGroupMute, soundGroupVolume, soundGroupHelper))
            {
                return false;
            }

            for (int i = 0; i < soundAgentHelperCount; i++)
            {
                if (!AddSoundAgentHelper(soundGroupName, soundGroupHelper, i))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取所有正在加载声音的序列编号。
        /// </summary>
        /// <returns>所有正在加载声音的序列编号。</returns>
        public int[] GetAllLoadingSoundSerialIds()
        {
            return _soundManager.GetAllLoadingSoundSerialIds();
        }

        /// <summary>
        /// 获取所有正在加载声音的序列编号。
        /// </summary>
        /// <param name="results">所有正在加载声音的序列编号。</param>
        public void GetAllLoadingSoundSerialIds(List<int> results)
        {
            _soundManager.GetAllLoadingSoundSerialIds(results);
        }

        /// <summary>
        /// 是否正在加载声音。
        /// </summary>
        /// <param name="serialId">声音序列编号。</param>
        /// <returns>是否正在加载声音。</returns>
        public bool IsLoadingSound(int serialId)
        {
            return _soundManager.IsLoadingSound(serialId);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName)
        {
            return PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, null, null);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, null, null, null);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, PlaySoundParams playSoundParams)
        {
            return PlaySound(soundAssetName, soundGroupName, DefaultPriority, playSoundParams, null, null);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, Entity bindingEntity)
        {
            return PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, bindingEntity, null);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="worldPosition">声音所在的世界坐标。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, Vector3 worldPosition)
        {
            return PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, worldPosition, null);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, object userData)
        {
            return PlaySound(soundAssetName, soundGroupName, DefaultPriority, null, null, userData);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, null, null);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, object userData)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, null, userData);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Entity bindingEntity)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, bindingEntity, null);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Entity bindingEntity, object userData)
        {
            return _soundManager.PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, PlaySoundInfo.Create(bindingEntity, Vector3.zero, userData));
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="worldPosition">声音所在的世界坐标。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Vector3 worldPosition)
        {
            return PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, worldPosition, null);
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="priority">加载声音资源的优先级。</param>
        /// <param name="playSoundParams">播放声音参数。</param>
        /// <param name="worldPosition">声音所在的世界坐标。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>声音的序列编号。</returns>
        public int PlaySound(string soundAssetName, string soundGroupName, int priority, PlaySoundParams playSoundParams, Vector3 worldPosition, object userData)
        {
            return _soundManager.PlaySound(soundAssetName, soundGroupName, priority, playSoundParams, PlaySoundInfo.Create(null, worldPosition, userData));
        }

        /// <summary>
        /// 停止播放声音。
        /// </summary>
        /// <param name="serialId">要停止播放声音的序列编号。</param>
        /// <returns>是否停止播放声音成功。</returns>
        public bool StopSound(int serialId)
        {
            return _soundManager.StopSound(serialId);
        }

        /// <summary>
        /// 停止播放声音。
        /// </summary>
        /// <param name="serialId">要停止播放声音的序列编号。</param>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        /// <returns>是否停止播放声音成功。</returns>
        public bool StopSound(int serialId, float fadeOutSeconds)
        {
            return _soundManager.StopSound(serialId, fadeOutSeconds);
        }

        /// <summary>
        /// 停止所有已加载的声音。
        /// </summary>
        public void StopAllLoadedSounds()
        {
            _soundManager.StopAllLoadedSounds();
        }

        /// <summary>
        /// 停止所有已加载的声音。
        /// </summary>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public void StopAllLoadedSounds(float fadeOutSeconds)
        {
            _soundManager.StopAllLoadedSounds(fadeOutSeconds);
        }

        /// <summary>
        /// 停止所有正在加载的声音。
        /// </summary>
        public void StopAllLoadingSounds()
        {
            _soundManager.StopAllLoadingSounds();
        }

        /// <summary>
        /// 暂停播放声音。
        /// </summary>
        /// <param name="serialId">要暂停播放声音的序列编号。</param>
        public void PauseSound(int serialId)
        {
            _soundManager.PauseSound(serialId);
        }

        /// <summary>
        /// 暂停播放声音。
        /// </summary>
        /// <param name="serialId">要暂停播放声音的序列编号。</param>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public void PauseSound(int serialId, float fadeOutSeconds)
        {
            _soundManager.PauseSound(serialId, fadeOutSeconds);
        }

        /// <summary>
        /// 恢复播放声音。
        /// </summary>
        /// <param name="serialId">要恢复播放声音的序列编号。</param>
        public void ResumeSound(int serialId)
        {
            _soundManager.ResumeSound(serialId);
        }

        /// <summary>
        /// 恢复播放声音。
        /// </summary>
        /// <param name="serialId">要恢复播放声音的序列编号。</param>
        /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
        public void ResumeSound(int serialId, float fadeInSeconds)
        {
            _soundManager.ResumeSound(serialId, fadeInSeconds);
        }

        /// <summary>
        /// 增加声音代理辅助器。
        /// </summary>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="soundGroupHelper">声音组辅助器。</param>
        /// <param name="index">声音代理辅助器索引。</param>
        /// <returns>是否增加声音代理辅助器成功。</returns>
        private bool AddSoundAgentHelper(string soundGroupName, SoundGroupHelperBase soundGroupHelper, int index)
        {
            SoundAgentHelperBase soundAgentHelper = Helper.CreateHelper(soundAgentHelperTypeName, customSoundAgentHelper, index);
            if (soundAgentHelper == null)
            {
                Log.Error("Can not create sound agent helper.");
                return false;
            }

            soundAgentHelper.name = Utility.Text.Format("Sound Agent Helper - {0} - {1}", soundGroupName, index);
            Transform transform = soundAgentHelper.transform;
            transform.SetParent(soundGroupHelper.transform);
            transform.localScale = Vector3.one;

            if (audioMixer != null)
            {
                AudioMixerGroup[] audioMixerGroups = audioMixer.FindMatchingGroups(Utility.Text.Format("Master/{0}/{1}", soundGroupName, index));
                if (audioMixerGroups.Length > 0)
                {
                    soundAgentHelper.AudioMixerGroup = audioMixerGroups[0];
                }
                else
                {
                    soundAgentHelper.AudioMixerGroup = soundGroupHelper.AudioMixerGroup;
                }
            }

            _soundManager.AddSoundAgentHelper(soundGroupName, soundAgentHelper);

            return true;
        }

        private void OnPlaySoundSuccess(object sender, GameFramework.Sound.PlaySoundSuccessEventArgs e)
        {
            PlaySoundInfo playSoundInfo = (PlaySoundInfo)e.UserData;
            if (playSoundInfo != null)
            {
                SoundAgentHelperBase soundAgentHelper = (SoundAgentHelperBase)e.SoundAgent.Helper;
                if (playSoundInfo.BindingEntity != null)
                {
                    soundAgentHelper.SetBindingEntity(playSoundInfo.BindingEntity);
                }
                else
                {
                    soundAgentHelper.SetWorldPosition(playSoundInfo.WorldPosition);
                }
            }

            _eventComponent.Fire(this, PlaySoundSuccessEventArgs.Create(e));
        }

        private void OnPlaySoundFailure(object sender, GameFramework.Sound.PlaySoundFailureEventArgs e)
        {
            string logMessage = Utility.Text.Format("Play sound failure, asset name '{0}', sound group name '{1}', error code '{2}', error message '{3}'.", e.SoundAssetName, e.SoundGroupName, e.ErrorCode, e.ErrorMessage);
            if (e.ErrorCode == PlaySoundErrorCode.IgnoredDueToLowPriority)
            {
                Log.Info(logMessage);
            }
            else
            {
                Log.Warning(logMessage);
            }

            _eventComponent.Fire(this, PlaySoundFailureEventArgs.Create(e));
        }

        private void OnPlaySoundUpdate(object sender, GameFramework.Sound.PlaySoundUpdateEventArgs e)
        {
            _eventComponent.Fire(this, PlaySoundUpdateEventArgs.Create(e));
        }

        private void OnPlaySoundDependencyAsset(object sender, GameFramework.Sound.PlaySoundDependencyAssetEventArgs e)
        {
            _eventComponent.Fire(this, PlaySoundDependencyAssetEventArgs.Create(e));
        }

        private void OnLoadSceneSuccess(object sender, GameFramework.Scene.LoadSceneSuccessEventArgs e)
        {
            RefreshAudioListener();
        }

        private void OnLoadSceneFailure(object sender, GameFramework.Scene.LoadSceneFailureEventArgs e)
        {
            RefreshAudioListener();
        }

        private void OnUnloadSceneSuccess(object sender, GameFramework.Scene.UnloadSceneSuccessEventArgs e)
        {
            RefreshAudioListener();
        }

        private void OnUnloadSceneFailure(object sender, GameFramework.Scene.UnloadSceneFailureEventArgs e)
        {
            RefreshAudioListener();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            RefreshAudioListener();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            RefreshAudioListener();
        }

        private void RefreshAudioListener()
        {
            _audioListener.enabled = FindObjectsOfType<AudioListener>().Length <= 1;
        }
    }
}
