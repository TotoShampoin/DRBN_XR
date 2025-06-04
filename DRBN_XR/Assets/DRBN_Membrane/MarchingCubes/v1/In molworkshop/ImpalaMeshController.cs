using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MarchingCubeSystem.V1;

public class ImpalaMeshController : MonoBehaviour
{
    [SerializeField] MeshGenerator meshGenerator;
    [SerializeField] SphereColliderPopulateV2 sphereColliderPopulateV2;

    [SerializeField] List<Generator> generators;

    [SerializeField] Transform paintAnchor;

    [SerializeField] InputActionReference activateBrush;
    [SerializeField] InputActionReference activateEraser;

    bool _isUpdated = false;
    bool _isPainting = false;
    bool _isErasing = false;
    int _currentGeneratorIndex = 0;

    void Start()
    {
        Regenerate();

        activateBrush.action.performed += _ => _isPainting = true;
        activateBrush.action.canceled += _ => _isPainting = false;
        activateEraser.action.performed += _ => _isErasing = true;
        activateEraser.action.canceled += _ => _isErasing = false;
    }

    bool IsInMeshBounds(Vector3 position, float margin = 0.0f)
    {
        Vector3 localPosition = meshGenerator.transform.InverseTransformPoint(position);
        Vector3 marginInBounds = new(
            margin / (meshGenerator.UpperBound.x - meshGenerator.LowerBound.x),
            margin / (meshGenerator.UpperBound.y - meshGenerator.LowerBound.y),
            margin / (meshGenerator.UpperBound.z - meshGenerator.LowerBound.z)
        );
        return localPosition.x >= meshGenerator.LowerBound.x - marginInBounds.x &&
               localPosition.x <= meshGenerator.UpperBound.x + marginInBounds.x &&
               localPosition.y >= meshGenerator.LowerBound.y - marginInBounds.y &&
               localPosition.y <= meshGenerator.UpperBound.y + marginInBounds.y &&
               localPosition.z >= meshGenerator.LowerBound.z - marginInBounds.z &&
               localPosition.z <= meshGenerator.UpperBound.z + marginInBounds.z;
    }

    void FixedUpdate()
    {
        if (_isPainting && IsInMeshBounds(paintAnchor.position, 2.0f))
        {
            meshGenerator.EditWeights(
                paintAnchor.position,
                2.0f, 1.0f,
                !_isErasing);
            _isUpdated = true;
        }

        if (_isUpdated)
        {
            MakeSpheres();
            _isUpdated = false;
        }
    }

    void MakeSpheres()
    {
        sphereColliderPopulateV2.ExtractAndPopulate(
            meshGenerator.GetComponent<MeshFilter>(),
            meshGenerator.transform);
    }

    public void Regenerate()
    {
        meshGenerator.Recreate(generators[_currentGeneratorIndex].Generate());
        MakeSpheres();
    }

    public void SelectGenerator(int index)
    {
        if (index < 0 || index >= generators.Count) return;
        _currentGeneratorIndex = index;
        Regenerate();
    }
}
