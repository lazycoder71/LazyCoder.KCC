using LFramework;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LFramework.Kcc.Demo02
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
        [SerializeField] private KCC_Character _character;

        [Title("Config")]
        [SerializeField] private OrientationMethod _orientationMethod = OrientationMethod.TowardsMovement;

        private Camera _camera;

        private KCC_InputPlayer _inputKCC = new KCC_InputPlayer();

        #region MonoBehaviour

        protected override void Start()
        {
            base.Start();

            _camera = Camera.main;
        }

        protected override void Tick()
        {
            // Adjust input by current camera view
            AdjustInputByCameraView(_camera.transform, _character.TransformCached.up);

            // Apply inputs to character
            _character.SetInputs(ref _inputKCC);
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
            _inputKCC.MoveVector = cameraPlanarRotation * _inputKCC.MoveVector;

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

        public void Input_Move(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();

            _inputKCC.MoveVector.x = moveInput.x;
            _inputKCC.MoveVector.z = moveInput.y;
        }

        public void Input_Jump(InputAction.CallbackContext context)
        {
            _inputKCC.Jump = context.ReadValueAsButton();
        }
    }
}