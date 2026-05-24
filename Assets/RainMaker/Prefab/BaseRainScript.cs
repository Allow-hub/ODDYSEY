//
// Rain Maker (c) 2015 Digital Ruby, LLC
// http://www.digitalruby.com
//

using UnityEngine;
using UnityEngine.Audio;
using TechC.Core.Manager;

namespace DigitalRuby.RainMaker
{
    public class BaseRainScript : MonoBehaviour
    {
        [Tooltip("Camera the rain should hover over, defaults to main camera")]
        public Camera Camera;

        [Tooltip("Whether rain should follow the camera.")]
        public bool FollowCamera = true;

        [Tooltip("Light rain looping clip")]
        public AudioClip RainSoundLight;

        [Tooltip("Medium rain looping clip")]
        public AudioClip RainSoundMedium;

        [Tooltip("Heavy rain looping clip")]
        public AudioClip RainSoundHeavy;

        [Tooltip("AudioMixer used for the rain sound")]
        public AudioMixerGroup RainSoundAudioMixer;

        [Tooltip("Intensity of rain (0-1)")]
        [Range(0.0f, 1.0f)]
        public float RainIntensity;

        [Tooltip("Rain particle system")]
        public ParticleSystem RainFallParticleSystem;

        [Tooltip("Particles system for when rain hits something")]
        public ParticleSystem RainExplosionParticleSystem;

        [Tooltip("Particle system to use for rain mist")]
        public ParticleSystem RainMistParticleSystem;

        [Tooltip("The threshold for intensity (0-1) at which mist starts to appear")]
        [Range(0.0f, 1.0f)]
        public float RainMistThreshold = 0.5f;

        [Tooltip("Wind looping clip")]
        public AudioClip WindSound;

        [Tooltip("Wind sound volume modifier")]
        public float WindSoundVolumeModifier = 0.5f;

        [Tooltip("Wind zone that will affect and follow the rain")]
        public WindZone WindZone;

        [Tooltip("X = min wind speed. Y = max wind speed. Z = sound multiplier.")]
        public Vector3 WindSpeedRange = new Vector3(50.0f, 500.0f, 500.0f);

        [Tooltip("How often the wind speed and direction changes")]
        public Vector2 WindChangeInterval = new Vector2(5.0f, 30.0f);

        [Tooltip("Whether wind should be enabled.")]
        public bool EnableWind = true;

        public float   fixedWindStrength    = 100f;
        public float   fixedWindTurbulence  = 50f;
        public Vector3 fixedWindDirection   = new Vector3(0, 0, 1);

        protected LoopingAudioSource audioSourceRainLight;
        protected LoopingAudioSource audioSourceRainMedium;
        protected LoopingAudioSource audioSourceRainHeavy;
        protected LoopingAudioSource audioSourceRainCurrent;
        protected LoopingAudioSource audioSourceWind;

        protected Material rainMaterial;
        protected Material rainExplosionMaterial;
        protected Material rainMistMaterial;

        private float lastRainIntensityValue = -1.0f;
        private float nextWindTime;

        private void CheckForRainChange()
        {
            if (lastRainIntensityValue == RainIntensity) return;
            lastRainIntensityValue = RainIntensity;

            if (RainIntensity <= 0.01f)
            {
                audioSourceRainCurrent?.Stop();
                audioSourceRainCurrent = null;

                DisableParticleSystem(RainFallParticleSystem);
                DisableParticleSystem(RainMistParticleSystem);
            }
            else
            {
                // 強度に応じた音源を選択
                LoopingAudioSource newSource =
                    RainIntensity >= 0.67f ? audioSourceRainHeavy :
                    RainIntensity >= 0.33f ? audioSourceRainMedium :
                                             audioSourceRainLight;

                if (audioSourceRainCurrent != newSource)
                {
                    audioSourceRainCurrent?.Stop();
                    audioSourceRainCurrent = newSource;

                    // ▼ AudioManager から音量スケールを取得してランダムピッチなしで再生
                    float volumeScale = AudioManager.I != null
                        ? AudioManager.I.GetAmbientVolumeScale()
                        : 1.0f;
                    audioSourceRainCurrent.Play(volumeScale);
                }

                UpdateParticleEmission(RainFallParticleSystem, RainFallEmissionRate());
                UpdateMistEmission();
            }
        }

        // ─── パーティクル補助 ────────────────────────────────────────────

        private static void DisableParticleSystem(ParticleSystem ps)
        {
            if (ps == null) return;
            var e  = ps.emission;
            e.enabled = false;
            ps.Stop();
        }

        private static void UpdateParticleEmission(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var e = ps.emission;
            e.enabled = ps.GetComponent<Renderer>().enabled = true;
            if (!ps.isPlaying) ps.Play();

            var curve = e.rateOverTime;
            curve.mode = ParticleSystemCurveMode.Constant;
            curve.constantMin = curve.constantMax = rate;
            e.rateOverTime = curve;
        }

        private void UpdateMistEmission()
        {
            if (RainMistParticleSystem == null) return;
            float rate = RainIntensity < RainMistThreshold ? 0f : MistEmissionRate();
            UpdateParticleEmission(RainMistParticleSystem, rate);
        }

