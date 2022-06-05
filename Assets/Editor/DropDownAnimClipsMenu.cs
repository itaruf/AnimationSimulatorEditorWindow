using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class DropDownAnimClipsMenu : DropDownMenu
{
    public AnimationClip animationClip;

    DropDownAnimClipsMenu()
    {
        label = "Select an animation";
        rect = new Rect(0, 80, 200, 200);
    }

    public override void DropDownButton()
    {
        if (!animator)
            return;

        if (!animator.runtimeAnimatorController)
            return;

        if (EditorGUILayout.DropdownButton(new GUIContent(label), FocusType.Passive))
            showDropDown = !showDropDown;

        SearchField();
    }

    public override void DrawDropDown()
    {
        if (!animator.runtimeAnimatorController)
            return;

        onOpeningDropDown?.Invoke();

        BeginWindows();
        rect = GUILayout.Window(123, rect, PopulateDropDown, "");

        if (Event.current.type == EventType.MouseDown)
        {
            if (!rect.Contains(Event.current.mousePosition))
                showDropDown = false;
        }

        EndWindows();
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
            if (!a.name.ToUpper().Contains(strResult.ToUpper()) && searchField.HasFocus())
                continue;

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
        if (animationClip && animator)
        {
            animationClip.SampleAnimation(animator.gameObject, 0);
            if (animator.runtimeAnimatorController)
                animationClip = animator.runtimeAnimatorController.animationClips[0];
        }

        if (Selection.activeGameObject)
        {
            if (Selection.activeGameObject.TryGetComponent(out animator))
            {
                if (!animator.runtimeAnimatorController)
                {
                    /*animationClip = null;*/
                    label = "Select an animation";
                    return;
                }
                animationClip = animator.runtimeAnimatorController.animationClips[0];
                label = animationClip.name;
                Selection.activeGameObject = animator.gameObject;
                return;
            }
           /* else
            {
                Debug.Log("HERE");
                *//*animationClip = null;*//*
                label = "Select an animation";
                return;
            }*/
        }
        CloseDropDown();
    }
}
