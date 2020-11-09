using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{

    MousePosition mousePosition;
    Vector3 mousePos;
    float speed = 5f;

    OscOut oscOut;

    // Start is called before the first frame update
    void Start()
    {
        mousePosition = GameObject.Find("MousePosition").GetComponent<MousePosition>();
        oscOut = GameObject.Find("OscOut").GetComponent<OscOut>();
    }

    // Update is called once per frame
    void Update()
    {
        mousePos = mousePosition.mousePos;
        transform.position = Vector3.Lerp(transform.position, mousePos, speed * Time.deltaTime);

        oscOut.Send("/position", transform.position.x, transform.position.z);
    }
}
