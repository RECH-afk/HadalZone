namespace EasyPeasyFirstPersonController
{
    using System;
    using System.Collections;
    using UnityEngine;

    public partial class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Range(0, 100)] public float mouseSensitivity = 50f;
        [Range(0f, 200f)] private float snappiness = 100f;
        [Range(0f, 20f)] public float walkSpeed = 3f;
        [Range(0f, 30f)] public float sprintSpeed = 5f;
        [Range(0f, 10f)] public float crouchSpeed = 1.5f;
        public float crouchHeight = 1f;
        public float crouchCameraHeight = 1f;

        [Header("Slide Settings")]
        public float slideSpeed = 8f;
        public float slideDuration = 0.7f;
        public float slideFovBoost = 5f;
        public float slideTiltAngle = 5f;

        [Header("Jump & Gravity")]
        [Range(0f, 15f)] public float jumpSpeed = 3f;
        [Range(0f, 50f)] public float gravity = 9.81f;
        [Range(0.01f, 0.3f)] public float coyoteTimeDuration = 0.2f;

        [Header("FOV Settings")]
        public float normalFov = 60f;
        public float sprintFov = 70f;
        public float fovChangeSpeed = 5f;

        [Header("Zoom Settings")]
        public KeyCode zoomKey = KeyCode.Mouse1;
        public float zoomFov = 40f;
        [Range(0.1f, 1f)] public float zoomSensitivityMultiplier = 0.5f;

        [Space(5)]
        [Header("Camera Settings")]
        public float bobbingAmount = 0.05f;
        public float walkingBobbingSpeed = 14f;
        public float idleBobbingAmount = 0.01f;
        public float idleBobbingSpeed = 2f;

        [Space(5)]
        public float bobPitchAmount = 0.5f;
        public float bobRollAmount = 0.5f;

        [Space(5)]
        public float strafeTiltAngle = 1.5f;
        public float lookSwayMultiplier = 1.5f;

        [Space(5)]
        public float landingDipAmount = 0.15f;
        public float jumpDipAmount = 0.1f;

        private float sprintBobMultiplier = 1.3f;
        private float recoilReturnSpeed = 8f;

        [Header("Toggles & Checks")]
        public bool canSlide = true;
        public bool canJump = true;
        public bool canSprint = true;
        public bool canCrouch = true;
        public bool canZoom = true;
        public bool coyoteTimeEnabled = true;
        public QueryTriggerInteraction ceilingCheckQueryTriggerInteraction = QueryTriggerInteraction.Ignore;
        public QueryTriggerInteraction groundCheckQueryTriggerInteraction = QueryTriggerInteraction.Ignore;
        public Transform groundCheck;
        public float groundDistance = 0.2f;
        public LayerMask groundMask;

        [Header("References")]
        public Transform playerCamera;
        public Transform cameraParent;

        private float rotX, rotY;
        private float xVelocity, yVelocity;
        private CharacterController characterController;
        private Vector3 moveDirection = Vector3.zero;
        private bool isGrounded;
        private bool wasGrounded;
        private Vector2 moveInput;

        public bool isSprinting;
        public bool isCrouching;
        public bool isSliding;
        public bool isZooming;

        private float slideTimer;
        private float postSlideCrouchTimer;
        private Vector3 slideDirection;
        private float originalHeight;
        private float originalCameraParentHeight;
        private float coyoteTimer;
        private Camera cam;
        private AudioSource slideAudioSource;

        private float bobTimer;
        private float currentBobAmplitude;
        private float currentBobSpeed;
        private float mouseXSway;
        private float defaultPosY;
        private Vector3 currentTilt = Vector3.zero;
        private bool isLook = true, isMove = true;

        private float currentCameraHeight;
        private float currentBobOffsetY;
        private float currentBobOffsetX;
        private float currentFov;
        private float fovVelocity;
        private float currentSlideSpeed;
        private float slideSpeedVelocity;
        private float currentTiltAngle;
        private float tiltVelocity;
        private float bodyDip;
        private float bodyDipVelocity;

        public float CurrentCameraHeight => isCrouching || isSliding ? crouchCameraHeight : originalCameraParentHeight;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            cam = playerCamera.GetComponent<Camera>();
            originalHeight = characterController.height;
            originalCameraParentHeight = cameraParent.localPosition.y;
            defaultPosY = cameraParent.localPosition.y;

            slideAudioSource = gameObject.AddComponent<AudioSource>();
            slideAudioSource.playOnAwake = false;
            slideAudioSource.loop = false;

            Cursor.lockState = CursorLockMode.Locked;

            currentCameraHeight = originalCameraParentHeight;
            currentFov = normalFov;

            currentBobAmplitude = idleBobbingAmount;
            currentBobSpeed = idleBobbingSpeed;

            rotX = transform.rotation.eulerAngles.y;
            rotY = playerCamera.localRotation.eulerAngles.x;
            xVelocity = rotX;
            yVelocity = rotY;
        }

        private void Update()
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, groundCheckQueryTriggerInteraction);

            if (canZoom && !isSprinting && !isSliding)
            {
                isZooming = Input.GetKey(zoomKey);
            }
            else
            {
                isZooming = false;
            }

            if (isGrounded && !wasGrounded)
            {
                if (moveDirection.y < -3f)
                {
                    bodyDip = -landingDipAmount * Mathf.Clamp01(Mathf.Abs(moveDirection.y) / 10f);
                }
                coyoteTimer = coyoteTimeEnabled ? coyoteTimeDuration : 0f;
            }
            else if (coyoteTimeEnabled && !isGrounded)
            {
                coyoteTimer -= Time.deltaTime;
            }

            bodyDip = Mathf.SmoothDamp(bodyDip, 0f, ref bodyDipVelocity, 0.15f);

            if (isGrounded && moveDirection.y < 0)
            {
                moveDirection.y = -2f;
            }

            if (isLook)
            {
                float currentSensitivity = mouseSensitivity * (isZooming ? zoomSensitivityMultiplier : 1f);

                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                mouseXSway = Mathf.Lerp(mouseXSway, mouseX, Time.deltaTime * 10f);

                rotX += mouseX * 10 * currentSensitivity * Time.deltaTime;
                rotY -= mouseY * 10 * currentSensitivity * Time.deltaTime;
                rotY = Mathf.Clamp(rotY, -90f, 90f);

                xVelocity = Mathf.Lerp(xVelocity, rotX, snappiness * Time.deltaTime);
                yVelocity = Mathf.Lerp(yVelocity, rotY, snappiness * Time.deltaTime);

                float targetTiltAngle = isSliding ? slideTiltAngle : 0f;
                currentTiltAngle = Mathf.SmoothDamp(currentTiltAngle, targetTiltAngle, ref tiltVelocity, 0.2f);

                playerCamera.transform.localRotation = Quaternion.Euler(yVelocity - currentTiltAngle, 0f, 0f);
                transform.rotation = Quaternion.Euler(0f, xVelocity, 0f);
            }

            HandleHeadBob();

            bool wantsToCrouch = canCrouch && Input.GetKey(KeyCode.LeftControl) && !isSliding;
            Vector3 point1 = transform.position + characterController.center - Vector3.up * (characterController.height * 0.5f);
            Vector3 point2 = point1 + Vector3.up * characterController.height * 0.6f;
            float capsuleRadius = characterController.radius * 0.95f;
            float castDistance = isSliding ? originalHeight + 0.2f : originalHeight - crouchHeight + 0.2f;
            bool hasCeiling = Physics.CapsuleCast(point1, point2, capsuleRadius, Vector3.up, castDistance, groundMask, ceilingCheckQueryTriggerInteraction);

            if (isSliding)
            {
                postSlideCrouchTimer = 0.3f;
            }
            if (postSlideCrouchTimer > 0)
            {
                postSlideCrouchTimer -= Time.deltaTime;
                isCrouching = canCrouch;
            }
            else
            {
                isCrouching = canCrouch && (wantsToCrouch || (hasCeiling && !isSliding));
            }

            if (canSlide && isSprinting && Input.GetKeyDown(KeyCode.LeftControl) && isGrounded)
            {
                isSliding = true;
                slideTimer = slideDuration;
                slideDirection = moveInput.magnitude > 0.1f ? (transform.right * moveInput.x + transform.forward * moveInput.y).normalized : transform.forward;
                currentSlideSpeed = sprintSpeed;
            }

            float slideProgress = slideTimer / slideDuration;
            if (isSliding)
            {
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0f || !isGrounded)
                {
                    isSliding = false;
                }
                float targetSlideSpeed = slideSpeed * Mathf.Lerp(0.7f, 1f, slideProgress);
                currentSlideSpeed = Mathf.SmoothDamp(currentSlideSpeed, targetSlideSpeed, ref slideSpeedVelocity, 0.2f);
                characterController.Move(slideDirection * currentSlideSpeed * Time.deltaTime);
            }

            float targetHeight = isCrouching || isSliding ? crouchHeight : originalHeight;
            characterController.height = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * 10f);
            characterController.center = new Vector3(0f, characterController.height * 0.5f, 0f);

            float targetFov = normalFov;
            if (isZooming)
            {
                targetFov = zoomFov;
            }
            else if (isSprinting)
            {
                targetFov = sprintFov;
            }
            else if (isSliding)
            {
                targetFov = sprintFov + (slideFovBoost * Mathf.Lerp(0f, 1f, 1f - slideProgress));
            }

            float fallFovBoost = (!isGrounded && moveDirection.y < -5f) ? Mathf.Clamp(Mathf.Abs(moveDirection.y) * 0.4f, 0f, 15f) : 0f;
            targetFov += fallFovBoost;

            currentFov = Mathf.SmoothDamp(currentFov, targetFov, ref fovVelocity, 1f / fovChangeSpeed);
            cam.fieldOfView = currentFov;

            HandleMovement();

            wasGrounded = isGrounded;
        }

        private void HandleHeadBob()
        {
            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            bool isMovingEnough = horizontalVelocity.magnitude > 0.1f;

            float targetBobSpeed = idleBobbingSpeed;
            float targetBobAmount = idleBobbingAmount;

            if (isMovingEnough && isGrounded && !isSliding)
            {
                float speedMultiplier = isSprinting ? sprintBobMultiplier : (isCrouching ? 0.7f : 1f);
                float amountMultiplier = isSprinting ? 1.3f : (isCrouching ? 0.6f : 1f);

                targetBobSpeed = walkingBobbingSpeed * speedMultiplier;
                targetBobAmount = bobbingAmount * amountMultiplier;
            }
            else if (!isGrounded || isSliding)
            {
                targetBobSpeed = 0f;
                targetBobAmount = 0f;
            }

            currentBobAmplitude = Mathf.Lerp(currentBobAmplitude, targetBobAmount, Time.deltaTime * 5f);
            currentBobSpeed = Mathf.Lerp(currentBobSpeed, targetBobSpeed, Time.deltaTime * 5f);

            bobTimer += Time.deltaTime * currentBobSpeed;

            currentBobOffsetY = Mathf.Sin(bobTimer) * currentBobAmplitude;
            currentBobOffsetX = Mathf.Cos(bobTimer / 2f) * currentBobAmplitude;

            float bobRatio = currentBobAmplitude / (bobbingAmount > 0.001f ? bobbingAmount : 1f);
            float currentBobPitch = -Mathf.Abs(Mathf.Sin(bobTimer)) * bobPitchAmount * bobRatio;
            float currentBobRoll = Mathf.Cos(bobTimer / 2f) * bobRollAmount * bobRatio;

            float targetStrafeTilt = 0f;
            if (isGrounded && !isSliding)
            {
                targetStrafeTilt = (-moveInput.x * strafeTiltAngle) - (mouseXSway * lookSwayMultiplier);
            }

            currentTilt.z = Mathf.Lerp(currentTilt.z, targetStrafeTilt + currentBobRoll, Time.deltaTime * recoilReturnSpeed);
            currentTilt.x = Mathf.Lerp(currentTilt.x, currentBobPitch, Time.deltaTime * recoilReturnSpeed);

            float targetCameraHeight = isCrouching || isSliding ? crouchCameraHeight : originalCameraParentHeight;
            currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, Time.deltaTime * 10f);

            cameraParent.localPosition = new Vector3(
                currentBobOffsetX,
                currentCameraHeight + currentBobOffsetY + bodyDip,
                cameraParent.localPosition.z
            );

            cameraParent.localRotation = Quaternion.Euler(currentTilt.x, 0f, currentTilt.z);
        }

        private void HandleMovement()
        {
            moveInput.x = Input.GetAxis("Horizontal");
            moveInput.y = Input.GetAxis("Vertical");

            isSprinting = canSprint && Input.GetKey(KeyCode.LeftShift) && moveInput.y > 0.1f && isGrounded && !isCrouching && !isSliding && !isZooming;

            float currentSpeed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            if (!isMove) currentSpeed = 0f;

            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 moveVector = transform.TransformDirection(direction) * currentSpeed;
            moveVector = Vector3.ClampMagnitude(moveVector, currentSpeed);

            if (isGrounded || coyoteTimer > 0f)
            {
                if (canJump && Input.GetKeyDown(KeyCode.Space) && !isSliding)
                {
                    moveDirection.y = jumpSpeed;
                    coyoteTimer = 0f;

                    bodyDip = -jumpDipAmount;
                }
            }
            else
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }

            if (!isSliding)
            {
                moveDirection = new Vector3(moveVector.x, moveDirection.y, moveVector.z);
                characterController.Move(moveDirection * Time.deltaTime);
            }
        }

        public void SetControl(bool newState)
        {
            SetLookControl(newState);
            SetMoveControl(newState);
        }

        public void SetLookControl(bool newState)
        {
            isLook = newState;
        }

        public void SetMoveControl(bool newState)
        {
            isMove = newState;
        }

        public void SetCursorVisibility(bool newVisibility)
        {
            Cursor.lockState = newVisibility ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = newVisibility;
        }
    }
}