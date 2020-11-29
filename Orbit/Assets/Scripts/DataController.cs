using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataController : MonoBehaviour
{

    public Transform player;
    public List<Transform> planets;

    private static int NUM_PLANETS = 2;
    private static int NUM_SCALES = 4;

    public int scaleIndex = 0;


    OscOut oscOut;
    OscIn oscIn;

    List<OscMessage> _planets; // xpos, ypos, dist, radius, scaleIndex (only for planet0)

    // Start is called before the first frame update
    void Start()
    {
        oscOut = GameObject.Find("OscOut").GetComponent<OscOut>();
        oscIn = GameObject.Find("OscIn").GetComponent<OscIn>();
        oscIn.MapDouble("/planet0", OnReceivePlanet0);
        oscIn.MapDouble("/planet1", OnReceivePlanet1);

        // Initialize Osc Messages
        _planets = new List<OscMessage> ();
        for (int i = 0; i < NUM_PLANETS; i++)
        {
            OscMessage _planet = new OscMessage("/planet" + i);
            _planet.Add(0).Add(0).Add(0).Add(0);
            if (i == 0)
            {
                _planet.Add(scaleIndex);
            }
            _planets.Add(_planet);
        }
    }

    void OnReceivePlanet0(double value)
    {
        float freq = (float)value * 15f;
        planets[0].GetComponent<PlanetController>().setPlanetFreq(freq);
    }

    void OnReceivePlanet1(double value)
    {
        float freq = (float)value * 5f;
        planets[1].GetComponent<PlanetController>().setPlanetFreq(freq);
    }

    // Update is called once per frame
    void Update()
    {

        // Set and send data to Max
        int i = 0;
        foreach (OscMessage _planet in _planets)
        {
            _planet.Set(0, getRelativePositionY(i)); // range ~ [-2, 2]
            _planet.Set(1, getRelativeDistance(i)); // range - [0, 5]
            _planet.Set(2, getPlanetRadius(i)); // range - [0.01, 4]
            _planet.Set(3, getRotationSpeed(i)); // range - [0, 15]

            if (i == 0)
            {
                _planet.Set(4, scaleIndex);
            }

            oscOut.Send(_planet);
            i++;
        }

        if (OVRInput.GetDown(OVRInput.Button.One)) {
            nextScaleIndex();
        }
    }

    public void nextScaleIndex()
    {
        scaleIndex = (scaleIndex + 1) % NUM_SCALES;
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
