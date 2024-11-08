//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Fsm;
using GameFramework.Procedure;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 流程组件。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Procedure")]
    public sealed class ProcedureComponent : GameFrameworkComponent
    {
        private IProcedureManager _mProcedureManager = null;
        private ProcedureBase _mEntranceProcedure = null;

        [FormerlySerializedAs("m_AvailableProcedureTypeNames")] [SerializeField]
        private string[] mAvailableProcedureTypeNames = null;

        [FormerlySerializedAs("m_EntranceProcedureTypeName")] [SerializeField]
        private string mEntranceProcedureTypeName = null;

        /// <summary>
        /// 获取当前流程。
        /// </summary>
        public ProcedureBase CurrentProcedure => _mProcedureManager.CurrentProcedure;

        /// <summary>
        /// 获取当前流程持续时间。
        /// </summary>
        public float CurrentProcedureTime => _mProcedureManager.CurrentProcedureTime;

        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _mProcedureManager = GameFrameworkEntry.GetModule<IProcedureManager>();
            if (_mProcedureManager == null)
            {
                Log.Fatal("Procedure manager is invalid.");
            }
        }

        private IEnumerator Start()
        {
            ProcedureBase[] procedures = new ProcedureBase[mAvailableProcedureTypeNames.Length];
            for (int i = 0; i < mAvailableProcedureTypeNames.Length; i++)
            {
                Type procedureType = Utility.Assembly.GetType(mAvailableProcedureTypeNames[i]);
                if (procedureType == null)
                {
                    Log.Error("Can not find procedure type '{0}'.", mAvailableProcedureTypeNames[i]);
                    yield break;
                }

                procedures[i] = (ProcedureBase)Activator.CreateInstance(procedureType);
                if (procedures[i] == null)
                {
                    Log.Error("Can not create procedure instance '{0}'.", mAvailableProcedureTypeNames[i]);
                    yield break;
                }

                if (mEntranceProcedureTypeName == mAvailableProcedureTypeNames[i])
                {
                    _mEntranceProcedure = procedures[i];
                }
            }

            if (_mEntranceProcedure == null)
            {
                Log.Error("Entrance procedure is invalid.");
                yield break;
            }

            _mProcedureManager.Initialize(GameFrameworkEntry.GetModule<IFsmManager>(), procedures);

            yield return new WaitForEndOfFrame();

            _mProcedureManager.StartProcedure(_mEntranceProcedure.GetType());
        }

        /// <summary>
        /// 是否存在流程。
        /// </summary>
        /// <typeparam name="T">要检查的流程类型。</typeparam>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure<T>() where T : ProcedureBase
        {
            return _mProcedureManager.HasProcedure<T>();
        }

        /// <summary>
        /// 是否存在流程。
        /// </summary>
        /// <param name="procedureType">要检查的流程类型。</param>
        /// <returns>是否存在流程。</returns>
        public bool HasProcedure(Type procedureType)
        {
            return _mProcedureManager.HasProcedure(procedureType);
        }

        /// <summary>
        /// 获取流程。
        /// </summary>
        /// <typeparam name="T">要获取的流程类型。</typeparam>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure<T>() where T : ProcedureBase
        {
            return _mProcedureManager.GetProcedure<T>();
        }

        /// <summary>
        /// 获取流程。
        /// </summary>
        /// <param name="procedureType">要获取的流程类型。</param>
        /// <returns>要获取的流程。</returns>
        public ProcedureBase GetProcedure(Type procedureType)
        {
            return _mProcedureManager.GetProcedure(procedureType);
        }
    }
}
