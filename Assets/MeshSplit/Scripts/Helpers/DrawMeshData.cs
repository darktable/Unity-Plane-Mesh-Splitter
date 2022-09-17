using System;
using UnityEngine;

namespace MeshSplit.Scripts.Helpers
{
    public class DrawMeshData : MonoBehaviour
    {
        private MeshRenderer _meshRenderer;

        private void Reset()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }
        
        private void OnDrawGizmos()
        {
            if (!_meshRenderer)
                _meshRenderer = GetComponent<MeshRenderer>();
            
            Gizmos.DrawWireCube(_meshRenderer.bounds.center, _meshRenderer.bounds.size);
        }
    }
}