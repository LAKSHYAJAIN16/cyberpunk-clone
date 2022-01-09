using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class GunScript : MonoBehaviour
{
    //Effects
    public int AmmoInClip;
    public int TotalAmmo;
    private int CurrentAmmo;
    public float ReloadDuration = 1f;
    private bool Reloading = false;

    //Shoot Vars
    public Transform GunTip;
    public float ShootRange = 1000f;
    private float normShootRange;

    //Aiming
    private bool Aiming = false;

    //Damage
    public int MaxDamage = 20;
    public int MinDamage = 10;
    private int normMaxD, normMinD;

    //Input
    public bool canHoldDown;
    private bool shootInput;

    //Throw
    private Rigidbody rb;
    public float ThrowForce = 20f;

    //PickUp
    public bool PickedUp = true;
    public Transform Player;
    public float PickUpDistance = 5f;
    public Transform Container;
    public static GunScript pickedUpWeapon;

    //EFFECTS
    public ParticleSystem MuzzleFlash;

    //Anims
    private Animator anim;
    public string ShootAnimName = "Shoot", AimingShootAnimName = "AShoot", ShootSpeed = "shootspd";

    //GunStats
    public string GunName;
    public Texture2D GunIcon;

    //UI
    public RawImage Icon;
    public TextMeshProUGUI NameTXT, AmmoTXT;

    //Random Element
    private System.Random random = new System.Random();

    private void Start() 
    {        
        //Set Ammo
        CurrentAmmo = AmmoInClip;

        //Normalize Values
        normShootRange = ShootRange;
        normMaxD = MaxDamage;
        normMinD = MinDamage;

        //Get COmponents
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        //If equipped, set stuff
        if (PickedUp){
            UpdateUI();
        }
    }

    private void Update()
    {
        WeaponInput();
        CheckForPickUp();
    }

    /// <summary>
    /// Method to check for pickup
    /// </summary>
    private void CheckForPickUp()
    {
        if (PickedUp) return;

        //Calculate Distance using TrignoMetry HAHAHAHAHAHA
        Vector3 difV = Player.position - transform.position;
        double xInit = Math.Pow(difV.x, 2f);
        double yInit = Math.Pow(difV.y, 2f);
        double zInit = Math.Pow(difV.z, 2f);
        float dist = (float)Math.Sqrt(xInit + yInit + zInit);

        if (dist < PickUpDistance && Input.GetKeyDown(KeyCode.E)){
            EquipWeapon();
        }
    }
    
    /// <summary>
    /// Input Method
    /// </summary>
    private void WeaponInput()
    {
        //Shoot Input
        if (canHoldDown) shootInput = Input.GetMouseButton(0);
        else if (!canHoldDown) shootInput = Input.GetMouseButtonDown(0);

        //If we press mouse 1, aim
        if (Input.GetMouseButton(1) && PickedUp) AimGun();

        //If we get Mouse 1 up, Reset Aim Values
        if (Input.GetMouseButtonUp(1) && PickedUp) ResetAim();

        //If we press shoot, shoot
        if (shootInput && PickedUp) Shoot();

        //if we press q, drop weapon
        if (Input.GetKeyDown(KeyCode.Q) && PickedUp) DropWeapon();  

        //If we press f, throw weapon
        if (Input.GetKeyDown(KeyCode.F) && PickedUp) ThrowWeapon();

        //Set Bool
        anim.SetBool("Aiming", Aiming);
    }

    private void Shoot()
    {
        //Check for Reload. If our ammo is less than or equal to nan, Reload and return
        if (CurrentAmmo <= 0 && !Reloading) {
            StartCoroutine(Reload());
            return;
        }

        //If we are reloading, return 
        if (Reloading) return;

        //Play particleSystem(s)
        MuzzleFlash.Play();

        //Enable animator
        anim.enabled = true;

        //Get random number
        float RandomD = (float)3 * UnityEngine.Random.Range(1f, 1.5f);

        //Set random float for animation variation
        anim.SetFloat(ShootSpeed, RandomD);

        //Set Shoot Anim Trigger
        if (!Aiming)
            anim.SetTrigger(ShootAnimName);
        else if (Aiming)
            anim.SetTrigger(AimingShootAnimName);

        //Raycast
        RaycastHit hit;
        if(Physics.Raycast(GunTip.position,GunTip.forward,out hit, ShootRange))
        {
            if(hit.transform.TryGetComponent<EnemyHealth>(out EnemyHealth eh)){
                eh.TakeDamage(random.Next(MinDamage, MaxDamage));
            }
        }

        //Decrease ammo
        CurrentAmmo--;

        //Update Ui
        UpdateUI();   
    }

    private void ThrowWeapon()
    {
        //Disable Animator
        anim.enabled = false;

        //Disable weaponSway
        this.transform.parent.GetComponent<WeaponSway>().enabled = false;

        //Set parent to null
        transform.SetParent(null);

        //Apply force
        rb.isKinematic = false;
        rb.AddForce(GunTip.forward * ThrowForce * Time.deltaTime, ForceMode.Impulse);
        rb.AddForce(GunTip.up * ThrowForce / 7 * Time.deltaTime, ForceMode.Impulse);

        //Set pickedup to false
        PickedUp = false;

        //Set pickedupweapon to nan
        pickedUpWeapon = null;
    }

    public void DropWeapon()
    {
        //Disable Animator
        anim.enabled = false;

        //Disable weaponSway
        this.transform.parent.GetComponent<WeaponSway>().enabled = false;

        //Set parent to null
        transform.SetParent(null);

        //Set rb top nan
        rb.isKinematic = false;

        //Set pickedup to false
        PickedUp = false;

        //Set pickedupweapon to nan
        pickedUpWeapon = null;
    }

    private void EquipWeapon()
    {
        //Set pickedup to true
        PickedUp = true;

        //Set parent to The container
        transform.SetParent(Container);

        //If we have already picked up a weapon, drop that BEFORE picking that up
        if (pickedUpWeapon != null) pickedUpWeapon.DropWeapon();

        //Set position, rotation and scale to nan
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.Euler(Vector3.zero);
        transform.localScale = Vector3.one;

        //Set LOCAL position,rotation and scale to nan
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(Vector3.zero);
        transform.localScale = Vector3.one;

        //Enable WeaponSway
        this.transform.parent.GetComponent<WeaponSway>().enabled = true;

        //Set rb to nan
        rb.isKinematic = true;

        //Set pickedupweapon to this
        pickedUpWeapon = this;
    }

    private void AimGun()
    {
        //Increase some values
        ShootRange = normShootRange * 1.5f;
        MaxDamage = normMaxD * 2;
        MinDamage = normMinD * 2;

        //We're aiming, sooo.
        Aiming = true;
    }

    private void ResetAim()
    {
        //Reset values
        ShootRange = normShootRange;
        MaxDamage = normMaxD;
        MinDamage = normMinD;

        //We're NOT aiming, soo..
        Aiming = false;
    }

    private void UpdateUI()
    {
        AmmoTXT.text = CurrentAmmo + "/" + TotalAmmo;
        NameTXT.text = GunName;
        Icon.texture = GunIcon;
    }

    private IEnumerator Reload()
    {
        Reloading = true;
        CurrentAmmo = 0;
        yield return new WaitForSeconds(ReloadDuration);

        if (TotalAmmo > 0){
            TotalAmmo -= AmmoInClip;
            CurrentAmmo = AmmoInClip;
            Reloading = false;
        }

        if (TotalAmmo <= 0)
        {
            Debug.LogError("Error : TotalAmmo limit donezo");
        }
    }
}