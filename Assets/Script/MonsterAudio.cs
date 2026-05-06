using UnityEngine;

public class MonsterAudio : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Clips")]
    public AudioClip walkClip;
    public AudioClip attackClip;
    public AudioClip roarClip;
    public AudioClip hitClip;
    public AudioClip deathClip;

    void PlayClip(AudioClip clip, bool loop = false, float pitch = 1f)
    {
        if (clip == null) return;

        // 避免同音效重播（包含 pitch）
        if (audioSource.isPlaying &&
            audioSource.clip == clip &&
            Mathf.Approximately(audioSource.pitch, pitch))
            return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.pitch = pitch;
        audioSource.Play();
    }

    // ===== 移動 =====
    public void PlayWalk(float speedMultiplier)
    {
        PlayClip(walkClip, true, speedMultiplier);
    }

    public void StopMove()
    {
        if (audioSource.loop)
            audioSource.Stop();
    }

    // ===== 行為音效 =====
    public void PlayAttack() => PlayClip(attackClip);
    public void PlayRoar()   => PlayClip(roarClip);
    public void PlayHit()    => PlayClip(hitClip);
    public void PlayDeath()  => PlayClip(deathClip);
}