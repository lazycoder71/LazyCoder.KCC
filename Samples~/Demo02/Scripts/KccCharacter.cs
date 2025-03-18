using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using Vertx.Debugging;
using System;
using LFramework;
using LFramework.Kcc;

namespace Game
{
    public class KccCharacter : MonoBase, IKccMotor
    {
        public enum State
        {
            Ground,
            Air,
            ClimbLadder,
        }

        [Title("Reference")]
        [SerializeField] private KccMotor _motor;

        [Title("Config")]
        [SerializeField] private Collider[] _ignoredColliders;
        [SerializeField] private KccCharacterConfig _config;

        // Current input value
        private Vector3 _inputMove;
        private Vector3 _inputRotation;

        // Ground
        private float _groundStableTime = 0f;

        // Jump
        private float _jumpTimeSinceLast = 0f;
        private float _jumpTimeSinceRequest = Mathf.Infinity;
        private float _jumpSpeedMultiple = 1f;
        private int _jumpCount = 0;

        private Vector3 _additiveVelocity = Vector3.zero;

        // Velocity that only affected by control
        private Vector3 _velocityBase;

        // Current ladder
        private KccLadder _ladder;
        private Vector3 _ladderClimbDirection;

        private RaycastHit _raycastHit;

        private StateMachine<State> _stateMachine;

        public StateMachine<State> StateMachine { get { return _stateMachine; } }

        public KccMotor Motor { get { return _motor; } }

        #region MonoBehaviour

        private void Awake()
        {
            // Init state machine
            InitStateMachine();

            // Assign the characterController to the motor
            _motor.CharacterController = this;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _stateMachine == null || _motor == null)
                return;

            D.raw(new Shape.Text(TransformCached.position + _motor.Capsule.center, _stateMachine.CurrentState));
        }

        private void OnCollisionEnter(Collision collision)
        {
            IKccCharacterCollidable collidable = collision.gameObject.GetComponent<IKccCharacterCollidable>();

            if (collidable != null)
                collidable.OnCollisionEnter(this);
        }

