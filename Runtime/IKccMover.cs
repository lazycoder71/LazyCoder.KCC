using UnityEngine;

namespace LazyCoder.Kcc
{
    public interface IKccMover
    {
        /// <summary>
        /// This is called to let you tell the KccMover where it should be right now
        /// </summary>
        void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime);
    }
}