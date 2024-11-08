//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 默认声音辅助器。
    /// </summary>
    public class DefaultSoundHelper : SoundHelperBase
    {
        private ResourceComponent _resourceComponent = null;

        /// <summary>
        /// 释放声音资源。
        /// </summary>
        /// <param name="soundAsset">要释放的声音资源。</param>
        public override void ReleaseSoundAsset(object soundAsset)
        {
            _resourceComponent.UnloadAsset(soundAsset);
        }

        private void Start()
        {
            _resourceComponent = GameEntry.GetComponent<ResourceComponent>();
            if (_resourceComponent == null)
            {
                Log.Fatal("Resource component is invalid.");
                return;
            }
        }
    }
}
