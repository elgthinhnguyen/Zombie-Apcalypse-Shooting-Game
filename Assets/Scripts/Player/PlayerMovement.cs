using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Thêm thư viện này để dùng Slider

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Player Health & Damage")]
    private int maxHealth = 100;
    public int currentHealth;
    private bool isDead;

    // Health slider được thêm vào đây
    public Slider healthSlider; 

    //death screen
    [Header("Player Movement")]
    public float movementSpeed = 5f;

    [Header("Jump & Gravity")]
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Foot steps")]
    public AudioSource leftFootAudioSource;
    public AudioSource rightFootAudioSource;
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.5f;

    private float nextFootstepTime;
    private bool isLeftFoot = true;
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Awake()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        // Cấu hình giá trị cho Slider lúc bắt đầu game chuẩn theo Video 36
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    void Update()
    {
        if (controller == null)
        {
            return;
        }

        CheckGround();
        HandleMovement();
        HandleJump();
        HandleGravity();

        if (isGrounded && controller.velocity.magnitude > 0.1f)
        {
            if (Time.time >= nextFootstepTime)
            {
                PlayerFootstepSounds();
                nextFootstepTime = Time.time + footstepInterval;
            }
        }
    }

    void CheckGround()
    {
        if (controller == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = controller.isGrounded;

        if (groundCheck != null)
        {
            isGrounded |= Physics.CheckSphere(
                groundCheck.position,
                groundDistance,
                groundMask
            );
        }
    }

    void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            input.x = (Keyboard.current.dKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.aKey.isPressed ? 1f : 0f);

            input.y = (Keyboard.current.wKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.sKey.isPressed ? 1f : 0f);
        }

        Vector3 movement = transform.right * input.x
                         + transform.forward * input.y;

        movement.y = 0f;

        controller.Move(
            movement * movementSpeed * Time.deltaTime
        );
    }

    void HandleJump()
    {
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void HandleGravity()
    {
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(
            velocity * Time.deltaTime
        );
    }

    void PlayerFootstepSounds()
    {
        AudioClip footstepClip = footstepSounds[Random.Range(0, footstepSounds.Length)];
        if (isLeftFoot)
        {
            leftFootAudioSource.PlayOneShot(footstepClip);
        }
        else
        {
            rightFootAudioSource.PlayOneShot(footstepClip);
        }

        isLeftFoot = !isLeftFoot;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead || damageAmount <= 0)
        {
            return;
        }

        currentHealth -= damageAmount;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }

        // Cập nhật lại giá trị trên thanh Slider khi nhận sát thương
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    private void Die()
    {
        isDead = true;
        if (GameManager.instance != null)
        {
            GameManager.instance.ShowGameOver();
        }
        else
        {
            Debug.LogError("GameManager instance not found; cannot show the death screen.");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }

        Debug.Log("Player has died.");
    }
}