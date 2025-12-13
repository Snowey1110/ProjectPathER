using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundController : MonoBehaviour
{
    public static SoundController instance;

    [SerializeField]
    AudioMixer mixer;

    // Keys for saving data to computer (PlayerPrefs)
    public const string MASTER_KEY = "masterVolume";
    public const string MUSIC_KEY = "musicVolume";
    public const string SFX_KEY = "SFXVolume";

    // Keys for talking to the Audio Mixer (Must match Exposed Parameters)
    public const string MIXER_MASTER = "MasterVol";
    public const string MIXER_MUSIC = "MusicVol";
    public const string MIXER_SFX = "SFXVol";

    private void Awake()
    {

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return; // Stop running code if we are destroying this duplicate
        }

        LoadVolume();
    }

    void LoadVolume()
    {
        // Get saved data (Default to 1.0f if not found)
        float masterVolume = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        // Apply to Mixer immediately
        // We use 0.0001f to prevent "Log10(0) = Error"
        mixer.SetFloat(MIXER_MASTER, Mathf.Log10(Mathf.Max(masterVolume, 0.0001f)) * 20);
        mixer.SetFloat(MIXER_MUSIC, Mathf.Log10(Mathf.Max(musicVolume, 0.0001f)) * 20);
        mixer.SetFloat(MIXER_SFX, Mathf.Log10(Mathf.Max(sfxVolume, 0.0001f)) * 20);
    }
}