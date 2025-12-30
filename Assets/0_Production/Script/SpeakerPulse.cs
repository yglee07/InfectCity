using DG.Tweening;
using UnityEngine;

public class SpeakerPulse : MonoBehaviour
{
    public float scaleUp = 1.15f;
    public float duration = 0.25f;

    Vector3 baseScale;
    Tween pulseTween;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        Stop();

        pulseTween = transform
            .DOScale(baseScale * scaleUp, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void Stop()
    {
        if (pulseTween != null)
        {
            pulseTween.Kill();
            pulseTween = null;
        }
        transform.localScale = baseScale;
    }

    void OnDisable()
    {
        Stop();
    }
}
