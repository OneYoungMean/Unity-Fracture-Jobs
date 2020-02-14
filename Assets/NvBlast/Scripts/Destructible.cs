using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour {

    public GameObject fracturedPrefab;

    private void OnMouseDown()
    {
        Instantiate(fracturedPrefab, transform.position, transform.rotation);
        Destroy(transform.root.gameObject);
    }
}
