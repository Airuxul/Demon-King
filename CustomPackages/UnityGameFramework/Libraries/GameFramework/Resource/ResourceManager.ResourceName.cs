//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        /// <summary>
        /// 资源名称。
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        private struct ResourceName : IComparable, IComparable<ResourceName>, IEquatable<ResourceName>
        {
            private static readonly Dictionary<ResourceName, string> s_ResourceFullNames = new Dictionary<ResourceName, string>();

            private readonly string _Name;
            private readonly string _Variant;
            private readonly string _Extension;

            /// <summary>
            /// 初始化资源名称的新实例。
            /// </summary>
            /// <param name="name">资源名称。</param>
            /// <param name="variant">变体名称。</param>
            /// <param name="extension">扩展名称。</param>
            public ResourceName(string name, string variant, string extension)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new GameFrameworkException("Resource name is invalid.");
                }

                if (string.IsNullOrEmpty(extension))
                {
                    throw new GameFrameworkException("Resource extension is invalid.");
                }

                _Name = name;
                _Variant = variant;
                _Extension = extension;
            }

            /// <summary>
            /// 获取资源名称。
            /// </summary>
            public string Name
            {
                get
                {
                    return _Name;
                }
            }

            /// <summary>
            /// 获取变体名称。
            /// </summary>
            public string Variant
            {
                get
                {
                    return _Variant;
                }
            }

            /// <summary>
            /// 获取扩展名称。
            /// </summary>
            public string Extension
            {
                get
                {
                    return _Extension;
                }
            }

            public string FullName
            {
                get
                {
                    string fullName = null;
                    if (s_ResourceFullNames.TryGetValue(this, out fullName))
                    {
                        return fullName;
                    }

                    fullName = _Variant != null ? Utility.Text.Format("{0}.{1}.{2}", _Name, _Variant, _Extension) : Utility.Text.Format("{0}.{1}", _Name, _Extension);
                    s_ResourceFullNames.Add(this, fullName);
                    return fullName;
                }
            }

            public override string ToString()
            {
                return FullName;
            }

            public override int GetHashCode()
            {
                if (_Variant == null)
                {
                    return _Name.GetHashCode() ^ _Extension.GetHashCode();
                }

                return _Name.GetHashCode() ^ _Variant.GetHashCode() ^ _Extension.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return (obj is ResourceName) && Equals((ResourceName)obj);
            }

            public bool Equals(ResourceName value)
            {
                return string.Equals(_Name, value._Name, StringComparison.Ordinal) && string.Equals(_Variant, value._Variant, StringComparison.Ordinal) && string.Equals(_Extension, value._Extension, StringComparison.Ordinal);
            }

            public static bool operator ==(ResourceName a, ResourceName b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(ResourceName a, ResourceName b)
            {
                return !(a == b);
            }

            public int CompareTo(object value)
            {
                if (value == null)
                {
                    return 1;
                }

                if (!(value is ResourceName))
                {
                    throw new GameFrameworkException("Type of value is invalid.");
                }

                return CompareTo((ResourceName)value);
            }

            public int CompareTo(ResourceName resourceName)
            {
                int result = string.CompareOrdinal(_Name, resourceName._Name);
                if (result != 0)
                {
                    return result;
                }

                result = string.CompareOrdinal(_Variant, resourceName._Variant);
                if (result != 0)
                {
                    return result;
                }

                return string.CompareOrdinal(_Extension, resourceName._Extension);
            }
        }
    }
}
