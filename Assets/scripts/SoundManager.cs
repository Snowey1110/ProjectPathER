using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    [SerializeField]
    AudioMixer mixer;

    [SerializeField]
    Slider MasterVolume;

    [SerializeField]
    Slider Music;

    [SerializeField]
    Slider SFX;

    public const string Mixer_Master = "MasterVolume";
    public const string Mixer_Music = "MusicVolume";
    public const string Mixer_SFX = "SFXVolume";

    void Start()
    {
        Music.value = PlayerPrefs.GetFloat(SoundController.MUSIC_KEY, 1f);
        SFX.value = PlayerPrefs.GetFloat(SoundController.SFX_KEY, 1f);
    }
    private void Awake()
    {
        MasterVolume.onValueChanged.AddListener(SetMasterVolume);
        Music.onValueChanged.AddListener(SetMusicVolume);
        SFX.onValueChanged.AddListener(SetSFXVolume);
    }

    private void OnDisable()
    {
        PlayerPrefs.SetFloat(SoundController.MUSIC_KEY, Music.value);
        PlayerPrefs.SetFloat(SoundController.SFX_KEY, SFX.value);
    }
    private void SetMasterVolume(float value)
    {
        mixer.SetFloat(Mixer_Master, Mathf.Log10(value) * 20);
    }
    private void SetMusicVolume(float value)
    {
        mixer.SetFloat(Mixer_Music, Mathf.Log10(value) * 20);
    }

    private void SetSFXVolume(float value)
    {
        mixer.SetFloat(Mixer_SFX, Mathf.Log10(value) * 20);
    }
}
