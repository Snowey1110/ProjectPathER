using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio References")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Game Settings")]
    [SerializeField] private Toggle cameraLockToggle; 

    // Mixer Parameter Names (Must match exactly what you typed in the Mixer window)
    private const string MIXER_MASTER = "MasterVol";
    private const string MIXER_MUSIC = "MusicVol";
    private const string MIXER_SFX = "SFXVol";

    // Save File Keys
    private const string KEY_MASTER = "MasterPref";
    private const string KEY_MUSIC = "MusicPref";
    private const string KEY_SFX = "SFXPref";
    private const string KEY_CAM_LOCK = "CameraLockPref";

    private void Start()
    {
        // Load saved values (or default to 1.0f / True)
        float savedMaster = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        float savedMusic = PlayerPrefs.GetFloat(KEY_MUSIC, 1f);
        float savedSFX = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        bool savedCamLock = PlayerPrefs.GetInt(KEY_CAM_LOCK, 1) == 1;

        // Set the UI elements to match
        masterSlider.value = savedMaster;
        musicSlider.value = savedMusic;
        sfxSlider.value = savedSFX;
        if (cameraLockToggle != null) cameraLockToggle.isOn = savedCamLock;

        // Apply volumes to the Mixer immediately!
        // (Without this, the game starts loud even if sliders are low)
        SetMasterVolume(savedMaster);
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);
    }

    private void Awake()
    {
        // Listen for slider changes
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        if (cameraLockToggle != null)
            cameraLockToggle.onValueChanged.AddListener(SetCameraLock);
    }

    // ----------------------------------------------------------------
    // Volume Logic
    // ----------------------------------------------------------------

    public void SetMasterVolume(float value)
    {
        // 0.0001f prevents "Log(0) = -Infinity" error
        mixer.SetFloat(MIXER_MASTER, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20);

        PlayerPrefs.SetFloat(KEY_MASTER, value);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        mixer.SetFloat(MIXER_MUSIC, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20);

        PlayerPrefs.SetFloat(KEY_MUSIC, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        mixer.SetFloat(MIXER_SFX, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20);

        PlayerPrefs.SetFloat(KEY_SFX, value);
        PlayerPrefs.Save();
    }

    // ----------------------------------------------------------------
    // Gameplay Settings Logic
    // ----------------------------------------------------------------

    public void SetCameraLock(bool isLocked)
    {
        PlayerPrefs.SetInt(KEY_CAM_LOCK, isLocked ? 1 : 0);
        PlayerPrefs.Save();
    }
}