using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    [System.Serializable]
    public class KccCharacterConfig : ScriptableObject
    {
        [System.Serializable]
        public enum OrientationBonusMethod
        {
            None,
            TowardsGravity,
            TowardsGroundSlopeAndGravity,
        }

        [Title("Orientation")]
        [SerializeField] private OrientationBonusMethod _orientationBonusMethod = OrientationBonusMethod.TowardsGravity;
        [SerializeField] private float _orientationSharpness = 10f;
        [SerializeField] private float _orientationBonusSharpness = 10f;

        [Title("Ground Movement")]
        [SerializeField] private float _groundMoveSpeedMax = 4.5f;
        [SerializeField] private float _groundMoveSharpness = 15f;

        [Title("Air Movement")]
        [SerializeField] private float _airMoveSpeedMax = 4.5f;
        [SerializeField] private float _airAccelerationSpeed = 15f;
        [SerializeField] private float _airDrag = 0.1f;

        [Title("Jumping")]
        [SerializeField] private float _jumpSpeed = 11f;
        [SerializeField] private int _jumpMultipleCount = 2;
        [SerializeField] private float _jumpMultipleDelayBetween = 0.5f;
        [SerializeField] private float _jumpRequestBufferTime = 0.5f;
        [SerializeField] private float _jumpCoyoteTime = 0.5f;
        [SerializeField] private float _jumpOffLadderSpeed = 3f;

        [Title("Ladder Climb")]
        [SerializeField] private float _ladderClimbDetectDistance = 0.1f;
        [Range(0f, 1f)]
        [SerializeField] private float _ladderClimbDetectHeight = 0.8f;
        [SerializeField] private LayerMask _ladderClimbDetectLayerMask;
        [SerializeField] private float _ladderClimbSpeedMax = 4.5f;
        [SerializeField] private float _ladderClimbSharpness = 10f;
        [Range(0f, 90f)]
        [SerializeField] private float _ladderClimbTowardAngleMax = 90f;

        [Title("Misc")]
        public Vector3 _gravity = new Vector3(0, -30f, 0);

        public OrientationBonusMethod OrientationBonus { get { return _orientationBonusMethod; } }
        public float OrientationSharpness { get { return _orientationBonusSharpness; } }
        public float OrientationBonusSharpness { get { return _orientationBonusSharpness; } }

        public float GroundMoveSpeedMax { get { return _groundMoveSpeedMax; } }
        public float GroundMoveSharpness { get { return _groundMoveSharpness; } }

        public float AirMoveSpeedMax { get { return _airMoveSpeedMax; } }
        public float AirAccelerationSpeed { get { return _airAccelerationSpeed; } }
        public float AirDrag { get { return _airDrag; } }

        public float JumpSpeed { get { return _jumpSpeed; } }
        public int JumpMultipleCount { get { return _jumpMultipleCount; } }
        public float JumpRequestBufferTime { get { return _jumpRequestBufferTime; } }
        public float JumpCoyoteTime { get { return _jumpCoyoteTime; } }
        public float JumpMultipleDelayBetween { get { return _jumpMultipleDelayBetween; } }
        public float JumpOffLadderSpeed { get { return _jumpOffLadderSpeed; } }

        public float LadderClimbDetectDistance { get { return _ladderClimbDetectDistance; } }
        public LayerMask LadderClimbDetectLayerMask { get { return _ladderClimbDetectLayerMask; } }
        public float LadderClimbDetectHeight { get { return _ladderClimbDetectHeight; } }
        public float LadderClimbSpeedMax { get { return _ladderClimbSpeedMax; } }
        public float LadderClimbSharpness { get { return _ladderClimbSharpness; } }
        public float LadderClimbTowardAngleMax { get { return _ladderClimbTowardAngleMax; } }

        public Vector3 Gravity { get { return _gravity; } }
    }
}
