using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Tetra4bica.UI
{

    [RequireComponent(typeof(TMP_Text))]
    public class TextInOutFader : MonoBehaviour
    {

        [SerializeField]
        private float _fadeTime = 1.0f;

        TMP_Text _text;

        Tween _tween;

        // Start is called before the first frame update
        void Start()
        {
            _text = GetComponent<TMP_Text>();
            _tween = DOTween.To(() => _text.alpha, a => _text.alpha = a, 0, _fadeTime).SetEase(Ease.InOutCirc);
            _tween.SetLoops(-1, LoopType.Yoyo);
            _tween.Play();
        }

        private void OnDisable()
        {
            if (_tween != null)
            {
                _tween.Kill();
            }
        }
    }
}
