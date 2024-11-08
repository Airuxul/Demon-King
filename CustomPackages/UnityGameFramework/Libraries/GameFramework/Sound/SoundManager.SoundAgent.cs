//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;

namespace GameFramework.Sound
{
    internal sealed partial class SoundManager : GameFrameworkModule, ISoundManager
    {
        /// <summary>
        /// 声音代理。
        /// </summary>
        private sealed class SoundAgent : ISoundAgent
        {
            private readonly SoundGroup _SoundGroup;
            private readonly ISoundHelper _SoundHelper;
            private readonly ISoundAgentHelper _SoundAgentHelper;
            private int _SerialId;
            private object _SoundAsset;
            private DateTime _SetSoundAssetTime;
            private bool _MuteInSoundGroup;
            private float _VolumeInSoundGroup;

            /// <summary>
            /// 初始化声音代理的新实例。
            /// </summary>
            /// <param name="soundGroup">所在的声音组。</param>
            /// <param name="soundHelper">声音辅助器接口。</param>
            /// <param name="soundAgentHelper">声音代理辅助器接口。</param>
            public SoundAgent(SoundGroup soundGroup, ISoundHelper soundHelper, ISoundAgentHelper soundAgentHelper)
            {
                if (soundGroup == null)
                {
                    throw new GameFrameworkException("Sound group is invalid.");
                }

                if (soundHelper == null)
                {
                    throw new GameFrameworkException("Sound helper is invalid.");
                }

                if (soundAgentHelper == null)
                {
                    throw new GameFrameworkException("Sound agent helper is invalid.");
                }

                _SoundGroup = soundGroup;
                _SoundHelper = soundHelper;
                _SoundAgentHelper = soundAgentHelper;
                _SoundAgentHelper.ResetSoundAgent += OnResetSoundAgent;
                _SerialId = 0;
                _SoundAsset = null;
                Reset();
            }

            /// <summary>
            /// 获取所在的声音组。
            /// </summary>
            public ISoundGroup SoundGroup
            {
                get
                {
                    return _SoundGroup;
                }
            }

            /// <summary>
            /// 获取或设置声音的序列编号。
            /// </summary>
            public int SerialId
            {
                get
                {
                    return _SerialId;
                }
                set
                {
                    _SerialId = value;
                }
            }

            /// <summary>
            /// 获取当前是否正在播放。
            /// </summary>
            public bool IsPlaying
            {
                get
                {
                    return _SoundAgentHelper.IsPlaying;
                }
            }

            /// <summary>
            /// 获取声音长度。
            /// </summary>
            public float Length
            {
                get
                {
                    return _SoundAgentHelper.Length;
                }
            }

            /// <summary>
            /// 获取或设置播放位置。
            /// </summary>
            public float Time
            {
                get
                {
                    return _SoundAgentHelper.Time;
                }
                set
                {
                    _SoundAgentHelper.Time = value;
                }
            }

            /// <summary>
            /// 获取是否静音。
            /// </summary>
            public bool Mute
            {
                get
                {
                    return _SoundAgentHelper.Mute;
                }
            }

            /// <summary>
            /// 获取或设置在声音组内是否静音。
            /// </summary>
            public bool MuteInSoundGroup
            {
                get
                {
                    return _MuteInSoundGroup;
                }
                set
                {
                    _MuteInSoundGroup = value;
                    RefreshMute();
                }
            }

            /// <summary>
            /// 获取或设置是否循环播放。
            /// </summary>
            public bool Loop
            {
                get
                {
                    return _SoundAgentHelper.Loop;
                }
                set
                {
                    _SoundAgentHelper.Loop = value;
                }
            }

            /// <summary>
            /// 获取或设置声音优先级。
            /// </summary>
            public int Priority
            {
                get
                {
                    return _SoundAgentHelper.Priority;
                }
                set
                {
                    _SoundAgentHelper.Priority = value;
                }
            }

            /// <summary>
            /// 获取音量大小。
            /// </summary>
            public float Volume
            {
                get
                {
                    return _SoundAgentHelper.Volume;
                }
            }

            /// <summary>
            /// 获取或设置在声音组内音量大小。
            /// </summary>
            public float VolumeInSoundGroup
            {
                get
                {
                    return _VolumeInSoundGroup;
                }
                set
                {
                    _VolumeInSoundGroup = value;
                    RefreshVolume();
                }
            }

            /// <summary>
            /// 获取或设置声音音调。
            /// </summary>
            public float Pitch
            {
                get
                {
                    return _SoundAgentHelper.Pitch;
                }
                set
                {
                    _SoundAgentHelper.Pitch = value;
                }
            }

