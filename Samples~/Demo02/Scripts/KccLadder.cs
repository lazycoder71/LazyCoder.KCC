using LFramework;
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;
using Vertx.Debugging;

namespace Game
{
    [SelectionBase]
    public class KccLadder : MonoBase
    {
        [Title("Config")]
        [SerializeField] private float _height;

        private BoxCollider _boxCollider;

        public BoxCollider BoxCollider { get { if (_boxCollider == null) _boxCollider = GetComponent<BoxCollider>(); return _boxCollider; } }

        public float Height { get { return _height; } }

        public Vector3 TopPosition { get { return TransformCached.TransformPoint(Vector3.up * _height); } }
        public Vector3 BottomPosition { get { return TransformCached.position; } }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            D.raw(new Shape.Line(transform.position, transform.TransformPoint(Vector3.up * _height)), Color.white);
        }

        private void OnValidate()
        {
            if (Application.isPlaying || UnityEditor.Selection.objects == null || !UnityEditor.Selection.objects.Contains(gameObject))
                return;

            KccLadderRenderer renderer = GetComponent<KccLadderRenderer>();

            if (renderer != null)
                renderer.UpdateRenderer();
        }

#endif
    }
}
