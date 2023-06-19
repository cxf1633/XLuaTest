using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Examples.Odin
{
    public class CustomValueDrawerExample : MonoBehaviour
    {
        [PropertyOrder(2)]
        public int First;
        
        [InfoBox("PropertyOrder用于更改inspector中属性的顺序")]
        
        [PropertyOrder(1)]
        public int Second;
        
        // public float Max = 100, Min = 0;
        //
        // [CustomValueDrawer("MyStaticCustomDrawerStatic")]
        // public float CustomDrawerStatic;
        // private static float MyStaticCustomDrawerStatic(float value, GUIContent label)
        // {
        //     return EditorGUILayout.Slider(label, value, 0f, 10f);
        // }
        //
        // [CustomValueDrawer("MyStaticCustomDrawerInstance")]
        // public float CustomDrawerInstance;
        // private float MyStaticCustomDrawerInstance(float value, GUIContent label)
        // {
        //     return EditorGUILayout.Slider(label, value, this.Min, this.Max);
        // }
        //
        // [CustomValueDrawer("MyStaticCustomDrawerArray")]
        // public float[] CustomDrawerArray = new float[] { 3f, 5f, 6f };
        //
        // private float MyStaticCustomDrawerArray(float value, GUIContent label)
        // {
        //     return EditorGUILayout.Slider(value, this.Min, this.Max);
        // }
        //
        //
        // [CustomValueDrawer("HaveLabelNameFunction")]
        // public string HaveLabelName;
        // public string HaveLabelNameFunction(string tempName, GUIContent label)
        // {
        //     return EditorGUILayout.TextField(label, tempName);
        // }
        //
        // [CustomValueDrawer("NoLabelNameFunction")]
        // public string NoLabelName;
        // public string NoLabelNameFunction(string tempName, GUIContent label)
        // {
        //     return EditorGUILayout.TextField(tempName);
        // }
        //
        //
        // public string showLabel = "显示标题";
        //
        // [HideLabel]
        // public string hideLabel = "隐藏标题";
        //
        // [ShowInInspector]
        // public string ShowPropertyLabel { get; set; }
        //
        // [HideLabel][ShowInInspector]
        // public string HidePropertyLabel { get; set; }
    }
}