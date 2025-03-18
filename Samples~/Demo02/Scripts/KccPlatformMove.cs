using DG.Tweening;
using LFramework;
using LFramework.AnimationSequence;
using LFramework.Kcc;
using UnityEngine;

namespace Game
{
    [SelectionBase]
    [RequireComponent(typeof(KccMover), typeof(AnimationSequence))]
    public class KccPlatformMove : MonoBase, IKccMover
    {
        private AnimationSequence _animationSequence;

        private KccMover _mover;

        private Sequence _sequence;

        public AnimationSequence AnimationSequence { get { return _animationSequence; } }

        #region MonoBehaviour

        private void Awake()
        {
            _animationSequence = GetComponent<AnimationSequence>();

            _mover = GetComponent<KccMover>();
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }

        protected override void Start()
        {
            base.Start();

            _mover.MoverController = this;
        }

        #endregion

        public void SetEnabled(bool enabled)
        {
            _mover.enabled = enabled;
        }

        // This is called every FixedUpdate by our PhysicsMover in order to tell it what pose it should go to
        void IKccMover.UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
        {
            // Remember pose before animation
            Vector3 _positionBeforeAnim = TransformCached.position;
            Quaternion _rotationBeforeAnim = TransformCached.rotation;

            // Update animation
            _animationSequence.Sequence.ManualUpdate(deltaTime, 0f);

            // Set our platform's goal pose to the animation's
            goalPosition = TransformCached.position;
            goalRotation = TransformCached.rotation;

            // Reset the actual transform pose to where it was before evaluating. 
            // This is so that the real movement can be handled by the physics mover; not the animation
            TransformCached.position = _positionBeforeAnim;
            TransformCached.rotation = _rotationBeforeAnim;
        }

#if UNITY_EDITOR
        [Sirenix.OdinInspector.Button]
        private void UpdateAnimationSequenceSettings()
        {
            AnimationSequence animationSequence = GetComponent<AnimationSequence>();

            animationSequence.UpdateType = UpdateType.Manual;
            animationSequence.OnEnableAction = 0;
            animationSequence.OnDisableAction = 0;

            UnityEditor.EditorUtility.SetDirty(animationSequence);
        }
#endif
    }
}