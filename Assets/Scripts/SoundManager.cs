using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("UI: Звуки")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    public AudioClip clockTick;
    private bool isTicking = false;

    #region UI ЗВУКИ
    public void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    public void StartTicking()
    {
        if (isTicking) return;

        if (audioSource != null && clockTick != null)
        {
            isTicking = true;
            audioSource.clip = clockTick;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    public void StopTicking()
    {
        if (!isTicking) return;

        isTicking = false;
        audioSource.Stop();
        audioSource.loop = false;
    }
    #endregion
}
