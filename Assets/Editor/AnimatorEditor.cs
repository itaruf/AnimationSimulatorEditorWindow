using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class AnimatorEditor : EditorWindow
{
    public Animator animator;
    public AnimationClip animationClip;
    public bool isPlaying = false;
    public bool isPaused = false;
    public double timeElapsed = 0;
    public  Stopwatch stopwatch = new Stopwatch();

    public float sliderAnimSpeed = 1.0f;
    public float sliderAnimTimestamp = 0f;

     bool animLoopBtn = true;

    public AnimatorEditor(Animator a)
    {
        animator = a;

        if (!animator.runtimeAnimatorController)
            return;

        if (animator.runtimeAnimatorController.animationClips.Length <= 0)
            return;

        animationClip = animator.runtimeAnimatorController.animationClips[0];
    }

    public void PlayClipBtn()
    {
        if (!animationClip)
            return;

        bool btn = false;
        GUIStyle buttonStyle;

        if (!isPlaying)
        {
            GUI.backgroundColor = new Color(1, 1, 1);
            buttonStyle = new GUIStyle(GUI.skin.button);
            btn = GUILayout.Button("Play", buttonStyle);
        }

        else
        {
            GUI.backgroundColor = Color.red;
            buttonStyle = new GUIStyle(GUI.skin.button);
            btn = GUILayout.Button("Play", buttonStyle);
        }

        if (btn)
        {
            EditorApplication.update -= PlayAnimationClip;
            EditorApplication.update -= RestartAnimationClip;
            EditorApplication.update += PlayAnimationClip;

            isPaused = false;
            stopwatch.Start();

            PlayAnimationClip();
        }
    }

    public void StopClipBtn()
    {
        if (!animationClip)
            return;

        bool btn = false;
        GUIStyle buttonStyle;

        if (!isPaused)
        {
            GUI.backgroundColor = new Color(1, 1, 1);
            buttonStyle = new GUIStyle(GUI.skin.button);
            btn = GUILayout.Button("Stop", buttonStyle);
        }

        else
        {
            GUI.backgroundColor = Color.yellow;
            buttonStyle = new GUIStyle(GUI.skin.button);
            btn = GUILayout.Button("Stop", buttonStyle);
        }

        if (btn)
        {
            if (!isPlaying)
                return;

            if (isPaused)
                return;

            EditorApplication.update -= RestartAnimationClip;
            isPaused = true;
            isPlaying = false;
        }
    }

    // Restart Button to restart the current animation clip
    public void RestartClipBtn()
    {
        if (!animationClip)
            return;
        
        
        if (!animator.runtimeAnimatorController)
        {
            timeElapsed = 0;
            EditorApplication.update -= PlayAnimationClip;
            isPlaying = false;
            stopwatch.Reset();
            stopwatch.Stop();
            return;
        }

        GUIStyle buttonStyle;
        GUI.backgroundColor = new Color(1, 1, 1);
        buttonStyle = new GUIStyle(GUI.skin.button);
        bool btn = GUILayout.Button("Restart", buttonStyle);

        if (btn)
        {
            EditorApplication.update -= RestartAnimationClip;
            EditorApplication.update -= PlayAnimationClip;
            EditorApplication.update += RestartAnimationClip;

            sliderAnimTimestamp = 0;
            isPaused = false;
            stopwatch.Restart();

            RestartAnimationClip();
        }
    }

    public void PlayAnimationClip()
    {
        if (!animator)
        {
            Reset();
            return;
        }

        if (!animationClip)
        {
            Reset();
            return;
        }

        if (!animator.runtimeAnimatorController)
        {
            Reset();
            return;
        }

        // The animation is now playing
        if (!isPlaying && !isPaused)
            isPlaying = true;

        else
        {
            if (isPaused)
                stopwatch.Stop();

            // Play the animation at a specific timestamp
            timeElapsed = sliderAnimSpeed * (stopwatch.Elapsed.TotalSeconds + sliderAnimTimestamp);
            animationClip.SampleAnimation(animator.gameObject, (float)timeElapsed);

            // Loop animation - Restarting chrono
            if (timeElapsed >= animationClip.length && animLoopBtn)
            {
                stopwatch.Restart();
            }

            // Stoping the animation from playing 
            if (timeElapsed >= animationClip.length && !animLoopBtn)
            {
                timeElapsed = 0;
                EditorApplication.update -= PlayAnimationClip;
                isPlaying = false;
                stopwatch.Reset();
                stopwatch.Stop();
            }
        }
    }

    public  void RestartAnimationClip()
    {
        if (!animator)
        {
            Reset();
            return;
        }

        if (!animationClip)
        {
            Reset();
            return;
        }

        if (!animator.runtimeAnimatorController)
        {
            Reset();
            return;
        }

        // The animation is now playing
        if (!isPlaying)
            isPlaying = true;

        if (isPaused)
            return;


        // Restart at the beginning of the animation clip
        timeElapsed = sliderAnimSpeed * stopwatch.Elapsed.TotalSeconds;
        animationClip.SampleAnimation(animator.gameObject, (float)timeElapsed);

        // Loop animation - Restarting chrono
        if (timeElapsed >= animationClip.length && animLoopBtn)
            stopwatch.Restart();

        // Stoping the animation from playing 
        if (timeElapsed >= animationClip.length && !animLoopBtn)
        {
            timeElapsed = 0;
            EditorApplication.update -= RestartAnimationClip;
            isPlaying = false;
            stopwatch.Reset();
            stopwatch.Stop();
        }
    }

    public void PrintAnimClipData()
    {
        if (!animator.runtimeAnimatorController)
            return;

        if (!animationClip)
        {
            if (animator.runtimeAnimatorController.animationClips != null)
                animationClip = animator.runtimeAnimatorController.animationClips[0];
            return;
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField($"[Preview]", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"Animation Speed", EditorStyles.label);
        sliderAnimSpeed = EditorGUILayout.Slider(sliderAnimSpeed, 0, 2);

        EditorGUILayout.LabelField($"Animation Starting Timestamp", EditorStyles.label);
        sliderAnimTimestamp = EditorGUILayout.Slider(sliderAnimTimestamp, 0, animationClip.length);

        EditorGUILayout.LabelField($"Animation timestamp : {Math.Round(timeElapsed, 2)}", EditorStyles.label);

        animLoopBtn = EditorGUILayout.Toggle("Loop Animation", animLoopBtn);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField($"[Animation Data]", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"Animation total length : {Math.Round(animationClip.length, 2)}", EditorStyles.label);
        EditorGUILayout.LabelField($"Is animation set as looping : {animationClip.isLooping}", EditorStyles.label);

        EditorGUILayout.Space();
    }

    public void Reset()
    {
        timeElapsed = 0;
        EditorApplication.update -= PlayAnimationClip;
        EditorApplication.update -= RestartAnimationClip;
        isPlaying = false;
        stopwatch.Reset();
        stopwatch.Stop();
        return;
    }
}

