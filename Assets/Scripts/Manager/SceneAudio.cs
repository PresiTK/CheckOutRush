using UnityEngine;

public class SceneAudio : MonoBehaviour
{
    [SerializeField] AudioClip music;
    [SerializeField] bool loopMusic = true;

    [SerializeField] AudioClip ambience;

    [SerializeField] bool stopMusicOnGameEnd = true;

    GameManager subscribed;

    void Start()
    {
        var sound = SoundManager.Instance;
        sound.PlayMusic(music, loopMusic);
        sound.PlayAmbience(ambience);

        subscribed = GameManager.Instance;
        if (stopMusicOnGameEnd && subscribed != null) subscribed.GameEnded += OnGameEnded;
    }

    void OnDestroy()
    {
        if (subscribed != null) subscribed.GameEnded -= OnGameEnded;
        subscribed = null;
    }

    void OnGameEnded(bool won) => SoundManager.Instance.StopMusic();
}
