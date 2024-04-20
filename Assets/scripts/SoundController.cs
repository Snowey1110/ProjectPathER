using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;



public class SoundController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    AudioMixer mixer;

    public const string MUSIC_KEY = "musicVolume";
    public const string SFX_KEY = "SFXVolume";
    public static SoundController instance;
    private void Awake()
    {
        if (instance = null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadVolume();
    }

    void LoadVolume()
    {
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 1f);
        mixer.SetFloat(SoundManager.Mixer_Music, Mathf.Log10(musicVolume) * 20);
        mixer.SetFloat(SoundManager.Mixer_SFX, Mathf.Log10(sfxVolume) * 20);
    }
}
