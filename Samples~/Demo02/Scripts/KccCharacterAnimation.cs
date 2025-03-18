using LFramework;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    public class KccCharacterAnimation : MonoBase
    {
        private static readonly int s_animIdMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int s_animIdAir = Animator.StringToHash("Air");
        private static readonly int s_animIdGround = Animator.StringToHash("Ground");
        private static readonly int s_animIdClimbLadder = Animator.StringToHash("ClimbLadder");
        private static readonly int s_animIdClimbLadderY = Animator.StringToHash("ClimbLadderY");

        [Title("Config")]
        [SerializeField] private float _climbLadderMultiple = 0.5f;

        private Animator _animator;

        private int _previousTrigger = 0;

        private KccCharacter _character;

        protected override void Start()
        {
            base.Start();

            _character = GetComponent<KccCharacter>();
            _character.StateMachine.EventStateChanged += StateMachine_EventStateChanged;

            _animator = GetComponentInChildren<Animator>();
        }

        protected override void Tick()
        {
            base.Tick();

            switch (_character.StateMachine.CurrentState)
            {
                case KccCharacter.State.Ground:
                    _animator.SetFloat(s_animIdMoveSpeed, _character.GetVelocityBaseNormalized().sqrMagnitude);
                    break;

                case KccCharacter.State.ClimbLadder:
                    _animator.SetFloat(s_animIdClimbLadderY, _character.GetClimbLadderPositionY() * _climbLadderMultiple);
                    break;
            }
        }

        private void StateMachine_EventStateChanged()
        {
            switch (_character.StateMachine.PreviousState)
            {
                case KccCharacter.State.Ground:
                    _animator.SetBool(s_animIdGround, false);
                    break;

                case KccCharacter.State.Air:
                    _animator.SetBool(s_animIdAir, false);
                    break;

                case KccCharacter.State.ClimbLadder:
                    _animator.SetBool(s_animIdClimbLadder, false);
                    break;
            }

            switch (_character.StateMachine.CurrentState)
            {
                case KccCharacter.State.Ground:
                    _animator.SetBool(s_animIdGround, true);
                    break;

                case KccCharacter.State.Air:
                    _animator.SetBool(s_animIdAir, true);
                    break;

                case KccCharacter.State.ClimbLadder:
                    _animator.SetBool(s_animIdClimbLadder, true);
                    break;
            }
        }

        private void SetTrigger(int triggerId)
        {
            if (_previousTrigger != 0)
                _animator.ResetTrigger(_previousTrigger);

            _animator.SetTrigger(triggerId);

            _previousTrigger = triggerId;
        }
    }
}
