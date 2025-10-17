using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{

    AudioSource soundEffect;
    public float jumpHeight = 10;

    public enum JumpType { Up = 0, Left = 1, Right = 2, UpLeft =3, UpRight = 4, DownLeft =5, DownRight=6, Down=7}
    public JumpType jumpType = JumpType.Up;

    private Vector3[] JumpDirection = {Vector3.up, Vector3.left, Vector3.right, (Vector3.up+Vector3.left),
    (Vector3.up+Vector3.right),(Vector3.down+Vector3.left),(Vector3.down+Vector3.right),Vector3.down};

    public bool isIgnoreEnemey = false;

    private void Start()
    {
        soundEffect = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.GetComponent<Rigidbody>())
        {
            if(isIgnoreEnemey && other.transform.tag == "Enemy")
            {

            }
            else
            {

                Bounce(other);
            }


        }
    }


    private void Bounce(Collider other)
    {
        Vector3 otherVel = other.transform.GetComponent<Rigidbody>().velocity;
        if (JumpDirection[(int)jumpType].y != 0)
        {
            other.transform.GetComponent<Rigidbody>().velocity = new Vector3(otherVel.x, 0, otherVel.z);
        }
        otherVel = other.transform.GetComponent<Rigidbody>().velocity;
        if (JumpDirection[(int)jumpType].x != 0)
        {
            other.transform.GetComponent<Rigidbody>().velocity = new Vector3(0, otherVel.y, otherVel.z);
        }

        other.transform.GetComponent<Rigidbody>().velocity += JumpDirection[(int)jumpType] * jumpHeight;

        if (other.transform.tag == "Player")
            other.transform.GetComponent<Player>().BounceFace();
        // other.transform.GetComponent<Rigidbody>().AddForce(gameObject.transform.up * jumpHeight, ForceMode.Impulse);
        soundEffect.Play();
    }


    /*
    private void OnTriggerStay(Collider other)
    {

        if (other.transform.GetComponent<Rigidbody>())
        {
            other.transform.GetComponent<Rigidbody>().AddForce(gameObject.transform.up * Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y), ForceMode.VelocityChange);
            soundEffect.Play();
            Debug.Log(Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y));
        }
    }*/
}
