using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance;
    public AudioSource _AudioSource;
    public AudioListener _AudioListener;

    public AudioClip[] Music;
    public AudioClip[] CombatMusic;

    private void Awake()
    {
        if (Instance)
        {
            Debug.Log("Error: Multiple music player instances detected");
            Destroy(gameObject);
            enabled = false;
        }
        else
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;

            if(!_AudioSource)
            {
                _AudioSource = GetComponent<AudioSource>();

                if(!_AudioSource)
                {
                    _AudioSource = gameObject.AddComponent<AudioSource>();
                }
            }


            if (!_AudioListener)
            {
                _AudioListener = GetComponent<AudioListener>();

                if (!_AudioListener)
                {
                    _AudioListener = gameObject.AddComponent<AudioListener>();
                }
            }

            _AudioListener.enabled = false;
        }
    }

    private void Start()
    {
        if (Music.Length > 0)
        {
            _AudioSource.clip = Music[0];
            _AudioSource.Play();
        }
    }
}