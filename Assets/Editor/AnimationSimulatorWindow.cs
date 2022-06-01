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

    void Start()
    {

    }

    void Update()
    {
        
    }

    [MenuItem("Window/Animator Simulator")]
    public static void ShowWindow()
    {
        window = (AnimationSimulatorWindow)GetWindowWithRect(typeof(AnimationSimulatorWindow), new Rect(0, 0, 300, 300));
        window.Show();
    }

    void OnGUI()
    {
        // Find all animators in the scene
        _animators = FindObjectsOfType<Animator>().ToList();

        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

        GUILayout.Label($"Animators : {_animators.Count}", EditorStyles.boldLabel);

        // List all animators in the scene
        ListAnimators();

        // List all animations of the selected animator
        ListAnimations();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void ListAnimators()
    {
        foreach (var a in _animators)
        {
            if (GUILayout.Button(a.name))
                Selection.activeGameObject = a.gameObject;
        }

    }
    void ListAnimations()
    {
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
    }
}
