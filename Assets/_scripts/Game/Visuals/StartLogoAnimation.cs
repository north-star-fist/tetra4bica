using DG.Tweening;
using Sergei.Safonov.Audio;
using Tetra4bica.Init;
using UnityEngine;
using Zenject;

namespace Tetra4bica.Graphics {

    public class StartLogoAnimation : MonoBehaviour {

        [Inject(Id = AudioSourceId.SoundEffects)]
        AudioSource audioSource;

        public float animationTime;
        public Vector3 startScale = Vector3.one;
        public Vector3 endScale = Vector3.one;
        public float startRotation;
        public float endRotation;

        public AudioResource sfx;


        void Start() {
            SoundUtils.PlaySound(audioSource, sfx);
            transform.eulerAngles = new Vector3(0, 0, startRotation);
            transform.localScale = startScale;
            transform.DOLocalRotate(new Vector3(0, 0, endRotation), animationTime, RotateMode.FastBeyond360).Play();
            //.SetEase(Ease.OutBounce);
            transform.DOScale(endScale, animationTime).Play();
        }
    }
}