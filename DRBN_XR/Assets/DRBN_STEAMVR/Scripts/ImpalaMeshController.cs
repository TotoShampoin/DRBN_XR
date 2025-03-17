using UnityEngine;
using UnityEngine.InputSystem;

public class ImpalaMeshController : MonoBehaviour
{
    [SerializeField] MeshGenerator meshGenerator;
    [SerializeField] SphereColliderPopulateV2 sphereColliderPopulateV2;

    [SerializeField] Generator generator;

    [SerializeField] Transform paintAnchor;

    [SerializeField] InputActionReference activateBrush;
    [SerializeField] InputActionReference activateEraser;

    bool _isUpdated = false;
    bool _isPainting = false;
    bool _isErasing = false;

    void Start()
    {
        meshGenerator.Recreate(generator.Generate());
        MakeSpheres();

        activateBrush.action.performed += _ => _isPainting = true;
        activateBrush.action.canceled += _ => _isPainting = false;
        activateEraser.action.performed += _ => _isErasing = true;
        activateEraser.action.canceled += _ => _isErasing = false;
    }

    void FixedUpdate()
    {
        if (_isPainting)
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
}
