using UnityEngine;

namespace LFramework.Kcc
{
    public interface IKccControllerMover
    {
        /// <summary>
        /// This is called to let you tell the PhysicsMover where it should be right now
        /// </summary>
        void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime);
    }
}