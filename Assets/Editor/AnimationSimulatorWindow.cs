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
    public static Animator animator = new Animator();
    static bool isPlaying = false;
    static bool isPaused = false;
    static double timeElapsed;

    static float sliderAnimSpeed = 1.0f;
    static float sliderAnimTimestamp = 1.0f;

    static bool animLoopBtn = true;

    // Scrollbar
    Vector2 scrollPos = Vector2.zero;

    // Menus
    static DropDownAnimatorsMenu animatorsMenu;
    static DropDownAnimClipsMenu animClipsMenu;

    List<DropDownMenu> dropdownMenus = new List<DropDownMenu>();

    public static Stopwatch stopwatch = new Stopwatch();

    static EditorApplication.HierarchyWindowItemCallback hierarchyItemCallback;


    // Subscribing to events
    static AnimationSimulatorWindow()
    {
        EditorSceneManager.sceneClosing += SceneClosing;
        EditorSceneManager.sceneOpening += SceneOpening;
        EditorSceneManager.sceneOpened += SceneOpened;
        EditorApplication.playModeStateChanged += LogPlayModeState;

        hierarchyItemCallback = new EditorApplication.HierarchyWindowItemCallback(Highlight);

        EditorApplication.hierarchyWindowItemOnGUI = (EditorApplication.HierarchyWindowItemCallback)Delegate.Combine(EditorApplication.hierarchyWindowItemOnGUI, hierarchyItemCallback);

        /*EditorApplication.update += OnEditorUpdate;*/
    }

    static void Highlight(int instanceID, Rect selectionRect)
    {
        GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (!gameObject)
            return;

        if (!animatorsMenu)
            return;

        if (!animatorsMenu.animator)
            return;

        if (gameObject.GetInstanceID() == animatorsMenu.animator.gameObject.GetInstanceID())
            EditorGUIUtility.PingObject(gameObject);
    }

    [MenuItem("Window/Animator Simulator")]
    static void ShowWindow()
    {
        if (Application.isPlaying)
            return;
      
        window = (AnimationSimulatorWindow)GetWindowWithRect(typeof(AnimationSimulatorWindow), new Rect(0, 0, 300, 300));
        window.Show();
    }

    void OnEnable()
    {
        Selection.selectionChanged += Reset;
    }

    void Update()
    {
        Repaint();
    }

    void OnGUI()
    {
        if (Application.isPlaying)
            this.Close();

        /*********DropDown Menus instanciation*********/
        if (!animatorsMenu)
        {
            animatorsMenu = CreateInstance<DropDownAnimatorsMenu>();

            if (!dropdownMenus.Contains(animatorsMenu))
                dropdownMenus.Add(animatorsMenu);

            Selection.selectionChanged += animatorsMenu.CloseDropDown;
        }

        if (!animClipsMenu)
        {
            animClipsMenu = CreateInstance<DropDownAnimClipsMenu>();

            if (!dropdownMenus.Contains(animClipsMenu))
                dropdownMenus.Add(animClipsMenu);

            Selection.selectionChanged += animClipsMenu.Reset;
            Selection.selectionChanged += animClipsMenu.CloseDropDown;

            animatorsMenu.onOpeningDropDown += animClipsMenu.CloseDropDown;
            animClipsMenu.onOpeningDropDown += animatorsMenu.CloseDropDown;
        }

        // Find all animators in the scene
        animatorsMenu.animators = GetAnimatorsInScene();

        /*********Drawings*********/

        // Scrollbar
        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

        /*EditorGUILayout.LabelField($"Animators in the scene : {animatorsMenu.animators.Length}", EditorStyles.boldLabel);*/

        if (Selection.activeGameObject)
        {
            if (Selection.activeGameObject.GetComponent<Animator>())
            {
                animatorsMenu.animator = Selection.activeGameObject.GetComponent<Animator>();
                animatorsMenu.label = animatorsMenu.animator.gameObject.name + " " + animatorsMenu.animator.gameObject.GetInstanceID().ToString();
                Selection.activeGameObject = animatorsMenu.animator.gameObject;
            }
        }

        animatorsMenu.DropDownButton();
        animClipsMenu.animator = animatorsMenu.animator;
        animClipsMenu.DropDownButton();

        if (animClipsMenu.showDropDown || animatorsMenu.showDropDown)
            goto exit;

        if (!animClipsMenu.animationClip)
            goto exit;

        if (!animClipsMenu.animator)
            goto exit;

        if (!animClipsMenu.animator.runtimeAnimatorController)
            goto exit;

        PrintAnimClipData();

        RestartClipBtn();
        PlayClipBtn();
        StopClipBtn();

        exit:

        if (animatorsMenu.showDropDown)
            animatorsMenu.DrawDropDown();

        if (animatorsMenu.animator && animClipsMenu.showDropDown)
            animClipsMenu.DrawDropDown();

        // If an animator was selected, make sure to retarget its gameobject when going back in the anim window
        SetFocusBackToGameObject();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    static void SetFocusBackToGameObject()
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

        EditorGUILayout.Space();

        EditorGUILayout.LabelField($"[Preview]", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"Animation Speed", EditorStyles.label);
        sliderAnimSpeed = EditorGUILayout.Slider(sliderAnimSpeed, 0, 2);

        EditorGUILayout.LabelField($"Animation Starting Timestamp", EditorStyles.label);
        sliderAnimTimestamp = EditorGUILayout.Slider(sliderAnimTimestamp, 0, animClipsMenu.animationClip.length);

        EditorGUILayout.LabelField($"Animation timestamp : {Math.Round(timeElapsed, 2)}", EditorStyles.label);

        animLoopBtn = EditorGUILayout.Toggle("Loop Animation", animLoopBtn);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField($"[Animation Data]", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"Animation total length : {Math.Round(animClipsMenu.animationClip.length, 2)}", EditorStyles.label);
        EditorGUILayout.LabelField($"Is animation set as looping : {animClipsMenu.animationClip.isLooping}", EditorStyles.label);

        EditorGUILayout.Space();
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

        bool btn = false;
        GUIStyle buttonStyle;

        if (!isPlaying)
        {
            GUI.backgroundColor = new Color(1, 1, 1);
            buttonStyle = new GUIStyle(GUI.skin.button);
            btn = GUILayout.Button("Play", buttonStyle);
        }

        else
        {
            GUI.backgroundColor = Color.red;
            buttonStyle = new GUIStyle(GUI.skin.button);
            btn = GUILayout.Button("Play", buttonStyle);
        }

        if (btn)
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

        bool btn = false;
        GUIStyle buttonStyle;

        if (!isPaused)
        {
            GUI.backgroundColor = new Color(1, 1, 1);
            buttonStyle = new GUIStyle(GUI.skin.button);
            btn = GUILayout.Button("Stop", buttonStyle);
        }

        else
        {
            GUI.backgroundColor = Color.yellow;
            buttonStyle = new GUIStyle(GUI.skin.button);
            btn = GUILayout.Button("Stop", buttonStyle);
        }

        if (btn)
        {
            if (!isPlaying)
                return;

            if (isPaused)
                return;

            EditorApplication.update -= RestartAnimationClip;
            isPaused = true;
            isPlaying = false;
        }
    }

    static void PlayAnimationClip()
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
    static void RestartAnimationClip()
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

    static void Reset()
    {
        bool result = Selection.activeGameObject.TryGetComponent(out animator);
        if (!result || (result && !animator.runtimeAnimatorController))
        {
            EditorApplication.update -= RestartAnimationClip;
            EditorApplication.update -= PlayAnimationClip;
            sliderAnimSpeed = 1;
            sliderAnimTimestamp = 0;
            stopwatch.Reset();
            isPlaying = false;
            isPaused = false;
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
        Reset();
        Selection.activeGameObject = null;
        animatorsMenu.Reset();
        animClipsMenu.Reset();
    }

    static void SceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        UnityEngine.Debug.Log("SceneOpened");
        Selection.activeGameObject = null;
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