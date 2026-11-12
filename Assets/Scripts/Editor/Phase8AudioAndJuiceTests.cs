#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Outpost404.Audio;
using Outpost404.CameraControl;
using Outpost404.Combat;
using Outpost404.Player;
using Outpost404.EditorTools;

namespace Outpost404.Tests
{
    public class Phase8AudioAndJuiceTests
    {
        private GameObject audioManagerObj;
        private AudioManager audioManager;
        private GameObject cameraObj;
        private TopDownCameraFollow cameraFollow;
        private GameObject playerObj;
        private PlayerController playerController;

        [SetUp]
        public void Setup()
        {
            audioManagerObj = new GameObject("TestAudioManager");
            audioManager = audioManagerObj.AddComponent<AudioManager>();
            audioManager.EnsureAudioSources();

            cameraObj = new GameObject("TestCamera");
            cameraFollow = cameraObj.AddComponent<TopDownCameraFollow>();

            playerObj = new GameObject("TestPlayer");
            playerObj.AddComponent<Rigidbody>();
            playerObj.AddComponent<CapsuleCollider>();
            playerController = playerObj.AddComponent<PlayerController>();
        }

        [TearDown]
        public void Teardown()
        {
            if (audioManagerObj != null) Object.DestroyImmediate(audioManagerObj);
            if (cameraObj != null) Object.DestroyImmediate(cameraObj);
            if (playerObj != null) Object.DestroyImmediate(playerObj);

            AudioManager.Instance = null;
        }

        // =========================================================================
        // 1. AudioManager Channel Buses & Volume Clamping
        // =========================================================================

        [Test]
        public void AudioManager_InitializesWithSensibleVolumes()
        {
            Assert.AreEqual(1.0f, audioManager.MasterVolume, 0.01f);
            Assert.AreEqual(0.85f, audioManager.SFXVolume, 0.01f);
            Assert.AreEqual(0.65f, audioManager.MusicVolume, 0.01f);
            Assert.IsFalse(audioManager.IsMasterMuted);
            Assert.IsFalse(audioManager.IsSFXMuted);
            Assert.IsFalse(audioManager.IsMusicMuted);
        }

        [Test]
        public void AudioManager_VolumeClamping_ClampsBetweenZeroAndOne()
        {
            audioManager.MasterVolume = 1.8f;
            Assert.AreEqual(1.0f, audioManager.MasterVolume, 0.01f);

            audioManager.MasterVolume = -0.5f;
            Assert.AreEqual(0f, audioManager.MasterVolume, 0.01f);

            audioManager.SFXVolume = 2.5f;
            Assert.AreEqual(1.0f, audioManager.SFXVolume, 0.01f);

            audioManager.SFXVolume = -1.0f;
            Assert.AreEqual(0f, audioManager.SFXVolume, 0.01f);

            audioManager.MusicVolume = 1.2f;
            Assert.AreEqual(1.0f, audioManager.MusicVolume, 0.01f);

            audioManager.MusicVolume = -0.1f;
            Assert.AreEqual(0f, audioManager.MusicVolume, 0.01f);
        }

        [Test]
        public void AudioManager_VolumeNaNAndInfinity_SafelyHandled()
        {
            audioManager.MasterVolume = float.NaN;
            Assert.AreEqual(0f, audioManager.MasterVolume, 0.01f);

            audioManager.SFXVolume = float.PositiveInfinity;
            Assert.AreEqual(0f, audioManager.SFXVolume, 0.01f);

            audioManager.MusicVolume = float.NegativeInfinity;
            Assert.AreEqual(0f, audioManager.MusicVolume, 0.01f);
        }

