using System.Collections;
using System.Collections.Generic;
using UnityChan;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Linq;

#if UNITY_EDITOR
[ExecuteInEditMode]
[CustomEditor(typeof(IdleChanger))]
public class AnimationSimulatorWindow : EditorWindow
{
    public Animator[] _animators;
    Vector2 scrollPos = Vector2.zero;
    string t = "This is a string inside a Scroll view!";
    static AnimationSimulatorWindow window;
    bool isPlaying = false;

    // Subscribing to events
    static AnimationSimulatorWindow()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += SceneClosing;
    }
    void Start()
    {
        Debug.Log("TEST");
    }

    void Update()
    {
        
    }

    [MenuItem("Window/Animator Simulator")]
    public static void ShowWindow()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += SceneClosing;

        window = (AnimationSimulatorWindow)GetWindowWithRect(typeof(AnimationSimulatorWindow), new Rect(0, 0, 300, 300));
        window.Show();
    }

    void Init()
    {

    }

    void OnGUI()
    {
        // Find all animators in the scene
        _animators = GetAnimatorsInScene();

        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

        GUILayout.Label($"Animators : {_animators.Length}", EditorStyles.boldLabel);

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
                AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
                foreach (var a in clips)
                {
                    if (GUILayout.Button(a.name))
                    {
                        isPlaying = false;
                        PlayClip(a, animator);
                    }
                }
            }
        }
    }

    void PlayClip(AnimationClip clip, Animator animator)
    {
        isPlaying = true;

        clip.SampleAnimation(animator.gameObject, Time.deltaTime);
        animator.Update(Time.deltaTime);
    }

    static Animator[] GetAnimatorsInScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid()) return null;

        List<Animator> AnimatorList = new List<Animator>();

        GameObject[] rootGameObjects = scene.GetRootGameObjects();
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            AnimatorList.AddRange(rootGameObject.GetComponentsInChildren<Animator>(true));
        }

        return AnimatorList.ToArray();
    }

    static void SceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
    {
        Debug.Log("SceneClosing");
    }
}

#endif