using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class DropDownAnimatorsMenu : DropDownMenu
{
    public delegate void OnAnimatorChange();
    public OnAnimatorChange onAnimatorChange;

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

    public override void DropDownButton()
    {
        if (!animator)
            return;

        if (EditorGUILayout.DropdownButton(new GUIContent(label), FocusType.Passive))
            showDropDown = !showDropDown;
    }

    public override void PopulateDropDown(int unusedWindowID)
    {
        foreach (var a in animators)
        {
            if (GUILayout.Button(a.name))
            {
                if (animator == a)
                    return;

                showDropDown = false;
                Selection.activeObject = a.gameObject;
                animator = a;
                label = a.name;

                onAnimatorChange?.Invoke();
            }
        }
    }

    public override void Reset()
    {
    }
}