        [Test]
        public void AudioManager_BusMuting_ZeroesEffectiveVolumes()
        {
            audioManager.MasterVolume = 0.8f;
            audioManager.SFXVolume = 0.7f;
            audioManager.MusicVolume = 0.6f;

            Assert.AreEqual(0.56f, audioManager.EffectiveSFXVolume, 0.01f);
            Assert.AreEqual(0.48f, audioManager.EffectiveMusicVolume, 0.01f);

            // Master mute zeroes both
            audioManager.IsMasterMuted = true;
            Assert.AreEqual(0f, audioManager.EffectiveSFXVolume, 0.001f);
            Assert.AreEqual(0f, audioManager.EffectiveMusicVolume, 0.001f);

            // Unmuting restores
            audioManager.IsMasterMuted = false;
            Assert.AreEqual(0.56f, audioManager.EffectiveSFXVolume, 0.01f);
            Assert.AreEqual(0.48f, audioManager.EffectiveMusicVolume, 0.01f);

            // Channel specific mutes
            audioManager.IsSFXMuted = true;
            Assert.AreEqual(0f, audioManager.EffectiveSFXVolume, 0.001f);
            Assert.AreEqual(0.48f, audioManager.EffectiveMusicVolume, 0.01f);

            audioManager.IsSFXMuted = false;
            audioManager.IsMusicMuted = true;
            Assert.AreEqual(0.56f, audioManager.EffectiveSFXVolume, 0.01f);
            Assert.AreEqual(0f, audioManager.EffectiveMusicVolume, 0.001f);
        }

        // =========================================================================
        // 2. Procedural Audio Synthesis Waveforms
        // =========================================================================

        [Test]
        public void ProceduralAudioSynth_CreateFootstep_ReturnsValidNonEmptyClip()
        {
            var clip = ProceduralAudioSynth.CreateFootstep();
            Assert.IsNotNull(clip);
            Assert.AreEqual(ProceduralAudioSynth.SampleRate, clip.frequency);
            Assert.Greater(clip.length, 0.04f);
            Assert.AreEqual(1, clip.channels);
        }

        [Test]
        public void ProceduralAudioSynth_CreateMeleeWhoosh_ReturnsValidNonEmptyClip()
        {
            var clip = ProceduralAudioSynth.CreateMeleeWhoosh();
            Assert.IsNotNull(clip);
            Assert.AreEqual(ProceduralAudioSynth.SampleRate, clip.frequency);
            Assert.Greater(clip.length, 0.1f);
        }

        [Test]
        public void ProceduralAudioSynth_CreateTimberChop_ReturnsValidNonEmptyClip()
        {
            var clip = ProceduralAudioSynth.CreateTimberChop();
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 0.1f);
        }

