//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Sound;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 默认声音代理辅助器。
    /// </summary>
    public class DefaultSoundAgentHelper : SoundAgentHelperBase
    {
        private Transform _cachedTransform = null;
        private AudioSource _audioSource = null;
        private EntityLogic _bindingEntityLogic = null;
        private float _volumeWhenPause = 0f;
        private bool _applicationPauseFlag = false;
        private EventHandler<ResetSoundAgentEventArgs> _resetSoundAgentEventHandler = null;

        /// <summary>
        /// 获取当前是否正在播放。
        /// </summary>
        public override bool IsPlaying => _audioSource.isPlaying;

        /// <summary>
        /// 获取声音长度。
        /// </summary>
        public override float Length => _audioSource.clip != null ? _audioSource.clip.length : 0f;

        /// <summary>
        /// 获取或设置播放位置。
        /// </summary>
        public override float Time
        {
            get => _audioSource.time;
            set => _audioSource.time = value;
        }

        /// <summary>
        /// 获取或设置是否静音。
        /// </summary>
        public override bool Mute
        {
            get => _audioSource.mute;
            set => _audioSource.mute = value;
        }

        /// <summary>
        /// 获取或设置是否循环播放。
        /// </summary>
        public override bool Loop
        {
            get => _audioSource.loop;
            set => _audioSource.loop = value;
        }

        /// <summary>
        /// 获取或设置声音优先级。
        /// </summary>
        public override int Priority
        {
            get => 128 - _audioSource.priority;
            set => _audioSource.priority = 128 - value;
        }

        /// <summary>
        /// 获取或设置音量大小。
        /// </summary>
        public override float Volume
        {
            get => _audioSource.volume;
            set => _audioSource.volume = value;
        }

        /// <summary>
        /// 获取或设置声音音调。
        /// </summary>
        public override float Pitch
        {
            get => _audioSource.pitch;
            set => _audioSource.pitch = value;
        }

        /// <summary>
        /// 获取或设置声音立体声声相。
        /// </summary>
        public override float PanStereo
        {
            get => _audioSource.panStereo;
            set => _audioSource.panStereo = value;
        }

        /// <summary>
        /// 获取或设置声音空间混合量。
        /// </summary>
        public override float SpatialBlend
        {
            get => _audioSource.spatialBlend;
            set => _audioSource.spatialBlend = value;
        }

        /// <summary>
        /// 获取或设置声音最大距离。
        /// </summary>
        public override float MaxDistance
        {
            get => _audioSource.maxDistance;

            set => _audioSource.maxDistance = value;
        }

        /// <summary>
        /// 获取或设置声音多普勒等级。
        /// </summary>
        public override float DopplerLevel
        {
            get => _audioSource.dopplerLevel;
            set => _audioSource.dopplerLevel = value;
        }

        /// <summary>
        /// 获取或设置声音代理辅助器所在的混音组。
        /// </summary>
        public override AudioMixerGroup AudioMixerGroup
        {
            get => _audioSource.outputAudioMixerGroup;
            set => _audioSource.outputAudioMixerGroup = value;
        }

        /// <summary>
        /// 重置声音代理事件。
        /// </summary>
        public override event EventHandler<ResetSoundAgentEventArgs> ResetSoundAgent
        {
            add => _resetSoundAgentEventHandler += value;
            remove => _resetSoundAgentEventHandler -= value;
        }

        /// <summary>
        /// 播放声音。
        /// </summary>
        /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
        public override void Play(float fadeInSeconds)
        {
            StopAllCoroutines();

            _audioSource.Play();
            if (fadeInSeconds > 0f)
            {
                float volume = _audioSource.volume;
                _audioSource.volume = 0f;
                StartCoroutine(FadeToVolume(_audioSource, volume, fadeInSeconds));
            }
        }

        /// <summary>
        /// 停止播放声音。
        /// </summary>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public override void Stop(float fadeOutSeconds)
        {
            StopAllCoroutines();

            if (fadeOutSeconds > 0f && gameObject.activeInHierarchy)
            {
                StartCoroutine(StopCo(fadeOutSeconds));
            }
            else
            {
                _audioSource.Stop();
            }
        }

        /// <summary>
        /// 暂停播放声音。
        /// </summary>
        /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
        public override void Pause(float fadeOutSeconds)
        {
            StopAllCoroutines();

            _volumeWhenPause = _audioSource.volume;
            if (fadeOutSeconds > 0f && gameObject.activeInHierarchy)
            {
                StartCoroutine(PauseCo(fadeOutSeconds));
            }
            else
            {
                _audioSource.Pause();
            }
        }

        /// <summary>
        /// 恢复播放声音。
        /// </summary>
        /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
        public override void Resume(float fadeInSeconds)
        {
            StopAllCoroutines();

            _audioSource.UnPause();
            if (fadeInSeconds > 0f)
            {
                StartCoroutine(FadeToVolume(_audioSource, _volumeWhenPause, fadeInSeconds));
            }
            else
            {
                _audioSource.volume = _volumeWhenPause;
            }
        }

        /// <summary>
        /// 重置声音代理辅助器。
        /// </summary>
        public override void Reset()
        {
            _cachedTransform.localPosition = Vector3.zero;
            _audioSource.clip = null;
            _bindingEntityLogic = null;
            _volumeWhenPause = 0f;
        }

        /// <summary>
        /// 设置声音资源。
        /// </summary>
        /// <param name="soundAsset">声音资源。</param>
        /// <returns>是否设置声音资源成功。</returns>
        public override bool SetSoundAsset(object soundAsset)
        {
            AudioClip audioClip = soundAsset as AudioClip;
            if (audioClip == null)
            {
                return false;
            }

            _audioSource.clip = audioClip;
            return true;
        }

        /// <summary>
        /// 设置声音绑定的实体。
        /// </summary>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        public override void SetBindingEntity(Entity bindingEntity)
        {
            _bindingEntityLogic = bindingEntity.Logic;
            if (_bindingEntityLogic != null)
            {
                UpdateAgentPosition();
                return;
            }

            if (_resetSoundAgentEventHandler != null)
            {
                ResetSoundAgentEventArgs resetSoundAgentEventArgs = ResetSoundAgentEventArgs.Create();
                _resetSoundAgentEventHandler(this, resetSoundAgentEventArgs);
                ReferencePool.Release(resetSoundAgentEventArgs);
            }
        }

        /// <summary>
        /// 设置声音所在的世界坐标。
        /// </summary>
        /// <param name="worldPosition">声音所在的世界坐标。</param>
        public override void SetWorldPosition(Vector3 worldPosition)
        {
            _cachedTransform.position = worldPosition;
        }

        private void Awake()
        {
            _cachedTransform = transform;
            _audioSource = gameObject.GetOrAddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.rolloffMode = AudioRolloffMode.Custom;
        }

        private void Update()
        {
            if (!_applicationPauseFlag && !IsPlaying && _audioSource.clip != null && _resetSoundAgentEventHandler != null)
            {
                ResetSoundAgentEventArgs resetSoundAgentEventArgs = ResetSoundAgentEventArgs.Create();
                _resetSoundAgentEventHandler(this, resetSoundAgentEventArgs);
                ReferencePool.Release(resetSoundAgentEventArgs);
                return;
            }

            if (_bindingEntityLogic != null)
            {
                UpdateAgentPosition();
            }
        }

        private void OnApplicationPause(bool pause)
        {
            _applicationPauseFlag = pause;
        }

        private void UpdateAgentPosition()
        {
            if (_bindingEntityLogic.Available)
            {
                _cachedTransform.position = _bindingEntityLogic.CachedTransform.position;
                return;
            }

            if (_resetSoundAgentEventHandler != null)
            {
                ResetSoundAgentEventArgs resetSoundAgentEventArgs = ResetSoundAgentEventArgs.Create();
                _resetSoundAgentEventHandler(this, resetSoundAgentEventArgs);
                ReferencePool.Release(resetSoundAgentEventArgs);
            }
        }

        private IEnumerator StopCo(float fadeOutSeconds)
        {
            yield return FadeToVolume(_audioSource, 0f, fadeOutSeconds);
            _audioSource.Stop();
        }

        private IEnumerator PauseCo(float fadeOutSeconds)
        {
            yield return FadeToVolume(_audioSource, 0f, fadeOutSeconds);
            _audioSource.Pause();
        }

        private IEnumerator FadeToVolume(AudioSource audioSource, float volume, float duration)
        {
            float time = 0f;
            float originalVolume = audioSource.volume;
            while (time < duration)
            {
                time += UnityEngine.Time.deltaTime;
                audioSource.volume = Mathf.Lerp(originalVolume, volume, time / duration);
                yield return new WaitForEndOfFrame();
            }

            audioSource.volume = volume;
        }
    }
}
