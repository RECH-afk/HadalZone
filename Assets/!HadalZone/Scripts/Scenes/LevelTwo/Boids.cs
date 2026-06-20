using UnityEngine;
using RKS.HadalZone.Core;

namespace RKS.HadalZone.Environment
{
    public class Boids : RKSBehaviour
    {
        public Transform[] waypoints;

        [Header("Movement")]
        public float minSpeed = 10f;
        public float maxSpeed = 18f;
        public float acceleration = 2f;
        public float turnSpeed = 2f;
        public float arriveDistance = 10f;
        public float waypointRadius = 25f;
        public float heightVariation = 8f;
        public float noiseStrength = 5f;
        public float noiseSpeed = 0.2f;
        public float minLoiterTime = 5f;
        public float maxLoiterTime = 15f;
        public float circleRadius = 20f;
        public float circleSpeed = 1f;

        private Transform currentTarget;
        private int currentIndex = -1;

        private Vector3 destination;

        private float currentSpeed;
        private float desiredSpeed;

        private bool loitering;
        private float loiterTimer;
        private float circleAngle;

        protected override void OnReady()
        {
            currentSpeed = Random.Range(minSpeed, maxSpeed);
            desiredSpeed = currentSpeed;

            PickNextTarget();
        }

        protected override void Update()
        {
            if (currentTarget == null)
                return;

            if (loitering)
            {
                UpdateLoitering();
                return;
            }

            UpdateFlight();
        }

        private void UpdateFlight()
        {
            if (Random.value < 0.002f)
            {
                desiredSpeed =
                    Random.Range(minSpeed, maxSpeed);
            }

            currentSpeed = Mathf.Lerp(
                currentSpeed,
                desiredSpeed,
                acceleration * Time.deltaTime
            );

            Vector3 noise = new Vector3(
                Mathf.PerlinNoise(Time.time * noiseSpeed, 0f) - 0.5f,
                Mathf.PerlinNoise(0f, Time.time * noiseSpeed) - 0.5f,
                Mathf.PerlinNoise(Time.time * noiseSpeed, 100f) - 0.5f
            ) * noiseStrength;

            Vector3 targetPosition =
                destination + noise;

            Vector3 direction =
                (targetPosition - transform.position).normalized;

            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            transform.position +=
                transform.forward *
                currentSpeed *
                Time.deltaTime;

            if (Vector3.Distance(
                    transform.position,
                    destination)
                <= arriveDistance)
            {
                StartLoitering();
            }
        }

        private void UpdateLoitering()
        {
            loiterTimer -= Time.deltaTime;

            circleAngle +=
                circleSpeed * Time.deltaTime;

            Vector3 center = destination;

            Vector3 orbitPoint =
                center +
                new Vector3(
                    Mathf.Cos(circleAngle),
                    0f,
                    Mathf.Sin(circleAngle)
                ) * circleRadius;

            Vector3 direction =
                (orbitPoint - transform.position).normalized;

            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            transform.position +=
                transform.forward *
                (currentSpeed * 0.6f) *
                Time.deltaTime;

            if (loiterTimer <= 0f)
            {
                loitering = false;
                PickNextTarget();
            }
        }

        private void StartLoitering()
        {
            if (loitering)
                return;

            loitering = true;

            loiterTimer = Random.Range(
                minLoiterTime,
                maxLoiterTime
            );

            circleAngle =
                Random.Range(0f, Mathf.PI * 2f);
        }

        private void PickNextTarget()
        {
            if (waypoints == null ||
                waypoints.Length == 0)
                return;

            int nextIndex;

            do
            {
                nextIndex =
                    Random.Range(
                        0,
                        waypoints.Length
                    );
            }
            while (
                waypoints.Length > 1 &&
                nextIndex == currentIndex
            );

            currentIndex = nextIndex;
            currentTarget = waypoints[nextIndex];

            destination =
                currentTarget.position +
                new Vector3(
                    Random.Range(
                        -waypointRadius,
                        waypointRadius
                    ),
                    Random.Range(
                        -heightVariation,
                        heightVariation
                    ),
                    Random.Range(
                        -waypointRadius,
                        waypointRadius
                    )
                );
        }
    }
}