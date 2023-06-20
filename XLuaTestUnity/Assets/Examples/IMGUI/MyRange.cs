using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 具体使用属性
/// </summary>
public class MyRange : MonoBehaviour
{
    public int myInt1;
    
    [MyRangeAttribute(0,10,"整型")]
    public int myInt2;
    
    [MyRangeAttribute(0,10,"浮点型")]
    public float myFloat1;
    
    public float myFloat2;
}
