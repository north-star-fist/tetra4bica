using DG.Tweening;
using Sergei.Safonov.Audio;
using Tetra4bica.Init;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Graphics
{

    public class StartLogoAnimation : MonoBehaviour
    {

        [Inject(Id = AudioSourceId.SoundEffects)]
        private AudioSource _audioSource;

        [SerializeField]
        private float _animationTime;
        [SerializeField]
        private Vector3 _startScale = Vector3.one;
        [SerializeField]
        private Vector3 _endScale = Vector3.one;
        [SerializeField]
        private float _startRotation;
        [SerializeField]
        private float _endRotation;
        [SerializeField]
        private AudioResource _sfx;


        void Start()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                // WebGL players mostly do not allow to play sound until any key pressed. So just skipping it
                SoundUtils.PlaySound(_audioSource, _sfx);
            }
            transform.eulerAngles = new Vector3(0, 0, _startRotation);
            transform.localScale = _startScale;
            transform.DOLocalRotate(new Vector3(0, 0, _endRotation), _animationTime, RotateMode.FastBeyond360).Play();
            //.SetEase(Ease.OutBounce);
            transform.DOScale(_endScale, _animationTime).Play();
        }
    }
}
