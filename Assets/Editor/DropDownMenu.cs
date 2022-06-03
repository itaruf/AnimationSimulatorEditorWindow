using UnityEditor;
using UnityEditor.IMGUI.Controls;
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

    public SearchField searchField;

    public abstract void DropDownButton();

    public abstract void DrawDropDown();

    public abstract void PopulateDropDown(int unusedWindowID);

    public abstract void Reset();

    public virtual void SearchField()
    {
        if (!showDropDown)
            return;

        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (searchField == null)
            searchField = new SearchField();

       searchField.OnToolbarGUI("Find an animator...");

        GUILayout.EndHorizontal();
    }
}