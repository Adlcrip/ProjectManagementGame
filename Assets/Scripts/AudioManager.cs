using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    public AudioClip background;
    public AudioClip cashier;
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        musicSource.clip = background;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}
