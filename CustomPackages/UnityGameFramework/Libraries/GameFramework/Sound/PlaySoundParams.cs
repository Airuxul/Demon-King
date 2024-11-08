//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Sound
{
    /// <summary>
    /// 播放声音参数。
    /// </summary>
    public sealed class PlaySoundParams : IReference
    {
        private bool _Referenced;
        private float _Time;
        private bool _MuteInSoundGroup;
        private bool _Loop;
        private int _Priority;
        private float _VolumeInSoundGroup;
        private float _FadeInSeconds;
        private float _Pitch;
        private float _PanStereo;
        private float _SpatialBlend;
        private float _MaxDistance;
        private float _DopplerLevel;

        /// <summary>
        /// 初始化播放声音参数的新实例。
        /// </summary>
        public PlaySoundParams()
        {
            _Referenced = false;
            _Time = Constant.DefaultTime;
            _MuteInSoundGroup = Constant.DefaultMute;
            _Loop = Constant.DefaultLoop;
            _Priority = Constant.DefaultPriority;
            _VolumeInSoundGroup = Constant.DefaultVolume;
            _FadeInSeconds = Constant.DefaultFadeInSeconds;
            _Pitch = Constant.DefaultPitch;
            _PanStereo = Constant.DefaultPanStereo;
            _SpatialBlend = Constant.DefaultSpatialBlend;
            _MaxDistance = Constant.DefaultMaxDistance;
            _DopplerLevel = Constant.DefaultDopplerLevel;
        }

        /// <summary>
        /// 获取或设置播放位置。
        /// </summary>
        public float Time
        {
            get
            {
                return _Time;
            }
            set
            {
                _Time = value;
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
            }
        }

        /// <summary>
        /// 获取或设置是否循环播放。
        /// </summary>
        public bool Loop
        {
            get
            {
                return _Loop;
            }
            set
            {
                _Loop = value;
            }
        }

        /// <summary>
        /// 获取或设置声音优先级。
        /// </summary>
        public int Priority
        {
            get
            {
                return _Priority;
            }
            set
            {
                _Priority = value;
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
            }
        }

        /// <summary>
        /// 获取或设置声音淡入时间，以秒为单位。
        /// </summary>
        public float FadeInSeconds
        {
            get
            {
                return _FadeInSeconds;
            }
            set
            {
                _FadeInSeconds = value;
            }
        }

        /// <summary>
        /// 获取或设置声音音调。
        /// </summary>
        public float Pitch
        {
            get
            {
                return _Pitch;
            }
            set
            {
                _Pitch = value;
            }
        }

        /// <summary>
        /// 获取或设置声音立体声声相。
        /// </summary>
        public float PanStereo
        {
            get
            {
                return _PanStereo;
            }
            set
            {
                _PanStereo = value;
            }
        }

        /// <summary>
        /// 获取或设置声音空间混合量。
        /// </summary>
        public float SpatialBlend
        {
            get
            {
                return _SpatialBlend;
            }
            set
            {
                _SpatialBlend = value;
            }
        }

        /// <summary>
        /// 获取或设置声音最大距离。
        /// </summary>
        public float MaxDistance
        {
            get
            {
                return _MaxDistance;
            }
            set
            {
                _MaxDistance = value;
            }
        }

        /// <summary>
        /// 获取或设置声音多普勒等级。
        /// </summary>
        public float DopplerLevel
        {
            get
            {
                return _DopplerLevel;
            }
            set
            {
                _DopplerLevel = value;
            }
        }

        internal bool Referenced
        {
            get
            {
                return _Referenced;
            }
        }

        /// <summary>
        /// 创建播放声音参数。
        /// </summary>
        /// <returns>创建的播放声音参数。</returns>
        public static PlaySoundParams Create()
        {
            PlaySoundParams playSoundParams = ReferencePool.Acquire<PlaySoundParams>();
            playSoundParams._Referenced = true;
            return playSoundParams;
        }

        /// <summary>
        /// 清理播放声音参数。
        /// </summary>
        public void Clear()
        {
            _Time = Constant.DefaultTime;
            _MuteInSoundGroup = Constant.DefaultMute;
            _Loop = Constant.DefaultLoop;
            _Priority = Constant.DefaultPriority;
            _VolumeInSoundGroup = Constant.DefaultVolume;
            _FadeInSeconds = Constant.DefaultFadeInSeconds;
            _Pitch = Constant.DefaultPitch;
            _PanStereo = Constant.DefaultPanStereo;
            _SpatialBlend = Constant.DefaultSpatialBlend;
            _MaxDistance = Constant.DefaultMaxDistance;
            _DopplerLevel = Constant.DefaultDopplerLevel;
        }
    }
}
