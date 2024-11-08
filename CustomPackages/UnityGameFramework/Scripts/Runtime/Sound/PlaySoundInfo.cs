//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    internal sealed class PlaySoundInfo : IReference
    {
        private Entity _bindingEntity;
        private Vector3 _worldPosition;
        private object _userData;

        public PlaySoundInfo()
        {
            _bindingEntity = null;
            _worldPosition = Vector3.zero;
            _userData = null;
        }

        public Entity BindingEntity => _bindingEntity;

        public Vector3 WorldPosition => _worldPosition;

        public object UserData => _userData;

        public static PlaySoundInfo Create(Entity bindingEntity, Vector3 worldPosition, object userData)
        {
            PlaySoundInfo playSoundInfo = ReferencePool.Acquire<PlaySoundInfo>();
            playSoundInfo._bindingEntity = bindingEntity;
            playSoundInfo._worldPosition = worldPosition;
            playSoundInfo._userData = userData;
            return playSoundInfo;
        }

        public void Clear()
        {
            _bindingEntity = null;
            _worldPosition = Vector3.zero;
            _userData = null;
        }
    }
}
