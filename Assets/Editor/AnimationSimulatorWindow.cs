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
   
    // Scrollbar
    Vector2 scrollPos = Vector2.zero;

    // Menus
    static DropDownAnimatorsMenu animatorsMenu;
    static DropDownAnimClipsMenu animClipsMenu;

    List<DropDownMenu> dropdownMenus = new List<DropDownMenu>();

    public static Stopwatch stopwatch = new Stopwatch();

    static EditorApplication.HierarchyWindowItemCallback hierarchyItemCallback;

    static List<AnimatorEditor> animatorEditors = new List<AnimatorEditor>();

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
        GetAnimatorsInScene();
    }

    void OnGUI()
    {
        if (Application.isPlaying)
            Close();

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

        foreach (var a in animatorEditors)
        {
            if (a.animator.gameObject.GetInstanceID() == Selection.activeInstanceID)
            {
                if (animatorsMenu.animator.gameObject.GetInstanceID() == a.animator.gameObject.GetInstanceID())
                {
                    a.animationClip = animClipsMenu.animationClip;
                    a.PrintAnimClipData();
                    a.PlayClipBtn();
                    a.StopClipBtn();
                    a.RestartClipBtn();
                }
            }
        }

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


   
    static void Reset()
    {
       /* bool result = false;
        if (Selection.activeGameObject)
            result = Selection.activeGameObject.TryGetComponent(out animator);
        else
            return;

        if (result && !animator.runtimeAnimatorController)
        {
            *//*EditorApplication.update -= RestartAnimationClip;
            EditorApplication.update -= PlayAnimationClip;*/
            /*sliderAnimSpeed = 1;
            sliderAnimTimestamp = 0;
            stopwatch.Reset();
            isPlaying = false;
            isPaused = false;*//*
        }*/
    }

    // Get all animators from gameobjects in the scene
    Animator[] GetAnimatorsInScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid()) return null;

        List<Animator> AnimatorList = new List<Animator>();

        GameObject[] rootGameObjects = scene.GetRootGameObjects();
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            AnimatorList.AddRange(rootGameObject.GetComponentsInChildren<Animator>(true));
        }

        foreach (var a in AnimatorList)
        {
            bool exist = false;
            foreach (var aE in animatorEditors)
            {
                if (aE.animator == a)
                {
                    exist = true;
                    break;
                }
            }

            if (!exist)
            {
                var aE = new AnimatorEditor(a);
                animatorEditors.Add(aE);
                /*EditorSceneManager.sceneClosing += aE.OnClosing;*/
            }
        }

        /*UnityEngine.Debug.Log(animatorEditors.Count);*/

        return AnimatorList.ToArray();
    }

    static void OnSceneClosing()
    {
        /*EditorApplication.update -= RestartAnimationClip;
        EditorApplication.update -= PlayAnimationClip;*/
      /*  sliderAnimSpeed = 1;
        sliderAnimTimestamp = 0;
        stopwatch.Reset();
        isPlaying = false;
        isPaused = false;*/
        Selection.activeGameObject = null;
        animatorsMenu.Reset();
        animClipsMenu.Reset();
    }

    static void SceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        UnityEngine.Debug.Log("SceneOpened");
        Selection.activeGameObject = null;
        animatorEditors.Clear();
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