using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public abstract class DropDownMenu : EditorWindow
{
    public bool showDropDown = false;
    public string label = " ";
    public Rect rect = new Rect(100, 100, 250, 500);
    public Vector2 scrollPos = Vector2.zero;

    public Animator[] animators;
    public Animator animator;



    public abstract void DropDownButton();

    public abstract void DrawDropDown();

    public abstract void PopulateDropDown(int unusedWindowID);

    public abstract void Reset();
}