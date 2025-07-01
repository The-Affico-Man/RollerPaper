using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    // ... all variables are unchanged ...
    #region Unchanged Variables
    public static SoundManager Instance { get; private set; }
    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource loopingSfxSource;
    [Header("Volume Controls")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    #endregion

    [Header("Audio Clips")]
    public List<AudioClip> backgroundMusicTracks;
    public List<AudioClip> meowSounds;
    public List<AudioClip> purrSounds;
    public AudioClip milestoneTriumphSound;
    public AudioClip buttonClickSound;
    public AudioClip speedBoostMusic;
    public AudioClip paperRollingSound;
    public AudioClip coinSound;
    public AudioClip purchaseSuccessSound;
    // --- THIS IS THE NEW PART ---
    [Tooltip("Sound for when a purchase fails (e.g., not enough coins).")]
    public AudioClip purchaseFailedSound;
    [Tooltip("Sound for when an item is successfully equipped.")]
    public AudioClip equipSound;
    // ----------------------------

    // ... All private variables and Awake/Start methods are unchanged ...
    #region Unchanged Setup
    private Coroutine rollingSoundFadeCoroutine;
    private float meowTimer = 0f;
    public float timeBetweenMeows = 10f;
    private bool isRollingSoundFadingOut = false;
    private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); } else { Instance = this; DontDestroyOnLoad(gameObject); } }
    private void Start() { StartNormalBGM(); if (loopingSfxSource != null) { loopingSfxSource.loop = true; } }
    #endregion

    // --- THESE ARE THE NEW PUBLIC METHODS ---
    public void PlayPurchaseFailed()
    {
        PlaySound(purchaseFailedSound, 1f, 1f); // No pitch shift for error sounds
    }

    public void PlayEquipSound()
    {
        PlaySound(equipSound);
    }
    // ------------------------------------

    // All other methods are unchanged and correct.
    #region Unchanged Methods
    private void Update() { if (meowTimer > 0) { meowTimer -= Time.deltaTime; } if (bgmSource != null) { bgmSource.volume = bgmVolume * masterVolume; } if (loopingSfxSource != null && loopingSfxSource.isPlaying && !isRollingSoundFadingOut) { loopingSfxSource.volume = sfxVolume * masterVolume; } }
    public void PlaySound(AudioClip clip, float minPitch = 0.95f, float maxPitch = 1.05f) { if (clip != null && sfxSource != null) { sfxSource.pitch = Random.Range(minPitch, maxPitch); sfxSource.PlayOneShot(clip, sfxVolume * masterVolume); } }
    public void PlayPurchaseSuccess() { PlaySound(purchaseSuccessSound, 1f, 1f); }
    public void StartPaperRollingSound() { if (isRollingSoundFadingOut) return; if (loopingSfxSource == null || paperRollingSound == null) return; if (rollingSoundFadeCoroutine != null) { StopCoroutine(rollingSoundFadeCoroutine); } if (!loopingSfxSource.isPlaying) { loopingSfxSource.clip = paperRollingSound; loopingSfxSource.Play(); } rollingSoundFadeCoroutine = StartCoroutine(FadeAudioSource(loopingSfxSource, 0.2f, sfxVolume * masterVolume)); }
    public void StopPaperRollingSound() { if (loopingSfxSource == null || !loopingSfxSource.isPlaying || isRollingSoundFadingOut) return; if (rollingSoundFadeCoroutine != null) { StopCoroutine(rollingSoundFadeCoroutine); } isRollingSoundFadingOut = true; rollingSoundFadeCoroutine = StartCoroutine(FadeAudioSource(loopingSfxSource, 0.5f, 0f, true)); }
    private IEnumerator FadeAudioSource(AudioSource source, float duration, float targetVolume, bool stopWhenDone = false) { float currentTime = 0; float startVolume = source.volume; while (currentTime < duration) { currentTime += Time.deltaTime; source.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration); yield return null; } source.volume = targetVolume; if (stopWhenDone && source.volume == 0) { source.Stop(); isRollingSoundFadingOut = false; } rollingSoundFadeCoroutine = null; }
    public void PlayRandomMeow() { if (meowSounds.Count > 0 && meowTimer <= 0) { AudioClip clip = meowSounds[Random.Range(0, meowSounds.Count)]; PlaySound(clip, 0.9f, 1.1f); meowTimer = Random.Range(timeBetweenMeows * 0.8f, timeBetweenMeows * 1.2f); } }
    public void PlayCoinSound() { PlaySound(coinSound, 0.98f, 1.02f); }
    public void PlayRandomPurr() { if (purrSounds.Count > 0) { AudioClip clip = purrSounds[Random.Range(0, purrSounds.Count)]; PlaySound(clip); } }
    public void PlayButtonClick() { PlaySound(buttonClickSound); }
    public void PlayMilestoneTriumph() { PlaySound(milestoneTriumphSound); }
    private Coroutine bgmCoroutine; public void StartNormalBGM() { if (bgmCoroutine != null) { StopCoroutine(bgmCoroutine); } bgmCoroutine = StartCoroutine(PlayRandomBGM()); }
    public void StartSpeedBoostMusic() { if (bgmCoroutine != null) { StopCoroutine(bgmCoroutine); bgmCoroutine = null; } if (bgmSource != null && speedBoostMusic != null) { bgmSource.clip = speedBoostMusic; bgmSource.loop = true; bgmSource.volume = bgmVolume * masterVolume; bgmSource.Play(); } }
    public void StopSpeedBoostMusic() { StartNormalBGM(); }
    private IEnumerator PlayRandomBGM() { if (backgroundMusicTracks.Count == 0 || bgmSource == null) yield break; bgmSource.loop = false; while (true) { int trackIndex = Random.Range(0, backgroundMusicTracks.Count); bgmSource.clip = backgroundMusicTracks[trackIndex]; bgmSource.volume = bgmVolume * masterVolume; bgmSource.Play(); yield return new WaitForSeconds(bgmSource.clip.length); } }
    #endregion
}