        [Test]
        public void ProceduralAudioSynth_CreateRockClink_ReturnsValidNonEmptyClip()
        {
            var clip = ProceduralAudioSynth.CreateRockClink();
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 0.2f);
        }

        [Test]
        public void ProceduralAudioSynth_CreateEnemyHit_ReturnsValidNonEmptyClip()
        {
            var clip = ProceduralAudioSynth.CreateEnemyHit();
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 0.1f);
        }

        [Test]
        public void ProceduralAudioSynth_CreatePickupChime_ReturnsValidNonEmptyClip()
        {
            var clip = ProceduralAudioSynth.CreatePickupChime();
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 0.15f);
        }

        [Test]
        public void ProceduralAudioSynth_CreateCraftingAlert_ReturnsValidNonEmptyClip()
        {
            var clip = ProceduralAudioSynth.CreateCraftingAlert();
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 0.25f);
        }

        [Test]
        public void ProceduralAudioSynth_CreateDayAmbient_ReturnsValidLoopableClip()
        {
            var clip = ProceduralAudioSynth.CreateDayAmbient();
            Assert.IsNotNull(clip);
            Assert.GreaterOrEqual(clip.length, 1.5f);
        }

        [Test]
        public void ProceduralAudioSynth_CreateNightAmbient_ReturnsValidLoopableClip()
        {
            var clip = ProceduralAudioSynth.CreateNightAmbient();
            Assert.IsNotNull(clip);
            Assert.GreaterOrEqual(clip.length, 2.0f);
        }

        [Test]
        public void ProceduralAudioSynth_CreateHypothermiaShiver_ReturnsValidNonEmptyClip()
        {
            var clip = ProceduralAudioSynth.CreateHypothermiaShiver();
            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 0.2f);
        }

        [Test]
        public void AudioManager_PlaybackTriggers_ExecuteWithoutException()
        {
            Assert.DoesNotThrow(() =>
            {
                audioManager.PlayFootstep();
                audioManager.PlayMeleeWhoosh();
                audioManager.PlayImpact(ImpactType.Timber);
                audioManager.PlayImpact(ImpactType.Rock);
                audioManager.PlayImpact(ImpactType.Enemy);
                audioManager.PlayImpact(ImpactType.Structure);
                audioManager.PlayPickupChime();
                audioManager.PlayCraftingSuccess();
                audioManager.PlayHypothermiaShiver();
                audioManager.PlayDayAmbient();
                audioManager.PlayNightAmbient();
                audioManager.SetDayNightAmbient(Outpost404.Environment.DayPhase.Day);
                audioManager.SetDayNightAmbient(Outpost404.Environment.DayPhase.Night);
            });
        }

        // =========================================================================
        // 3. Screen Shake & Camera Follow Juice
        // =========================================================================

        [Test]
        public void ScreenShake_TriggerShake_ActivatesShakeState()
        {
            cameraFollow.TriggerShake(0.35f, 0.25f);

            Assert.IsTrue(cameraFollow.IsShaking);
            Assert.AreEqual(0.25f, cameraFollow.ShakeDuration, 0.001f);
            Assert.AreEqual(0.35f, cameraFollow.CurrentShakeIntensity, 0.05f);
        }

        [Test]
        public void ScreenShake_UpdateShake_DecaysIntensityOverTime()
        {
            cameraFollow.TriggerShake(0.40f, 0.20f);

            // Step halfway
            cameraFollow.UpdateShake(0.10f);
            Assert.IsTrue(cameraFollow.IsShaking);
            Assert.AreEqual(0.20f, cameraFollow.CurrentShakeIntensity, 0.05f);

            // Step past end
            cameraFollow.UpdateShake(0.12f);
            Assert.IsFalse(cameraFollow.IsShaking);
            Assert.AreEqual(0f, cameraFollow.CurrentShakeIntensity, 0.001f);
            Assert.AreEqual(Vector3.zero, cameraFollow.CurrentShakeOffset);
        }

        [Test]
        public void ScreenShake_ZeroOrNegativeInputs_SafelyIgnored()
        {
            cameraFollow.TriggerShake(0f, 0.2f);
            Assert.IsFalse(cameraFollow.IsShaking);

            cameraFollow.TriggerShake(-0.5f, 0.2f);
            Assert.IsFalse(cameraFollow.IsShaking);

            cameraFollow.TriggerShake(0.5f, 0f);
            Assert.IsFalse(cameraFollow.IsShaking);

            cameraFollow.TriggerShake(0.5f, -0.2f);
            Assert.IsFalse(cameraFollow.IsShaking);

            cameraFollow.TriggerShake(float.NaN, 0.2f);
            Assert.IsFalse(cameraFollow.IsShaking);

            cameraFollow.TriggerShake(0.5f, float.PositiveInfinity);
            Assert.IsFalse(cameraFollow.IsShaking);
        }

        [Test]
        public void ScreenShake_StopShake_InstantlyResets()
        {
            cameraFollow.TriggerShake(0.5f, 0.3f);
            Assert.IsTrue(cameraFollow.IsShaking);

            cameraFollow.StopShake();
            Assert.IsFalse(cameraFollow.IsShaking);
            Assert.AreEqual(0f, cameraFollow.CurrentShakeIntensity, 0.001f);
            Assert.AreEqual(Vector3.zero, cameraFollow.CurrentShakeOffset);
        }

        // =========================================================================
        // 4. Footstep Cadence in PlayerController
        // =========================================================================

        [Test]
        public void FootstepCadence_MovingPlayer_TriggersStepAtInterval()
        {
            int stepCount = 0;
            playerController.OnFootstepTaken += () => stepCount++;

            // Simulate walking forward
            playerController.SetDirectTargetVelocity(new Vector3(3f, 0f, 0f));
            Assert.IsTrue(playerController.IsMoving);

            // Advance by half interval
            playerController.UpdateFootsteps(0.18f);
            Assert.AreEqual(0, stepCount);

            // Advance remaining to complete walk step (0.36s)
            playerController.UpdateFootsteps(0.19f);
            Assert.AreEqual(1, stepCount);
        }

        [Test]
        public void FootstepCadence_IdlePlayer_DoesNotAdvanceOrTriggerStep()
        {
            int stepCount = 0;
            playerController.OnFootstepTaken += () => stepCount++;

            playerController.SetDirectTargetVelocity(Vector3.zero);
            Assert.IsFalse(playerController.IsMoving);

            playerController.UpdateFootsteps(1.0f);
            Assert.AreEqual(0, stepCount);
            Assert.AreEqual(0f, playerController.StepTimer, 0.001f);
        }

        // =========================================================================
        // 5. Scene Opener & Editor Build Settings Validation
        // =========================================================================

        [Test]
        public void ProjectSceneOpener_EnsureBuildSettingsConfigured_RegistersSampleScene()
        {
            bool addedOrVerified = ProjectSceneOpener.EnsureBuildSettingsConfigured();

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.IsNotNull(scenes);
            Assert.Greater(scenes.Length, 0, "Build settings must contain at least one scene.");

            bool foundSampleScene = false;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == ProjectSceneOpener.DefaultScenePath && scenes[i].enabled)
                {
                    foundSampleScene = true;
                    break;
                }
            }

            Assert.IsTrue(foundSampleScene, "Assets/Scenes/SampleScene.unity must be registered and enabled in EditorBuildSettings.");
        }

        // =========================================================================
        // 6. Scene Integration & Wiring Suite
        // =========================================================================

        [Test, Order(1)]
        public void Scene_BuildPhase8Scene_ExecutesCleanlyAndSaves()
        {
            Assert.DoesNotThrow(() =>
            {
                Phase8SceneSetup.BuildPhase8Scene();
            }, "Phase8SceneSetup.BuildPhase8Scene must execute without errors.");
        }

        [Test, Order(2)]
        public void Scene_AudioManager_ConfiguredWithBusesAndSources()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var audioMgr = Object.FindFirstObjectByType<AudioManager>();
            if (audioMgr == null)
            {
                Phase8SceneSetup.BuildPhase8Scene();
                audioMgr = Object.FindFirstObjectByType<AudioManager>();
            }

            Assert.IsNotNull(audioMgr, "AudioManager component must exist in SampleScene.");

            Assert.IsNotNull(audioMgr.MusicSource, "MusicSource must be configured on AudioManager.");
            Assert.IsNotNull(audioMgr.SFXSource, "SFXSource must be configured on AudioManager.");

            Assert.IsTrue(audioMgr.MusicSource.loop, "MusicSource must be configured to loop.");
            Assert.IsFalse(audioMgr.SFXSource.loop, "SFXSource must not loop.");

            Assert.IsNotNull(audioMgr.FootstepClip, "FootstepClip must be synthesized.");
            Assert.IsNotNull(audioMgr.MeleeWhooshClip, "MeleeWhooshClip must be synthesized.");
            Assert.IsNotNull(audioMgr.DayAmbientClip, "DayAmbientClip must be synthesized.");
            Assert.IsNotNull(audioMgr.NightAmbientClip, "NightAmbientClip must be synthesized.");
        }

        [Test, Order(3)]
        public void Scene_Camera_ConfiguredWithShakeSupport()
        {
            var cam = Camera.main;
            Assert.IsNotNull(cam, "Main Camera must exist in SampleScene.");

            var follow = cam.GetComponent<TopDownCameraFollow>();
            Assert.IsNotNull(follow, "Main Camera must have TopDownCameraFollow component.");
            Assert.IsFalse(follow.IsShaking, "Camera should not start in shaking state.");
        }
    }
}
#endif
