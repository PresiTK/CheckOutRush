using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSettingsUI : MonoBehaviour
{
    [Serializable]
    class Row
    {
        public Slider slider;
        public TMP_Text value;
    }

    [SerializeField] Row master = new Row();
    [SerializeField] Row music = new Row();
    [SerializeField] Row sfx = new Row();
    [SerializeField] Row ambience = new Row();
    [SerializeField] Row effects = new Row();

    void Start()
    {
        var sound = SoundManager.Instance;
        Bind(master, () => sound.MasterVolume, v => sound.MasterVolume = v);
        Bind(music, () => sound.MusicVolume, v => sound.MusicVolume = v);
        Bind(sfx, () => sound.SfxVolume, v => sound.SfxVolume = v);
        Bind(ambience, () => sound.AmbienceVolume, v => sound.AmbienceVolume = v);
        Bind(effects, () => sound.EffectsVolume, v => sound.EffectsVolume = v);
    }

    static void Bind(Row row, Func<float> get, Action<float> set)
    {
        if (row.slider == null) return;
        row.slider.minValue = 0f;
        row.slider.maxValue = 1f;
        row.slider.SetValueWithoutNotify(get());
        Show(row.value, get());
        row.slider.onValueChanged.AddListener(v =>
        {
            set(v);
            Show(row.value, v);
        });
    }

    static void Show(TMP_Text label, float value)
    {
        if (label != null) label.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
