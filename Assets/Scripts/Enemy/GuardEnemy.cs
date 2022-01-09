using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Player;

public class GuardEnemy : MonoBehaviour
{
    private Animator anim;
    public static Transform mehPlayer;
    public static bool sensed = false;

    public float SenseArea = 10f, ShootRange = 1000f;
    public ParticleSystem MuzzleFlash;
    public LayerMask whatIsPlayer;
    public NavMeshAgent nav;
    public Transform GunTip;

    private void Start()
    {
        //Get Animator
        anim = GetComponent<Animator>();

        //Set random values for more VARIETY
        SetRandomValues();
    }

    private void Update()
    {
        SensePlayer();
    }

    private void SensePlayer()
    {
        //Raycast to check for Player
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, whatIsPlayer) || Physics.Raycast(transform.position, transform.right, out hit, whatIsPlayer) || Physics.Raycast(transform.position, -transform.right, out hit, whatIsPlayer))
        {
            if (hit.transform.TryGetComponent<PlayerMovement>(out PlayerMovement ph))
            {
                anim.SetBool("shoot", true);
                anim.SetBool("walk", false);
                mehPlayer = hit.transform;
                nav.SetDestination(transform.position);
                sensed = true;
                return;
            }
        }

        //Do Physics. overlap sphere because it takes the least memory
        Collider[] colls = Physics.OverlapSphere(transform.position, SenseArea, whatIsPlayer);

        if (!sensed) {
            foreach (Collider item in colls)
            {
                anim.SetBool("shoot", true);
                anim.SetBool("walk", false);

                mehPlayer = item.transform;
                nav.SetDestination(transform.position);
                sensed = true;
                break;
            }
        }

        else if (sensed) {

            //Look at player if he is still there
            if (colls.Length != 0)
            {
                //Found Player
                transform.LookAt(new Vector3(mehPlayer.position.x, transform.position.y, mehPlayer.position.z));
                nav.SetDestination(transform.position);
                anim.SetBool("walk", false);
                anim.SetBool("shoot", true);
                nav.SetDestination(transform.position);
                sensed = true;
            }

            //If the player is not there, don't shoot but follow him
            if (colls.Length == 0)
            {
                anim.SetBool("shoot", false);
                anim.SetBool("walk", true);
                nav.SetDestination(mehPlayer.position);
                transform.LookAt(new Vector3(mehPlayer.position.x, transform.position.y, mehPlayer.position.z));
            }
        }
    }

    private void SetRandomValues()
    {
        //Set speed
        float speed = Random.Range(0.3f, 1f);
        anim.SetFloat("speed", speed);

        //Set mirror
        double MirrorProb = Random.value;
        if (MirrorProb >= 0.8f) anim.SetBool("mirror", true);
        else
            anim.SetBool("mirror", false);
    }

    private void Shoot()
    {
        //EFFECTS
        MuzzleFlash.Play();

        //Raycast
        RaycastHit hit;
        if (Physics.Raycast(GunTip.position, GunTip.forward, out hit, ShootRange))
        {
            //Check if its the player
            if (hit.transform.TryGetComponent<PlayerMovement>(out PlayerMovement ph))
            {
                Destroy(ph.gameObject);
            }
        }

        else if (!Physics.Raycast(GunTip.position, GunTip.forward, out hit, ShootRange))
        {
            if (Physics.SphereCast(GunTip.position, 2f, GunTip.forward, out hit, ShootRange))
            {
                //Check if its the player
                if (hit.transform.TryGetComponent<PlayerMovement>(out PlayerMovement ph))
                {
                    Destroy(ph.gameObject);
                }
            }
        }
    }

    public void Alert()
    {
        mehPlayer = (GameObject.FindGameObjectWithTag("Player")).transform;
        sensed = true;
    }
}
