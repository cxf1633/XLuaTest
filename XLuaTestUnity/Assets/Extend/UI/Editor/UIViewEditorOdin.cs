using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Examples;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Extend.UI.Editor
{
    public class UIViewEditorOdin: OdinEditorWindow
    {
        [MenuItem("OdinTools/UIViewEditorOdin")]
        private static void OpenWindow()
        {
            GetWindow<UIViewEditorOdin>().Show();
        }
        
        [PropertyOrder(1)]
        [ButtonGroup]
        private void New()
        {
        }
        
        [PropertyOrder(1)]
        [ButtonGroup]
        private void Remove()
        {
        }
        
        [PropertyOrder(1)]
        [ButtonGroup]
        private void Revert()
        {
        }
        
        [PropertyOrder(1)]
        [ButtonGroup]
        private void Save()
        {
        }
        
        [PropertyOrder(1)]
        [ButtonGroup]
        private void ToLua()
        {
        }
        
        [PropertyOrder(2)]
        [HideLabel]
        [ReadOnly]
        [HorizontalGroup("Group 1", LabelWidth = 10)]
        public string A = "Name";
        
        [PropertyOrder(2)]
        [HideLabel]
        [ReadOnly]
        [HorizontalGroup("Group 1")]
        public string B = "UI View";
        
        [PropertyOrder(2)]
        [HideLabel]
        [ReadOnly]
        [HorizontalGroup("Group 1")]
        public string C = "Background Fx";
    }
}