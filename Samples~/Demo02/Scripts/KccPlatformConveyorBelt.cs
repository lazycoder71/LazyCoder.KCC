using LFramework;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using Vertx.Debugging;

namespace Game
{
    public class KccPlatformConveyorBelt : MonoBase, IKccCharacterCollidable
    {
        [Title("Config")]
        [SerializeField] private float _velocity = 2f;
        [SerializeField] private Vector3 _rotationOffset;

        private Collider _collider;

        private List<KccCharacter> _characters = new List<KccCharacter>();

        #region MonoBehaviour

        protected override void Start()
        {
            base.Start();

            _collider = GetComponent<Collider>();
        }

        protected override void Tick()
        {
            base.Tick();

            for (int i = 0; i < _characters.Count; i++)
            {
                if (!_characters[i].Motor.GroundingStatus.IsStableOnGround)
                    continue;

                if (_characters[i].Motor.GroundingStatus.GroundCollider != _collider)
                    continue;

                _characters[i].AddVelocity(Quaternion.Euler(_rotationOffset) * TransformCached.forward * _velocity * Time.deltaTime);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Collider collider = GetComponent<Collider>();

            Vector3 dir = Quaternion.Euler(_rotationOffset) * TransformCached.forward;

            float extends = Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y, collider.bounds.extents.z);

            D.raw(new Shape.Arrow(collider.bounds.center - dir * extends, Quaternion.LookRotation(dir), extends * 2f), Color.cyan);
        }

        #endregion

        #region IKccCharacterCollidable

        void IKccCharacterCollidable.OnCollisionEnter(KccCharacter character)
        {
            _characters.Add(character);
        }

        void IKccCharacterCollidable.OnCollisionExit(KccCharacter character)
        {
            _characters.Remove(character);
        }

        void IKccCharacterCollidable.OnTriggerEnter(KccCharacter character)
        {
        }

        void IKccCharacterCollidable.OnTriggerExit(KccCharacter character)
        {
        }

        #endregion
    }
}
