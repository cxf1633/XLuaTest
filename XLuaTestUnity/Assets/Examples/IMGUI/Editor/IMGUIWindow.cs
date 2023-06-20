using UnityEditor;
using UnityEngine;

namespace Examples.IMGUI.Editor
{
    public class IMGUIWindow : EditorWindow
    {
        string myString = "Hello World";
        bool groupEnabled;
        bool myBool = true;
        float myFloat = 1.23f;
        private static IMGUIWindow _window;
        [MenuItem ("IMGUI/MyWindow")]
        // public static void  ShowWindow () {
        //     EditorWindow.GetWindow(typeof(IMGUIWindow));
        // }

        private static void OpenWindow()
        {
            _window = GetWindow<IMGUIWindow>();
            _window.titleContent = new GUIContent("UI View List");
            _window.Show();
        }

        void OnGUI () 
        {
            GUILayout.Label ("Base Settings", EditorStyles.boldLabel);
            myString = EditorGUILayout.TextField ("Text Field", myString);
        
            groupEnabled = EditorGUILayout.BeginToggleGroup ("Optional Settings", groupEnabled);
            myBool = EditorGUILayout.Toggle ("Toggle", myBool);
            myFloat = EditorGUILayout.Slider ("Slider", myFloat, -3, 3);
            EditorGUILayout.EndToggleGroup ();
        }
    }
}
