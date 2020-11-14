using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataController : MonoBehaviour
{

    public Transform player;
    public Transform planet;


    OscOut oscOut;

    OscMessage _planet; // xpos, ypos, dist, radius

    // Start is called before the first frame update
    void Start()
    {
        oscOut = GameObject.Find("OscOut").GetComponent<OscOut>();

        // Initialize Osc Message
        _planet = new OscMessage("/planet");
        _planet.Add(0).Add(0).Add(0).Add(0);

    }

    // Update is called once per frame
    void Update()
    {

        //Debug.Log("AvgRotation: " + getAvgRotation());
        //Debug.Log("Relative position Y: " + getRelativePositionY());
        //Debug.Log("Relative position X: " + getRelativePositionX());
        //Debug.Log("Relative distance: " + getRelativeDistance());

        // Set and send data to Max
        _planet.Set(0, getRelativePositionX()); // range - [-5, 5]
        _planet.Set(1, getRelativePositionY()); // range ~ [0.01, 1.9]
        _planet.Set(2, getRelativeDistance()); // range - [0, 5]
        _planet.Set(3, getPlanetRadius()); // range - [0.01, 4]

        oscOut.Send(_planet);
    }

    public float getAvgRotation()
    {
        return (planet.eulerAngles.x + planet.eulerAngles.y + planet.eulerAngles.z / 3f);
    }

    public float getRelativePositionX()
    {
        return Mathf.Clamp((planet.position.x - player.position.x), -5f, 5f);
    }

    public float getRelativePositionY()
    {
        return (planet.position.y - player.position.y);
    }

    public float getRelativePositionZ()
    {
        return (planet.position.z - player.position.z);
    }

    public float getRelativeDistance()
    {
        return Mathf.Clamp(Vector3.Distance(planet.position, player.position), 0f, 5f);
    }

    public float getPlanetRadius()
    {
        return planet.GetComponent<PlanetController>().getPlanetRadius();
    }

}
