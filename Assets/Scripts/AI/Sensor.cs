using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Expression;

public class Sensor : MonoBehaviour {
    public Agent Agent;

    #region Constants
        static float TIME_STEP = 0.5f;
        static int NUM_RINGS = 10;
        static int RAYS_PER_RING = 10;
        static float D_THETA = (2 * Mathf.PI) / RAYS_PER_RING;
    #endregion

    #region Fields
        public Transform FullBodyTransform;
    #endregion

    // @Note for separate sense modalities,
    // we'd want this to become an abstract class,
    // making some sort of abstract method like
    // CaptureModality() or something which
    // does the processing for a specific sense.
    // 
    // Especially important may be so-called
    // "proprioception" or self-perception of movement,
    // do be directly linked to the actuator.
    // 
    // Right now, assume it's a vision module.
    public IEnumerator ReceiveStimulus() {
        while (true) {
            // we'll put all the objects this sensor can see
            // so we avoid redundancy.
            var visibleObjects = new HashSet<GameObject>();

            int layerMask = 1 << 9;

            RaycastHit hit;
            bool collided;

            var forwardDirection = FullBodyTransform.TransformDirection(Vector3.forward);
            var rightDirection = FullBodyTransform.TransformDirection(Vector3.right);
            var upDirection = FullBodyTransform.TransformDirection(Vector3.up);

            collided = Physics.Raycast(
                transform.position,
                forwardDirection,
                out hit,
                layerMask);

            // Here, we can assert whatever information
            // we gather from this raycast hit.
            void OnCollision(RaycastHit theHit) {
                visibleObjects.Add(theHit.transform.gameObject);
                var param = new Expression(new Parameter(SemanticType.INDIVIDUAL, Agent.MentalState.GetNextParameterID()));
                Agent.MentalState.StartCoroutine(Agent.MentalState.Assert(
                    new Expression(PERCEIVE, SELF, new Expression(TREE, param))));

                var position = theHit.transform.gameObject.transform.position;
                Agent.MentalState.Locations.Add(param, new Vector3(position.x, position.y, position.z));
            }

            if (collided) {
                OnCollision(hit);
            }

            Debug.DrawRay(transform.position, forwardDirection * 100, collided ? Color.blue : Color.white);

            for (int i = 1; i < NUM_RINGS + 1; i++) {
                // @Note here, we randomize the raycast within
                // the bounds of its bounding range within a circle.
                // this is to remove blindspots.
                // 
                // TODO:
                // We'll want to do something similar for
                // the regions between the rings, too.
                // 
                float theta = Random.Range(0, i * (Mathf.PI / RAYS_PER_RING));
                for (int j = 0; j < RAYS_PER_RING; j++) {
                    // here, we set up the components of a conical raycast.
                    Vector3 zDir = forwardDirection * (float) i;
                    Vector3 xDir = rightDirection * Mathf.Cos(theta);
                    Vector3 yDir = upDirection * Mathf.Sin(theta);

                    // Raycast
                    collided = Physics.Raycast(
                        transform.position,
                        transform.TransformDirection(zDir + xDir + yDir),
                        out hit,
                        layerMask);

                    Debug.DrawRay(transform.position, (zDir + xDir + yDir) * 100, collided ? Color.blue : Color.white);

                    if (collided && !visibleObjects.Contains(hit.transform.gameObject)) {
                        OnCollision(hit);
                    }

                    theta += D_THETA;
                }
            }

            yield return null; // new WaitForSeconds(TIME_STEP);
        }
    }
}
