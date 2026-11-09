using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GrassField : MonoBehaviour
{
    public Material grassMaterial;// uses the GrassBillboard shader
    public Vector2 areaSize = new Vector2(20f, 20f); //Size of grass field
    public int count = 5000;
    public Vector2 scaleRange = new Vector2(0.8f, 1.3f);
    public LayerMask groundMask;
    public Color tintA = new Color(0.55f, 0.8f, 0.3f);
    public Color tintB = new Color(0.3f, 0.6f, 0.2f);

    private const int BatchSize = 1023;// max instances per draw call
    private Mesh quad;
    private readonly List<Matrix4x4[]> matrices = new List<Matrix4x4[]>();
    private readonly List<Vector4[]> colors = new List<Vector4[]>();
    private readonly List<int> batchCounts = new List<int>();
    private readonly List<MaterialPropertyBlock> blocks = new List<MaterialPropertyBlock>();

    void Start()
    {
        grassMaterial.enableInstancing = true;
        //Match the quad's shape to the texture so the grass isn't stretched
        float aspect = 1f;
        Texture tex = grassMaterial.mainTexture;
        if (tex != null) aspect = (float)tex.width / tex.height;
        quad = BuildQuad();
        Matrix4x4[] matrix = null; Vector4[] countN = null; int number = 0;
        for (int i = 0; i < count; i++)
        {
            if (number == 0) { matrix = new Matrix4x4[BatchSize]; countN = new Vector4[BatchSize]; }

            Vector3 pos = transform.position + new Vector3(
                Random.Range(-areaSize.x / 2f, areaSize.x / 2f), 0f,
                Random.Range(-areaSize.y / 2f, areaSize.y / 2f));

            if (groundMask.value != 0)
            {
                RaycastHit hit;
                if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out hit, 100f, groundMask))
                    pos = hit.point;
            }

            float scale = Random.Range(scaleRange.x, scaleRange.y);
            matrix[number] = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(scale * aspect, scale, 1f));
            countN[number] = Color.Lerp(tintA, tintB, Random.value);
            number++;
            if (number == BatchSize || i == count - 1)
            {
                matrices.Add(matrix); colors.Add(countN); batchCounts.Add(number);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetVectorArray("_Color", countN);
                blocks.Add(block);
                number = 0;
            }
        }
    }

    void Update()
    {
        // One draw call per 1023 blades, no shadows
        for (int i = 0; i < matrices.Count; i++)
        {
            Graphics.DrawMeshInstanced(quad, 0, grassMaterial, matrices[i], batchCounts[i],
                blocks[i], ShadowCastingMode.Off, false);
        }
    }

    //1x1 quad with grass sitting on the ground
    Mesh BuildQuad()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(-0.5f, 1, 0), new Vector3(0.5f, 1, 0) };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, 0.1f, areaSize.y));
    }
}