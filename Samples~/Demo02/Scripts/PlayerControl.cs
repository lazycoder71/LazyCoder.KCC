using LFramework;
using LFramework.View;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class PlayerControl : MonoBase
    {
        [System.Serializable]
        public enum OrientationMethod
        {
            TowardsCamera,
            TowardsMovement,
        }

        [Title("Reference")]
        [SerializeField] private KccCharacter _character;

        [Title("Config")]
        [SerializeField] private OrientationMethod _orientationMethod = OrientationMethod.TowardsMovement;
        [SerializeField] private GameObject _gui;

        [Space]

        [Required]
        [SerializeField] private InputActionReference _inputActionReferenceMove;

        [Required]
        [SerializeField] private InputActionReference _inputActionReferenceJump;

        private Camera _camera;

        private KccInputPlayer _inputKCC = new KccInputPlayer();

        private Vector3 _inputMove;

        private GameObject _objGui;

        #region MonoBehaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            _inputActionReferenceJump.action.started += InputActionReference_Jump;
            _inputActionReferenceJump.action.performed += InputActionReference_Jump;
            _inputActionReferenceJump.action.canceled += InputActionReference_Jump;

            _inputActionReferenceMove.action.started += InputActionReference_Move;
            _inputActionReferenceMove.action.performed += InputActionReference_Move;
            _inputActionReferenceMove.action.canceled += InputActionReference_Move;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            _inputActionReferenceJump.action.started -= InputActionReference_Jump;
            _inputActionReferenceJump.action.performed -= InputActionReference_Jump;
            _inputActionReferenceJump.action.canceled -= InputActionReference_Jump;

            _inputActionReferenceMove.action.started -= InputActionReference_Move;
            _inputActionReferenceMove.action.performed -= InputActionReference_Move;
            _inputActionReferenceMove.action.canceled -= InputActionReference_Move;
        }

        protected override void Start()
        {
            base.Start();

            _objGui = _gui.Create(ViewContainer.Instance.TransformCached, false);
        }

        private void OnDestroy()
        {
            if (_objGui != null)
                Destroy(_objGui);
        }

        protected override void Tick()
        {
            if (_camera == null)
                _camera = Camera.main;

            // Adjust input by current camera view
            AdjustInputByCameraView(_camera.transform, _character.TransformCached.up);

            // Apply inputs to character
            _character.SetInputs(ref _inputKCC);

            _inputKCC.Jump = false;
        }

        #endregion

        private void AdjustInputByCameraView(Transform cameraTransform, Vector3 characterUp)
        {
            // Calculate camera direction and rotation on the character plane
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(cameraTransform.rotation * Vector3.forward, characterUp).normalized;

            if (cameraPlanarDirection.sqrMagnitude == 0f)
                cameraPlanarDirection = Vector3.ProjectOnPlane(cameraTransform.rotation * Vector3.up, characterUp).normalized;

            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, characterUp);

            // Move and look inputs
            _inputKCC.MoveVector = cameraPlanarRotation * _inputMove;

            switch (_orientationMethod)
            {
                case OrientationMethod.TowardsCamera:
                    _inputKCC.LookVector = cameraPlanarDirection;
                    break;
                case OrientationMethod.TowardsMovement:
                    _inputKCC.LookVector = _inputKCC.MoveVector.normalized;
                    break;
            }
        }

        public void InputActionReference_Move(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();

            _inputMove.x = moveInput.x;
            _inputMove.z = moveInput.y;
            _inputMove.y = 0f;

            // Clamp input
            _inputMove = Vector3.ClampMagnitude(_inputMove, 1f);
        }

        public void InputActionReference_Jump(InputAction.CallbackContext context)
        {
            _inputKCC.Jump = context.ReadValueAsButton();
        }
    }
}