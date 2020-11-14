using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PlanetController : MonoBehaviour
{
    OVRGrabbable mGrabbable;

    public OVRGrabber LGrabber;
    public OVRGrabber RGrabber;
    public VisualEffect planet;

    private Transform LGrabberTransform;
    private Transform RGrabberTransform;

    private float controllerDist;
    private float controllerDistChangeRate;

    private bool changeRadiusStarted;

    private float planetRadius;

    // Start is called before the first frame update
    void Start()
    {
        mGrabbable = this.GetComponent<OVRGrabbable>();
        controllerDistChangeRate = 0f;
        controllerDist = -69f;

        planetRadius = 0.2f;

        changeRadiusStarted = false;

        LGrabberTransform = LGrabber.GetComponent<Transform>();
        RGrabberTransform = RGrabber.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (mGrabbable.grabbedBy == LGrabber)
        {
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch) > 0.9f)
            {
                UpdatePlanetRadius();
            }
            else
            {
                StopUpdateRadius();
            }
        } else if (mGrabbable.grabbedBy == RGrabber)
        {
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch) > 0.9f)
            {
                UpdatePlanetRadius();
            }
            else
            {
                StopUpdateRadius();
            }
        }

    }

    private void StopUpdateRadius()
    {
        changeRadiusStarted = false;
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
    }

    // Change radius of sphere if both grip buttons are held and one is dragged
    private void UpdatePlanetRadius()
    {

        float newControllerDist = Vector3.Distance(LGrabberTransform.position, RGrabberTransform.position);

        if (changeRadiusStarted == false)
        {
            // Set initial distance and don't mess with change in distance yet
            changeRadiusStarted = true;
        }
        else
        {
            // Set updated planet radius
            controllerDistChangeRate = Mathf.Clamp(10f * (newControllerDist - controllerDist), -0.05f, 0.05f);
            planetRadius = Mathf.Clamp(planet.GetFloat("sphere_radius") + controllerDistChangeRate, 0.001f, 4f);
            planet.SetFloat("sphere_radius", planetRadius);

            // Change sphere collider size as well.
            this.GetComponent<SphereCollider>().radius = Mathf.Clamp(planetRadius, 0.08f, 0.3f);

            // Add some haptics
            OVRInput.SetControllerVibration(0.3f, Mathf.Abs(11f * controllerDistChangeRate), OVRInput.Controller.RTouch);
            OVRInput.SetControllerVibration(0.3f, Mathf.Abs(11f * controllerDistChangeRate), OVRInput.Controller.LTouch);
        }

        controllerDist = newControllerDist;
    }

    public float getPlanetRadius()
    {
        return planetRadius;
    }
}
