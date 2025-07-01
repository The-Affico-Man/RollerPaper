using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource loopingSfxSource;

    // --- THESE ARE NOW PUBLIC for the debug menu to access ---
    [Header("Volume Controls")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    // --------------------------------------------------------

    [Header("Audio Clips")]
    public List<AudioClip> backgroundMusicTracks;
    public List<AudioClip> meowSounds;
    public List<AudioClip> purrSounds;
    public AudioClip milestoneTriumphSound;
    public AudioClip buttonClickSound;
    public AudioClip speedBoostMusic;
    public AudioClip paperRollingSound;
    public AudioClip coinSound;

    private Coroutine bgmCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    private void Start()
    {
        StartNormalBGM();
        if (loopingSfxSource != null)
        {
            loopingSfxSource.loop = true;
        }
    }

    // --- THIS IS THE NEW UPDATE METHOD ---
    private void Update()
    {
        // Continuously apply volume settings. This allows real-time changes from the debug menu.
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume * masterVolume;
        }
        if (sfxSource != null)
        {
            // The sfxSource volume is set per-sound in PlaySound, but we can adjust the base sfxVolume here.
        }
        if (loopingSfxSource != null && loopingSfxSource.isPlaying)
        {
            loopingSfxSource.volume = sfxVolume * masterVolume;
        }
    }

    // All other methods are unchanged and correct.
    #region Unchanged Methods
    public void PlaySound(AudioClip clip, float minPitch = 0.95f, float maxPitch = 1.05f) { if (clip != null && sfxSource != null) { sfxSource.pitch = Random.Range(minPitch, maxPitch); sfxSource.PlayOneShot(clip, sfxVolume * masterVolume); } }
    public void StartNormalBGM() { if (bgmCoroutine != null) { StopCoroutine(bgmCoroutine); } bgmCoroutine = StartCoroutine(PlayRandomBGM()); }
    public void StartSpeedBoostMusic() { if (bgmCoroutine != null) { StopCoroutine(bgmCoroutine); bgmCoroutine = null; } if (bgmSource != null && speedBoostMusic != null) { bgmSource.clip = speedBoostMusic; bgmSource.loop = true; bgmSource.volume = bgmVolume * masterVolume; bgmSource.Play(); } }
    public void StopSpeedBoostMusic() { StartNormalBGM(); }
    private IEnumerator PlayRandomBGM() { if (backgroundMusicTracks.Count == 0 || bgmSource == null) yield break; bgmSource.loop = false; while (true) { int trackIndex = Random.Range(0, backgroundMusicTracks.Count); bgmSource.clip = backgroundMusicTracks[trackIndex]; bgmSource.volume = bgmVolume * masterVolume; bgmSource.Play(); yield return new WaitForSeconds(bgmSource.clip.length); } }
    public void StartPaperRollingSound() { if (loopingSfxSource == null || paperRollingSound == null) return; if (rollingSoundFadeCoroutine != null) { StopCoroutine(rollingSoundFadeCoroutine); } if (!loopingSfxSource.isPlaying) { loopingSfxSource.clip = paperRollingSound; loopingSfxSource.Play(); } rollingSoundFadeCoroutine = StartCoroutine(FadeAudioSource(loopingSfxSource, 0.2f, sfxVolume * masterVolume)); }
    public void StopPaperRollingSound() { if (loopingSfxSource == null || !loopingSfxSource.isPlaying) return; if (rollingSoundFadeCoroutine != null) { StopCoroutine(rollingSoundFadeCoroutine); } rollingSoundFadeCoroutine = StartCoroutine(FadeAudioSource(loopingSfxSource, 0.5f, 0f, true)); }
    private Coroutine rollingSoundFadeCoroutine; private IEnumerator FadeAudioSource(AudioSource source, float duration, float targetVolume, bool stopWhenDone = false) { float currentTime = 0; float startVolume = source.volume; while (currentTime < duration) { currentTime += Time.deltaTime; source.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration); yield return null; } source.volume = targetVolume; if (stopWhenDone && source.volume == 0) { source.Stop(); } rollingSoundFadeCoroutine = null; }
    public void PlayRandomMeow() { if (meowSounds.Count > 0) { AudioClip clip = meowSounds[Random.Range(0, meowSounds.Count)]; PlaySound(clip, 0.9f, 1.1f); } }
    public void PlayCoinSound() { PlaySound(coinSound, 0.98f, 1.02f); }
    public void PlayRandomPurr() { if (purrSounds.Count > 0) { AudioClip clip = purrSounds[Random.Range(0, purrSounds.Count)]; PlaySound(clip); } }
    public void PlayButtonClick() { PlaySound(buttonClickSound); }
    public void PlayMilestoneTriumph() { PlaySound(milestoneTriumphSound); }
    #endregion
}