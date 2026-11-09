/*
First Date: 7/11/2026
Date: 9/11/2026
Author: Tony
Use by: Nothing to set up (code only, no component). ButtonFX and TitleMenu play their sounds through it.
Unity Editor: Nothing to set. Drag sound files into the sound slots of TitleMenu (or a ButtonFX) to hear them.
              An empty slot is simply silent.
Purpose: Plays short menu sounds (hover, click, whoosh) from one shared 2D Audio Source, made the first time a
         sound plays and kept across scenes. Keeps playing while the game is paused.
Requirement: UISound.cs <-x ButtonFX.cs, TitleMenu.cs
*/

using UnityEngine;

public static class UISound
{
    // ===== Fields =====
    static AudioSource source;   // made the first time a sound plays

    // ===== My methods =====

    // Plays a sound once (empty = nothing). volume: 1 = as loud as the file.
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        if (source == null)
        {
            GameObject go = new GameObject("UI Sound");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;          // 2D: the same in both ears, wherever the camera is
            source.ignoreListenerPause = true; // still plays when the game is paused
        }
        source.PlayOneShot(clip, volume);
    }
}
