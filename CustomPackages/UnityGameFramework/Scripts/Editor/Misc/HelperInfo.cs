//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor
{
    internal sealed class HelperInfo<T> where T : MonoBehaviour
    {
        private const string CustomOptionName = "<Custom>";

        private readonly string _Name;

        private SerializedProperty _HelperTypeName;
        private SerializedProperty _CustomHelper;
        private string[] _HelperTypeNames;
        private int _HelperTypeNameIndex;

        public HelperInfo(string name)
        {
            _Name = name;

            _HelperTypeName = null;
            _CustomHelper = null;
            _HelperTypeNames = null;
            _HelperTypeNameIndex = 0;
        }

        public void Init(SerializedObject serializedObject)
        {
            _HelperTypeName = serializedObject.FindProperty(Utility.Text.Format("_{0}HelperTypeName", _Name));
            _CustomHelper = serializedObject.FindProperty(Utility.Text.Format("_Custom{0}Helper", _Name));
        }

        public void Draw()
        {
            string displayName = FieldNameForDisplay(_Name);
            int selectedIndex = EditorGUILayout.Popup(Utility.Text.Format("{0} Helper", displayName), _HelperTypeNameIndex, _HelperTypeNames);
            if (selectedIndex != _HelperTypeNameIndex)
            {
                _HelperTypeNameIndex = selectedIndex;
                _HelperTypeName.stringValue = selectedIndex <= 0 ? null : _HelperTypeNames[selectedIndex];
            }

            if (_HelperTypeNameIndex <= 0)
            {
                EditorGUILayout.PropertyField(_CustomHelper);
                if (_CustomHelper.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(Utility.Text.Format("You must set Custom {0} Helper.", displayName), MessageType.Error);
                }
            }
        }

        public void Refresh()
        {
            List<string> helperTypeNameList = new List<string>
            {
                CustomOptionName
            };

            helperTypeNameList.AddRange(Type.GetRuntimeTypeNames(typeof(T)));
            _HelperTypeNames = helperTypeNameList.ToArray();

            _HelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(_HelperTypeName.stringValue))
            {
                _HelperTypeNameIndex = helperTypeNameList.IndexOf(_HelperTypeName.stringValue);
                if (_HelperTypeNameIndex <= 0)
                {
                    _HelperTypeNameIndex = 0;
                    _HelperTypeName.stringValue = null;
                }
            }
        }

        private string FieldNameForDisplay(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return string.Empty;
            }

            string str = Regex.Replace(fieldName, @"^_", string.Empty);
            str = Regex.Replace(str, @"((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", @" $1").TrimStart();
            return str;
        }
    }
}
