using UnityEngine;

namespace RKS.HadalZone.Core.Dialogue
{
    [RequireComponent(typeof(DialogueTrigger))]
    public class NpcBrain : MonoBehaviour
    {
        [Header("Tracking")]
        [SerializeField] private bool trackPlayer;
        [SerializeField] private float noticeDistance = 5f;
        [SerializeField] private float bodyRotateSpeed = 2f;

        [Header("Head Tracking")]
        [SerializeField] private Transform headBone;
        [SerializeField] private float headTrackingSpeed = 3f;
        [SerializeField] private float maxHeadAngle = 60f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string talkParam = "talk";

        [Header("Presence")]
        [SerializeField][Range(0f, 0.1f)] private float jitterIntensity;
        [SerializeField][Range(0.1f, 10f)] private float jitterFrequency = 2f;

        [Header("Indicator")]
        [SerializeField] private GameObject indicator;

        [Header("Dialogue")]
        [SerializeField] private bool lookAtPlayerWhileTalking = true;

        private Transform _player;
        private Quaternion _originalBodyRotation;
        private Quaternion _originalHeadRotation;
        private DialogueTrigger _trigger;
        private bool _isTalking;
        private Vector3 _jitterOffset;
        private float _jitterSeed;

        private void Start()
        {
            _originalBodyRotation = transform.rotation;

            if (headBone != null)
                _originalHeadRotation = headBone.rotation;

            _jitterSeed = Random.Range(0f, 100f);

            _trigger = GetComponent<DialogueTrigger>();
            _trigger.onDialogueStarted.AddListener(OnDialogueStart);
            _trigger.onDialogueEnded.AddListener(OnDialogueEnd);

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _player = player.transform;

            if (indicator != null) indicator.SetActive(false);
        }

        private void Update()
        {
            if (_player == null) return;

            UpdateJitter();

            float distance = Vector3.Distance(transform.position, _player.position);
            bool inRange = distance <= noticeDistance;
            bool canFacePlayer = _isTalking ? lookAtPlayerWhileTalking && inRange : trackPlayer && inRange;

            if (canFacePlayer)
            {
                Vector3 direction = _player.position - transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion target = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, target, bodyRotateSpeed * Time.deltaTime);
                }
            }
            else if (!_isTalking)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, _originalBodyRotation, bodyRotateSpeed * Time.deltaTime);
            }

            UpdateHeadTracking();
        }

        private void UpdateJitter()
        {
            if (jitterIntensity <= 0f) return;

            float t = Time.time * jitterFrequency + _jitterSeed;
            _jitterOffset.x = Mathf.PerlinNoise(t, 0f) * 2f - 1f;
            _jitterOffset.y = Mathf.PerlinNoise(0f, t) * 2f - 1f;
            _jitterOffset.z = Mathf.PerlinNoise(t + 10f, t + 10f) * 2f - 1f;
            _jitterOffset *= jitterIntensity;

            transform.localPosition = _jitterOffset;
        }

        private void UpdateHeadTracking()
        {
            if (headBone == null || _player == null) return;
            if (!_isTalking && !trackPlayer) return;
            if (_isTalking && !lookAtPlayerWhileTalking) return;

            Vector3 direction = _player.position - headBone.position;
            Quaternion target = Quaternion.LookRotation(direction);
            Quaternion current = headBone.rotation;

            float angle = Quaternion.Angle(_originalHeadRotation, target);
            if (angle > maxHeadAngle)
                target = Quaternion.RotateTowards(_originalHeadRotation, target, maxHeadAngle);

            headBone.rotation = Quaternion.Slerp(current, target, headTrackingSpeed * Time.deltaTime);
        }

        private void OnDialogueStart()
        {
            _isTalking = true;

            if (animator != null && !string.IsNullOrEmpty(talkParam))
                animator.SetBool(talkParam, true);

            if (_player != null && lookAtPlayerWhileTalking)
            {
                Vector3 direction = _player.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(direction);
            }

            if (indicator != null)
                indicator.SetActive(false);
        }

        private void OnDialogueEnd()
        {
            _isTalking = false;

            if (animator != null && !string.IsNullOrEmpty(talkParam))
                animator.SetBool(talkParam, false);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, noticeDistance);
        }
    }
}
