using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IsAttackable
{
    public float SPEED = 3.0f;

    // Стан
    public int MAX_HEALTH;
    [HideInInspector]
    public int maxHealth;
    private float health;
    public float Health
    {
        get { return health; }
        private set { health = Mathf.Clamp(value, 0, maxHealth); }
    }
    [HideInInspector]
    public int coins;
    public int COIN;
    [HideInInspector]
    public float speed;
    public enum PlayerControlMode { Human }
    public PlayerControlMode controlMode = PlayerControlMode.Human;

    [HideInInspector]
    public bool isTemporarySuperWeapon = false;
    [HideInInspector]
    public float superWeaponTimer = 0f;

    public void EnableSuperWeapon(float duration)
    {
        isTemporarySuperWeapon = true;
        superWeaponTimer = duration;
        if (UI != null)
        {
            UI.SetRoomTaskText("АКТИВОВАНО ТИМЧАСОВУ СУПЕР-ЗБРОЮ!");
        }
    }

    [HideInInspector]
    public bool isAbleSwitch;
    public bool isLive;
    public bool isControllable = true;
    bool isInvincible;
    [HideInInspector]
    public int bulletNum = 1;
    public float shotTiming;
    public GunType initial;

    [Header("自身 (Компоненти)")]
    float horizontal;
    float vertical;
    Rigidbody2D rigidbody2d;
    public GameObject Head;
    public GameObject Body;
    SpriteRenderer bodyRenderer;
    SpriteRenderer headRenderer;
    private Animator headAnimator;
    private Animator bodyAnimator;
    private Animator wholeAnimator;
    Vector2 lookDirection = new Vector2(1, 0);
    public GameObject[] guns;
    private bool[] isGunAvailable;
    private int gunNum; // Поточний індекс зброї

    [Header("Інші менеджери")]
    UIManager UI;
    Level level;

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        headAnimator = Head.GetComponent<Animator>();
        bodyAnimator = Body.GetComponent<Animator>();
        wholeAnimator = GetComponent<Animator>();
        headRenderer = Head.GetComponent<SpriteRenderer>();
        bodyRenderer = Body.GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        level = GameManager.Instance.level;

        // ДИПЛОМ: Запобігання вильоту, якщо UIManager або його наслідники ініціалізуються інакше
        UI = FindObjectOfType<UIManager>();

        PlayerInitialize();
    }

    private void Update()
    {
        if (isTemporarySuperWeapon)
        {
            superWeaponTimer -= Time.deltaTime;
            if (superWeaponTimer <= 0)
            {
                isTemporarySuperWeapon = false;
                if (UI != null)
                {
                    UI.SetRoomTaskText("ТИМЧАСОВА СУПЕР-ЗБРОЯ ЗАКІНЧИЛАСЬ!");
                }
            }
        }
        if (Input.GetKey(KeyCode.O))
        {
            PlayerDeath();
        }
        if (Input.GetKey(KeyCode.P))
        {
            PlayerInitialize();
        }
        if (Input.GetKey(KeyCode.H))
        {
            BeAttacked(1, -rigidbody2d.velocity);
        }
        if (!isControllable) { return; }
        UpdateMovement();
        UpdateControl();
        if (isAbleSwitch)
        {
            SwitchGun();
        }
    }

    void UpdateControl()
    {
        bool up = Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.DownArrow);
        bool left = Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.RightArrow);

        if (up)
        {
            LaunchBullet(KeyCode.UpArrow);
        }
        else if (down)
        {
            LaunchBullet(KeyCode.DownArrow);
        }
        else if (left)
        {
            LaunchBullet(KeyCode.LeftArrow);
        }
        else if (right)
        {
            LaunchBullet(KeyCode.RightArrow);
        }
        else
        {
            if (headAnimator != null && headAnimator.gameObject.activeInHierarchy)
            {
                headAnimator.SetBool("IsShooting", false);
            }
        }
    }

    void LaunchBullet(KeyCode key)
    {
        shotTiming += Time.deltaTime;
        if (headAnimator != null && headAnimator.gameObject.activeInHierarchy)
        {
            headAnimator.SetBool("IsShooting", true);
        }
        Gun Gun = guns[gunNum].GetComponent<Gun>();

        if (key == KeyCode.UpArrow)
        {
            if (headAnimator != null && headAnimator.gameObject.activeInHierarchy) headAnimator.Play("HeadUp");
            Gun.shootDirection = new Vector2(0, 1);
        }
        else if (key == KeyCode.DownArrow)
        {
            if (headAnimator != null && headAnimator.gameObject.activeInHierarchy) headAnimator.Play("HeadDown");
            Gun.shootDirection = new Vector2(0, -1);
        }
        else if (key == KeyCode.LeftArrow)
        {
            if (headAnimator != null && headAnimator.gameObject.activeInHierarchy) headAnimator.Play("HeadLeft");
            Gun.shootDirection = new Vector2(-1, 0);
        }
        else if (key == KeyCode.RightArrow)
        {
            if (headAnimator != null && headAnimator.gameObject.activeInHierarchy) headAnimator.Play("HeadRight");
            Gun.shootDirection = new Vector2(1, 0);
        }
    }

    public void SwitchToGun(GunType gunType)
    {
        isGunAvailable[(int)gunType] = true;
        guns[gunNum].SetActive(false);
        gunNum = (int)gunType;
        guns[gunNum].GetComponent<Gun>().isPlayer = true;
        guns[gunNum].GetComponent<Gun>().NUM = bulletNum;
        guns[gunNum].SetActive(true);
    }

    public Transform GetGunNow()
    {
        return guns[gunNum].transform;
    }

    void SwitchGun()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            guns[gunNum].SetActive(false);
            do
            {
                if (--gunNum < 0)
                {
                    gunNum = guns.Length - 1;
                }
            } while (!isGunAvailable[gunNum]);
            guns[gunNum].GetComponent<Gun>().NUM = bulletNum;
            guns[gunNum].SetActive(true);
            if (UI != null) UI.UpdateStatus();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            guns[gunNum].SetActive(false);
            do
            {
                if (++gunNum == guns.Length)
                {
                    gunNum = 0;
                }
            } while (!isGunAvailable[gunNum]);
            guns[gunNum].GetComponent<Gun>().NUM = bulletNum;
            guns[gunNum].SetActive(true);
            if (UI != null) UI.UpdateStatus();
        }
    }

    void UpdateMovement()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        Vector2 move = new Vector2(horizontal, vertical);

        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            lookDirection.Set(move.x, move.y);
            lookDirection.Normalize();
            headAnimator.SetBool("IsWalking", true);
            bodyAnimator.SetBool("IsWalking", true);
        }
        else
        {
            lookDirection.Set(0, 0);
            bodyAnimator.SetBool("IsWalking", false);
            headAnimator.SetBool("IsWalking", false);
        }
        bodyAnimator.SetFloat("MoveX", lookDirection.x);
        bodyAnimator.SetFloat("MoveY", lookDirection.y);
        headAnimator.SetFloat("MoveX", lookDirection.x);
        headAnimator.SetFloat("MoveY", lookDirection.y);
    }

    void FixedUpdate()
    {
        Vector2 position = rigidbody2d.position;
        position.x = position.x + speed * horizontal * Time.deltaTime;
        position.y = position.y + speed * vertical * Time.deltaTime;

        rigidbody2d.MovePosition(position);
    }

    public void PlayerPause()
    {
        isControllable = false;
        if (speed > 0.001f)
        {
            SPEED = speed;
        }
        speed = 0;
        rigidbody2d.velocity = Vector2.zero;
        headAnimator.speed = 0;
        bodyAnimator.speed = 0;
    }

    public void PlayerResume()
    {
        // Prevent unpausing player controls if the main menu overlay is active
        if (UIManager.Instance != null && UIManager.Instance.IsMainMenuActive())
        {
            isControllable = false;
            speed = 0f;
            return;
        }

        isControllable = true;
        if (SPEED > 0.001f)
        {
            speed = SPEED;
        }
        else
        {
            speed = 3.0f; // Safe fallback speed if SPEED was somehow corrupted
        }
        if (headAnimator != null) headAnimator.speed = 1;
        if (bodyAnimator != null) bodyAnimator.speed = 1;
    }

    public void PlayerDeath()
    {
        isLive = false;
        isControllable = false;
        wholeAnimator.SetBool("IsDeath", true);
        PlayerPause();
    }

    public void PlayerInitialize()
    {
        maxHealth = MAX_HEALTH;
        health = maxHealth;

        speed = SPEED;
        coins = COIN;
        isGunAvailable = new bool[guns.Length];
        SwitchToGun(initial);
        shotTiming = 0;

        isAbleSwitch = false;
        isLive = true;
        isControllable = true;
        isInvincible = false;

        wholeAnimator.ResetTrigger("IsDeath");
        wholeAnimator.Play("Respawn");
        PlayerResume();

        if (UI != null) UI.PlayerUIInitialize();
    }

    public void AddHealth(int health)
    {
        Health += health;
        if (UI != null && UI.hp != null) UI.hp.UpdateHP();
    }

    public void ReduceHealth(float damage)
    {
        Health -= damage;
        if (UI != null && UI.hp != null) UI.hp.UpdateHP();

        // ДИПЛОМ: Надсилаємо дані про отриману шкоду в менеджер адаптації складності
        if (AdaptiveDifficultyManager.Instance != null && damage > 0)
        {
            AdaptiveDifficultyManager.Instance.LogPlayerDamage((int)damage);
        }

        if (Health == 0) { PlayerDeath(); }
    }

    public void Accelerate(int spd)
    {
        speed += spd;
    }

    public void BeAttacked(float damage, Vector2 direction, float forceMultiple = 1)
    {
        if (isInvincible || !isLive) { return; }
        ReduceHealth(damage);
        if (isLive)
        {
            StartCoroutine(knockBackCoroutine(direction * forceMultiple));
            StartCoroutine(Invincible());
        }
    }

    IEnumerator knockBackCoroutine(Vector2 force)
    {
        float length = 0.3f;
        float overTime = 0.1f;
        float timeleft = overTime;
        while (force != null && timeleft > 0)
        {
            transform.Translate(force * length * Time.deltaTime / overTime);
            timeleft -= Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator Invincible()
    {
        isInvincible = true;
        Color red = new Color(1, 0.2f, 0.2f, 1);

        float time = 0;
        float flashCD = 0;

        while (time < 1f)
        {
            time += Time.deltaTime;
            flashCD += Time.deltaTime;
            if (flashCD > 0)
            {
                if (bodyRenderer.color == Color.white)
                {
                    bodyRenderer.color = red;
                    headRenderer.color = red;
                }
                else if (bodyRenderer.color == red)
                {
                    bodyRenderer.color = Color.white;
                    headRenderer.color = Color.white;
                }
                flashCD -= 0.13f;
            }
            yield return null;
        }
        bodyRenderer.color = Color.white;
        headRenderer.color = Color.white;
        isInvincible = false;
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        CheckDoorCollision(collider);
    }

    void OnTriggerStay2D(Collider2D collider)
    {
        CheckDoorCollision(collider);
    }

    private void CheckDoorCollision(Collider2D collider)
    {
        if (isControllable && collider.transform.CompareTag("Door"))
        {
            string flag = collider.transform.parent.gameObject.name;
            if (flag == "DoorUp")
            {
                level.MoveToNextRoom(Vector2.up);
            }
            else if (flag == "DoorDown")
            {
                level.MoveToNextRoom(Vector2.down);
            }
            else if (flag == "DoorLeft")
            {
                level.MoveToNextRoom(Vector2.left);
            }
            else if (flag == "DoorRight")
            {
                level.MoveToNextRoom(Vector2.right);
            }
        }
    }
}