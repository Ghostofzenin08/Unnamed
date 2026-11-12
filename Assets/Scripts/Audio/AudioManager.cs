using System;
using System.Collections.Generic;
using UnityEngine;
using Outpost404.Environment;

namespace Outpost404.Audio
{
    public enum ImpactType
    {
        Timber,
        Rock,
        Enemy,
        Structure,
        Default
    }

    /// <summary>
    /// Central audio manager managing Master, SFX, and Ambient/Music channel busses,
    /// volume clamping, muting toggles, and dynamic procedural audio synthesis via AudioClip.Create.
    /// Provides audio feedback without requiring external audio assets.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; set; }

        [Header("Channel Volume Buses [0, 1]")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1.0f;

        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.85f;

        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.65f;

        [Header("Bus Mute States")]
        [SerializeField] private bool isMasterMuted = false;
        [SerializeField] private bool isSfxMuted = false;
        [SerializeField] private bool isMusicMuted = false;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        // Procedural Audio Cache
        private AudioClip footstepClip;
        private AudioClip meleeWhooshClip;
        private AudioClip timberChopClip;
        private AudioClip rockClinkClip;
        private AudioClip enemyHitClip;
        private AudioClip pickupChimeClip;
        private AudioClip craftingSuccessClip;
        private AudioClip dayAmbientClip;
        private AudioClip nightAmbientClip;
        private AudioClip shiveringClip;

        // Public Volume Bus Properties
        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = ClampVolume(value);
                ApplyBusVolumes();
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = ClampVolume(value);
                ApplyBusVolumes();
            }
        }

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = ClampVolume(value);
                ApplyBusVolumes();
            }
        }

        public bool IsMasterMuted
        {
            get => isMasterMuted;
            set
            {
                isMasterMuted = value;
                ApplyBusVolumes();
            }
        }

        public bool IsSFXMuted
        {
            get => isSfxMuted;
            set
            {
                isSfxMuted = value;
                ApplyBusVolumes();
            }
        }

        public bool IsMusicMuted
        {
            get => isMusicMuted;
            set
            {
                isMusicMuted = value;
                ApplyBusVolumes();
            }
        }

        public float EffectiveSFXVolume => (isMasterMuted || isSfxMuted) ? 0f : (masterVolume * sfxVolume);
        public float EffectiveMusicVolume => (isMasterMuted || isMusicMuted) ? 0f : (masterVolume * musicVolume);

        public AudioSource MusicSource { get => musicSource; set => musicSource = value; }
        public AudioSource SFXSource { get => sfxSource; set => sfxSource = value; }

        private void Awake()
        {
            if (Application.isPlaying && Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureAudioSources();
            PrewarmProceduralClips();
            ApplyBusVolumes();
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
            SubscribeToEnvironment();
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            UnsubscribeFromEnvironment();
        }

        private void Start()
        {
            // Initial ambient playback based on day night cycle
            if (DayNightCycleManager.Instance != null)
            {
                SetDayNightAmbient(DayNightCycleManager.Instance.CurrentPhase);
            }
            else
            {
                PlayDayAmbient();
            }
        }

        public void EnsureAudioSources()
        {
            AudioSource[] existing = GetComponents<AudioSource>();
            if (musicSource == null)
            {
                if (existing.Length >= 1) musicSource = existing[0];
                else musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f; // 2D ambient
            }

            if (sfxSource == null)
            {
                if (existing.Length >= 2 && existing[1] != musicSource) sfxSource = existing[1];
                else sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D UI & feedback
            }
        }

        public void ApplyBusVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = EffectiveMusicVolume;
            }
            if (sfxSource != null)
            {
                sfxSource.volume = EffectiveSFXVolume;
            }
        }

        public static float ClampVolume(float vol)
        {
            if (float.IsNaN(vol) || float.IsInfinity(vol)) return 0f;
            return Mathf.Clamp01(vol);
        }

        // ==========================================
        // Procedural Audio Cache & Lazy Accessors
        // ==========================================
        public AudioClip FootstepClip => footstepClip ??= ProceduralAudioSynth.CreateFootstep();
        public AudioClip MeleeWhooshClip => meleeWhooshClip ??= ProceduralAudioSynth.CreateMeleeWhoosh();
        public AudioClip TimberChopClip => timberChopClip ??= ProceduralAudioSynth.CreateTimberChop();
        public AudioClip RockClinkClip => rockClinkClip ??= ProceduralAudioSynth.CreateRockClink();
        public AudioClip EnemyHitClip => enemyHitClip ??= ProceduralAudioSynth.CreateEnemyHit();
        public AudioClip PickupChimeClip => pickupChimeClip ??= ProceduralAudioSynth.CreatePickupChime();
        public AudioClip CraftingSuccessClip => craftingSuccessClip ??= ProceduralAudioSynth.CreateCraftingAlert();
        public AudioClip DayAmbientClip => dayAmbientClip ??= ProceduralAudioSynth.CreateDayAmbient();
        public AudioClip NightAmbientClip => nightAmbientClip ??= ProceduralAudioSynth.CreateNightAmbient();
        public AudioClip ShiveringClip => shiveringClip ??= ProceduralAudioSynth.CreateHypothermiaShiver();

        public void PrewarmProceduralClips()
        {
            _ = FootstepClip;
            _ = MeleeWhooshClip;
            _ = TimberChopClip;
            _ = RockClinkClip;
            _ = EnemyHitClip;
            _ = PickupChimeClip;
            _ = CraftingSuccessClip;
            _ = DayAmbientClip;
            _ = NightAmbientClip;
            _ = ShiveringClip;
        }

        // ==========================================
        // Sound Playback Triggers
        // ==========================================

        public void PlayFootstep(float pitchJitter = 0.08f)
        {
            PlayOneShotSFX(FootstepClip, 0.45f, 1.0f + UnityEngine.Random.Range(-pitchJitter, pitchJitter));
        }

        public void PlayMeleeWhoosh()
        {
            PlayOneShotSFX(MeleeWhooshClip, 0.8f, UnityEngine.Random.Range(0.95f, 1.05f));
        }

        public void PlayImpact(ImpactType type, float volumeScale = 1.0f)
        {
            AudioClip clip = type switch
            {
                ImpactType.Timber => TimberChopClip,
                ImpactType.Rock => RockClinkClip,
                ImpactType.Enemy => EnemyHitClip,
                ImpactType.Structure => TimberChopClip,
                _ => TimberChopClip
            };

            float pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            PlayOneShotSFX(clip, 0.9f * volumeScale, pitch);
        }

        public void PlayPickupChime()
        {
            PlayOneShotSFX(PickupChimeClip, 0.75f, 1.0f);
        }

        public void PlayCraftingSuccess()
        {
            PlayOneShotSFX(CraftingSuccessClip, 0.85f, 1.0f);
        }

        public void PlayHypothermiaShiver()
        {
            PlayOneShotSFX(ShiveringClip, 0.7f, UnityEngine.Random.Range(0.95f, 1.05f));
        }

        public void PlayOneShotSFX(AudioClip clip, float volumeScale = 1.0f, float pitch = 1.0f)
        {
            if (clip == null || EffectiveSFXVolume <= 0.0001f) return;

            if (sfxSource != null)
            {
                sfxSource.pitch = Mathf.Clamp(pitch, 0.5f, 2.0f);
                sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale * EffectiveSFXVolume));
            }
        }

        // ==========================================
        // Diurnal Ambient Audio Management
        // ==========================================

        public void SetDayNightAmbient(DayPhase phase)
        {
            if (phase == DayPhase.Night)
            {
                PlayNightAmbient();
            }
            else
            {
                PlayDayAmbient();
            }
        }

        public void PlayDayAmbient()
        {
            SwitchAmbientClip(DayAmbientClip);
        }

        public void PlayNightAmbient()
        {
            SwitchAmbientClip(NightAmbientClip);
        }

        private void SwitchAmbientClip(AudioClip targetClip)
        {
            if (musicSource == null || targetClip == null) return;

            if (musicSource.clip == targetClip && musicSource.isPlaying) return;

            musicSource.clip = targetClip;
            musicSource.volume = EffectiveMusicVolume;
            musicSource.loop = true;
            if (EffectiveMusicVolume > 0.0001f)
            {
                musicSource.Play();
            }
        }

        private void SubscribeToEnvironment()
        {
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
                DayNightCycleManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void UnsubscribeFromEnvironment()
        {
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(DayPhase newPhase)
        {
            SetDayNightAmbient(newPhase);
        }
    }

    /// <summary>
    /// Dynamic procedural audio waveform generator.
    /// Synthesizes sine, sawtooth, harmonic tones, and shaped noise bursts into Unity AudioClips
    /// with zero external file requirements.
    /// </summary>
    public static class ProceduralAudioSynth
    {
        public const int SampleRate = 44100;

        /// <summary>
        /// Short damped tap with subtle noise transient simulating footsteps.
        /// </summary>
        public static AudioClip CreateFootstep()
        {
            float duration = 0.07f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Fast pitch drop from 110Hz to 45Hz
                float freq = Mathf.Lerp(110f, 45f, progress);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.35f;

                float envelope = Mathf.Exp(-35f * t);
                data[i] = Mathf.Clamp((tone + noise) * envelope * 0.8f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Footstep_Procedural", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Fast sweeping whoosh sound for weapon swings.
        /// </summary>
        public static AudioClip CreateMeleeWhoosh()
        {
            float duration = 0.18f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Pitch bends down rapidly from 520Hz to 120Hz
                float freq = Mathf.Lerp(520f, 120f, Mathf.Pow(progress, 1.4f));
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.65f;

                // Smooth bell envelope (attack then decay)
                float envelope = Mathf.Sin(progress * Mathf.PI);
                data[i] = Mathf.Clamp((tone * 0.5f + noise * 0.5f) * envelope * 0.9f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("MeleeWhoosh_Procedural", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Organic resonant thud for chopping alien timber trees.
        /// </summary>
        public static AudioClip CreateTimberChop()
        {
            float duration = 0.16f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                float freq = Mathf.Lerp(160f, 55f, progress);
                float woodTone = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * (freq * 2.3f) * t);
                float click = (progress < 0.12f) ? (UnityEngine.Random.value * 2f - 1f) * 0.7f : 0f;

                float envelope = Mathf.Exp(-24f * t);
                data[i] = Mathf.Clamp((woodTone * 0.7f + click * 0.3f) * envelope, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("TimberChop_Procedural", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Resonant crystalline/metallic ping for mining pulse vein rock.
        /// </summary>
        public static AudioClip CreateRockClink()
        {
            float duration = 0.28f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;

                // Dual crystalline harmonics (1320Hz + 2640Hz)
                float f1 = 1320f;
                float f2 = 2640f;
                float ring = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.4f;
                float transient = (t < 0.015f) ? (UnityEngine.Random.value * 2f - 1f) * 0.8f : 0f;

                float envelope = Mathf.Exp(-12f * t);
                data[i] = Mathf.Clamp((ring + transient) * envelope * 0.85f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("RockClink_Procedural", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Visceral squelch / fleshy strike for hitting predator beasts.
        /// </summary>
        public static AudioClip CreateEnemyHit()
        {
            float duration = 0.15f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                float freq = Mathf.Lerp(220f, 65f, progress);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float squelch = (UnityEngine.Random.value * 2f - 1f) * 0.7f;

                float envelope = Mathf.Exp(-20f * t);
                data[i] = Mathf.Clamp((tone * 0.6f + squelch * 0.4f) * envelope, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("EnemyHit_Procedural", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Bright ascending chime for collecting loot pickups.
        /// </summary>
        public static AudioClip CreatePickupChime()
        {
            float duration = 0.22f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;

                // 3 quick harmonic arpeggio notes: C5 (523Hz), E5 (659Hz), G5 (784Hz)
                float freq = (t < 0.07f) ? 523.25f : ((t < 0.14f) ? 659.25f : 783.99f);
                float chime = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * freq * t);

                float stepT = t % 0.07f;
                float noteEnv = Mathf.Exp(-10f * stepT);
                float masterEnv = 1f - (t / duration);

                data[i] = Mathf.Clamp(chime * noteEnv * masterEnv * 0.75f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("PickupChime_Procedural", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Satisfying notification chime for completed crafting recipes.
        /// </summary>
        public static AudioClip CreateCraftingAlert()
        {
            float duration = 0.35f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;

                // Two-chord chime (A4 440Hz -> A5 880Hz + E6 1320Hz)
                float freq = (t < 0.15f) ? 440f : 880f;
                float chord = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t);

                float decay = Mathf.Exp(-7f * t);
                data[i] = Mathf.Clamp(chord * decay * 0.8f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("CraftingAlert_Procedural", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Loopable gentle wind breeze for daytime planetary atmosphere.
        /// </summary>
        public static AudioClip CreateDayAmbient()
        {
            float duration = 2.0f; // 2 seconds seamless loop
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            float lastSample = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;

                // Low-pass filtered noise to simulate gentle wind
                float white = UnityEngine.Random.value * 2f - 1f;
                lastSample = Mathf.Lerp(lastSample, white, 0.08f);

                // Gentle swell modulation
                float modulation = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
                data[i] = Mathf.Clamp(lastSample * modulation * 0.4f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("DayAmbient_Procedural", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Loopable ominous sub-bass drone with eerie anomaly pulsing for nighttime.
        /// </summary>
        public static AudioClip CreateNightAmbient()
        {
            float duration = 2.5f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;

                // Deep sub-bass with detuned harmonic pulsing (55Hz + 57.5Hz = 2.5Hz beat rate)
                float sub1 = Mathf.Sin(2f * Mathf.PI * 55f * t);
                float sub2 = Mathf.Sin(2f * Mathf.PI * 57.5f * t);

                // Anomaly high-frequency hum (220Hz with vibrato)
                float vibrato = Mathf.Sin(2f * Mathf.PI * 3f * t) * 6f;
                float hum = Mathf.Sin(2f * Mathf.PI * (220f + vibrato) * t) * 0.18f;

                float combined = (sub1 * 0.45f + sub2 * 0.45f + hum) * 0.5f;
                data[i] = Mathf.Clamp(combined, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("NightAmbient_Procedural", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Rhythmic teeth chattering / shivering sound for hypothermia warnings.
        /// </summary>
        public static AudioClip CreateHypothermiaShiver()
        {
            float duration = 0.30f;
            int totalSamples = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[totalSamples];

            float clickRate = 16f; // clicks per second
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;

                // Micro-burst pulses
                float cycle = (t * clickRate) % 1.0f;
                float click = (cycle < 0.25f) ? Mathf.Sin(2f * Mathf.PI * 800f * t) * Mathf.Exp(-30f * cycle) : 0f;

                data[i] = Mathf.Clamp(click * 0.75f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Shivering_Procedural", totalSamples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
