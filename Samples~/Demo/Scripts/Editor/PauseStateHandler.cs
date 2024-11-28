using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LFramework.KCC;

public class PauseStateHandler
{
    [RuntimeInitializeOnLoadMethod()]
    public static void Init()
    {
        EditorApplication.pauseStateChanged += HandlePauseStateChange;
    }

    private static void HandlePauseStateChange(PauseState state)
    {
        foreach(KCCMotor motor in KCCSystem.CharacterMotors)
        {
            motor.SetPositionAndRotation(motor.Transform.position, motor.Transform.rotation, true);
        }
    }
}
