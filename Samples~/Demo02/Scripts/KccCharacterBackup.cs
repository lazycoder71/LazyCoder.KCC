using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using Vertx.Debugging;
using System;
using UnityEngine.SocialPlatforms;
using LFramework;
using LFramework.Kcc;

namespace Game
{
    public class KccCharacterBackup : MonoBase, IKccMotor
    {
        [System.Serializable]
        public enum OrientationBonusMethod
        {
            None,
            TowardsGravity,
            TowardsGroundSlopeAndGravity,
        }

        public enum State
        {
            Ground,
            Air,
            LadderClimb,
        }

        [Title("Reference")]
        [SerializeField] private KccMotor _motor;

        [Title("Ground Movement")]
        [SerializeField] private float _groundMoveSpeedMax = 4.5f;
        [SerializeField] private float _groundMoveSharpness = 1000f;

        [Title("Orientation")]
        [SerializeField] private OrientationBonusMethod _orientationBonusMethod = OrientationBonusMethod.TowardsGravity;
        [SerializeField] private float _orientationSharpness = 10f;
        [SerializeField] private float _orientationBonusSharpness = 10f;

        [Title("Air Movement")]
        [SerializeField] private float _airMoveSpeedMax = 4.5f;
        [SerializeField] private float _airAccelerationSpeed = 1000f;
        [SerializeField] private float _airDrag = 0.1f;

        [Title("Jumping")]
        [SerializeField] private float _jumpSpeed = 11f;
        [SerializeField] private float _jumpRequestBufferTime = 0.5f;
        [SerializeField] private float _jumpCoyoteTime = 0.5f;

        [Title("Ladder Climb")]
        [SerializeField] private float _ladderClimbDetectDistance = 0.1f;
        [SerializeField] private LayerMask _ladderClimbDetectLayerMask;
        [Range(0f, 1f)]
        [SerializeField] private float _ladderClimbDetectHeight = 0.8f;
        [SerializeField] private float _ladderClimbSpeedMax = 4.5f;
        [SerializeField] private float _ladderClimbSharpness = 1000f;

        [Title("Misc")]
        [SerializeField] private Collider[] _ignoredColliders;
        [SerializeField] private Vector3 _gravity = new Vector3(0, -30f, 0);
        [SerializeField] private Transform _meshRoot;

        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;

        private bool _jumpRequested = false;
        private bool _jumpable = false;
        private bool _jumpedThisFrame = false;
        private float _jumpTimeSinceRequested = Mathf.Infinity;
        private float _jumpSpeedMultiple = 1f;

        private float _timeSinceLastStableOnGround = 0f;
        private Vector3 _internalVelocityAdd = Vector3.zero;

        // Movement velocity affected by control
        private Vector3 _moveControlVelocity;

        // Current ladder
        private KccLadder _ladder;
        private Vector3 _ladderClimbDirection;
        private float _ladderClimbIgnoreTime;

        private StateMachine<State> _stateMachine;

        private void Awake()
        {
            // Init state machine
            InitStateMachine();

            // Assign the characterController to the motor
            _motor.CharacterController = this;
        }

        private void InitStateMachine()
        {
            _stateMachine = new StateMachine<State>();

            _stateMachine.AddState(State.Ground);
            _stateMachine.AddState(State.Air);
            _stateMachine.AddState(State.LadderClimb);

            // Set default state
            _stateMachine.CurrentState = State.Air;
        }

        private void CheckLadderAhead()
        {
            if (_ladderClimbIgnoreTime > 0f)
                return;

            RaycastHit hitInfo;

            // Cast bottom
            DrawPhysics.Raycast(TransformCached.position, TransformCached.forward, out hitInfo, _motor.Capsule.radius + _ladderClimbDetectDistance, _ladderClimbDetectLayerMask);

            // If nothing detect when cast bottom then cast top
            if (hitInfo.collider == null)
                DrawPhysics.Raycast(TransformCached.TransformPoint(Vector3.up * _motor.Capsule.height * _ladderClimbDetectHeight), TransformCached.forward, out hitInfo, _motor.Capsule.radius + _ladderClimbDetectDistance, _ladderClimbDetectLayerMask);

            // There is no valid collider ahead
            if (hitInfo.collider == null)
                return;

            _ladder = hitInfo.collider.GetComponent<KccLadder>();

            // There is no ladder component
            if (_ladder == null)
                return;

            _ladderClimbDirection = -hitInfo.normal;
            _ladderClimbDirection.y = 0f;

            // If input is heading toward ladder, then change to climb state
            if (_moveInputVector.magnitude > Mathf.Epsilon && Vector3.Angle(_moveInputVector, _ladderClimbDirection) < 90.0f)
                _stateMachine.CurrentState = State.LadderClimb;
        }

