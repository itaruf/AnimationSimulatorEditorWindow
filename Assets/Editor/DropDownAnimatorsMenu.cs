using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System;

[ExecuteInEditMode]
public class DropDownAnimatorsMenu : DropDownMenu
{
    public delegate void OnAnimatorChange();
    public OnAnimatorChange onAnimatorChange;

    DropDownAnimatorsMenu()
    {
        label = "Select an animator";
        rect = new Rect(0, 60, 200, 200);
    }

    public override void DrawDropDown()
    {
        onOpeningDropDown?.Invoke();

        BeginWindows();
        rect = GUILayout.Window(200, rect, PopulateDropDown, "");

        if (Event.current.type == EventType.MouseDown)
        {
            if (!rect.Contains(Event.current.mousePosition))
                showDropDown = false;
        }
        EndWindows();
    }

    public override void DropDownButton()
    {
        /*if (Selection.activeGameObject)
        {
            if (!Selection.activeGameObject.GetComponent<Animator>())
                label = "Select an animator";
        }*/

        if (EditorGUILayout.DropdownButton(new GUIContent(label), FocusType.Passive))
                showDropDown = !showDropDown;
        SearchField();

    }

    public override void PopulateDropDown(int unusedWindowID)
    {
        EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true);

        foreach (var a in animators)
        {
            if (!a.name.ToUpper().Contains(strResult.ToUpper()) && searchField.HasFocus())
                continue;

            if (GUILayout.Button(a.name))
            {
                if (animator == a)
                    return;

                CloseDropDown();

                Selection.activeObject = a.gameObject;
                animator = a;
                label = a.name;

                onAnimatorChange?.Invoke();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    public override void Reset()
    {
        label = "Select an animator";
        CloseDropDown();
    }
}
