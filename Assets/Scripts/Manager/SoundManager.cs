using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    const string MasterKey = "volume.master";
    const string MusicKey = "volume.music";
    const string SfxKey = "volume.sfx";
    const string AmbienceKey = "volume.ambience";
    const string EffectsKey = "volume.effects";

    static SoundManager instance;
    static bool quitting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        quitting = false;
    }

    public static SoundManager Instance
    {
        get
        {
            if (instance == null && !quitting)
            {
                instance = FindAnyObjectByType<SoundManager>();
                if (instance == null)
                {
                    var prefab = Resources.Load<SoundManager>("SoundManager");
                    instance = prefab != null ? Instantiate(prefab) : new GameObject("SoundManager").AddComponent<SoundManager>();
                    instance.name = "SoundManager";
                }
            }
            return instance;
        }
    }

    [Header("Mixer")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioMixerGroup musicGroup;
    [SerializeField] AudioMixerGroup ambienceGroup;
    [SerializeField] AudioMixerGroup effectsGroup;

    [Header("Volúmenes (0 a 1)")]
    [SerializeField, Range(0f, 1f)] float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] float musicVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] float ambienceVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] float effectsVolume = 1f;

    [Header("Botones")]
    [SerializeField] AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] float clickVolume = 1f;
    [SerializeField] Vector2 clickPitch = new Vector2(0.9f, 1.15f);

    public event Action VolumeChanged;

    AudioSource musicSource;
    AudioSource ambienceSource;
    AudioSource sfxSource;
    AudioSource uiSource;
    readonly HashSet<Button> buttonsWithClick = new HashSet<Button>();

    public float MasterVolume { get => masterVolume; set => Set(ref masterVolume, value, MasterKey); }
    public float MusicVolume { get => musicVolume; set => Set(ref musicVolume, value, MusicKey); }
    public float SfxVolume { get => sfxVolume; set => Set(ref sfxVolume, value, SfxKey); }
    public float AmbienceVolume { get => ambienceVolume; set => Set(ref ambienceVolume, value, AmbienceKey); }
    public float EffectsVolume { get => effectsVolume; set => Set(ref effectsVolume, value, EffectsKey); }

    public AudioMixerGroup EffectsGroup => effectsGroup;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        masterVolume = PlayerPrefs.GetFloat(MasterKey, masterVolume);
        musicVolume = PlayerPrefs.GetFloat(MusicKey, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxKey, sfxVolume);
        ambienceVolume = PlayerPrefs.GetFloat(AmbienceKey, ambienceVolume);
        effectsVolume = PlayerPrefs.GetFloat(EffectsKey, effectsVolume);

        musicSource = CreateSource(musicGroup, true);
        ambienceSource = CreateSource(ambienceGroup, true);
        ambienceSource.ignoreListenerPause = true;
        sfxSource = CreateSource(effectsGroup, false);
        uiSource = CreateSource(effectsGroup, false);
        uiSource.ignoreListenerPause = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
        for (int i = 0; i < SceneManager.sceneCount; i++) AddClickSounds(SceneManager.GetSceneAt(i));
    }

    void Start() => Apply();

    void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AddClickSounds(scene);

    void AddClickSounds(Scene scene)
    {
        if (!scene.isLoaded) return;
        buttonsWithClick.RemoveWhere(b => b == null);

        foreach (var root in scene.GetRootGameObjects())
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (!buttonsWithClick.Add(button)) continue;

                var trigger = button.GetComponent<EventTrigger>();
                if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                var target = button;
                entry.callback.AddListener(_ => { if (target.interactable) PlayClick(); });
                trigger.triggers.Add(entry);
            }
    }

    public void PlayClick()
    {
        if (clickClip == null) return;
        uiSource.pitch = UnityEngine.Random.Range(clickPitch.x, clickPitch.y);
        uiSource.PlayOneShot(clickClip, clickVolume);
    }

    void OnApplicationQuit() => quitting = true;

    void OnValidate()
    {
        if (Application.isPlaying && instance == this) Apply();
    }

    AudioSource CreateSource(AudioMixerGroup group, bool loop)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = group;
        source.loop = loop;
        source.playOnAwake = false;
        return source;
    }

    public void PlayMusic(AudioClip clip, bool loop = true) => Play(musicSource, clip, loop);

    public void StopMusic() => musicSource.Stop();

    public void PlayAmbience(AudioClip clip) => Play(ambienceSource, clip, true);

    public void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void PlaySfx(AudioClip clip, float volumeScale, float pitch)
    {
        if (clip == null) return;

        var source = new GameObject("Sfx " + clip.name).AddComponent<AudioSource>();
        source.transform.SetParent(transform, false);
        source.outputAudioMixerGroup = effectsGroup;
        source.playOnAwake = false;
        source.clip = clip;
        source.volume = volumeScale;
        source.pitch = pitch;
        source.Play();
        Destroy(source.gameObject, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) + 0.1f);
    }

    static void Play(AudioSource source, AudioClip clip, bool loop)
    {
        source.loop = loop;
        if (source.clip == clip && source.isPlaying) return;
        source.clip = clip;
        if (clip != null) source.Play();
        else source.Stop();
    }

    void Set(ref float field, float value, string key)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(field, value)) return;
        field = value;
        PlayerPrefs.SetFloat(key, value);
        Apply();
    }

    void Apply()
    {
        if (mixer != null)
        {
            mixer.SetFloat("MasterVolume", ToDecibels(masterVolume));
            mixer.SetFloat("MusicVolume", ToDecibels(musicVolume));
            mixer.SetFloat("SFXVolume", ToDecibels(sfxVolume));
            mixer.SetFloat("AmbienceVolume", ToDecibels(ambienceVolume));
            mixer.SetFloat("EffectsVolume", ToDecibels(effectsVolume));
        }
        else
        {
            AudioListener.volume = masterVolume;
        }
        VolumeChanged?.Invoke();
    }

    static float ToDecibels(float volume) => volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
}
