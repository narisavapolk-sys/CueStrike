using UnityEngine;

namespace CueStrike.TitleScene
{
    public class TitleSceneParticles : MonoBehaviour
    {
        [Header("Fog / Smoke")]
        [SerializeField] private ParticleSystem fogParticles;
        [SerializeField] private float fogEmissionRate = 3f;

        [Header("Dust Motes")]
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private float dustEmissionRate = 8f;

        [Header("Auto Setup")]
        [SerializeField] private bool autoConfigureOnStart = true;

        // Public properties for Editor access
        public ParticleSystem FogParticles { get => fogParticles; set => fogParticles = value; }
        public ParticleSystem DustParticles { get => dustParticles; set => dustParticles = value; }

        private void Start()
        {
            if (autoConfigureOnStart)
            {
                if (fogParticles != null) ConfigureFog();
                if (dustParticles != null) ConfigureDust();
            }
        }

        public void ConfigureFog()
        {
            if (fogParticles == null) return;

            var main = fogParticles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(12f, 18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startColor = new Color(1f, 1f, 1f, 0.08f);
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = fogParticles.emission;
            emission.rateOverTime = fogEmissionRate;

            var shape = fogParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(25f, 1f, 15f);

            var vel = fogParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);

            var renderer = fogParticles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = -1;
            }
        }

        public void ConfigureDust()
        {
            if (dustParticles == null) return;

            var main = dustParticles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 20f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new Color(1f, 0.95f, 0.8f, 0.25f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = dustParticles.emission;
            emission.rateOverTime = dustEmissionRate;

            var shape = dustParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 8f, 12f);

            var vel = dustParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.03f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

            var col = dustParticles.collision;
            col.enabled = false;
        }
    }
}
