using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class DropDownAnimClipsMenu : DropDownMenu
{
    public AnimationClip animationClip;

    DropDownAnimClipsMenu()
    {
        label = "Select an animation";
    }
  
    public override void DrawDropDown()
    {
        if (!animator.runtimeAnimatorController)
            return;

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

    public override void DropDownButton()
    {
        if (!animator)
            return;

        if (!animator.runtimeAnimatorController)
            return;

        if (EditorGUILayout.DropdownButton(new GUIContent(label), FocusType.Passive))
            showDropDown = !showDropDown;
    }

    public override void PopulateDropDown(int unusedWindowID)
    {
        if (!animator)
            return;

        if (!animator.runtimeAnimatorController)
            return;

        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true);

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        foreach (var a in clips)
        {
            if (GUILayout.Button(a.name))
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