        // ─── ライフサイクル ───────────────────────────────────────────────

        protected virtual void Start()
        {
#if DEBUG
            if (RainFallParticleSystem == null)
            {
                Debug.LogError("Rain fall particle system must be set to a particle system");
                return;
            }
#endif
            if (Camera == null) Camera = Camera.main;

            // LoopingAudioSource は自分でピッチを変えないのでランダムピッチなし
            audioSourceRainLight  = new LoopingAudioSource(this, RainSoundLight,  RainSoundAudioMixer);
            audioSourceRainMedium = new LoopingAudioSource(this, RainSoundMedium, RainSoundAudioMixer);
            audioSourceRainHeavy  = new LoopingAudioSource(this, RainSoundHeavy,  RainSoundAudioMixer);
            audioSourceWind       = new LoopingAudioSource(this, WindSound,        RainSoundAudioMixer);

            InitParticleSystem(RainFallParticleSystem,      ref rainMaterial,          softParticles: false);
            InitParticleSystem(RainExplosionParticleSystem, ref rainExplosionMaterial,  softParticles: false);
            InitParticleSystem(RainMistParticleSystem,      ref rainMistMaterial,       softParticles: UseRainMistSoftParticles);

            if (WindZone != null)
            {
                WindZone.windMain       = fixedWindStrength;
                WindZone.windTurbulence = fixedWindTurbulence;
                WindZone.transform.rotation = Quaternion.LookRotation(fixedWindDirection);
            }
        }

        private static void InitParticleSystem(ParticleSystem ps, ref Material mat, bool softParticles)
        {
            if (ps == null) return;
            var e        = ps.emission;
            e.enabled    = false;
            var renderer = ps.GetComponent<Renderer>();
            renderer.enabled = false;
            mat = new Material(renderer.material);
            mat.EnableKeyword(softParticles ? "SOFTPARTICLES_ON" : "SOFTPARTICLES_OFF");
            renderer.material = mat;
        }

        protected virtual void Update()
        {
#if DEBUG
            if (RainFallParticleSystem == null)
            {
                Debug.LogError("Rain fall particle system must be set to a particle system");
                return;
            }
#endif
            CheckForRainChange();

            // 毎フレーム音量スケールを AudioManager に合わせて更新
            UpdateAmbientVolumeScale();

            audioSourceRainLight.Update();
            audioSourceRainMedium.Update();
            audioSourceRainHeavy.Update();
        }

        /// <summary>
        /// AudioManager の ambientVolume * masterVolume の変化を
        /// 再生中の LoopingAudioSource に反映する。
        /// </summary>
        private void UpdateAmbientVolumeScale()
        {
            if (AudioManager.I == null || audioSourceRainCurrent == null) return;

            float scale = AudioManager.I.GetAmbientVolumeScale();
            // LoopingAudioSource の TargetVolume を上書きして音量を追従させる
            audioSourceRainCurrent.SetTargetVolumeScale(scale);
        }

        // ─── 計算 ─────────────────────────────────────────────────────────

        protected virtual float RainFallEmissionRate()
            => (RainFallParticleSystem.main.maxParticles
                / RainFallParticleSystem.main.startLifetime.constant)
               * RainIntensity;

        protected virtual float MistEmissionRate()
            => (RainMistParticleSystem.main.maxParticles
                / RainMistParticleSystem.main.startLifetime.constant)
               * RainIntensity * RainIntensity;

        protected virtual bool UseRainMistSoftParticles => true;
    }

    // ─── LoopingAudioSource ───────────────────────────────────────────────

    public class LoopingAudioSource
    {
        public AudioSource AudioSource  { get; private set; }
        public float       TargetVolume { get; private set; }

        private float volumeScale = 1.0f; // AudioManager からのスケール

        public LoopingAudioSource(MonoBehaviour script, AudioClip clip, AudioMixerGroup mixer)
        {
            AudioSource = script.gameObject.AddComponent<AudioSource>();
            if (mixer != null) AudioSource.outputAudioMixerGroup = mixer;
            AudioSource.loop        = true;
            AudioSource.clip        = clip;
            AudioSource.playOnAwake = false;
            AudioSource.volume      = 0.0f;
            AudioSource.Stop();
            TargetVolume = 1.0f;
        }

        public void Play(float targetVolume)
        {
            if (!AudioSource.isPlaying)
            {
                AudioSource.volume = 0.0f;
                AudioSource.Play();
            }
            TargetVolume = targetVolume;
        }

        public void Stop() => TargetVolume = 0.0f;

        /// <summary>
        /// AudioManager の音量スケールを外から設定する。
        /// TargetVolume はこのスケールを掛けた値になる。
        /// </summary>
        public void SetTargetVolumeScale(float scale)
        {
            volumeScale  = scale;
        }

        public void Update()
        {
            float target = TargetVolume * volumeScale;
            AudioSource.volume = Mathf.Lerp(AudioSource.volume, target, Time.deltaTime);

            if (AudioSource.isPlaying && AudioSource.volume <= 0.001f && target <= 0.001f)
                AudioSource.Stop();
        }
    }
}