using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 属性的数据模型层
/// 必须继承PropertyAttribute
/// </summary>
public class MyRangeAttribute : PropertyAttribute 
{
    //绘制需要的数据
    public float min;
    public float max;
    public string label;

    public MyRangeAttribute(float min, float max,string label = "") 
    {
        this.min = min;
        this.max = max;
        this.label = label;
    }
}
