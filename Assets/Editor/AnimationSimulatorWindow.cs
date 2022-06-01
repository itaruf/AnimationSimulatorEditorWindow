using System.Collections;
using System.Collections.Generic;
using UnityChan;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Linq;

[CustomEditor(typeof(IdleChanger))]
public class AnimationSimulatorWindow : EditorWindow
{
    public List<Animator> _animators;
    Vector2 scrollPos = Vector2.zero;
    string t = "This is a string inside a Scroll view!";
    static AnimationSimulatorWindow window;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [MenuItem("Examples/Modify internal Quaternion")]
    static void Init()
    {
        window = (AnimationSimulatorWindow) GetWindowWithRect(typeof(AnimationSimulatorWindow), new Rect(200, 200, 100, 150));
        window.Show();
    }

    string myString = "Animators";
    bool groupEnabled;
    bool myBool = true;
    float myFloat = 1.23f;

    // Add menu item named "My Window" to the Window menu
    [MenuItem("Window/Animator Simulator")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        /*GetWindow(typeof(AnimationSimulatorWindow));*/
        window = (AnimationSimulatorWindow)GetWindowWithRect(typeof(AnimationSimulatorWindow), new Rect(0, 0, 300, 300));
        window.Show();
    }

    void OnGUI()
    {
        // Find all animators in the scene
        _animators = FindObjectsOfType<Animator>().ToList();

        /*foreach (var a in _animators)
            Debug.Log(a.gameObject.name);*/

        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        myString = EditorGUILayout.TextField("Text Field", myString);
        GUILayout.Label("Animators", EditorStyles.boldLabel);
        myString = EditorGUILayout.TextField(_animators.Count.ToString(), _animators.Count.ToString());

        /*EditorGUILayout.BeginVertical();*/
        // List all animators in the scene
        foreach (var a in _animators)
        {
            if (GUILayout.Button(a.name))
            {
                Selection.activeGameObject = a.gameObject;
            }

            /* foreach (var animation in animator.runtimeAnimatorController.animationClips)
             {
                 GUILayout.Label(animation.name, EditorStyles.boldLabel);
             }
             GUILayout.Label(" ", EditorStyles.boldLabel);*/
        }

        if (Selection.activeGameObject)
        {
            if (Selection.activeGameObject.TryGetComponent(out Animator animator))
            {
                foreach (var animation in animator.runtimeAnimatorController.animationClips)
                {
                    GUILayout.Label(animation.name, EditorStyles.boldLabel);
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
}