            /// <summary>
            /// 获取或设置声音立体声声相。
            /// </summary>
            public float PanStereo
            {
                get
                {
                    return _SoundAgentHelper.PanStereo;
                }
                set
                {
                    _SoundAgentHelper.PanStereo = value;
                }
            }

            /// <summary>
            /// 获取或设置声音空间混合量。
            /// </summary>
            public float SpatialBlend
            {
                get
                {
                    return _SoundAgentHelper.SpatialBlend;
                }
                set
                {
                    _SoundAgentHelper.SpatialBlend = value;
                }
            }

            /// <summary>
            /// 获取或设置声音最大距离。
            /// </summary>
            public float MaxDistance
            {
                get
                {
                    return _SoundAgentHelper.MaxDistance;
                }
                set
                {
                    _SoundAgentHelper.MaxDistance = value;
                }
            }

            /// <summary>
            /// 获取或设置声音多普勒等级。
            /// </summary>
            public float DopplerLevel
            {
                get
                {
                    return _SoundAgentHelper.DopplerLevel;
                }
                set
                {
                    _SoundAgentHelper.DopplerLevel = value;
                }
            }

            /// <summary>
            /// 获取声音代理辅助器。
            /// </summary>
            public ISoundAgentHelper Helper
            {
                get
                {
                    return _SoundAgentHelper;
                }
            }

            /// <summary>
            /// 获取声音创建时间。
            /// </summary>
            internal DateTime SetSoundAssetTime
            {
                get
                {
                    return _SetSoundAssetTime;
                }
            }

            /// <summary>
            /// 播放声音。
            /// </summary>
            public void Play()
            {
                _SoundAgentHelper.Play(Constant.DefaultFadeInSeconds);
            }

            /// <summary>
            /// 播放声音。
            /// </summary>
            /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
            public void Play(float fadeInSeconds)
            {
                _SoundAgentHelper.Play(fadeInSeconds);
            }

            /// <summary>
            /// 停止播放声音。
            /// </summary>
            public void Stop()
            {
                _SoundAgentHelper.Stop(Constant.DefaultFadeOutSeconds);
            }

            /// <summary>
            /// 停止播放声音。
            /// </summary>
            /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
            public void Stop(float fadeOutSeconds)
            {
                _SoundAgentHelper.Stop(fadeOutSeconds);
            }

            /// <summary>
            /// 暂停播放声音。
            /// </summary>
            public void Pause()
            {
                _SoundAgentHelper.Pause(Constant.DefaultFadeOutSeconds);
            }

            /// <summary>
            /// 暂停播放声音。
            /// </summary>
            /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
            public void Pause(float fadeOutSeconds)
            {
                _SoundAgentHelper.Pause(fadeOutSeconds);
            }

            /// <summary>
            /// 恢复播放声音。
            /// </summary>
            public void Resume()
            {
                _SoundAgentHelper.Resume(Constant.DefaultFadeInSeconds);
            }

            /// <summary>
            /// 恢复播放声音。
            /// </summary>
            /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
            public void Resume(float fadeInSeconds)
            {
                _SoundAgentHelper.Resume(fadeInSeconds);
            }

            /// <summary>
            /// 重置声音代理。
            /// </summary>
            public void Reset()
            {
                if (_SoundAsset != null)
                {
                    _SoundHelper.ReleaseSoundAsset(_SoundAsset);
                    _SoundAsset = null;
                }

                _SetSoundAssetTime = DateTime.MinValue;
                Time = Constant.DefaultTime;
                MuteInSoundGroup = Constant.DefaultMute;
                Loop = Constant.DefaultLoop;
                Priority = Constant.DefaultPriority;
                VolumeInSoundGroup = Constant.DefaultVolume;
                Pitch = Constant.DefaultPitch;
                PanStereo = Constant.DefaultPanStereo;
                SpatialBlend = Constant.DefaultSpatialBlend;
                MaxDistance = Constant.DefaultMaxDistance;
                DopplerLevel = Constant.DefaultDopplerLevel;
                _SoundAgentHelper.Reset();
            }

            internal bool SetSoundAsset(object soundAsset)
            {
                Reset();
                _SoundAsset = soundAsset;
                _SetSoundAssetTime = DateTime.UtcNow;
                return _SoundAgentHelper.SetSoundAsset(soundAsset);
            }

            internal void RefreshMute()
            {
                _SoundAgentHelper.Mute = _SoundGroup.Mute || _MuteInSoundGroup;
            }

            internal void RefreshVolume()
            {
                _SoundAgentHelper.Volume = _SoundGroup.Volume * _VolumeInSoundGroup;
            }

            private void OnResetSoundAgent(object sender, ResetSoundAgentEventArgs e)
            {
                Reset();
            }
        }
    }
}
