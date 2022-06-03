using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class DropDownAnimatorsMenu : DropDownMenu
{
    public Animator[] animators;
    public Animator animator;
    DropDownAnimatorsMenu()
    {
        label = "Select an animator";
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
        foreach (var a in animators)
        {
            if (GUILayout.Button(a.name))
            {
                showDropDown = false;
                Selection.activeObject = a.gameObject;
                /*isAnimatorSelected = true;*/
                animator = a;
                label = a.name;
            }
        }
    }

    public override void Reset()
    {
    }
}
