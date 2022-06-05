using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEditor.SceneManagement;
using System.Diagnostics;
using System.Linq;

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

    // Animator Editor Data
    AnimatorEditor animatorEdit;
    static List<AnimatorEditor> animatorEditors = new List<AnimatorEditor>();


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

    // Highlight the last current gameobject with an animator 
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

    }

    void Update()
    {
        Repaint();

        if (!animatorsMenu)
            return;

        // Get all the animators in the scene
        animatorsMenu.animators = GetAnimatorsInScene();
    }

    void OnGUI()
    {
        // Stopping the application when playing
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

        // Drawing drop down buttons (animation one only if there's an animator selected
        animatorsMenu.DropDownButton();
        animClipsMenu.animator = animatorsMenu.animator;
        animClipsMenu.DropDownButton();

        /*Skips*/

        if (animClipsMenu.showDropDown || animatorsMenu.showDropDown)
            goto exit;

        if (!animClipsMenu.animationClip)
            goto exit;

        if (!animClipsMenu.animator)
            goto exit;

        if (!animClipsMenu.animator.runtimeAnimatorController)
            goto exit;

        //Deleting the references without animators
        if (animatorsMenu.animators != null)
        {
            for (int i = 0; i < animatorsMenu.animators.Length; ++i)
            {
                if (animatorsMenu.animators[i] == null)
                {
                    animatorsMenu.label = "Select an animator";
                    animatorsMenu.animators = RemoveAt(animatorsMenu.animators, i);
                }
            }
        }

        // Deleting the references without animators*/
        for (int i = 0; i < animatorEditors.Count; ++i)
        {
            if (animatorEditors[i].animator == null)
            {
                animatorEditors.RemoveAt(i);
                i--;
            }
            // Selecting the current selected gameobject with animator in the scene*/
            else 
            {
                if (animatorEditors[i].animator.gameObject.GetInstanceID() == Selection.activeInstanceID)
                {
                    if (animatorsMenu.animator.gameObject.GetInstanceID() == animatorEditors[i].animator.gameObject.GetInstanceID())
                    {
                        animatorEdit = animatorEditors[i];
                        animatorEditors[i].animationClip = animClipsMenu.animationClip;
                    }
                }
            }
        }

        // Printing its animation data
        if (animatorEdit)
        {
            animatorEdit.PrintAnimClipData();
            animatorEdit.PlayClipBtn();
            animatorEdit.StopClipBtn();
            animatorEdit.RestartClipBtn();
        }

        // We are currently selecting an item in one of the drop downs
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

    // Removing an item from a container
    public T[] RemoveAt<T>(T[] arr, int index)
    {
        return arr.Where((e, i) => i != index).ToArray();
    }

    // Focusing back a gameobject
    static void SetFocusBackToGameObject()
    {
        if (Event.current.type == EventType.MouseDown)
        {
            if (animatorsMenu.animator)
                Selection.activeGameObject = animatorsMenu.animator.gameObject;
        }
    }


    // Get all animators from all the gameobjects in the scene
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

        // Filling the animator editors at the same time
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
            }
        }

        return AnimatorList.ToArray();
    }

    static void OnSceneClosing()
    {
        Selection.activeGameObject = null;

        if (animatorsMenu)
            animatorsMenu.Reset();

        if (animClipsMenu)
            animClipsMenu.Reset();
    }

    static void SceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        /*UnityEngine.Debug.Log("SceneOpened");*/

        // Find all animators in the scene
        if (!animatorsMenu)
            animatorsMenu.animators = GetAnimatorsInScene();

        Selection.activeGameObject = null;

        if (animatorEditors != null)
            animatorEditors.Clear();
    }

    static void SceneOpening(string path, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        /*UnityEngine.Debug.Log("SceneOpening");*/
    }
   
    static void SceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
    {
        /*UnityEngine.Debug.Log("SceneClosing");*/
        OnSceneClosing();
    }

    static void LogPlayModeState(PlayModeStateChange state)
    {
        OnSceneClosing();
    }
}

#endif