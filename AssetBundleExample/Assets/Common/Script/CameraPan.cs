using UnityEngine;
using System.Collections;

public class CameraPan : MonoBehaviour 
{
    Vector3 previousPosition = Vector3.zero;

    public Plane Plane
    {
        get
        {
            return new Plane(new Vector3(0.0f, 0.0f, 1.0f), Vector3.zero);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            previousPosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 currentScreenPosition = Input.mousePosition;

            Ray currentRay = Camera.main.ScreenPointToRay(currentScreenPosition);
            Ray previousRay = Camera.main.ScreenPointToRay(previousPosition);

            float distCurrent;
            float distPrevious;

            if (Plane.Raycast(currentRay, out distCurrent) && Plane.Raycast(previousRay, out distPrevious))
            {
                Vector3 previousWorldPosition = previousRay.GetPoint(distPrevious);
                Vector3 currentWorldPosition = currentRay.GetPoint(distCurrent);

                camera.transform.position -= currentWorldPosition - previousWorldPosition;
            }

            previousPosition = currentScreenPosition;
        }
    }
}
