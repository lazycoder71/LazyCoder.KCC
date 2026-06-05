using UnityEngine;
using UnityEditor;
using LazyCoder.Kcc;

namespace LazyCoder.Kcc.Demo
{
    public class PauseStateHandler
    {
        [RuntimeInitializeOnLoadMethod()]
        public static void Init()
        {
            EditorApplication.pauseStateChanged += HandlePauseStateChange;
        }

        private static void HandlePauseStateChange(PauseState state)
        {
            foreach (KccMotor motor in KccSystem.Motors)
            {
                motor.SetPositionAndRotation(motor.Transform.position, motor.Transform.rotation, true);
            }
        }
    }
}
