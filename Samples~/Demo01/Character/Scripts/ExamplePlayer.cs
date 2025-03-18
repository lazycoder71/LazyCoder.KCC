using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LFramework.Kcc.Demo01
{
    public class ExamplePlayer : MonoBehaviour
    {
        public ExampleCharacterController Character;
        public ExampleCharacterCamera CharacterCamera;

        public InputActionReference _inputActionReferenceMove;
        public InputActionReference _inputActionReferenceLook;
        public InputActionReference _inputActionReferenceJump;
        public InputActionReference _inputActionReferenceZoom;
        public InputActionReference _inputActionReferenceCrouch;

        private Vector2 _moveValue;
        private Vector2 _lookValue;
        private float _zoomValue;
        private bool _jumpValue;
        private bool _crouchValue;

        private void OnEnable()
        {
            _inputActionReferenceMove.action.started += InputMove;
            _inputActionReferenceMove.action.performed += InputMove;
            _inputActionReferenceMove.action.canceled += InputMove;

            _inputActionReferenceLook.action.started += InputLook;
            _inputActionReferenceLook.action.performed += InputLook;
            _inputActionReferenceLook.action.canceled += InputLook;

            _inputActionReferenceZoom.action.started += InputZoom;
            _inputActionReferenceZoom.action.performed += InputZoom;
            _inputActionReferenceZoom.action.canceled += InputZoom;

            _inputActionReferenceJump.action.started += InputJump;
            _inputActionReferenceJump.action.performed += InputJump;
            _inputActionReferenceJump.action.canceled += InputJump;
            
            _inputActionReferenceCrouch.action.started += InputCrouch;
            _inputActionReferenceCrouch.action.performed += InputCrouch;
            _inputActionReferenceCrouch.action.canceled += InputCrouch;
        }

        private void OnDisable()
        {
            _inputActionReferenceMove.action.started -= InputMove;
            _inputActionReferenceMove.action.performed -= InputMove;
            _inputActionReferenceMove.action.canceled -= InputMove;

            _inputActionReferenceLook.action.started -= InputLook;
            _inputActionReferenceLook.action.performed -= InputLook;
            _inputActionReferenceLook.action.canceled -= InputLook;

            _inputActionReferenceZoom.action.started -= InputZoom;
            _inputActionReferenceZoom.action.performed -= InputZoom;
            _inputActionReferenceZoom.action.canceled -= InputZoom;

            _inputActionReferenceJump.action.started -= InputJump;
            _inputActionReferenceJump.action.performed -= InputJump;
            _inputActionReferenceJump.action.canceled -= InputJump;

            _inputActionReferenceCrouch.action.started -= InputCrouch;
            _inputActionReferenceCrouch.action.performed -= InputCrouch;
            _inputActionReferenceCrouch.action.canceled -= InputCrouch;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // Tell camera to follow transform
            CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);

            // Ignore the character's collider(s) for camera obstruction checks
            CharacterCamera.IgnoredColliders.Clear();
            CharacterCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
        }

        private void Update()
        {
            HandleCharacterInput();
        }

        private void LateUpdate()
        {
            // Handle rotating the camera along with physics movers
            if (CharacterCamera.RotateWithPhysicsMover && Character.Motor.AttachedRigidbody != null)
            {
                CharacterCamera.PlanarDirection = Character.Motor.AttachedRigidbody.GetComponent<KccMover>().RotationDeltaFromInterpolation * CharacterCamera.PlanarDirection;
                CharacterCamera.PlanarDirection = Vector3.ProjectOnPlane(CharacterCamera.PlanarDirection, Character.Motor.CharacterUp).normalized;
            }

            HandleCameraInput();
        }

        private void InputMove(InputAction.CallbackContext obj)
        {
            _moveValue = obj.ReadValue<Vector2>();
        }

        private void InputLook(InputAction.CallbackContext obj)
        {
            _lookValue = obj.ReadValue<Vector2>();
        }

        private void InputZoom(InputAction.CallbackContext obj)
        {
            _zoomValue = obj.ReadValue<Vector2>().y;
        }

        private void InputJump(InputAction.CallbackContext obj)
        {
            _jumpValue = obj.ReadValueAsButton();
        }

        private void InputCrouch(InputAction.CallbackContext obj)
        {
            _crouchValue = obj.ReadValueAsButton();
        }

        private void HandleCameraInput()
        {
            // Create the look input vector for the camera
            float mouseLookAxisUp = _lookValue.y;
            float mouseLookAxisRight = _lookValue.x;
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // Prevent moving the camera while the cursor isn't locked
            //if (Cursor.lockState != CursorLockMode.Locked)
            //{
            //    lookInputVector = Vector3.zero;
            //}

            // Input for zooming the camera (disabled in WebGL because it can cause problems)
            float scrollInput = -_zoomValue;
#if UNITY_WEBGL
            scrollInput = 0f;
#endif

            // Apply inputs to the camera
            CharacterCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

            // Handle toggling zoom level
            //if (Input.GetMouseButtonDown(1))
            //{
            //    CharacterCamera.TargetDistance = (CharacterCamera.TargetDistance == 0f) ? CharacterCamera.DefaultDistance : 0f;
            //}
        }

        private void HandleCharacterInput()
        {
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // Build the CharacterInputs struct
            characterInputs.MoveAxisForward = _moveValue.y;
            characterInputs.MoveAxisRight = _moveValue.x;
            characterInputs.CameraRotation = CharacterCamera.Transform.rotation;
            characterInputs.JumpDown = _jumpValue;
            characterInputs.CrouchDown = _crouchValue;
            characterInputs.CrouchUp = !_crouchValue;

            // Apply inputs to character
            Character.SetInputs(ref characterInputs);
        }
    }
}