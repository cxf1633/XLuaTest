using Sirenix.OdinInspector;
using UnityEngine;

namespace Examples.Odin
{
    public class ButtonExample: MonoBehaviour
    {
        public string ButtonName = "Default";
        //改变按钮名字
        [Button("$ButtonName")]
        private void Default()
        {
            Debug.Log("Default");
        }
        
        [Button("$ButtonName")]
        private void Default(float a, float b, GameObject c)
        {
            Debug.Log("Default GameObject");
        }
        
        //可以通过特殊字符@ 写入方法体调用
        [Button("@\"Now Time: \" + DateTime.Now.ToString(\"HH:mm:ss\")")]
        private void Default(float t, float b, float[] c)
        {
            Debug.Log("Default Array");
        }
        
        //改变按钮尺寸
        [Button(ButtonSizes.Small), GUIColor(0.3f,0.8f,1)]
        public void SmallButton()
        {
            Debug.Log("SmallButton!");
        }

        [Button(90)]
        public void CustomSizedButton()
        {
            Debug.Log("CustomSizedButton!");
        }
        
        //按钮样式
        [Button(ButtonSizes.Medium, ButtonStyle.FoldoutButton)]
        private int FoldoutButton(int a = 2, int b = 2)
        {
            return a + b;
        }

        [Button(ButtonSizes.Medium, ButtonStyle.FoldoutButton)]
        private void FoldoutButton(int a, int b, ref int result)
        {
            result = a + b;
        }

        [Button(ButtonSizes.Large, ButtonStyle.Box)]
        private void Box(float a, float b, out float c)
        {
            c = a + b;
        }

        //强制展开
        [Button(ButtonSizes.Large, ButtonStyle.Box, Expanded = true)]
        private void Box(int a, float b, out float c)
        {
            c = a + b;
        }
        [Button(ButtonSizes.Large, ButtonStyle.CompactBox)]
        public void CompactBox(int a, float b, out float c)
        {
            c = a + b;
        }
    }
}