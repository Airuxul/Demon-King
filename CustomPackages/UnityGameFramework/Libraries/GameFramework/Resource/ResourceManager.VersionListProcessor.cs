//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Download;
using System;
using System.IO;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        /// <summary>
        /// 版本资源列表处理器。
        /// </summary>
        private sealed class VersionListProcessor
        {
            private readonly ResourceManager _ResourceManager;
            private IDownloadManager _DownloadManager;
            private int _VersionListLength;
            private int _VersionListHashCode;
            private int _VersionListCompressedLength;
            private int _VersionListCompressedHashCode;

            public GameFrameworkAction<string, string> VersionListUpdateSuccess;
            public GameFrameworkAction<string, string> VersionListUpdateFailure;

            /// <summary>
            /// 初始化版本资源列表处理器的新实例。
            /// </summary>
            /// <param name="resourceManager">资源管理器。</param>
            public VersionListProcessor(ResourceManager resourceManager)
            {
                _ResourceManager = resourceManager;
                _DownloadManager = null;
                _VersionListLength = 0;
                _VersionListHashCode = 0;
                _VersionListCompressedLength = 0;
                _VersionListCompressedHashCode = 0;

                VersionListUpdateSuccess = null;
                VersionListUpdateFailure = null;
            }

            /// <summary>
            /// 关闭并清理版本资源列表处理器。
            /// </summary>
            public void Shutdown()
            {
                if (_DownloadManager != null)
                {
                    _DownloadManager.DownloadSuccess -= OnDownloadSuccess;
                    _DownloadManager.DownloadFailure -= OnDownloadFailure;
                }
            }

            /// <summary>
            /// 设置下载管理器。
            /// </summary>
            /// <param name="downloadManager">下载管理器。</param>
            public void SetDownloadManager(IDownloadManager downloadManager)
            {
                if (downloadManager == null)
                {
                    throw new GameFrameworkException("Download manager is invalid.");
                }

                _DownloadManager = downloadManager;
                _DownloadManager.DownloadSuccess += OnDownloadSuccess;
                _DownloadManager.DownloadFailure += OnDownloadFailure;
            }

            /// <summary>
            /// 检查版本资源列表。
            /// </summary>
            /// <param name="latestInternalResourceVersion">最新的内部资源版本号。</param>
            /// <returns>检查版本资源列表结果。</returns>
            public CheckVersionListResult CheckVersionList(int latestInternalResourceVersion)
            {
                if (string.IsNullOrEmpty(_ResourceManager._ReadWritePath))
                {
                    throw new GameFrameworkException("Read-write path is invalid.");
                }

                string versionListFileName = Utility.Path.GetRegularPath(Path.Combine(_ResourceManager._ReadWritePath, RemoteVersionListFileName));
                if (!File.Exists(versionListFileName))
                {
                    return CheckVersionListResult.NeedUpdate;
                }

                int internalResourceVersion = 0;
                FileStream fileStream = null;
                try
                {
                    fileStream = new FileStream(versionListFileName, FileMode.Open, FileAccess.Read);
                    object internalResourceVersionObject = null;
                    if (!_ResourceManager._UpdatableVersionListSerializer.TryGetValue(fileStream, "InternalResourceVersion", out internalResourceVersionObject))
                    {
                        return CheckVersionListResult.NeedUpdate;
                    }

                    internalResourceVersion = (int)internalResourceVersionObject;
                }
                catch
                {
                    return CheckVersionListResult.NeedUpdate;
                }
                finally
                {
                    if (fileStream != null)
                    {
                        fileStream.Dispose();
                        fileStream = null;
                    }
                }

                if (internalResourceVersion != latestInternalResourceVersion)
                {
                    return CheckVersionListResult.NeedUpdate;
                }

                return CheckVersionListResult.Updated;
            }

            /// <summary>
            /// 更新版本资源列表。
            /// </summary>
            /// <param name="versionListLength">版本资源列表大小。</param>
            /// <param name="versionListHashCode">版本资源列表哈希值。</param>
            /// <param name="versionListCompressedLength">版本资源列表压缩后大小。</param>
            /// <param name="versionListCompressedHashCode">版本资源列表压缩后哈希值。</param>
            public void UpdateVersionList(int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode)
            {
                if (_DownloadManager == null)
                {
                    throw new GameFrameworkException("You must set download manager first.");
                }

                _VersionListLength = versionListLength;
                _VersionListHashCode = versionListHashCode;
                _VersionListCompressedLength = versionListCompressedLength;
                _VersionListCompressedHashCode = versionListCompressedHashCode;
                string localVersionListFilePath = Utility.Path.GetRegularPath(Path.Combine(_ResourceManager._ReadWritePath, RemoteVersionListFileName));
                int dotPosition = RemoteVersionListFileName.LastIndexOf('.');
                string latestVersionListFullNameWithCrc32 = Utility.Text.Format("{0}.{2:x8}.{1}", RemoteVersionListFileName.Substring(0, dotPosition), RemoteVersionListFileName.Substring(dotPosition + 1), _VersionListHashCode);
                _DownloadManager.AddDownload(localVersionListFilePath, Utility.Path.GetRemotePath(Path.Combine(_ResourceManager._UpdatePrefixUri, latestVersionListFullNameWithCrc32)), this);
            }

            private void OnDownloadSuccess(object sender, DownloadSuccessEventArgs e)
            {
                VersionListProcessor versionListProcessor = e.UserData as VersionListProcessor;
                if (versionListProcessor == null || versionListProcessor != this)
                {
                    return;
                }

                try
                {
                    using (FileStream fileStream = new FileStream(e.DownloadPath, FileMode.Open, FileAccess.ReadWrite))
                    {
                        int length = (int)fileStream.Length;
                        if (length != _VersionListCompressedLength)
                        {
                            fileStream.Close();
                            string errorMessage = Utility.Text.Format("Latest version list compressed length error, need '{0}', downloaded '{1}'.", _VersionListCompressedLength, length);
                            DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                            OnDownloadFailure(this, downloadFailureEventArgs);
                            ReferencePool.Release(downloadFailureEventArgs);
                            return;
                        }

                        fileStream.Position = 0L;
                        int hashCode = Utility.Verifier.GetCrc32(fileStream);
                        if (hashCode != _VersionListCompressedHashCode)
                        {
                            fileStream.Close();
                            string errorMessage = Utility.Text.Format("Latest version list compressed hash code error, need '{0}', downloaded '{1}'.", _VersionListCompressedHashCode, hashCode);
                            DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                            OnDownloadFailure(this, downloadFailureEventArgs);
                            ReferencePool.Release(downloadFailureEventArgs);
                            return;
                        }

                        fileStream.Position = 0L;
                        _ResourceManager.PrepareCachedStream();
                        if (!Utility.Compression.Decompress(fileStream, _ResourceManager._CachedStream))
                        {
                            fileStream.Close();
                            string errorMessage = Utility.Text.Format("Unable to decompress latest version list '{0}'.", e.DownloadPath);
                            DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                            OnDownloadFailure(this, downloadFailureEventArgs);
                            ReferencePool.Release(downloadFailureEventArgs);
                            return;
                        }

                        int uncompressedLength = (int)_ResourceManager._CachedStream.Length;
                        if (uncompressedLength != _VersionListLength)
                        {
                            fileStream.Close();
                            string errorMessage = Utility.Text.Format("Latest version list length error, need '{0}', downloaded '{1}'.", _VersionListLength, uncompressedLength);
                            DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                            OnDownloadFailure(this, downloadFailureEventArgs);
                            ReferencePool.Release(downloadFailureEventArgs);
                            return;
                        }

                        fileStream.Position = 0L;
                        fileStream.SetLength(0L);
                        fileStream.Write(_ResourceManager._CachedStream.GetBuffer(), 0, uncompressedLength);
                    }

                    if (VersionListUpdateSuccess != null)
                    {
                        VersionListUpdateSuccess(e.DownloadPath, e.DownloadUri);
                    }
                }
                catch (Exception exception)
                {
                    string errorMessage = Utility.Text.Format("Update latest version list '{0}' with error message '{1}'.", e.DownloadPath, exception);
                    DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                    OnDownloadFailure(this, downloadFailureEventArgs);
                    ReferencePool.Release(downloadFailureEventArgs);
                }
            }

            private void OnDownloadFailure(object sender, DownloadFailureEventArgs e)
            {
                VersionListProcessor versionListProcessor = e.UserData as VersionListProcessor;
                if (versionListProcessor == null || versionListProcessor != this)
                {
                    return;
                }

                if (File.Exists(e.DownloadPath))
                {
                    File.Delete(e.DownloadPath);
                }

                if (VersionListUpdateFailure != null)
                {
                    VersionListUpdateFailure(e.DownloadUri, e.ErrorMessage);
                }
            }
        }
    }
}
