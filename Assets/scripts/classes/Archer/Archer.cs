using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Archer : MonoBehaviour
{
    public GameObject arrowPrefab;
    public float chargingSpeed = 0.025f; //incremental multiplier
    public float maxCharge = 2f; //max multiplier
    public float shootForce = 5000f; //increased base shoot force
    private float currentCharge = 1f;
    private Vector3 shootDir;
    private bool isCharging = false;
    public GameObject aimIndicator;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            aimIndicator.transform.position = mousePos;
            aimIndicator.SetActive(true);
        }

        if (isCharging)
        {
            currentCharge += chargingSpeed * Time.deltaTime;
            if (currentCharge > maxCharge)
            {
                currentCharge = maxCharge;
            }
        }

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            isCharging = false;
            aimIndicator.SetActive(false);
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            shootDir = (mousePos - transform.position).normalized;
            GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity) as GameObject;
            Rigidbody2D rb2d = arrow.GetComponent<Rigidbody2D>();
            rb2d.AddForce(shootDir * shootForce * currentCharge);
            currentCharge = 1f; //reset the current charge after firing
        }
    }
}