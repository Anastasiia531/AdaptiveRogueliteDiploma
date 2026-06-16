using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Play a procedurally generated audio clip.
    /// </summary>
    /// <param name="frequency">Frequency in Hz (for sine waves)</param>
    /// <param name="duration">Clip duration in seconds</param>
    /// <param name="volume">Master volume coefficient</param>
    /// <param name="isNoise">True for white noise (explosions/damage), false for pure tone (beeps/music)</param>
    public void PlaySound(float frequency, float duration, float volume = 0.2f, bool isNoise = false)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        if (isNoise)
        {
            // Procedural white noise with decay envelope
            for (int i = 0; i < sampleCount; i++)
            {
                float decay = 1f - ((float)i / sampleCount);
                samples[i] = (Random.value * 2f - 1f) * volume * decay;
            }
        }
        else
        {
            // Procedural sine wave with decay envelope
            float timeStep = 1f / sampleRate;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i * timeStep;
                float decay = 1f - (t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * decay;
            }
        }

        AudioClip clip = AudioClip.Create("ProceduralSFX", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
