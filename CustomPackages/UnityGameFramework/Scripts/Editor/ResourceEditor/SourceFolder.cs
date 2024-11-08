//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed class SourceFolder
    {
        private static Texture s_CachedIcon = null;

        private readonly List<SourceFolder> _Folders;
        private readonly List<SourceAsset> _Assets;

        public SourceFolder(string name, SourceFolder folder)
        {
            _Folders = new List<SourceFolder>();
            _Assets = new List<SourceAsset>();

            Name = name;
            Folder = folder;
        }

        public string Name
        {
            get;
            private set;
        }

        public SourceFolder Folder
        {
            get;
            private set;
        }

        public string FromRootPath
        {
            get
            {
                return Folder == null ? string.Empty : (Folder.Folder == null ? Name : Utility.Text.Format("{0}/{1}", Folder.FromRootPath, Name));
            }
        }

        public int Depth
        {
            get
            {
                return Folder != null ? Folder.Depth + 1 : 0;
            }
        }

        public static Texture Icon
        {
            get
            {
                if (s_CachedIcon == null)
                {
                    s_CachedIcon = AssetDatabase.GetCachedIcon("Assets");
                }

                return s_CachedIcon;
            }
        }

        public void Clear()
        {
            _Folders.Clear();
            _Assets.Clear();
        }

        public SourceFolder[] GetFolders()
        {
            return _Folders.ToArray();
        }

        public SourceFolder GetFolder(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Source folder name is invalid.");
            }

            foreach (SourceFolder folder in _Folders)
            {
                if (folder.Name == name)
                {
                    return folder;
                }
            }

            return null;
        }

        public SourceFolder AddFolder(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Source folder name is invalid.");
            }

            SourceFolder folder = GetFolder(name);
            if (folder != null)
            {
                throw new GameFrameworkException("Source folder is already exist.");
            }

            folder = new SourceFolder(name, this);
            _Folders.Add(folder);

            return folder;
        }

        public SourceAsset[] GetAssets()
        {
            return _Assets.ToArray();
        }

        public SourceAsset GetAsset(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Source asset name is invalid.");
            }

            foreach (SourceAsset asset in _Assets)
            {
                if (asset.Name == name)
                {
                    return asset;
                }
            }

            return null;
        }

        public SourceAsset AddAsset(string guid, string path, string name)
        {
            if (string.IsNullOrEmpty(guid))
            {
                throw new GameFrameworkException("Source asset guid is invalid.");
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new GameFrameworkException("Source asset path is invalid.");
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new GameFrameworkException("Source asset name is invalid.");
            }

            SourceAsset asset = GetAsset(name);
            if (asset != null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Source asset '{0}' is already exist.", name));
            }

            asset = new SourceAsset(guid, path, name, this);
            _Assets.Add(asset);

            return asset;
        }
    }
}
