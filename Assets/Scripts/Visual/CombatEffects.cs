using UnityEngine;

namespace Netologia.TowerDefence
{


    public static class CombatEffects
    {
        private static Material _particleMaterial;

        public static void Hit(Vector3 position)
        {
            CreateBurst(position, new Color(1f, 0.78f, 0.18f, 1f),
                particles: 9, size: 0.07f, speed: 0.85f, life: 0.25f);
        }

        public static void Death(Vector3 position)
        {
            CreateBurst(position, new Color(0.55f, 0.68f, 0.57f, 0.9f),
                particles: 14, size: 0.11f, speed: 0.5f, life: 0.52f);
        }

        private static void CreateBurst(Vector3 position, Color color, int particles,
            float size, float speed, float life)
        {
            var effectObject = new GameObject("CombatEffect");
            effectObject.transform.position = position;

            var system = effectObject.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.loop = false;
            main.duration = 0.15f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particles) });

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.03f;

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 20;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var material = GetParticleMaterial();
            if (material != null)
                renderer.sharedMaterial = material;

            system.Play();
            Object.Destroy(effectObject, life + 0.6f);
        }

        private static Material GetParticleMaterial()
        {
            if (_particleMaterial != null)
                return _particleMaterial;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            _particleMaterial = new Material(shader)
            {
                name = "ParticleMaterial",
                hideFlags = HideFlags.DontSave
            };
            return _particleMaterial;
        }
    }
}
