using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DropDownAnimClipsMenu : DropDownMenu
{
    public Animator[] animators;
    public Animator animator;
    public AnimationClip animationClip;

    DropDownAnimClipsMenu()
    {
        label = "Select an animation";
    }
  
    public override void DrawDropDown()
    {
        BeginWindows();
        rect = GUILayout.Window(123, rect, PopulateDropDown, "");

        if (Event.current.type == EventType.MouseDown)
        {
            if (!rect.Contains(Event.current.mousePosition))
            {
                showDropDown = false;
            }
        }

        EndWindows();
    }

    public override void PopulateDropDown(int unusedWindowID)
    {
        if (!animator)
            return;

        /*if (!Selection.activeGameObject)
            return;

        if (!Selection.activeGameObject.TryGetComponent(out animator))
            return;*/

        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true);

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        foreach (var a in clips)
        {
            if (GUILayout.Button(a.name, GUILayout.ExpandWidth(true)))
            {
                label = a.name;
                showDropDown = false;
                Selection.activeObject = animator.gameObject;
                animationClip = a;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    public override void Reset()
    {
        
    }
}
