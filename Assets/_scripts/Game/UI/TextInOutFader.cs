using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TextInOutFader : MonoBehaviour {

    public float fadeTime = 1.0f;

    TMP_Text text;

    Tween tween;

    // Start is called before the first frame update
    void Start() {
        text = GetComponent<TMP_Text>();
        tween = DOTween.To(() => text.alpha, a => text.alpha = a, 0, fadeTime).SetEase(Ease.InOutCirc);
        tween.SetLoops(-1, LoopType.Yoyo);
        tween.Play();
    }

    private void OnDisable() {
        if (tween != null) {
            tween.Kill();
        }
    }
}
