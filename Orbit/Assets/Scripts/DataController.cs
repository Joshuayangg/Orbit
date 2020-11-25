using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataController : MonoBehaviour
{

    public Transform player;
    public List<Transform> planets;

    private static int NUM_PLANETS = 2;


    OscOut oscOut;

    List<OscMessage> _planets; // xpos, ypos, dist, radius

    // Start is called before the first frame update
    void Start()
    {
        oscOut = GameObject.Find("OscOut").GetComponent<OscOut>();

        // Initialize Osc Messages
        _planets = new List<OscMessage> ();
        for (int i = 0; i < NUM_PLANETS; i++)
        {
            OscMessage _planet = new OscMessage("/planet" + i);
            _planet.Add(0).Add(0).Add(0).Add(0);
            _planets.Add(_planet);
        }
        

    }

    // Update is called once per frame
    void Update()
    {

        //Debug.Log("AvgRotation: " + getAvgRotation());
        //Debug.Log("Relative position Y: " + getRelativePositionY());
        //Debug.Log("Relative position X: " + getRelativePositionX());
        //Debug.Log("Relative distance: " + getRelativeDistance());

        // Set and send data to Max
        int i = 0;
        foreach (OscMessage _planet in _planets)
        {
            _planet.Set(0, getRelativePositionY(i)); // range ~ [-2, 2]
            _planet.Set(1, getRelativeDistance(i)); // range - [0, 5]
            _planet.Set(2, getPlanetRadius(i)); // range - [0.01, 4]
            _planet.Set(3, getRotationSpeed(i)); // range - [0, 15]

            oscOut.Send(_planet);
            i++;
        }
    }

    public float getRotationSpeed(int i)
    {
        float planetRotationSpeed = planets[i].GetComponent<Rigidbody>().angularVelocity.magnitude;
        return Mathf.Clamp(planetRotationSpeed, 0, 15f);
    }

    public float getRelativePositionY(int i)
    {
        return Mathf.Clamp((planets[i].position.y - player.position.y), -2f, 2f);
    }

    public float getRelativePositionZ(int i)
    {
        return (planets[i].position.z - player.position.z);
    }

    public float getRelativeDistance(int i)
    {
        return Mathf.Clamp(Vector3.Distance(planets[i].position, player.position), 0f, 5f);
    }

    public float getPlanetRadius(int i)
    {
        return planets[i].GetComponent<PlanetController>().getPlanetRadius();
    }

}
