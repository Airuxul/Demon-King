//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.ObjectPool;
using System.Collections.Generic;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            /// <summary>
            /// 资源对象。
            /// </summary>
            private sealed class ResourceObject : ObjectBase
            {
                private List<object> _DependencyResources;
                private IResourceHelper _ResourceHelper;
                private ResourceLoader _ResourceLoader;

                public ResourceObject()
                {
                    _DependencyResources = new List<object>();
                    _ResourceHelper = null;
                    _ResourceLoader = null;
                }

                public override bool CustomCanReleaseFlag
                {
                    get
                    {
                        int targetReferenceCount = 0;
                        _ResourceLoader._ResourceDependencyCount.TryGetValue(Target, out targetReferenceCount);
                        return base.CustomCanReleaseFlag && targetReferenceCount <= 0;
                    }
                }

                public static ResourceObject Create(string name, object target, IResourceHelper resourceHelper, ResourceLoader resourceLoader)
                {
                    if (resourceHelper == null)
                    {
                        throw new GameFrameworkException("Resource helper is invalid.");
                    }

                    if (resourceLoader == null)
                    {
                        throw new GameFrameworkException("Resource loader is invalid.");
                    }

                    ResourceObject resourceObject = ReferencePool.Acquire<ResourceObject>();
                    resourceObject.Initialize(name, target);
                    resourceObject._ResourceHelper = resourceHelper;
                    resourceObject._ResourceLoader = resourceLoader;
                    return resourceObject;
                }

                public override void Clear()
                {
                    base.Clear();
                    _DependencyResources.Clear();
                    _ResourceHelper = null;
                    _ResourceLoader = null;
                }

                public void AddDependencyResource(object dependencyResource)
                {
                    if (Target == dependencyResource)
                    {
                        return;
                    }

                    if (_DependencyResources.Contains(dependencyResource))
                    {
                        return;
                    }

                    _DependencyResources.Add(dependencyResource);

                    int referenceCount = 0;
                    if (_ResourceLoader._ResourceDependencyCount.TryGetValue(dependencyResource, out referenceCount))
                    {
                        _ResourceLoader._ResourceDependencyCount[dependencyResource] = referenceCount + 1;
                    }
                    else
                    {
                        _ResourceLoader._ResourceDependencyCount.Add(dependencyResource, 1);
                    }
                }

                protected internal override void Release(bool isShutdown)
                {
                    if (!isShutdown)
                    {
                        int targetReferenceCount = 0;
                        if (_ResourceLoader._ResourceDependencyCount.TryGetValue(Target, out targetReferenceCount) && targetReferenceCount > 0)
                        {
                            throw new GameFrameworkException(Utility.Text.Format("Resource target '{0}' reference count is '{1}' larger than 0.", Name, targetReferenceCount));
                        }

                        foreach (object dependencyResource in _DependencyResources)
                        {
                            int referenceCount = 0;
                            if (_ResourceLoader._ResourceDependencyCount.TryGetValue(dependencyResource, out referenceCount))
                            {
                                _ResourceLoader._ResourceDependencyCount[dependencyResource] = referenceCount - 1;
                            }
                            else
                            {
                                throw new GameFrameworkException(Utility.Text.Format("Resource target '{0}' dependency asset reference count is invalid.", Name));
                            }
                        }
                    }

                    _ResourceLoader._ResourceDependencyCount.Remove(Target);
                    _ResourceHelper.Release(Target);
                }
            }
        }
    }
}
