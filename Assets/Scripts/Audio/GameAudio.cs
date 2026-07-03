using UnityEngine;

/// <summary>
/// Persistent audio helper with one looping music channel and a one-shot SFX channel.
/// Survives scene loads, so music started on the title screen keeps playing across menus.
/// Clips are assigned in the Inspector of whichever component triggers them.
/// </summary>
public static class GameAudio
{
    private static AudioSource musicSource;
    private static AudioSource sfxSource;

    /// <summary>
    /// Plays a clip as looping music. If the same clip is already playing it is
    /// left untouched, so re-entering a scene never restarts the track.
    /// </summary>
    public static void PlayMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        EnsureSources();

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.loop = true;
        musicSource.Play();
    }

    public static void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public static void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        EnsureSources();
        sfxSource.PlayOneShot(clip, volume);
    }

    private static void EnsureSources()
    {
        if (musicSource != null && sfxSource != null)
        {
            return;
        }

        GameObject host = new("GameAudio");
        Object.DontDestroyOnLoad(host);
        musicSource = host.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        sfxSource = host.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }
}
