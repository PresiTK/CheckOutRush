using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NPCVoice : MonoBehaviour
{
    [SerializeField] AudioClip[] clips;

    [SerializeField] Vector2 interval = new Vector2(4f, 10f);

    [SerializeField] Vector2 pitch = new Vector2(0.85f, 1.2f);

    [SerializeField, Min(0f)] float fullVolumeDistance = 20f;

    [SerializeField, Min(0f)] float maxDistance = 40f;

    AudioSource source;
    float nextTime;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = fullVolumeDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
    }

    void Start()
    {
        if (source.outputAudioMixerGroup == null) source.outputAudioMixerGroup = SoundManager.Instance.EffectsGroup;
        nextTime = Time.time + Random.Range(0f, interval.y);
    }

    void Update()
    {
        if (clips.Length == 0 || source.isPlaying || Time.time < nextTime) return;

        source.pitch = Random.Range(pitch.x, pitch.y);
        source.clip = clips[Random.Range(0, clips.Length)];
        source.Play();
        nextTime = Time.time + source.clip.length / source.pitch + Random.Range(interval.x, interval.y);
    }
}
