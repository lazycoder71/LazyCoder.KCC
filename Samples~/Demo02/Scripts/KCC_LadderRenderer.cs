using Sirenix.OdinInspector;
using UnityEngine;

namespace LFramework.Kcc.Demo02
{
    [RequireComponent(typeof(KCC_Ladder))]
    public class KCC_LadderRenderer : MonoBehaviour
    {
        [Title("Reference")]
        [SerializeField] private Transform _modelRoot;

        [SerializeField] private Material _modelMaterial;
        [SerializeField] private float _modelHeight;
        [SerializeField] private GameObject _modelPrefab;

#if UNITY_EDITOR

        [Button]
        public void UpdateRenderer()
        {
            float height = GetComponent<KCC_Ladder>().Height;

            BoxCollider collider = GetComponent<BoxCollider>();

            var size = collider.size;
            var center = collider.center;

            size.y = height;
            center.y = height * 0.5f;

            collider.size = size;
            collider.center = center;

            if (_modelRoot == null || _modelPrefab == null)
                return;

            UpdateModel(height);

            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void UpdateModel(float height)
        {
            if (_modelHeight <= 0f)
                return;

            int modelCount = Mathf.CeilToInt(height / _modelHeight);

            for (int i = 0; i < Mathf.Max(modelCount, _modelRoot.childCount); i++)
            {
                GameObject model = null;

                if (i < modelCount)
                {
                    if (i >= _modelRoot.childCount)
                        model = UnityEditor.PrefabUtility.InstantiatePrefab(_modelPrefab, _modelRoot) as GameObject;
                    else
                        model = _modelRoot.GetChild(i).gameObject;
                }
                else
                {
                    Object.DestroyImmediate(_modelRoot.GetChild(i).gameObject);
                    i--;
                    continue;
                }

                model.transform.localPosition = Vector3.up * i * _modelHeight;

                if (i == modelCount - 1)
                    model.transform.localScale = new Vector3(1f, (height - _modelHeight * (modelCount - 1)) / _modelHeight, 1f);
                else
                    model.transform.localScale = Vector3.one;

                model.GetComponentInChildren<MeshRenderer>().material = _modelMaterial;
            }
        }

#endif
    }
}
