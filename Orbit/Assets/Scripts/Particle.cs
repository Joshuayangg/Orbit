using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{

    MousePosition mousePosition;
    Vector3 mousePos;
    float speed = 5f;

    OscOut oscOut;
    OscMessage _position;

    // Start is called before the first frame update
    void Start()
    {
        mousePosition = GameObject.Find("MousePosition").GetComponent<MousePosition>();
        oscOut = GameObject.Find("OscOut").GetComponent<OscOut>();

        _position = new OscMessage("/position");
        _position.Add(0).Add(0);
    }

    // Update is called once per frame
    void Update()
    {
        mousePos = mousePosition.mousePos;
        transform.position = Vector3.Lerp(transform.position, mousePos, speed * Time.deltaTime);

        _position.Set(0, transform.position.x);
        _position.Set(1, transform.position.z);

        Debug.Log(_position);

        oscOut.Send(_position);
    }
}
