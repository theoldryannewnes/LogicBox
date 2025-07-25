using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private AudioSource[] AudioSources;

    // Start is called before the first frame update
    void Start()
    {
        AudioSources = GetComponents<AudioSource>();
    }

    public void FlipSound()
    {
        AudioSources[1].Play();
    }

    public void MatchSound()
    {
        AudioSources[2].Play();
    }

    public void NoMatchSound()
    {
        AudioSources[3].Play();
    }

    public void GameOver()
    {
        AudioSources[4].Play();
    }

}
