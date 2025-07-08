using UnityEngine;
using UnityEngine.Audio;
using VContainer;

namespace Tetra4bica.Init
{

    [CreateAssetMenu(fileName = "Settings Installer", menuName = "Tetra4bica/DI Installers/Settings")]
    public class GameSettingsInstaller : AScriptableInstaller
    {
        [SerializeField]
        private AudioMixer _audioMixer;
        [SerializeField]
        private AudioMixerGroup _masterAudioMixerGroup;
        [SerializeField]
        private AudioMixerGroup _musicAudioMixerGroup;
        [SerializeField]
        private AudioMixerGroup _sfxAudioMixerGroup;
        [SerializeField]
        private string _masterVolumeAudioMixerParamName = "masterVolume";
        [SerializeField]
        private string _musicVolumeAudioMixerParamName = "musicVolume";
        [SerializeField]
        private string _sfxVolumeAudioMixerParamName = "soundsVolume";
        [SerializeField]
        private float _minVolumeDb = -80;
        [SerializeField]
        private float _maxVolumeDb = 20;
        [SerializeField]
        private float _initialVolume01 = 0.8f;

        public override void Install(IContainerBuilder builder)
        {
            builder.Register<SettingsManager>(Lifetime.Scoped)
                .WithParameter(SettingsManager.ParamNameAudioMixer, _audioMixer)
                .WithParameter(SettingsManager.ParamNameMasterAudioMixerGroup, _masterAudioMixerGroup)
                .WithParameter(SettingsManager.ParamNameMusicAudioMixerGroup, _musicAudioMixerGroup)
                .WithParameter(SettingsManager.ParamNameSoundsAudioMixerGroup, _sfxAudioMixerGroup)
                .WithParameter(SettingsManager.ParamNameMasterVolumeAudioMixerParamName, _masterVolumeAudioMixerParamName)
                .WithParameter(SettingsManager.ParamNameMusicVolumeAudioMixerParamName, _musicVolumeAudioMixerParamName)
                .WithParameter(SettingsManager.ParamNameSfxVolumeAudioMixerParamName, _sfxVolumeAudioMixerParamName)
                .WithParameter(SettingsManager.ParamNameMinVolumeDb, _minVolumeDb)
                .WithParameter(SettingsManager.ParamNameMaxVolumeDb, _maxVolumeDb)
                .WithParameter(SettingsManager.ParamNameInitialVolume01, _initialVolume01)
                .As<ISettingsManager>();
        }
    }
}
