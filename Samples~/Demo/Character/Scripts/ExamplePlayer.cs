using LazyCoder.Kcc;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LazyCoder.Kcc.Demo
{
    public class ExamplePlayer : MonoBehaviour
    {
        public ExampleCharacterController Character;
        public ExampleCharacterCamera CharacterCamera;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);

            CharacterCamera.IgnoredColliders.Clear();
            CharacterCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            HandleCharacterInput();
        }

        private void LateUpdate()
        {
            if (CharacterCamera.RotateWithPhysicsMover && Character.Motor.AttachedRigidbody != null)
            {
                CharacterCamera.PlanarDirection = Character.Motor.AttachedRigidbody.GetComponent<KccMover>().RotationDeltaFromInterpolation * CharacterCamera.PlanarDirection;
                CharacterCamera.PlanarDirection = Vector3.ProjectOnPlane(CharacterCamera.PlanarDirection, Character.Motor.CharacterUp).normalized;
            }

            HandleCameraInput();
        }

        private void HandleCameraInput()
        {
            Vector3 lookInputVector = Vector3.zero;
            if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                lookInputVector = new Vector3(mouseDelta.x, mouseDelta.y, 0f);
            }

            float scrollInput = 0f;
#if !UNITY_WEBGL
            if (Mouse.current != null)
                scrollInput = -Mouse.current.scroll.ReadValue().y;
#endif

            CharacterCamera.UpdateWithInput(Time.deltaTime, scrollInput, lookInputVector);

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                CharacterCamera.TargetDistance = (CharacterCamera.TargetDistance == 0f) ? CharacterCamera.DefaultDistance : 0f;
            }
        }

        private void HandleCharacterInput()
        {
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            if (Keyboard.current != null)
            {
                characterInputs.MoveAxisForward = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
                characterInputs.MoveAxisRight = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
                characterInputs.JumpDown = Keyboard.current.spaceKey.wasPressedThisFrame;
                characterInputs.CrouchDown = Keyboard.current.cKey.wasPressedThisFrame;
                characterInputs.CrouchUp = Keyboard.current.cKey.wasReleasedThisFrame;
            }

            characterInputs.CameraRotation = CharacterCamera.Transform.rotation;

            Character.SetInputs(ref characterInputs);
        }
    }
}