using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkInfo : MonoBehaviour
{
    public Vector3 startPosition;
    public Quaternion startRotation;
    public Bounds startBounds;

    void Reset()
    {
        MeshRenderer mr = this.GetComponent<MeshRenderer>();
        
        startBounds = mr.bounds;
        startPosition = this.transform.position;
        startRotation = this.transform.rotation;
    }

    public void UpdatePreview(float distance)
    {
        this.transform.position = this.transform.parent.TransformPoint(startBounds.center * distance);
    }
}
