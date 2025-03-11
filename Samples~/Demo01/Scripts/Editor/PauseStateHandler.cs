using UnityEngine;
using UnityEditor;

namespace LFramework.KCC.Demo01
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
            foreach (KCCMotor motor in KCCSystem.CharacterMotors)
            {
                motor.SetPositionAndRotation(motor.Transform.position, motor.Transform.rotation, true);
            }
        }
    }
}