using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 属性的UI层
/// 继承PropertyDrawer, 必须放入Editor文件夹下
/// </summary>
[CustomPropertyDrawer(typeof(MyRangeAttribute))]
public class MyRangeAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //获取绘制描述类
        MyRangeAttribute range = this.attribute as MyRangeAttribute;
        
        //判断字段是那种类型，进行不同的绘制
        if(property.propertyType == SerializedPropertyType.Float)
        {
            // Debug.Log("float类型");
            EditorGUI.Slider(position,property,range.min,range.max,label);
        }
        else if(property.propertyType == SerializedPropertyType.Integer)
        {
            // Debug.Log("Integer类型");
            if (range.label != string.Empty) 
            {
                label.text = range.label;
            }

            EditorGUI.IntSlider(position, property, (int)range.min, (int)range.max, label);
        }
    }
}
