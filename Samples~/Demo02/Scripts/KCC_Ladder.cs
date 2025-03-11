using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;
using Vertx.Debugging;

namespace LFramework.KCC.Demo02
{
    [SelectionBase]
    public class KCC_Ladder : MonoBase
    {
        [Title("Config")]
        [SerializeField] private float _height;

        public float Height { get { return _height; } }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            D.raw(new Shape.Line(transform.position, transform.TransformPoint(Vector3.up * _height)), Color.white);
        }

        private void OnValidate()
        {
            if (Application.isPlaying || UnityEditor.Selection.objects == null || !UnityEditor.Selection.objects.Contains(gameObject))
                return;

            KCC_LadderRenderer renderer = GetComponent<KCC_LadderRenderer>();

            if (renderer != null)
                renderer.UpdateRenderer();
        }

#endif
    }
}
