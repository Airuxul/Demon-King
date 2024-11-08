//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Resource;
using System;
using System.IO;
using System.Text;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 内置版本资源列表序列化器。
    /// </summary>
    public static partial class BuiltinVersionListSerializer
    {
#if UNITY_EDITOR

        /// <summary>
        /// 序列化单机模式版本资源列表（版本 0）回调函数。
        /// </summary>
        /// <param name="stream">目标流。</param>
        /// <param name="versionList">要序列化的单机模式版本资源列表（版本 0）。</param>
        /// <returns>是否序列化单机模式版本资源列表（版本 0）成功。</returns>
        public static bool PackageVersionListSerializeCallback_V0(Stream stream, PackageVersionList versionList)
        {
            if (!versionList.IsValid)
            {
                return false;
            }

            Utility.Random.GetRandomBytes(SCachedHashBytes);
            using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8))
            {
                binaryWriter.Write(SCachedHashBytes);
                binaryWriter.WriteEncryptedString(versionList.ApplicableGameVersion, SCachedHashBytes);
                binaryWriter.Write(versionList.InternalResourceVersion);
                PackageVersionList.Asset[] assets = versionList.GetAssets();
                binaryWriter.Write(assets.Length);
                PackageVersionList.Resource[] resources = versionList.GetResources();
                binaryWriter.Write(resources.Length);
                foreach (PackageVersionList.Resource resource in resources)
                {
                    binaryWriter.WriteEncryptedString(resource.Name, SCachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Variant, SCachedHashBytes);
                    binaryWriter.Write(resource.LoadType);
                    binaryWriter.Write(resource.Length);
                    binaryWriter.Write(resource.HashCode);
                    int[] assetIndexes = resource.GetAssetIndexes();
                    binaryWriter.Write(assetIndexes.Length);
                    byte[] hashBytes = new byte[CachedHashBytesLength];
                    foreach (int assetIndex in assetIndexes)
                    {
                        Utility.Converter.GetBytes(resource.HashCode, hashBytes);
                        PackageVersionList.Asset asset = assets[assetIndex];
                        binaryWriter.WriteEncryptedString(asset.Name, hashBytes);
                        int[] dependencyAssetIndexes = asset.GetDependencyAssetIndexes();
                        binaryWriter.Write(dependencyAssetIndexes.Length);
                        foreach (int dependencyAssetIndex in dependencyAssetIndexes)
                        {
                            binaryWriter.WriteEncryptedString(assets[dependencyAssetIndex].Name, hashBytes);
                        }
                    }
                }

                PackageVersionList.ResourceGroup[] resourceGroups = versionList.GetResourceGroups();
                binaryWriter.Write(resourceGroups.Length);
                foreach (PackageVersionList.ResourceGroup resourceGroup in resourceGroups)
                {
                    binaryWriter.WriteEncryptedString(resourceGroup.Name, SCachedHashBytes);
                    int[] resourceIndexes = resourceGroup.GetResourceIndexes();
                    binaryWriter.Write(resourceIndexes.Length);
                    foreach (ushort resourceIndex in resourceIndexes)
                    {
                        binaryWriter.Write(resourceIndex);
                    }
                }
            }

            Array.Clear(SCachedHashBytes, 0, CachedHashBytesLength);
            return true;
        }

        /// <summary>
        /// 序列化单机模式版本资源列表（版本 1）回调函数。
        /// </summary>
        /// <param name="stream">目标流。</param>
        /// <param name="versionList">要序列化的单机模式版本资源列表（版本 1）。</param>
        /// <returns>是否序列化单机模式版本资源列表（版本 1）成功。</returns>
        public static bool PackageVersionListSerializeCallback_V1(Stream stream, PackageVersionList versionList)
        {
            if (!versionList.IsValid)
            {
                return false;
            }

            Utility.Random.GetRandomBytes(SCachedHashBytes);
            using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8))
            {
                binaryWriter.Write(SCachedHashBytes);
                binaryWriter.WriteEncryptedString(versionList.ApplicableGameVersion, SCachedHashBytes);
                binaryWriter.Write7BitEncodedInt32(versionList.InternalResourceVersion);
                PackageVersionList.Asset[] assets = versionList.GetAssets();
                binaryWriter.Write7BitEncodedInt32(assets.Length);
                foreach (PackageVersionList.Asset asset in assets)
                {
                    binaryWriter.WriteEncryptedString(asset.Name, SCachedHashBytes);
                    int[] dependencyAssetIndexes = asset.GetDependencyAssetIndexes();
                    binaryWriter.Write7BitEncodedInt32(dependencyAssetIndexes.Length);
                    foreach (int dependencyAssetIndex in dependencyAssetIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(dependencyAssetIndex);
                    }
                }

                PackageVersionList.Resource[] resources = versionList.GetResources();
                binaryWriter.Write7BitEncodedInt32(resources.Length);
                foreach (PackageVersionList.Resource resource in resources)
                {
                    binaryWriter.WriteEncryptedString(resource.Name, SCachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Variant, SCachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Extension != DefaultExtension ? resource.Extension : null, SCachedHashBytes);
                    binaryWriter.Write(resource.LoadType);
                    binaryWriter.Write7BitEncodedInt32(resource.Length);
                    binaryWriter.Write(resource.HashCode);
                    int[] assetIndexes = resource.GetAssetIndexes();
                    binaryWriter.Write7BitEncodedInt32(assetIndexes.Length);
                    foreach (int assetIndex in assetIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(assetIndex);
                    }
                }

                PackageVersionList.ResourceGroup[] resourceGroups = versionList.GetResourceGroups();
                binaryWriter.Write7BitEncodedInt32(resourceGroups.Length);
                foreach (PackageVersionList.ResourceGroup resourceGroup in resourceGroups)
                {
                    binaryWriter.WriteEncryptedString(resourceGroup.Name, SCachedHashBytes);
                    int[] resourceIndexes = resourceGroup.GetResourceIndexes();
                    binaryWriter.Write7BitEncodedInt32(resourceIndexes.Length);
                    foreach (int resourceIndex in resourceIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(resourceIndex);
                    }
                }
            }

            Array.Clear(SCachedHashBytes, 0, CachedHashBytesLength);
            return true;
        }

        /// <summary>
        /// 序列化单机模式版本资源列表（版本 2）回调函数。
        /// </summary>
        /// <param name="stream">目标流。</param>
        /// <param name="versionList">要序列化的单机模式版本资源列表（版本 2）。</param>
        /// <returns>是否序列化单机模式版本资源列表（版本 2）成功。</returns>
        public static bool PackageVersionListSerializeCallback_V2(Stream stream, PackageVersionList versionList)
        {
            if (!versionList.IsValid)
            {
                return false;
            }

            Utility.Random.GetRandomBytes(SCachedHashBytes);
            using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8))
            {
                binaryWriter.Write(SCachedHashBytes);
                binaryWriter.WriteEncryptedString(versionList.ApplicableGameVersion, SCachedHashBytes);
                binaryWriter.Write7BitEncodedInt32(versionList.InternalResourceVersion);
                PackageVersionList.Asset[] assets = versionList.GetAssets();
                binaryWriter.Write7BitEncodedInt32(assets.Length);
                foreach (PackageVersionList.Asset asset in assets)
                {
                    binaryWriter.WriteEncryptedString(asset.Name, SCachedHashBytes);
                    int[] dependencyAssetIndexes = asset.GetDependencyAssetIndexes();
                    binaryWriter.Write7BitEncodedInt32(dependencyAssetIndexes.Length);
                    foreach (int dependencyAssetIndex in dependencyAssetIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(dependencyAssetIndex);
                    }
                }

                PackageVersionList.Resource[] resources = versionList.GetResources();
                binaryWriter.Write7BitEncodedInt32(resources.Length);
                foreach (PackageVersionList.Resource resource in resources)
                {
                    binaryWriter.WriteEncryptedString(resource.Name, SCachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Variant, SCachedHashBytes);
                    binaryWriter.WriteEncryptedString(resource.Extension != DefaultExtension ? resource.Extension : null, SCachedHashBytes);
                    binaryWriter.Write(resource.LoadType);
                    binaryWriter.Write7BitEncodedInt32(resource.Length);
                    binaryWriter.Write(resource.HashCode);
                    int[] assetIndexes = resource.GetAssetIndexes();
                    binaryWriter.Write7BitEncodedInt32(assetIndexes.Length);
                    foreach (int assetIndex in assetIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(assetIndex);
                    }
                }

                PackageVersionList.FileSystem[] fileSystems = versionList.GetFileSystems();
                binaryWriter.Write7BitEncodedInt32(fileSystems.Length);
                foreach (PackageVersionList.FileSystem fileSystem in fileSystems)
                {
                    binaryWriter.WriteEncryptedString(fileSystem.Name, SCachedHashBytes);
                    int[] resourceIndexes = fileSystem.GetResourceIndexes();
                    binaryWriter.Write7BitEncodedInt32(resourceIndexes.Length);
                    foreach (int resourceIndex in resourceIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(resourceIndex);
                    }
                }

                PackageVersionList.ResourceGroup[] resourceGroups = versionList.GetResourceGroups();
                binaryWriter.Write7BitEncodedInt32(resourceGroups.Length);
                foreach (PackageVersionList.ResourceGroup resourceGroup in resourceGroups)
                {
                    binaryWriter.WriteEncryptedString(resourceGroup.Name, SCachedHashBytes);
                    int[] resourceIndexes = resourceGroup.GetResourceIndexes();
                    binaryWriter.Write7BitEncodedInt32(resourceIndexes.Length);
                    foreach (int resourceIndex in resourceIndexes)
                    {
                        binaryWriter.Write7BitEncodedInt32(resourceIndex);
                    }
                }
            }

            Array.Clear(SCachedHashBytes, 0, CachedHashBytesLength);
            return true;
        }

#endif
    }
}
