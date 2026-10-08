using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SwitchCar : MonoBehaviour
{
    private const string CarName = "Car ";
    [SerializeField] private List<GameObject> cars = new List<GameObject>();
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int currentIntCar;

    public event Action<int> OnCarChanged;

    public int CurrentIntCar{get => currentIntCar; set { currentIntCar = value; Refresh(); }}

    private void OnEnable() => Refresh();
    private void OnValidate() => Refresh();

    //Refresh Car
    private void Refresh()
    {
#if UNITY_EDITOR
        if (EditorUtility.IsPersistent(this)) return;

        if (!Application.isPlaying)
        {
            EditorApplication.delayCall -= Spawn;
            EditorApplication.delayCall += Spawn;
            return;
        }
#endif
        Spawn();
    }

    //Spawn Car and Delete the other
    private void Spawn()
    {
        if (this == null || cars.Count == 0) return;

        currentIntCar = Mathf.Clamp(currentIntCar, 0, cars.Count - 1);
        GameObject prefab = cars[currentIntCar];
        if (prefab == null) return;

        Transform root = spawnPoint != null ? spawnPoint : transform;

        // Remove every previously spawned car
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (!child.name.StartsWith(CarName)) continue;

            child.SetParent(null);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        //Name the car game object
        GameObject car = Instantiate(prefab, root);
        car.name = CarName + (char)('A' + currentIntCar);
        car.transform.localPosition = Vector3.zero;
        car.transform.localRotation = Quaternion.identity;

    #if UNITY_EDITOR
        if (!Application.isPlaying)
            foreach (Transform t in car.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.DontSave;
    #endif
        OnCarChanged?.Invoke(currentIntCar);
    }
}