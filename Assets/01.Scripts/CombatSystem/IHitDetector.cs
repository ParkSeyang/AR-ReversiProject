using UnityEngine;

public interface IHitDetector
{
    ICombatAgent Owner { get; }
    LayerMask DetectionLayer { get; }
    
    void Initialize(ICombatAgent owner);
    void EnableDetection();
    void DisableDetection();
}
