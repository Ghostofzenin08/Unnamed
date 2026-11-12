using UnityEngine;
using Outpost404.Audio;

namespace Outpost404.Combat
{
    /// <summary>
    /// Spawns procedural visual combat juice effects including melee slash arcs,
    /// material hit flashes, and particle bursts for wood splinters, rock sparks, and enemy blood.
    /// Operates without external asset dependencies.
    /// </summary>
    public static class ProceduralCombatEffects
    {
        private static Material defaultParticleMaterial;
        private static Material slashArcMaterial;

        public static Material GetParticleMaterial()
        {
            if (defaultParticleMaterial == null)
            {
                Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    defaultParticleMaterial = new Material(shader);
                }
            }
            return defaultParticleMaterial;
        }

        public static Material GetSlashMaterial()
        {
            if (slashArcMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    slashArcMaterial = new Material(shader);
                    slashArcMaterial.color = new Color(0.35f, 0.85f, 1.0f, 0.8f);
                }
            }
            return slashArcMaterial;
        }

        /// <summary>
        /// Spawns a procedural particle burst at the hit location with colors and physics tuned to impact type.
        /// </summary>
        public static GameObject SpawnImpactParticles(Vector3 position, Vector3 normal, ImpactType type)
        {
            GameObject go = new GameObject($"ImpactParticles_{type}");
            go.transform.position = position;
            if (normal.sqrMagnitude > 0.01f)
            {
                go.transform.forward = normal;
            }

            var ps = go.AddComponent<ParticleSystem>();
            var pRenderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = GetParticleMaterial();
            if (mat != null) pRenderer.material = mat;

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            Color minColor;
            Color maxColor;
            float speed;
            int count;

            switch (type)
            {
                case ImpactType.Timber:
                    // Organic wood splinters
                    minColor = new Color(0.55f, 0.35f, 0.15f, 1f);
                    maxColor = new Color(0.85f, 0.65f, 0.35f, 1f);
                    speed = 4.5f;
                    count = 14;
                    main.gravityModifier = 1.4f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
                    break;

                case ImpactType.Rock:
                    // Crystalline blue/white sparks
                    minColor = new Color(0.3f, 0.85f, 1f, 1f);
                    maxColor = new Color(0.9f, 0.95f, 1f, 1f);
                    speed = 6.0f;
                    count = 16;
                    main.gravityModifier = 1.0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.20f, 0.35f);
                    break;

                case ImpactType.Enemy:
                    // Visceral bioluminescent alien blood
                    minColor = new Color(0.1f, 0.95f, 0.4f, 1f);
                    maxColor = new Color(0.2f, 1.0f, 0.8f, 1f);
                    speed = 4.0f;
                    count = 18;
                    main.gravityModifier = 1.6f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.50f);
                    break;

                default:
                    minColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                    maxColor = Color.white;
                    speed = 3.5f;
                    count = 10;
                    main.gravityModifier = 1.0f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.40f);
                    break;
            }

            main.startColor = new ParticleSystem.MinMaxGradient(minColor, maxColor);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;

            ps.Play();
            if (Application.isPlaying)
            {
                Object.Destroy(go, 0.6f);
            }
            return go;
        }

        /// <summary>
        /// Generates a fast forward visual melee slash arc mesh in front of the player.
        /// </summary>
        public static GameObject SpawnMeleeSlashArc(Transform origin, float radius = 1.6f, float sweepAngle = 110f)
        {
            if (origin == null) return null;

            GameObject arcObj = new GameObject("MeleeSlashArc");
            arcObj.transform.position = origin.position + Vector3.up * 0.9f;
            arcObj.transform.rotation = origin.rotation;

            var lr = arcObj.AddComponent<LineRenderer>();
            var mat = GetSlashMaterial();
            if (mat != null) lr.material = mat;

            int segments = 16;
            lr.positionCount = segments;
            lr.useWorldSpace = false;
            lr.startWidth = 0.22f;
            lr.endWidth = 0.04f;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.3f, 0.9f, 1f), 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            lr.colorGradient = gradient;

            float startAngle = -sweepAngle * 0.5f;
            float step = sweepAngle / (segments - 1);

            for (int i = 0; i < segments; i++)
            {
                float angle = (startAngle + (step * i)) * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                lr.SetPosition(i, new Vector3(x, 0f, z));
            }

            if (Application.isPlaying)
            {
                Object.Destroy(arcObj, 0.12f);
            }
            return arcObj;
        }
    }
}
