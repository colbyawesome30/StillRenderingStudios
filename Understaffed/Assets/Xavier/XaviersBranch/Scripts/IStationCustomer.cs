using UnityEngine;

public interface IStationCustomer
{
    bool IsAtLineSpot { get; }
    void moveToLinePosition(Vector3 position);
    void SetBeingServed(bool served);
    int QuantityFor(foodStation station);
    void OnServiceFinished(foodStation station);
    void OnStationRemoved(foodStation station);
}
