//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Sound
{
    internal sealed partial class SoundManager : GameFrameworkModule, ISoundManager
    {
        private sealed class PlaySoundInfo : IReference
        {
            private int _SerialId;
            private SoundGroup _SoundGroup;
            private PlaySoundParams _PlaySoundParams;
            private object _UserData;

            public PlaySoundInfo()
            {
                _SerialId = 0;
                _SoundGroup = null;
                _PlaySoundParams = null;
                _UserData = null;
            }

            public int SerialId
            {
                get
                {
                    return _SerialId;
                }
            }

            public SoundGroup SoundGroup
            {
                get
                {
                    return _SoundGroup;
                }
            }

            public PlaySoundParams PlaySoundParams
            {
                get
                {
                    return _PlaySoundParams;
                }
            }

            public object UserData
            {
                get
                {
                    return _UserData;
                }
            }

            public static PlaySoundInfo Create(int serialId, SoundGroup soundGroup, PlaySoundParams playSoundParams, object userData)
            {
                PlaySoundInfo playSoundInfo = ReferencePool.Acquire<PlaySoundInfo>();
                playSoundInfo._SerialId = serialId;
                playSoundInfo._SoundGroup = soundGroup;
                playSoundInfo._PlaySoundParams = playSoundParams;
                playSoundInfo._UserData = userData;
                return playSoundInfo;
            }

            public void Clear()
            {
                _SerialId = 0;
                _SoundGroup = null;
                _PlaySoundParams = null;
                _UserData = null;
            }
        }
    }
}
