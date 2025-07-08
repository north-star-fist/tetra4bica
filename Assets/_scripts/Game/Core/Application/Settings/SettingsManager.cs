using Sergei.Safonov.Persistence;
using UnityEngine.Audio;
using VContainer;

namespace Tetra4bica.Init
{
    public class SettingsManager : ISettingsManager {
        public const string ParamNameIsAutoDigging = "isAutoDigging";
        public const string ParamNameAudioMixer = "audioMixer";
        public const string ParamNameMasterAudioMixerGroup = "masterAudioMixerGroup";
        public const string ParamNameMusicAudioMixerGroup = "musicAudioMixerGroup";
        public const string ParamNameSoundsAudioMixerGroup = "sfxAudioMixerGroup";
        public const string ParamNameMasterVolumeAudioMixerParamName = "masterVolumeAudioMixerParamName";
        public const string ParamNameMusicVolumeAudioMixerParamName = "musicVolumeAudioMixerParamName";
        public const string ParamNameSfxVolumeAudioMixerParamName = "sfxVolumeAudioMixerParamName";
        public const string ParamNameMinVolumeDb = "minVolumeDb";
        public const string ParamNameMaxVolumeDb = "maxVolumeDb";
        public const string ParamNameInitialVolume01 = "initialVolume01";

        private const string SaveFileName = "settings";


        private ISaveManager _saveManager;

        private AudioMixerGroup _masterAudioMixerGroup;
        private AudioMixerGroup _musicAudioMixerGroup;
        private AudioMixerGroup _sfxAudioMixerGroup;
        private string _masterVolumeAudioMixerParamName = "masterVolume";
        private string _musicVolumeAudioMixerParamName = "musicVolume";
        private string _sfxVolumeAudioMixerParamName = "soundsVolume";
        private float _minVolumeDb = -80f;
        private float _maxVolumeDb = 20f;
        private float _initialVolume01 = 0.8f;


        [Inject]
        public void Init(
            ISaveManager saveManager,
            AudioMixerGroup masterAudioMixerGroup,
            AudioMixerGroup musicAudioMixerGroup,
            AudioMixerGroup sfxAudioMixerGroup,
            string masterVolumeAudioMixerParamName = "masterVolume",
            string musicVolumeAudioMixerParamName = "musicVolume",
            string sfxVolumeAudioMixerParamName = "soundsVolume",
            float minVolumeDb = -80f,
            float maxVolumeDb = 20f,
            float initialVolume01 = 0.8f
        ) {
            _saveManager = saveManager;
            _masterAudioMixerGroup = masterAudioMixerGroup;
            _musicAudioMixerGroup = musicAudioMixerGroup;
            _sfxAudioMixerGroup = sfxAudioMixerGroup;
            _masterVolumeAudioMixerParamName = masterVolumeAudioMixerParamName;
            _musicVolumeAudioMixerParamName = musicVolumeAudioMixerParamName;
            _sfxVolumeAudioMixerParamName = sfxVolumeAudioMixerParamName;
            _minVolumeDb = minVolumeDb;
            _maxVolumeDb = maxVolumeDb;
            _initialVolume01 = initialVolume01;
        }

        public void ActivateSettings(Settings settings) {
            // Assumimng nobody invoke this method in Awake
            _masterAudioMixerGroup.audioMixer.SetFloat(
                _masterVolumeAudioMixerParamName,
                Convert01ToDb(settings.MasterVolume)
            );
            _musicAudioMixerGroup.audioMixer.SetFloat(
                _musicVolumeAudioMixerParamName,
                Convert01ToDb(settings.MusicVolume)
            );
            _sfxAudioMixerGroup.audioMixer.SetFloat(
                _sfxVolumeAudioMixerParamName,
                Convert01ToDb(settings.SfxVolume)
            );
        }

        public Settings LoadSettings() {
            var loadResult = _saveManager.Load<Settings>("settings");
            return loadResult.IsSuccessful ? loadResult.Value : null;
        }

        public void SaveSettings(Settings settings) {
            _saveManager.Save(SaveFileName, settings);
        }

        public Settings GetCurrentGameSettings() {
            var currentSettings = LoadSettings();
            if (currentSettings == null) {
                currentSettings = AssembleCurrentSettings();
                // When it's null there is no normal audio settings so let's set em
                currentSettings.MasterVolume = _initialVolume01;
                currentSettings.MusicVolume = _initialVolume01;
                currentSettings.SfxVolume = _initialVolume01;
            }
            return currentSettings;
        }

        public Settings AssembleCurrentSettings() {
            var settings = new Settings();
            _masterAudioMixerGroup.audioMixer.GetFloat(_masterVolumeAudioMixerParamName, out settings.MasterVolume);
            _sfxAudioMixerGroup.audioMixer.GetFloat(_sfxVolumeAudioMixerParamName, out settings.SfxVolume);
            _musicAudioMixerGroup.audioMixer.GetFloat(_musicVolumeAudioMixerParamName, out settings.MusicVolume);
            return settings;
        }


        private float Convert01ToDb(float val01) {
            return _minVolumeDb + (_maxVolumeDb - _minVolumeDb) * val01;
        }
    }
}
