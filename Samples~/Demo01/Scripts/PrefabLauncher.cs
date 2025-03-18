using UnityEngine;
using UnityEngine.InputSystem;

namespace LFramework.Kcc.Demo01
{
    public class PrefabLauncher : MonoBehaviour
    {
        public Rigidbody ToLaunch;
        public float Force;
        public InputActionReference _inputLaunch;

        private void OnEnable()
        {
            _inputLaunch.action.started += InputLanch_Started;
        }

        private void OnDisable()
        {
            _inputLaunch.action.started -= InputLanch_Started;
        }

        private void InputLanch_Started(InputAction.CallbackContext obj)
        {
            Rigidbody inst = Instantiate(ToLaunch, transform.position, transform.rotation);
            inst.AddForce(transform.forward * Force, ForceMode.VelocityChange);
            Destroy(inst.gameObject, 8f);
        }
    }
}