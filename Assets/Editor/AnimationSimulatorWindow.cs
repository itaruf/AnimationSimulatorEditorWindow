using System.Collections;
using System.Collections.Generic;
using UnityChan;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Linq;
using System;
using UnityEditor.SceneManagement;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class AnimationSimulatorWindow : EditorWindow
{
    static AnimationSimulatorWindow window;

    // Animation Data
    static bool isPlaying = false;
    static bool isPaused = false;
    static double endTime;
    double timeElapsed;

    static float sliderAnimSpeed = 1.0f;
    static float sliderAnimTimestamp = 1.0f;

    bool animLoopBtn = true;

    // Scrollbar
    Vector2 scrollPos = Vector2.zero;

    // Menus
    DropDownAnimatorsMenu animatorsMenu;
    DropDownAnimClipsMenu animClipsMenu;

    List<DropDownMenu> dropdownMenus = new List<DropDownMenu>();

    // Subscribing to events
    static AnimationSimulatorWindow()
    {
        EditorSceneManager.sceneClosing += SceneClosing;
        EditorSceneManager.sceneOpening += SceneOpening;
        EditorSceneManager.sceneOpened += SceneOpened;
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    [MenuItem("Window/Animator Simulator")]
    private static void ShowWindow()
    {
        window = (AnimationSimulatorWindow)GetWindowWithRect(typeof(AnimationSimulatorWindow), new Rect(0, 0, 300, 300));
        window.Show();
    }

    private void Update()
    {
        Repaint();
    }

    void OnGUI()
    {
        /*Instanciation des dropdown menus*/
        if (!animatorsMenu)
        {
            animatorsMenu = CreateInstance<DropDownAnimatorsMenu>();
            if (!dropdownMenus.Contains(animatorsMenu))
                dropdownMenus.Add(animatorsMenu);
        }

        if (!animClipsMenu)
        {
            animClipsMenu = CreateInstance<DropDownAnimClipsMenu>();
            if (!dropdownMenus.Contains(animClipsMenu))
                dropdownMenus.Add(animClipsMenu);
        }

        // Find all animators in the scene
        animatorsMenu.animators = GetAnimatorsInScene();

        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

        GUILayout.Label($"Animators : {animatorsMenu.animators.Length}", EditorStyles.boldLabel);

        if (Selection.activeGameObject)
        {
            if (!Selection.activeGameObject.TryGetComponent(out animatorsMenu.animator))
            {
                ResetData();
            }
            else
            {
                animClipsMenu.animator = animatorsMenu.animator;
                animatorsMenu.label = animatorsMenu.animator.gameObject.name + " " + animatorsMenu.animator.gameObject.GetInstanceID().ToString();
                Selection.activeGameObject = animatorsMenu.animator.gameObject;
            }
        }

        else
        {
            isPlaying = false;
            EditorApplication.update -= RestartAnimationClip;
            EditorApplication.update -= PlayAnimationClip;
        }

        animatorsMenu.DropDownButton();

        if (animatorsMenu.showDropDown)
            animatorsMenu.DrawDropDown();

        if (animatorsMenu.animator)
        {
            animClipsMenu.DropDownButton();

            if (animClipsMenu.showDropDown)
                animClipsMenu.DrawDropDown();

            if (animClipsMenu.animationClip)
            {
                GUILayout.Label($"Current Animation Speed", EditorStyles.boldLabel);
                sliderAnimSpeed = EditorGUILayout.Slider(sliderAnimSpeed, 0, 2);

                GUILayout.Label($"Current Animation Timestamp", EditorStyles.boldLabel);
                sliderAnimTimestamp = EditorGUILayout.Slider(sliderAnimTimestamp, 0, animClipsMenu.animationClip.length);
                PrintAnimClipData();

                animLoopBtn = EditorGUILayout.Toggle("Loop Animation", animLoopBtn);

            }
        }

        RestartClipBtn();
        PlayClipBtn();
        StopClipBtn();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void PrintAnimClipData()
    {
        if (!animClipsMenu.animationClip)
            return;

        GUILayout.Label($"Current Animation Data", EditorStyles.boldLabel);
        GUILayout.Label($"Animation total length : {animClipsMenu.animationClip.length}", EditorStyles.label);
        GUILayout.Label($"Current Animation timestamp : {Math.Round(timeElapsed, 2)}", EditorStyles.label);
        GUILayout.Label($"Is animation set as Looping: {animClipsMenu.animationClip.isLooping}", EditorStyles.label);
    }

    // Restart Button to restart the current animation clip
    void RestartClipBtn()
    {
        if (!animClipsMenu.animationClip)
            return;

        if (GUILayout.Button("Restart"))
        {
            isPaused = false;

            EditorApplication.update -= RestartAnimationClip;
            EditorApplication.update -= PlayAnimationClip;
            EditorApplication.update += RestartAnimationClip;
            endTime = EditorApplication.timeSinceStartup;
            RestartAnimationClip();
        }
    }

    void PlayClipBtn()
    {
        if (!animClipsMenu.animationClip)
            return;

        if (GUILayout.Button("Play"))
        {
            isPaused = false;

            EditorApplication.update -= PlayAnimationClip;
            EditorApplication.update -= RestartAnimationClip;
            EditorApplication.update += PlayAnimationClip;
            PlayAnimationClip();
        }

    }

    void StopClipBtn()
    {
        if (!animClipsMenu.animationClip)
            return;

        if (GUILayout.Button("Stop"))
        {
            if (!isPlaying)
                return;

            if (isPaused)
                return;

            EditorApplication.update -= RestartAnimationClip;
            /*EditorApplication.update -= PlayAnimationClip;*/
            isPaused = true;
            /*isPlaying = false;*/
        }
    }

    double startTime;
    double pauseTime = 0;
    private void PlayAnimationClip()
    {
        if (!animatorsMenu.animator)
            return;

        if (!animClipsMenu.animationClip)
            return;

        // The animation is now playing
        if (!isPlaying && !isPaused)
        {
            endTime = EditorApplication.timeSinceStartup;
            isPlaying = true;
            pauseTime = 0;
        }

        if (isPaused)
        {
            pauseTime = EditorApplication.timeSinceStartup;
            startTime -= pauseTime - endTime;
        }

        else
        {
            // Play the animation at a specific timestamp
            timeElapsed = sliderAnimSpeed * (startTime - endTime);
            animClipsMenu.animationClip.SampleAnimation(animatorsMenu.animator.gameObject, (float)timeElapsed);

            // Loop animation - Restarting chrono
            if (timeElapsed >= animClipsMenu.animationClip.length && animLoopBtn)
            {
                endTime = EditorApplication.timeSinceStartup;
            }

            // Stoping the animation from playing 
            if (timeElapsed >= animClipsMenu.animationClip.length && !animLoopBtn)
            {
                timeElapsed = 0;
                EditorApplication.update -= PlayAnimationClip;
                isPlaying = false;
            }
        }

        if (isPlaying)
        {
            startTime = EditorApplication.timeSinceStartup;
        }
    }

    /*RESTART*/
    private void RestartAnimationClip()
    {
        if (!animatorsMenu.animator)
            return;

        if (!animClipsMenu.animationClip)
            return;

        // The animation is now playing
        if (!isPlaying)
            isPlaying = true;

        if (!isPaused)
        {
            // Restart at the beginning of the animation clip
            timeElapsed = sliderAnimSpeed * (EditorApplication.timeSinceStartup - endTime);
            animClipsMenu.animationClip.SampleAnimation(animatorsMenu.animator.gameObject, (float)timeElapsed);

            // Loop animation - Restarting chrono
            if (timeElapsed >= animClipsMenu.animationClip.length && animLoopBtn)
            {
                endTime = EditorApplication.timeSinceStartup;
            }

            // Stoping the animation from playing 
            if (timeElapsed >= animClipsMenu.animationClip.length && !animLoopBtn)
            {
                timeElapsed = 0;
                EditorApplication.update -= RestartAnimationClip;
                isPlaying = false;
            }
        }
    }

    static void ResetData()
    {
    }

    void CloseDropDown()
    {
        foreach(var dd in dropdownMenus)
        {
            if (dd.showDropDown)
                dd.showDropDown = false;
        }
    }

    // Get all animators from gameobjects in the scene
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

    static void OnSceneClosing()
    {
        isPlaying = false;
        Selection.activeGameObject = null;
        /*EditorApplication.update -= PlayAnimationClip;*/
    }

    static void SceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        Debug.Log("SceneOpened");
    }
    
    static void SceneOpening(string path, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        Debug.Log("SceneOpening");
    }
   
    static void SceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
    {
        Debug.Log("SceneClosing");
        OnSceneClosing();
    }

    static void LogPlayModeState(PlayModeStateChange state)
    {
        OnSceneClosing();
    }
}

#endif