        private void OnCollisionExit(Collision collision)
        {
            IKccCharacterCollidable collidable = collision.gameObject.GetComponent<IKccCharacterCollidable>();

            if (collidable != null)
                collidable.OnCollisionExit(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            IKccCharacterCollidable collidable = other.GetComponent<IKccCharacterCollidable>();

            if (collidable != null)
                collidable.OnTriggerEnter(this);
        }

        private void OnTriggerExit(Collider other)
        {
            IKccCharacterCollidable collidable = other.GetComponent<IKccCharacterCollidable>();

            if (collidable != null)
                collidable.OnTriggerExit(this);
        }

        #endregion

        #region Function -> Private

        private void InitStateMachine()
        {
            _stateMachine = new StateMachine<State>();

            _stateMachine.AddState(State.Ground);
            _stateMachine.AddState(State.Air);
            _stateMachine.AddState(State.ClimbLadder);

            // Set default state
            _stateMachine.CurrentState = State.Air;
        }

        private bool CanClimbLadder()
        {
            // Cast bottom
            DrawPhysics.Raycast(TransformCached.position, TransformCached.forward, out _raycastHit, _motor.Capsule.radius + _config.LadderClimbDetectDistance, _config.LadderClimbDetectLayerMask);

            // If nothing detect when cast bottom then cast top
            if (_raycastHit.collider == null)
                DrawPhysics.Raycast(TransformCached.TransformPoint(Vector3.up * _motor.Capsule.height * _config.LadderClimbDetectHeight), TransformCached.forward, out _raycastHit, _motor.Capsule.radius + _config.LadderClimbDetectDistance, _config.LadderClimbDetectLayerMask);

            if (_raycastHit.collider == null)
                return false;

            _ladder = _raycastHit.collider.GetComponent<KccLadder>();

            if (_ladder == null)
                return false;

            _ladderClimbDirection = -_raycastHit.normal;
            _ladderClimbDirection.y = 0f;

            // If input is heading toward ladder, then change to climb state
            if (Vector3.Angle(_inputMove, _ladderClimbDirection) < _config.LadderClimbTowardAngleMax)
                return true;

            return false;
        }

        private bool IsLeavingLadder()
        {
            if (TransformCached.position.y > _ladder.TopPosition.y)
                return true;

            if (TransformCached.position.y + _motor.Capsule.height * _config.LadderClimbDetectHeight < _ladder.BottomPosition.y)
                return true;

            return false;
        }

        private bool CanJump()
        {
            // Number of jump exceed limit
            if (_jumpCount >= _config.JumpMultipleCount)
                return false;

            switch (_stateMachine.CurrentState)
            {
                case State.Air:
                    // Check jump coyote time when character haven't jumped 
                    if (_jumpCount <= 0 && _groundStableTime > _config.JumpCoyoteTime)
                        return false;
                    break;
            }

            // Check jump multiple delay
            if (_jumpCount >= 1 && _jumpTimeSinceLast < _config.JumpMultipleDelayBetween)
                return false;

            // Check jump request time is outside buffer time
            if (_jumpTimeSinceRequest > _config.JumpRequestBufferTime)
                return false;

            return true;
        }

        private void DoJump(ref Vector3 currentVelocity)
        {
            // Calculate jump direction before ungrounding
            Vector3 jumpDirection = _motor.CharacterUp;

            if (_motor.GroundingStatus.FoundAnyGround && !_motor.GroundingStatus.IsStableOnGround)
                jumpDirection = _motor.GroundingStatus.GroundNormal;

            // Makes the character skip ground probing/snapping on its next update. 
            // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
            _motor.ForceUnground();

            // Add to the return velocity and reset jump state
            currentVelocity += (jumpDirection * _config.JumpSpeed * _jumpSpeedMultiple) - Vector3.Project(currentVelocity, _motor.CharacterUp);

            // Reset jump time request to infinity, must request again for another jump
            _jumpTimeSinceRequest = Mathf.Infinity;
            // Set jump time since last
            _jumpTimeSinceLast = 0f;
            // Increase jump count
            _jumpCount++;
        }

        private void DoJumpOffLadder(ref Vector3 currentVelocity)
        {
            // Add to the return velocity and reset jump state
            currentVelocity += -_ladderClimbDirection * _config.JumpOffLadderSpeed;

            // Reset jump time request to infinity, must request again for another jump
            _jumpTimeSinceRequest = Mathf.Infinity;
            // Set jump time since last
            _jumpTimeSinceLast = 0f;
            // Increase jump count
            _jumpCount++;
        }

        private void HandleAdditiveVelocity(ref Vector3 currentVelocity)
        {
            if (_additiveVelocity.sqrMagnitude <= 0f)
                return;

            currentVelocity += _additiveVelocity;
            _additiveVelocity = Vector3.zero;
        }

        #endregion

        #region Function -> Public

        /// <summary>
        /// This is called every frame by Player in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref KccInputPlayer input)
        {
            _inputMove = input.MoveVector;
            _inputRotation = input.LookVector;

            // Jumping input
            if (input.Jump)
                _jumpTimeSinceRequest = 0f;
        }

        /// <summary>
        /// This is called every frame by the AI script in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref KccInputAI input)
        {
            _inputMove = input.MoveVector;
            _inputRotation = input.LookVector;

            if (input.Jump)
                _jumpTimeSinceRequest = 0f;
        }

        public void AddVelocity(Vector3 velocity, bool isAirForce = false)
        {
            switch (_stateMachine.CurrentState)
            {
                case State.Ground:
                    if (isAirForce)
                        _motor.ForceUnground();

                    _additiveVelocity += velocity;
                    break;

                case State.Air:
                    if (isAirForce)
                        _additiveVelocity += velocity;
                    break;
            }
        }

        public Vector3 GetVelocityBaseNormalized()
        {
            switch (_stateMachine.CurrentState)
            {
                case State.Ground:
                    return _velocityBase / _config.GroundMoveSpeedMax;

                case State.Air:
                    return _velocityBase / _config.AirMoveSpeedMax;

                default: return _velocityBase / _config.GroundMoveSpeedMax;
            }
        }

        public float GetClimbLadderPositionY()
        {
            if (_ladder == null)
                return 0f;

            return _ladder.TransformCached.TransformPoint(TransformCached.position).y;
        }

        #endregion

        #region IKccMotor

        void IKccMotor.BeforeCharacterUpdate(float deltaTime)
        {
            switch (_stateMachine.CurrentState)
            {
                case State.Air:
                case State.Ground:
                    if (_inputMove.sqrMagnitude > Mathf.Epsilon && CanClimbLadder())
                        _stateMachine.CurrentState = State.ClimbLadder;
                    break;
            }
        }

        void IKccMotor.UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            Vector3 lookVector = Vector3.zero;

            switch (_stateMachine.CurrentState)
            {
                case State.Air:
                case State.Ground:
                    lookVector = _inputRotation;
                    break;

                case State.ClimbLadder:
                    lookVector = _ladderClimbDirection;
                    break;
            }

            if (lookVector.sqrMagnitude > 0f && _config.OrientationBonusSharpness > 0f)
            {
                // Smoothly interpolate from current to target look direction
                Vector3 smoothedLookInputDirection = Vector3.Slerp(_motor.CharacterForward, lookVector, 1 - Mathf.Exp(-_config.OrientationBonusSharpness * deltaTime)).normalized;

                // Set the current rotation (which will be used by the KinematicCharacterMotor)
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, _motor.CharacterUp);
            }

