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
                showDropDown = false;
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
        {
            showDropDown = !showDropDown;
            Selection.activeObject = animator.gameObject;
        }
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
                CloseDropDown();
                Selection.activeObject = animator.gameObject;
                animationClip = a;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    public override void Reset()
    {
        if (Selection.activeGameObject)
        {
            if (Selection.activeGameObject.TryGetComponent(out animator))
            {
                if (!animator.runtimeAnimatorController)
                {
                    animationClip = null;
                    label = "Select an animation";
                    Debug.Log(2);
                    return;
                }
                animationClip = animator.runtimeAnimatorController.animationClips[0];
                label = animationClip.name;
                Selection.activeGameObject = animator.gameObject;
                Debug.Log(3);
                return;
            }
            else
            {
                animationClip = null;
                label = "Select an animation";
                Debug.Log(4);
                return;
            }
        }
    }
}
