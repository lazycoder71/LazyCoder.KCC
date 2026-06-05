using UnityEngine;
using UnityEngine.InputSystem;

namespace LazyCoder.Kcc.Demo
{
    public class PrefabLauncher : MonoBehaviour
    {
        public Rigidbody ToLaunch;
        public float Force;

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                Rigidbody inst = Instantiate(ToLaunch, transform.position, transform.rotation);
                inst.AddForce(transform.forward * Force, ForceMode.VelocityChange);
                Destroy(inst.gameObject, 8f);
            }
        }
    }
}