        private bool CanJump()
        {
            // Can't jump flag
            if (!_jumpable)
                return false;

            // Jump is not requested
            if (!_jumpRequested)
                return false;

            // Check jump coyote time (No affect climb state)
            if (_timeSinceLastStableOnGround > _jumpCoyoteTime && _stateMachine.CurrentState != State.LadderClimb)
                return false;

            // Check jump buffer time
            if (_jumpTimeSinceRequested > _jumpRequestBufferTime)
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
            currentVelocity += (jumpDirection * _jumpSpeed * _jumpSpeedMultiple) - Vector3.Project(currentVelocity, _motor.CharacterUp);

            _jumpRequested = false;
            _jumpTimeSinceRequested = Mathf.Infinity;
            _jumpable = false;
            _jumpedThisFrame = true;
        }

        private void HandleAdditiveVelocity(ref Vector3 currentVelocity)
        {
            if (_internalVelocityAdd.sqrMagnitude <= 0f)
                return;

            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }

        /// <summary>
        /// This is called every frame by Player in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref KccInputPlayer input)
        {
            _moveInputVector = input.MoveVector;
            _lookInputVector = input.LookVector;

            // Jumping input
            if (input.Jump)
            {
                _jumpTimeSinceRequested = 0f;
                _jumpRequested = true;
            }
        }

        /// <summary>
        /// This is called every frame by the AI script in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref KccInputAI inputs)
        {
            _moveInputVector = inputs.MoveVector;
            _lookInputVector = inputs.LookVector;

            if (inputs.Jump)
            {
                _jumpTimeSinceRequested = 0f;
                _jumpRequested = true;
            }
            else
            {
                _jumpRequested = false;
            }
        }

        void IKccMotor.BeforeCharacterUpdate(float deltaTime)
        {
            switch (_stateMachine.CurrentState)
            {
                case State.Air:
                case State.Ground:
                    CheckLadderAhead();
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
                    lookVector = _lookInputVector;
                    break;

                case State.LadderClimb:
                    lookVector = _ladderClimbDirection;
                    break;
            }

            if (lookVector.sqrMagnitude > 0f && _orientationSharpness > 0f)
            {
                // Smoothly interpolate from current to target look direction
                Vector3 smoothedLookInputDirection = Vector3.Slerp(_motor.CharacterForward, lookVector, 1 - Mathf.Exp(-_orientationSharpness * deltaTime)).normalized;

                // Set the current rotation (which will be used by the KinematicCharacterMotor)
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, _motor.CharacterUp);
            }

            Vector3 currentUp = (currentRotation * Vector3.up);

