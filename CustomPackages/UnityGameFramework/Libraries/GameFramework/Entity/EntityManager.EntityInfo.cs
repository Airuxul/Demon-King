//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;

namespace GameFramework.Entity
{
    internal sealed partial class EntityManager : GameFrameworkModule, IEntityManager
    {
        /// <summary>
        /// 实体信息。
        /// </summary>
        private sealed class EntityInfo : IReference
        {
            private IEntity _Entity;
            private EntityStatus _Status;
            private IEntity _ParentEntity;
            private List<IEntity> _ChildEntities;

            public EntityInfo()
            {
                _Entity = null;
                _Status = EntityStatus.Unknown;
                _ParentEntity = null;
                _ChildEntities = new List<IEntity>();
            }

            public IEntity Entity
            {
                get
                {
                    return _Entity;
                }
            }

            public EntityStatus Status
            {
                get
                {
                    return _Status;
                }
                set
                {
                    _Status = value;
                }
            }

            public IEntity ParentEntity
            {
                get
                {
                    return _ParentEntity;
                }
                set
                {
                    _ParentEntity = value;
                }
            }

            public int ChildEntityCount
            {
                get
                {
                    return _ChildEntities.Count;
                }
            }

            public static EntityInfo Create(IEntity entity)
            {
                if (entity == null)
                {
                    throw new GameFrameworkException("Entity is invalid.");
                }

                EntityInfo entityInfo = ReferencePool.Acquire<EntityInfo>();
                entityInfo._Entity = entity;
                entityInfo._Status = EntityStatus.WillInit;
                return entityInfo;
            }

            public void Clear()
            {
                _Entity = null;
                _Status = EntityStatus.Unknown;
                _ParentEntity = null;
                _ChildEntities.Clear();
            }

            public IEntity GetChildEntity()
            {
                return _ChildEntities.Count > 0 ? _ChildEntities[0] : null;
            }

            public IEntity[] GetChildEntities()
            {
                return _ChildEntities.ToArray();
            }

            public void GetChildEntities(List<IEntity> results)
            {
                if (results == null)
                {
                    throw new GameFrameworkException("Results is invalid.");
                }

                results.Clear();
                foreach (IEntity childEntity in _ChildEntities)
                {
                    results.Add(childEntity);
                }
            }

            public void AddChildEntity(IEntity childEntity)
            {
                if (_ChildEntities.Contains(childEntity))
                {
                    throw new GameFrameworkException("Can not add child entity which is already exist.");
                }

                _ChildEntities.Add(childEntity);
            }

            public void RemoveChildEntity(IEntity childEntity)
            {
                if (!_ChildEntities.Remove(childEntity))
                {
                    throw new GameFrameworkException("Can not remove child entity which is not exist.");
                }
            }
        }
    }
}
