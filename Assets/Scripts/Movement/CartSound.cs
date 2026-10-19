using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CartSound : MonoBehaviour
{
    [SerializeField] PlayerMovement player;

    [SerializeField] AudioClip clip;

    [SerializeField, Range(0f, 1f)] float volume = 0.8f;

    [SerializeField, Min(0.01f)] float fade = 0.15f;

    [SerializeField, Min(0.1f)] float pitchAtMaxSpeed = 1.15f;

    AudioSource source;

    void Awake()
    {
        if (player == null) player = GetComponent<PlayerMovement>();
        source = GetComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
    }

    void Start()
    {
        if (source.outputAudioMixerGroup == null) source.outputAudioMixerGroup = SoundManager.Instance.EffectsGroup;
    }

    void Update()
    {
        float moving = Time.timeScale > 0f ? player.SpeedFraction : 0f;
        float target = moving > 0.01f ? volume : 0f;
        source.volume = Mathf.MoveTowards(source.volume, target, volume / fade * Time.unscaledDeltaTime);
        source.pitch = Mathf.Lerp(1f, pitchAtMaxSpeed, moving);

        if (source.volume > 0f && !source.isPlaying) source.Play();
        else if (source.volume <= 0f && source.isPlaying) source.Stop();
    }
}
