//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Resource
{
    /// <summary>
    /// 版本资源列表更新回调函数集。
    /// </summary>
    public sealed class UpdateVersionListCallbacks
    {
        private readonly UpdateVersionListSuccessCallback _UpdateVersionListSuccessCallback;
        private readonly UpdateVersionListFailureCallback _UpdateVersionListFailureCallback;

        /// <summary>
        /// 初始化版本资源列表更新回调函数集的新实例。
        /// </summary>
        /// <param name="updateVersionListSuccessCallback">版本资源列表更新成功回调函数。</param>
        public UpdateVersionListCallbacks(UpdateVersionListSuccessCallback updateVersionListSuccessCallback)
            : this(updateVersionListSuccessCallback, null)
        {
        }

        /// <summary>
        /// 初始化版本资源列表更新回调函数集的新实例。
        /// </summary>
        /// <param name="updateVersionListSuccessCallback">版本资源列表更新成功回调函数。</param>
        /// <param name="updateVersionListFailureCallback">版本资源列表更新失败回调函数。</param>
        public UpdateVersionListCallbacks(UpdateVersionListSuccessCallback updateVersionListSuccessCallback, UpdateVersionListFailureCallback updateVersionListFailureCallback)
        {
            if (updateVersionListSuccessCallback == null)
            {
                throw new GameFrameworkException("Update version list success callback is invalid.");
            }

            _UpdateVersionListSuccessCallback = updateVersionListSuccessCallback;
            _UpdateVersionListFailureCallback = updateVersionListFailureCallback;
        }

        /// <summary>
        /// 获取版本资源列表更新成功回调函数。
        /// </summary>
        public UpdateVersionListSuccessCallback UpdateVersionListSuccessCallback
        {
            get
            {
                return _UpdateVersionListSuccessCallback;
            }
        }

        /// <summary>
        /// 获取版本资源列表更新失败回调函数。
        /// </summary>
        public UpdateVersionListFailureCallback UpdateVersionListFailureCallback
        {
            get
            {
                return _UpdateVersionListFailureCallback;
            }
        }
    }
}
