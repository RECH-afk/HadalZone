using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace RKS.HadalZone.Environment
{
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        public enum FlickerMode
        {
            Random,
            Sine,
            Pulse,
            Strobe,
            Candle,
            ElectricalFault,
            PerlinNoise
        }

        [Header("General")]
        public FlickerMode mode = FlickerMode.Random;

        public float minIntensity = 1000f;
        public float maxIntensity = 7000f;

        [Header("Speed")]
        public float speed = 5f;

        [Header("Random")]
        public float randomUpdateRate = 0.05f;

        [Header("Strobe")]
        public float strobeFrequency = 15f;

        [Header("Electrical Fault")]
        public float faultChance = 0.03f;
        public float faultOffTime = 0.1f;

        private HDAdditionalLightData hdLight;
        private Light unityLight;

        private float randomTimer;
        private float targetIntensity;
        private float faultTimer;
        private bool faultActive;

        private void Awake()
        {
            unityLight = GetComponent<Light>();
            hdLight = GetComponent<HDAdditionalLightData>();

            targetIntensity = maxIntensity;
        }

        private void Update()
        {
            switch (mode)
            {
                case FlickerMode.Random:
                    RandomFlicker();
                    break;

                case FlickerMode.Sine:
                    SineFlicker();
                    break;

                case FlickerMode.Pulse:
                    PulseFlicker();
                    break;

                case FlickerMode.Strobe:
                    StrobeFlicker();
                    break;

                case FlickerMode.Candle:
                    CandleFlicker();
                    break;

                case FlickerMode.ElectricalFault:
                    ElectricalFault();
                    break;

                case FlickerMode.PerlinNoise:
                    PerlinNoiseFlicker();
                    break;
            }
        }

        private void SetIntensity(float value)
        {
            if (hdLight != null)
                hdLight.SetIntensity(value, unityLight.lightUnit);
            else
                unityLight.intensity = value;
        }

        private void RandomFlicker()
        {
            randomTimer += Time.deltaTime;

            if (randomTimer >= randomUpdateRate)
            {
                randomTimer = 0;
                targetIntensity = Random.Range(minIntensity, maxIntensity);
            }

            float current = Mathf.Lerp(
                unityLight.intensity,
                targetIntensity,
                Time.deltaTime * speed);

            SetIntensity(current);
        }

        private void SineFlicker()
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;

            SetIntensity(
                Mathf.Lerp(minIntensity, maxIntensity, t)
            );
        }

        private void PulseFlicker()
        {
            float t = Mathf.PingPong(Time.time * speed, 1f);

            SetIntensity(
                Mathf.Lerp(minIntensity, maxIntensity, t)
            );
        }

        private void StrobeFlicker()
        {
            bool on =
                Mathf.Sin(Time.time * strobeFrequency * Mathf.PI * 2f) > 0;

            SetIntensity(on ? maxIntensity : minIntensity);
        }

        private void CandleFlicker()
        {
            float noise =
                Mathf.PerlinNoise(Time.time * speed, 0f);

            noise = Mathf.Pow(noise, 2f);

            SetIntensity(
                Mathf.Lerp(minIntensity, maxIntensity, noise)
            );
        }

        private void ElectricalFault()
        {
            if (faultActive)
            {
                faultTimer -= Time.deltaTime;

                if (faultTimer <= 0)
                    faultActive = false;

                SetIntensity(minIntensity);
                return;
            }

            if (Random.value < faultChance)
            {
                faultActive = true;
                faultTimer = faultOffTime;
            }

            float noise =
                Mathf.PerlinNoise(Time.time * speed * 2f, 0f);

            SetIntensity(
                Mathf.Lerp(minIntensity, maxIntensity, noise)
            );
        }

        private void PerlinNoiseFlicker()
        {
            float noise =
                Mathf.PerlinNoise(
                    Time.time * speed,
                    123.456f
                );

            SetIntensity(
                Mathf.Lerp(minIntensity, maxIntensity, noise)
            );
        }
    }
}