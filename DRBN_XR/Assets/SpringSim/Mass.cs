using System;
using UnityEditor;
using UnityEngine;

namespace Assets.SpringSim
{

    public class Mass : MonoBehaviour
    {
        [NonSerialized] public Vector3 massPosition;
        [NonSerialized] public float size;

        bool isSelected;
        public bool IsSelected => isSelected;

        void Update()
        {
            if (IsSelected)
                massPosition = transform.localPosition;
            else
                transform.localPosition = massPosition;

            transform.localScale = size * Vector3.one;
            isSelected = Selection.activeGameObject == gameObject;
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
        }
    }
}
