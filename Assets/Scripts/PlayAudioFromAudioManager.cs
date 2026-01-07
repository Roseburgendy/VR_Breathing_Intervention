using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAudioFromAudioManager : MonoBehaviour
{
    public string target;

    public void Play()
    {
        AudioManager.Instance.Play(target);
    }

    public void Play(string audioName)
    {
        AudioManager.Instance.Play(audioName);
    }
}
