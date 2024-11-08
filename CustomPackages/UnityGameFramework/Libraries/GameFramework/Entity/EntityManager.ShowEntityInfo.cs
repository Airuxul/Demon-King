//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Entity
{
    internal sealed partial class EntityManager : GameFrameworkModule, IEntityManager
    {
        private sealed class ShowEntityInfo : IReference
        {
            private int _SerialId;
            private int _EntityId;
            private EntityGroup _EntityGroup;
            private object _UserData;

            public ShowEntityInfo()
            {
                _SerialId = 0;
                _EntityId = 0;
                _EntityGroup = null;
                _UserData = null;
            }

            public int SerialId
            {
                get
                {
                    return _SerialId;
                }
            }

            public int EntityId
            {
                get
                {
                    return _EntityId;
                }
            }

            public EntityGroup EntityGroup
            {
                get
                {
                    return _EntityGroup;
                }
            }

            public object UserData
            {
                get
                {
                    return _UserData;
                }
            }

            public static ShowEntityInfo Create(int serialId, int entityId, EntityGroup entityGroup, object userData)
            {
                ShowEntityInfo showEntityInfo = ReferencePool.Acquire<ShowEntityInfo>();
                showEntityInfo._SerialId = serialId;
                showEntityInfo._EntityId = entityId;
                showEntityInfo._EntityGroup = entityGroup;
                showEntityInfo._UserData = userData;
                return showEntityInfo;
            }

            public void Clear()
            {
                _SerialId = 0;
                _EntityId = 0;
                _EntityGroup = null;
                _UserData = null;
            }
        }
    }
}
