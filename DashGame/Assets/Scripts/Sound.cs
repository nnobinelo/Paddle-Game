﻿using UnityEngine.Audio;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public AudioClip clip;

    public string name;
    [Range(0,1)]
    public float volume;
    [Range(-3, 3)]
    public float pitch;
    public bool loop;

    [HideInInspector] //hides this variable since source is set in the awake method of the AudioManager
    public AudioSource source;
}