            Vector3 currentUp = (currentRotation * Vector3.up);
            Vector3 smoothedGravityDir;

            switch (_config.OrientationBonus)
            {
                case KccCharacterConfig.OrientationBonusMethod.TowardsGravity:
                    // Rotate from current up to invert gravity
                    smoothedGravityDir = Vector3.Slerp(currentUp, -_config.Gravity.normalized, 1 - Mathf.Exp(-_config.OrientationBonusSharpness * deltaTime));
                    currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                    break;

                case KccCharacterConfig.OrientationBonusMethod.TowardsGroundSlopeAndGravity:
                    if (_motor.GroundingStatus.IsStableOnGround)
                    {
                        Vector3 initialCharacterBottomHemiCenter = _motor.TransientPosition + (currentUp * _motor.Capsule.radius);

                        Vector3 smoothedGroundNormal = Vector3.Slerp(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-_config.OrientationBonusSharpness * deltaTime));
                        currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                        // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                        _motor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * _motor.Capsule.radius));
                    }
                    else
                    {
                        smoothedGravityDir = Vector3.Slerp(currentUp, -_config.Gravity.normalized, 1 - Mathf.Exp(-_config.OrientationBonusSharpness * deltaTime));
                        currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                    }
                    break;

                default:
                    smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-_config.OrientationSharpness * deltaTime));
                    currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                    break;
            }
        }

        void IKccMotor.UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (_stateMachine.CurrentState)
            {
                case State.Air:
                    // Add move input
                    if (_inputMove.sqrMagnitude > 0f)
                    {
                        Vector3 addedVelocity = _inputMove * _config.AirAccelerationSpeed * deltaTime;

                        Vector3 currentVelocityOnPlane = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);

                        // Clamp addedVel to make total vel not exceed max vel on inputs plane
                        Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnPlane + addedVelocity, _config.AirMoveSpeedMax);
                        addedVelocity = newTotal - currentVelocityOnPlane;

                        // Prevent air-climbing sloped walls
                        if (_motor.GroundingStatus.FoundAnyGround)
                        {
                            if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                            {
                                Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal), _motor.CharacterUp).normalized;
                                addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                            }
                        }

                        // Apply added velocity
                        currentVelocity += addedVelocity;
                    }

                    // Gravity
                    currentVelocity += _config.Gravity * deltaTime;

                    // Drag
                    currentVelocity *= (1f / (1f + (_config.AirDrag * deltaTime)));

                    // Check if jump
                    if (CanJump())
                        DoJump(ref currentVelocity);

                    // Handle additive velocity
                    HandleAdditiveVelocity(ref currentVelocity);

                    break;

                case State.Ground:
                    if (_motor.GroundingStatus.IsStableOnGround)
                    {
                        float currentVelocityMagnitude = currentVelocity.magnitude;

                        Vector3 effectiveGroundNormal = _motor.GroundingStatus.GroundNormal;

                        // Reorient velocity on slope
                        currentVelocity = _motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                        // Calculate target velocity
                        Vector3 inputRight = Vector3.Cross(_inputMove, _motor.CharacterUp);
                        Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _inputMove.magnitude;
                        Vector3 targetMovementVelocity = reorientedInput * _config.GroundMoveSpeedMax;

                        // Smooth movement velocity
                        currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-_config.GroundMoveSharpness * deltaTime));
                    }

                    // Check if jump
                    if (CanJump())
                        DoJump(ref currentVelocity);

                    _velocityBase = currentVelocity;

                    // Handle additive velocity
                    HandleAdditiveVelocity(ref currentVelocity);

                    break;

                case State.ClimbLadder:
                    float desireVelocityY = 0f;
                    // Move foward climb direction equal climb up and vice versa
                    desireVelocityY = TransformCached.InverseTransformPoint(TransformCached.position + _inputMove).z * _config.LadderClimbSpeedMax;

                    // Smooth movement velocity
                    currentVelocity.y = Mathf.Lerp(currentVelocity.y, desireVelocityY, 1f - Mathf.Exp(-_config.LadderClimbSharpness * deltaTime));
                    currentVelocity.x = 0f;
                    currentVelocity.z = 0f;

                    // Force unground if moving up ladder
                    if (desireVelocityY > 0f)
                        _motor.ForceUnground();

                    // Check if jump
                    if (CanJump())
                    {
                        // Force change to air state
                        _stateMachine.CurrentState = State.Air;

                        DoJumpOffLadder(ref currentVelocity);
                    }

                    break;
            }
        }

        void IKccMotor.AfterCharacterUpdate(float deltaTime)
        {
            switch (_stateMachine.CurrentState)
            {
                case State.Ground:
                    if (!_motor.GroundingStatus.IsStableOnGround)
                        _stateMachine.CurrentState = State.Air;
                    break;

                case State.Air:
                    if (_motor.GroundingStatus.IsStableOnGround)
                        _stateMachine.CurrentState = State.Ground;
                    break;

                case State.ClimbLadder:
                    if (_motor.GroundingStatus.IsStableOnGround)
                        _stateMachine.CurrentState = State.Ground;

                    if (IsLeavingLadder())
                        _stateMachine.CurrentState = State.Air;

                    break;
            }

            // Update jump count
            if (_motor.GroundingStatus.IsStableOnGround || _stateMachine.CurrentState == State.ClimbLadder)
            {
                if (_jumpTimeSinceLast > 0f)
                    _jumpCount = 0;
            }

            // Update jump time since requested
            _jumpTimeSinceRequest += deltaTime;

            // Update jump time since last
            _jumpTimeSinceLast += deltaTime;

            // Update time since last stable on ground
            if (_motor.GroundingStatus.IsStableOnGround)
                _groundStableTime = 0f;
            else
                _groundStableTime += deltaTime;
        }

        void IKccMotor.PostGroundingUpdate(float deltaTime)
        {
        }

        bool IKccMotor.IsColliderValidForCollisions(Collider collider)
        {
            if (_ignoredColliders.Length == 0)
                return true;

            if (_ignoredColliders.Contains(collider))
                return false;

            return true;
        }

        void IKccMotor.OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        void IKccMotor.OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        void IKccMotor.ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        void IKccMotor.OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        #endregion
    }
}