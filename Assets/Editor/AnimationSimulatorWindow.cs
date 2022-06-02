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
    public Animator[] _animators;
    public Animator _animator;
    public Dictionary<int, Animator> pairs = new Dictionary<int, Animator>();
    public AnimationClip _animationClip;
    static bool isPlaying = false;
    static bool isPaused = false;
    static double endTime;
    double timeElapsed;

    List<bool> dropDowns = new List<bool>(2) { false, false };

    float animSpeed = 2;
    static float sliderAnimSpeed = 1.0f;
    static float sliderAnimTimestamp = 1.0f;

    bool animLoopBtn = true;

    // Scrollbar
    Vector2 scrollPos = Vector2.zero;
    Vector2 scrollPosAnimClips = Vector2.zero;
    Vector2 scrollPosAnimators = Vector2.zero;

    // DropDowns
    static bool showAnimClipsDropDown = false;
    static string animClipLabel = "Select an animation clip";
    public Rect animClipsRect = new Rect(100, 100, 200, 200);

    static bool showAnimatorsDropDown = false;
    static string animatorLabel = "Select an animator";
    public Rect animatorsRect = new Rect(100, 100, 200, 200);

    static bool isAnimatorSelected = false;

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
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpening += SceneOpening;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += SceneOpened;

        window = (AnimationSimulatorWindow)GetWindowWithRect(typeof(AnimationSimulatorWindow), new Rect(0, 0, 300, 300));
        window.Show();
    }

    private void Update()
    {
        Repaint();
    }

    void OnGUI()
    {
        // Find all animators in the scene
        _animators = GetAnimatorsInScene();

        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

        GUILayout.Label($"Animators : {_animators.Length}", EditorStyles.boldLabel);

        if (Selection.activeGameObject)
        {
            if (!Selection.activeGameObject.TryGetComponent(out _animator))
            {
                ResetData();
            }
            else
            {
                isAnimatorSelected = true;
                animatorLabel = _animator.gameObject.name + " " + _animator.gameObject.GetInstanceID().ToString();
                Selection.activeGameObject = _animator.gameObject;
            }
        }

        else
        {
            _animationClip = null;
            isPlaying = false;
            isAnimatorSelected = false;
            showAnimClipsDropDown = false;
            animClipLabel = "Select an animation clip";
            animatorLabel = "Select an animator";
            EditorApplication.update -= RestartAnimationClip;
            EditorApplication.update -= PlayAnimationClip;
        }

        // List all animators in the scene
        ListAnimators();

        // List all animations of the selected animator

        if (isAnimatorSelected)
            ListAnimationClips();

        /*Debug.Log(animatorLabel);*/
        PrintAnimClipData();

        if (_animationClip)
        {
            GUILayout.Label($"Current Animation Speed", EditorStyles.boldLabel);
            sliderAnimSpeed = EditorGUILayout.Slider(sliderAnimSpeed, 0, 2);

            GUILayout.Label($"Current Animation Timestamp", EditorStyles.boldLabel);
            sliderAnimTimestamp = EditorGUILayout.Slider(sliderAnimTimestamp, 0, _animationClip.length);
        }
        animLoopBtn = EditorGUILayout.Toggle("Loop Animation", animLoopBtn);

        dropDowns[0] = showAnimatorsDropDown;
        dropDowns[1] = showAnimClipsDropDown;

        RestartClipBtn();
        PlayClipBtn();
        StopClipBtn();

        /*Debug.Log(dropDowns.Count);*/

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void PrintAnimClipData()
    {
        if (!_animationClip)
            return;

        GUILayout.Label($"Current Animation Data", EditorStyles.boldLabel);
        GUILayout.Label($"Animation total length : {_animationClip.length}", EditorStyles.label);
        GUILayout.Label($"Current Animation timestamp : {Math.Round(timeElapsed, 2)}", EditorStyles.label);
        GUILayout.Label($"Is animation set as Looping: {_animationClip.isLooping}", EditorStyles.label);
    }

    // Restart Button to restart the current animation clip
    void RestartClipBtn()
    {
        if (!_animationClip)
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
        if (!_animationClip)
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
        if (!_animationClip)
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

    void AnimatorsInDropDown(int unusedWindowID)
    {
        /*EditorGUILayout.BeginVertical();
        scrollPosAnimators = EditorGUILayout.BeginScrollView(scrollPosAnimators, false, true);*/

        foreach (var a in _animators)
        {
            if (GUILayout.Button(a.name, GUILayout.ExpandWidth(true)))
            {
                animatorLabel = a.name;
                showAnimatorsDropDown = false;
                Selection.activeObject = a.gameObject;
                isAnimatorSelected = true;
            }
        }

        /*EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();*/
    }

    void ListAnimators()
    {
        if (!showAnimatorsDropDown)
            return;

        if (EditorGUILayout.DropdownButton(new GUIContent(animatorLabel), FocusType.Passive))
        {
            CloseDropDown();

            if (showAnimatorsDropDown)
                showAnimatorsDropDown = false;
            else
                showAnimatorsDropDown = true;
        }

        // Draw Dropdown
        BeginWindows();
        animatorsRect = GUILayout.Window(123, animatorsRect, AnimatorsInDropDown, "");

        if (Event.current.type == EventType.MouseDown)
        {
            if (animatorsRect.Contains(Event.current.mousePosition) == false)
            {
                showAnimatorsDropDown = false;
            }
        }

        EndWindows();
    }

    void AnimClipsInDropDown(int unusedWindowID)
    {
        if (!Selection.activeGameObject)
            return;

        if (!Selection.activeGameObject.TryGetComponent(out _animator))
            return;

        EditorGUILayout.BeginVertical();
        scrollPosAnimClips = EditorGUILayout.BeginScrollView(scrollPosAnimClips, false, true);

        AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;

        foreach (var a in clips)
        {
            if (GUILayout.Button(a.name, GUILayout.ExpandWidth(true)))
            {
                animClipLabel = a.name;
                showAnimClipsDropDown = false;
                Selection.activeObject = _animator.gameObject;

                /*endTime = EditorApplication.timeSinceStartup;*/
                _animationClip = a;

               /* PlayAnimationClip();*/
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void ListAnimationClips()
    {
        if (!isAnimatorSelected)
        {
            showAnimClipsDropDown = false;
            EndWindows();
            return;
        }

        if (EditorGUILayout.DropdownButton(new GUIContent(animClipLabel), FocusType.Passive))
        {
            CloseDropDown();

            if (showAnimClipsDropDown)
                showAnimClipsDropDown = false;
            else
                showAnimClipsDropDown = true;
        }

        if (!showAnimClipsDropDown)
            return;

        // Draw Dropdown
        BeginWindows();

        animClipsRect = GUILayout.Window(123, animClipsRect, AnimClipsInDropDown, "");

        if (Event.current.type == EventType.MouseDown)
        {
            if (animClipsRect.Contains(Event.current.mousePosition) == false)
            {
                showAnimClipsDropDown = false;
            }
        }
        EndWindows();
    }
  
    private void OnEnable()
    {
        dropDowns[0] = showAnimatorsDropDown;
        dropDowns[1] = showAnimClipsDropDown;
    }

    double startTime;
    double pauseTime = 0;
    private void PlayAnimationClip()
    {
        if (!_animator)
            return;

        if (!_animationClip)
            return;

        // The animation is now playing
        if (!isPlaying && !isPaused)
        {
            endTime = EditorApplication.timeSinceStartup;
            isPlaying = true;
            pauseTime = 0;
        }

        if (isPlaying)
        {
            startTime = EditorApplication.timeSinceStartup;
        }

        if (isPaused)
        {
            pauseTime = EditorApplication.timeSinceStartup;
            startTime -= pauseTime;
        }

        else
        {
            // Play the animation at a specific timestamp
            timeElapsed = sliderAnimSpeed * (startTime  - endTime);
            _animationClip.SampleAnimation(_animator.gameObject, (float)timeElapsed);

            // Loop animation - Restarting chrono
            if (timeElapsed >= _animationClip.length && animLoopBtn)
            {
                endTime = EditorApplication.timeSinceStartup;
            }

            // Stoping the animation from playing 
            if (timeElapsed >= _animationClip.length && !animLoopBtn)
            {
                timeElapsed = 0;
                EditorApplication.update -= PlayAnimationClip;
                isPlaying = false;
            }
        }
    }

    /*RESTART*/
    private void RestartAnimationClip()
    {
        if (!_animator)
            return;

        if (!_animationClip)
            return;

        // The animation is now playing
        if (!isPlaying)
            isPlaying = true;

        if (!isPaused)
        {
            // Restart at the beginning of the animation clip
            timeElapsed = sliderAnimSpeed * (EditorApplication.timeSinceStartup - endTime);
            _animationClip.SampleAnimation(_animator.gameObject, (float)timeElapsed);

            // Loop animation - Restarting chrono
            if (timeElapsed >= _animationClip.length && animLoopBtn)
            {
                endTime = EditorApplication.timeSinceStartup;
            }

            // Stoping the animation from playing 
            if (timeElapsed >= _animationClip.length && !animLoopBtn)
            {
                timeElapsed = 0;
                EditorApplication.update -= RestartAnimationClip;
                isPlaying = false;
            }
        }
    }

    private static void ResetData()
    {
        isPlaying = false;
        isAnimatorSelected = false;
        showAnimatorsDropDown = false;
        showAnimClipsDropDown = false;

        animClipLabel = "Select an animation clip";
        animatorLabel = "Select an animator";
    }

    void CloseDropDown()
    {
        for (int i = 0; i < dropDowns.Count; ++i)
        {
            if (dropDowns[i])
            {
                /*Debug.Log(i);*/
                dropDowns[i] = false;
            }
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
        isAnimatorSelected = false;
        showAnimatorsDropDown = false;
        showAnimClipsDropDown = false;

        animClipLabel = "Select an animation clip";
        animatorLabel = "Select an animator";

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
        Debug.Log(state);

        OnSceneClosing();
    }
}

#endif