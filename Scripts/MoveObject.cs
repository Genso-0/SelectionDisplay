using UnityEngine;

namespace Assets.Scripts.Prototyping.UIEntityRect
{
    class MoveObject : MonoBehaviour
    {
       public Vector3 rotate;
       public Vector3 move;
        void Update()
        {
            transform.Rotate(rotate);
            transform.position += new Vector3(move.x*Mathf.Sin(Time.unscaledTime), move.y * Mathf.Sin(Time.unscaledTime), move.z * Mathf.Cos(Time.unscaledTime));
        }
    }
}
