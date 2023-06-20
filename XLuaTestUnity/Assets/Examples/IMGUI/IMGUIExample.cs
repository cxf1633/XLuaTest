using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IMGUIExample : MonoBehaviour
{
    private void OnGUI()
    {
        // if (GUILayout.Button("Press Me"))
        // {
        //     Debug.Log("Hello");
        // }
        
        //坐标系基于左上角
        // GUI.Box (new Rect (0,0,100,50), "Top-left");
        // GUI.Box (new Rect (Screen.width - 100,0,100,50), "Top-right");
        // GUI.Box (new Rect (0,Screen.height - 50,100,50), "Bottom-left");
        // GUI.Box (new Rect (Screen.width - 100,Screen.height - 50,100,50), "Bottom-right");

        // Test_Button();
        Test_GUI_Changed();
    }

    private void Test_Label()
    {
        GUI.Label (new Rect (25, 25, 100, 30), "Label");
    }

    public Texture2D icon;
    private void Test_Button()
    {
        if (GUI.Button (new Rect (10,10, 100, 50), icon)) 
        {
            print ("you clicked the icon");
        }
    
        if (GUI.Button (new Rect (10,70, 100, 20), "This is text")) 
        {
            print ("you clicked the text button");
        }
    }

    private void Test_RepeatButton()
    {
        if (GUI.RepeatButton (new Rect (25, 25, 100, 30), "RepeatButton")) 
        {
            // RepeatButton 保持点击状态时的每一帧都将执行此代码
        }
    }

    private string textFieldString = "text field";
    private void Test_TextField()
    {
        textFieldString = GUI.TextField (new Rect (25, 25, 100, 30), textFieldString);
    }

    private int selectedToolbar = 0;
    private string[] toolbarStrings = {"One", "Two"};
    private void Test_GUI_Changed()
    {
        // 确定哪个按钮处于激活状态，是否在此帧进行了点击
        selectedToolbar = GUI.Toolbar (new Rect (50, 10, Screen.width - 100, 30), selectedToolbar, toolbarStrings);
    
        // 如果用户在此帧点击了新的工具栏按钮，我们将处理他们的输入
        if (GUI.changed)
        {
            Debug.Log("The toolbar was clicked");
    
            if (0 == selectedToolbar)
            {
                Debug.Log("First button was clicked");
            }
            else
            {
                Debug.Log("Second button was clicked");
            }
        }
    }
}
