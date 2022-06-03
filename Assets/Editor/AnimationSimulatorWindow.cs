using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEditor.SceneManagement;
using System.Diagnostics;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class AnimationSimulatorWindow : EditorWindow
{
    static AnimationSimulatorWindow window;

    // Animation Data
    public Animator animator = new Animator();
    static bool isPlaying = false;
    static bool isPaused = false;
    double timeElapsed;

    static float sliderAnimSpeed = 1.0f;
    static float sliderAnimTimestamp = 1.0f;

    bool animLoopBtn = true;

    // Scrollbar
    Vector2 scrollPos = Vector2.zero;

    // Menus
    static DropDownAnimatorsMenu animatorsMenu;
    static DropDownAnimClipsMenu animClipsMenu;

    List<DropDownMenu> dropdownMenus = new List<DropDownMenu>();

    public Stopwatch stopwatch = new Stopwatch();

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
        if (Application.isPlaying)
            return;
      
        window = (AnimationSimulatorWindow)GetWindowWithRect(typeof(AnimationSimulatorWindow), new Rect(0, 0, 300, 300));
        window.Show();
    }

    private void OnEnable()
    {
        Selection.selectionChanged += Reset;
    }

    private void Update()
    {
        Repaint();
    }

    void OnGUI()
    {
        if (Application.isPlaying)
            this.Close();

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
            animatorsMenu.onAnimatorChange += animClipsMenu.Reset;
            Selection.selectionChanged += animClipsMenu.Reset;
        }

        // Find all animators in the scene
        animatorsMenu.animators = GetAnimatorsInScene();

        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

        GUILayout.Label($"Animators : {animatorsMenu.animators.Length}", EditorStyles.boldLabel);

        if (Selection.activeGameObject)
        {
            if (Selection.activeGameObject.TryGetComponent(out animatorsMenu.animator))
            {
                animatorsMenu.label = animatorsMenu.animator.gameObject.name + " " + animatorsMenu.animator.gameObject.GetInstanceID().ToString();
                Selection.activeGameObject = animatorsMenu.animator.gameObject;
            }
        }

        /*if (animatorsMenu)*/
            animatorsMenu.DropDownButton();

        if (animatorsMenu.showDropDown)
            animatorsMenu.DrawDropDown();

        if (animatorsMenu.animator)
        {
            animClipsMenu.animator = animatorsMenu.animator;

            animClipsMenu.DropDownButton();

            // Print the animation clips list only if we click on the drop down button
            if (animClipsMenu.showDropDown)
                animClipsMenu.DrawDropDown();

            // Get more data when there's an animation clip selected
            if (animClipsMenu.animationClip)
            {
                if (animClipsMenu.animator.runtimeAnimatorController)
                {

                    GUILayout.Label($"[Preview] Current Animation Speed", EditorStyles.boldLabel);
                    sliderAnimSpeed = EditorGUILayout.Slider(sliderAnimSpeed, 0, 2);

                    GUILayout.Label($"[Preview] Animation Starting Timestamp", EditorStyles.boldLabel);
                    sliderAnimTimestamp = EditorGUILayout.Slider(sliderAnimTimestamp, 0, animClipsMenu.animationClip.length);
                    PrintAnimClipData();

                    animLoopBtn = EditorGUILayout.Toggle("[Preview] Loop Animation", animLoopBtn);

                    RestartClipBtn();
                    PlayClipBtn();
                    StopClipBtn();
                }
            }
        }

        // If an animator was selected, make sure to retarget its gameobject when going back in the anim window
        SetFocusBackToGameObject();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private static void SetFocusBackToGameObject()
    {
        if (Event.current.type == EventType.MouseDown)
        {
            if (animatorsMenu.animator)
                Selection.activeGameObject = animatorsMenu.animator.gameObject;
        }
    }

    void PrintAnimClipData()
    {
        if (!animClipsMenu.animationClip)
            return;

        if (!animClipsMenu.animator.runtimeAnimatorController)
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
            EditorApplication.update -= RestartAnimationClip;
            EditorApplication.update -= PlayAnimationClip;
            EditorApplication.update += RestartAnimationClip;

            sliderAnimTimestamp = 0;
            isPaused = false;
            stopwatch.Restart();

            RestartAnimationClip();
        }
    }

    void PlayClipBtn()
    {
        if (!animClipsMenu.animationClip)
            return;

        if (GUILayout.Button("Play"))
        {
            EditorApplication.update -= PlayAnimationClip;
            EditorApplication.update -= RestartAnimationClip;
            EditorApplication.update += PlayAnimationClip;

            isPaused = false;
            stopwatch.Start();

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

    private void PlayAnimationClip()
    {
        if (!animatorsMenu.animator)
            return;

        if (!animClipsMenu.animationClip)
            return;

        // The animation is now playing
        if (!isPlaying && !isPaused)
            isPlaying = true;

        else
        {
            if (isPaused)
                stopwatch.Stop();

            // Play the animation at a specific timestamp
            timeElapsed = sliderAnimSpeed * (stopwatch.Elapsed.TotalSeconds + sliderAnimTimestamp);
            animClipsMenu.animationClip.SampleAnimation(animatorsMenu.animator.gameObject, (float)timeElapsed);

            // Loop animation - Restarting chrono
            if (timeElapsed >= animClipsMenu.animationClip.length && animLoopBtn)
            {
                stopwatch.Restart();
            }

            // Stoping the animation from playing 
            if (timeElapsed >= animClipsMenu.animationClip.length && !animLoopBtn)
            {
                timeElapsed = 0;
                EditorApplication.update -= PlayAnimationClip;
                isPlaying = false;
                stopwatch.Reset();
                stopwatch.Stop();
            }
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
            timeElapsed = sliderAnimSpeed * stopwatch.Elapsed.TotalSeconds;
            animClipsMenu.animationClip.SampleAnimation(animatorsMenu.animator.gameObject, (float)timeElapsed);

            // Loop animation - Restarting chrono
            if (timeElapsed >= animClipsMenu.animationClip.length && animLoopBtn)
                stopwatch.Restart();

            // Stoping the animation from playing 
            if (timeElapsed >= animClipsMenu.animationClip.length && !animLoopBtn)
            {
                timeElapsed = 0;
                EditorApplication.update -= RestartAnimationClip;
                isPlaying = false;
                stopwatch.Reset();
                stopwatch.Stop();
            }
        }
    }

    public void Reset()
    {
        stopwatch.Reset();
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
        UnityEngine.Debug.Log("SceneOpened");
        Selection.activeGameObject = null;
        /*animatorsMenu = null;*/
        animClipsMenu = null;
        animatorsMenu.animators = GetAnimatorsInScene();
    }

    static void SceneOpening(string path, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        UnityEngine.Debug.Log("SceneOpening");
    }
   
    static void SceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
    {
        UnityEngine.Debug.Log("SceneClosing");
        OnSceneClosing();
    }

    static void LogPlayModeState(PlayModeStateChange state)
    {
        OnSceneClosing();
    }
}

#endif