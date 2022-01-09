using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

public class WeaponSway : MonoBehaviour
{
    public float intensity;
    public float smooth;
    public float swayCounter = 0f;
    public float swayMagnitude = .1f;
    private Quaternion origin_rotation;

    private bool Override = false;
    private void Start()
    {
        origin_rotation = transform.localRotation;
    }

    private void Update()
    {
        UpdateSway();
    }

    private void UpdateSway()
    {
        //controls
        float t_x_mouse = Input.GetAxis("Mouse X");
        float t_y_mouse = Input.GetAxis("Mouse Y");

        if (Override){
            t_x_mouse = 0f;
            t_y_mouse = 0f;
        }

        //calculate target rotation
        Quaternion t_x_adj = Quaternion.AngleAxis(-intensity * t_x_mouse, Vector3.up);
        Quaternion t_y_adj = Quaternion.AngleAxis(intensity * t_y_mouse, Vector3.right);
        Quaternion target_rotation = origin_rotation * t_x_adj * t_y_adj;

        //rotate towards target rotation
        transform.localRotation = Quaternion.Lerp(transform.localRotation, target_rotation, Time.deltaTime * smooth);

        if (!PlayerMovement.Instance.isMoving())
        {
            float addAmount = Mathf.Sin(0.01f * swayCounter) * swayMagnitude * Time.deltaTime;
            transform.localPosition += Vector3.up * addAmount;
        }

    }

    public void OverrideRot(bool wtf)
    {
        Override = wtf;
    }

}
