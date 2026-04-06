using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioSystem : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer _mixer;

    [Header("BGM")]
    [SerializeField] private AudioSource _bgmSourceA;
    [SerializeField] private AudioSource _bgmSourceB;

    [Header("Voice")]
    [SerializeField] private AudioSource _voiceSource;

    [Header("SFX")]
    [SerializeField] private List<AudioSource> _sfxSources;
    [SerializeField] private int _sfxMaxSize = 10;

    public BgmPlayer           Bgm     { get; private set; }
    public VoicePlayer         Voice   { get; private set; }
    public SfxPool             Sfx     { get; private set; }
    public AudioVolumeSettings Volume  { get; private set; }

    public void Initialize()
    {
        Volume = new AudioVolumeSettings(_mixer);
        Volume.Load();

        Bgm   = new BgmPlayer(_bgmSourceA, _bgmSourceB, this);
        Voice = new VoicePlayer(_voiceSource);
        Sfx   = new SfxPool(_sfxSources, _sfxMaxSize);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            PlayerPrefs.Save();
    }
}