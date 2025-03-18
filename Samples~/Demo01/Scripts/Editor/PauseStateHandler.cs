using UnityEngine;
using UnityEditor;

namespace LFramework.Kcc.Demo01
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