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
        AudioSource audioSource;

        [SerializeField]
        private float animationTime;
        [SerializeField]
        private Vector3 startScale = Vector3.one;
        [SerializeField]
        private Vector3 endScale = Vector3.one;
        [SerializeField]
        private float startRotation;
        [SerializeField]
        private float endRotation;
        [SerializeField]
        private AudioResource sfx;


        void Start()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                // WebGL players mostly do not allow to play sound until any key pressed. So just skipping it
                SoundUtils.PlaySound(audioSource, sfx);
            }
            transform.eulerAngles = new Vector3(0, 0, startRotation);
            transform.localScale = startScale;
            transform.DOLocalRotate(new Vector3(0, 0, endRotation), animationTime, RotateMode.FastBeyond360).Play();
            //.SetEase(Ease.OutBounce);
            transform.DOScale(endScale, animationTime).Play();
        }
    }
}
