using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

public class Rebake : MonoBehaviour
{
    [SerializeField] private NavMeshSurface surface;
    [SerializeField] private bool rebuildAsync = true;
    //Incase we need to ignore objects for nav
    [SerializeField] private GameObject[] ignoredObjects;

    private AsyncOperation buildOperation;

    //On awake set ingored objects and find surface modifier
    private void Awake()
    {
        if (surface == null) surface = FindAnyObjectByType<NavMeshSurface>();

        foreach (GameObject go in ignoredObjects)
        {
            if (go == null) continue;

            NavMeshModifier mod = go.GetComponent<NavMeshModifier>();
            if (mod == null) mod = go.AddComponent<NavMeshModifier>();

            mod.ignoreFromBuild = true;
            mod.applyToChildren = true;
        }
    }

    public void RebakeMesh()
    {
        if (surface == null)
        {
            Debug.LogWarning("NavMeshRebaker: no NavMeshSurface assigned.", this);
            return;
        }

        // Wait one frame so newly enabled colliders/renderers are counted
        StartCoroutine(RebakeNextFrame());
    }

    //Rebake nav mesh
    private IEnumerator RebakeNextFrame()
    {
        yield return null;

        //Rebuild with the latest areas purchased
        while (buildOperation != null && !buildOperation.isDone) yield return null;

        //Rebuild nav mesh component
        if (!rebuildAsync || surface.navMeshData == null)
        {
            surface.BuildNavMesh();
            yield break;
        }

        buildOperation = surface.UpdateNavMesh(surface.navMeshData);
    }
}