            if (_orientationBonusMethod == OrientationBonusMethod.TowardsGravity)
            {
                // Rotate from current up to invert gravity
                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -_gravity.normalized, 1 - Mathf.Exp(-_orientationBonusSharpness * deltaTime));
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
            }
            else if (_orientationBonusMethod == OrientationBonusMethod.TowardsGroundSlopeAndGravity)
            {
                if (_motor.GroundingStatus.IsStableOnGround)
                {
                    Vector3 initialCharacterBottomHemiCenter = _motor.TransientPosition + (currentUp * _motor.Capsule.radius);

                    Vector3 smoothedGroundNormal = Vector3.Slerp(_motor.CharacterUp, _motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-_orientationBonusSharpness * deltaTime));
                    currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                    // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                    _motor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * _motor.Capsule.radius));
                }
                else
                {
                    Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -_gravity.normalized, 1 - Mathf.Exp(-_orientationBonusSharpness * deltaTime));
                    currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                }
            }
            else
            {
                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-_orientationBonusSharpness * deltaTime));
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
            }
        }

        void IKccMotor.UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (_stateMachine.CurrentState)
            {
                case State.Air:
                    // Add move input
                    if (_moveInputVector.sqrMagnitude > 0f && _ladderClimbIgnoreTime <= 0f)
                    {
                        Vector3 addedVelocity = _moveInputVector * _airAccelerationSpeed * deltaTime;

                        Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);

                        // clamp addedVel to make total vel not exceed max vel on inputs plane
                        Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, _airMoveSpeedMax);
                        addedVelocity = newTotal - currentVelocityOnInputsPlane;

                        /*
                        // Limit air velocity from inputs
                        if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
                        {
                            // clamp addedVel to make total vel not exceed max vel on inputs plane
                            Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
                            addedVelocity = newTotal - currentVelocityOnInputsPlane;
                        }
                        else
                        {
                            // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                            if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                            {
                                addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                            }
                        }
                        */

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
                    currentVelocity += _gravity * deltaTime;

                    // Drag
                    currentVelocity *= (1f / (1f + (_airDrag * deltaTime)));

                    if (CanJump())
                        DoJump(ref currentVelocity);

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
                        Vector3 inputRight = Vector3.Cross(_moveInputVector, _motor.CharacterUp);
                        Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                        Vector3 targetMovementVelocity = reorientedInput * _groundMoveSpeedMax;

                        // Smooth movement Velocity
                        currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-_groundMoveSharpness * deltaTime));
                    }


                    if (CanJump())
                        DoJump(ref currentVelocity);

                    _moveControlVelocity = currentVelocity;

                    HandleAdditiveVelocity(ref currentVelocity);

                    break;

                case State.LadderClimb:
                    // Update climb animation
                    //_animator.SetClimbY(TransformCached.position.y - _climbLadder.TransformCached.position.y);

                    Vector3 desireVelocity = Vector3.zero;
                    // Move foward climb direction equal climb up and vice versa
                    desireVelocity.y = TransformCached.InverseTransformPoint(TransformCached.position + _moveInputVector).z * _ladderClimbSpeedMax;
                    currentVelocity = Vector3.Lerp(currentVelocity, desireVelocity, 1f - Mathf.Exp(-_groundMoveSharpness * deltaTime));

                    if (desireVelocity.y > 0f)
                        _motor.ForceUnground();

                    // Check if leave ladder
                    if (TransformCached.position.y > _ladder.TransformCached.position.y + _ladder.Height || TransformCached.position.y + _motor.Capsule.height * _ladderClimbDetectHeight < _ladder.TransformCached.position.y)
                    {
                        _jumpRequested = false;
                        _jumpTimeSinceRequested = Mathf.Infinity;
                        _jumpable = false;
                        _jumpedThisFrame = true;

                        _stateMachine.CurrentState = State.Air;

                        currentVelocity = Vector3.zero;
                    }
                    else if (CanJump())
                    {
                        _jumpRequested = false;
                        _jumpTimeSinceRequested = Mathf.Infinity;
                        _jumpable = false;
                        _jumpedThisFrame = true;

                        _stateMachine.CurrentState = State.Air;

                        currentVelocity = -_ladderClimbDirection * _ladderClimbSpeedMax;

                        _ladderClimbIgnoreTime = 0.3f;
                    }

                    break;
            }
        }

        void IKccMotor.AfterCharacterUpdate(float deltaTime)
        {
            switch (_stateMachine.CurrentState)
            {
                case State.Ground:
                    //_animator.SetOnGround(true);

                    if (!_motor.GroundingStatus.IsStableOnGround)
                        _stateMachine.CurrentState = State.Air;

                    break;

                case State.Air:
                    //_animator.SetOnGround(false);

                    if (_motor.GroundingStatus.IsStableOnGround)
                        _stateMachine.CurrentState = State.Ground;
                    break;

                case State.LadderClimb:
                    if (_motor.GroundingStatus.IsStableOnGround)
                        _stateMachine.CurrentState = State.Ground;
                    break;
            }

            // Update jump time since requested
            _jumpTimeSinceRequested += deltaTime;

            // Update climb ignore time
            _ladderClimbIgnoreTime -= deltaTime;

            // Update time since last stable on ground
            if (_motor.GroundingStatus.IsStableOnGround)
                _timeSinceLastStableOnGround = 0f;
            else
                _timeSinceLastStableOnGround += deltaTime;

            // Update jumpable
            if (_motor.GroundingStatus.IsStableOnGround || _stateMachine.CurrentState == State.LadderClimb)
            {
                if (!_jumpedThisFrame)
                    _jumpable = true;
            }

            // Update jumped this frame flag
            if (_jumpedThisFrame)
                _jumpedThisFrame = false;

            // Update animator
            //if (_motor.GroundingStatus.IsStableOnGround)
            //    _animator.SetVelocityZ(Mathf.InverseLerp(0, _groundMoveSpeedMax, _moveControlVelocity.magnitude));
        }

        void IKccMotor.PostGroundingUpdate(float deltaTime)
        {
            /*
            // Handle landing and leaving ground
            if (_motor.GroundingStatus.IsStableOnGround && !_motor.LastGroundingStatus.IsStableOnGround)
            {
                _animator.SetJumping(false);
            }
            else if (!_motor.GroundingStatus.IsStableOnGround && _motor.LastGroundingStatus.IsStableOnGround)
            {
                _animator.SetJumping(true);
            }
            */
        }

        bool IKccMotor.IsColliderValidForCollisions(Collider coll)
        {
            if (_ignoredColliders.Length == 0)
                return true;

            if (_ignoredColliders.Contains(coll))
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

        public void AddVelocity(Vector3 velocity, bool applyOnAir, bool forceUnground)
        {
            switch (_stateMachine.CurrentState)
            {
                case State.Ground:
                    if (forceUnground)
                        _motor.ForceUnground();

                    _internalVelocityAdd += velocity;
                    break;

                case State.Air:
                    if (applyOnAir)
                        _internalVelocityAdd += velocity;
                    break;
            }
        }
    }
}