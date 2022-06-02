using System.Collections;
using System.Collections.Generic;
using UnityChan;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Linq;
using System;

#if UNITY_EDITOR
[ExecuteInEditMode]
[CustomEditor(typeof(IdleChanger))]
public class AnimationSimulatorWindow : EditorWindow
{
    static AnimationSimulatorWindow window;

    // Animation Data
    public Animator[] _animators;
    public Animator _animator;
    public AnimationClip _animationClip;
    bool isPlaying = false;
    double endTime;

    // Scrollbar
    Vector2 scrollPos = Vector2.zero;


    // Subscribing to events
    static AnimationSimulatorWindow()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += SceneClosing;
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    [MenuItem("Window/Animator Simulator")]
    public static void ShowWindow()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += SceneClosing;

        window = (AnimationSimulatorWindow)GetWindowWithRect(typeof(AnimationSimulatorWindow), new Rect(0, 0, 300, 300));
        window.Show();
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
        ListAnimationClips();

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

    void ListAnimationClips()
    {
        if (Selection.activeGameObject)
        {
            if (Selection.activeGameObject.TryGetComponent(out _animator))
            {
                AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;
                foreach (var a in clips)
                {
                    if (GUILayout.Button(a.name))
                    {
                        endTime = EditorApplication.timeSinceStartup;

                        _animationClip = a;

                        EditorApplication.update += PlayAnimationClip;
                        isPlaying = true;
                        PlayAnimationClip();
                    }

                    if (GUILayout.Button("STOP"))
                    {
                        EditorApplication.update -= PlayAnimationClip;
                        isPlaying = false;
                    }
                }
            }
        }
    }

    private void PlayAnimationClip()
    {
        if (!_animator)
            return;

        if (!isPlaying)
            return;

        Debug.Log(_animationClip.name);
        Debug.Log(_animator.gameObject.name);

        double timeElapsed = EditorApplication.timeSinceStartup - endTime;

        _animationClip.SampleAnimation(_animator.gameObject, (float) timeElapsed);
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

    void OnSceneClosing()
    {
        isPlaying = false;
        EditorApplication.update -= PlayAnimationClip;
    }

    static void SceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
    {
        Debug.Log("SceneClosing");
        AnimationSimulatorWindow animationSimulator = new AnimationSimulatorWindow();
        animationSimulator.OnSceneClosing();
    }

   static void LogPlayModeState(PlayModeStateChange state)
    {
        Debug.Log(state);
        AnimationSimulatorWindow animationSimulator = new AnimationSimulatorWindow();
        animationSimulator.OnSceneClosing();
    }
}

#endif