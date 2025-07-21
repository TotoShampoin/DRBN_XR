using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TestSHM : MonoBehaviour
{
    public float cellSize = 0.1f;
    public int amount = 64;

    public TestParticle prefab;
    public Transform cursor;
    public float radius;
    public float boxSize;

    SpatialHash<TestParticle> map;
    List<TestParticle> surrounding;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        map = new(cellSize);
        for (int i = 0; i < amount; i++)
        {
            var part = Instantiate(prefab, transform);
            part.transform.position = new Vector3(
                Random.Range(-boxSize, boxSize),
                Random.Range(-boxSize, boxSize),
                Random.Range(-boxSize, boxSize)
            );
            part.gameObject.SetActive(true);
            map.AddAt(part.transform.position, part);
        }
    }

    // Update is called once per frame
    void Update()
    {
        surrounding?.ForEach(obj => obj.Color = Color.white);
        surrounding = map.GetSurrounding(cursor.position, radius);
        surrounding.ForEach(obj => obj.Color = Color.red);
    }

    public void TriggerMove(Vector3 from, Vector3 to, TestParticle item)
    {
        map.Move(from, to, item);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || map == null || cursor == null)
            return;
        var cell = map.GetCell(cursor.position);
        Gizmos.DrawWireCube((Vector3)cell * cellSize, Vector3.one * cellSize);
        Gizmos.DrawWireSphere(cursor.position, radius);
    }
}
