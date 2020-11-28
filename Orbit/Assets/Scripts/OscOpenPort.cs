using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OscOpenPort : MonoBehaviour
{
    OscOut oscOut;
    OscIn oscIn;
    // Start is called before the first frame update
    void Start()
    {
        oscOut = GameObject.Find("OscOut").GetComponent<OscOut>();
        oscIn = GameObject.Find("OscIn").GetComponent<OscIn>();

        oscOut.Open(7000);
        oscIn.Open(7400);
    }
}
