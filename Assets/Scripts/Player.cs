using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float attackRate = 2f;
    public float dodgeCoolDown = 1.5f;
    public float parryCoolDown = 0.5f;

    // cached reference
    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 inputDirection;
    private Vector2 faceDirection;

    private float nextAttackTime = 0f;
    private float attackPhaseExpirationTime = 0f;
    private int lastAttackPhase = 0;

    private float nextDodgeTime = 0f;
    private float dodgeSpeed = 0f;
    private float maxDodgeSpeed = 20f;
    private bool maxDodgeSpeedReached = false;

    private const float minimumBlockHeldDuration = 0.25f;
    private float blockPressedTime = 0;
    private bool blockHeld = false;
    private float nextParryTime = 0f;

    private static int ATTACK_PHASE_COUNT = 3;

    private State state = State.Idle;
    private enum State
    {
        Idle,
        Attack,
        Parry,
        Block,
        Dodge
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        SetFaceDirection(new Vector2(0, 1));
    }

    void Update()
    {
        inputDirection.x = Input.GetAxisRaw("Horizontal");
        inputDirection.y = Input.GetAxisRaw("Vertical");
        UpdateFaceDirection();

        switch (state)
        {
            case State.Idle:
                HandleParry();
                HandleDodge();
                StartCoroutine(HandleAttack());
                HandleRun();
                break;
            case State.Dodge:
                Dodge();
                break;
            case State.Parry:
            case State.Block:
                StartCoroutine(ParryOrBlock());
                break;
        }
    }

    void FixedUpdate()
    {
        if (!MovementFreezed())
        {
            rb.MovePosition(rb.position + inputDirection.normalized * movementSpeed * Time.fixedDeltaTime);
        }
    }

    public void SetFaceDirection(Vector2 direction)
    {
        faceDirection = direction;
        animator.SetFloat("Horizontal", direction.x);
        animator.SetFloat("Vertical", direction.y);
    }

    private bool MovementFreezed()
    {
        return (state == State.Attack || state == State.Dodge || state == State.Block || state == State.Parry);
    }

    private void UpdateFaceDirection()
    {
        if (!MovementFreezed() && (inputDirection.x != 0 || inputDirection.y !=0))
        {
            faceDirection = inputDirection;
            animator.SetFloat("Horizontal", inputDirection.x);
            animator.SetFloat("Vertical", inputDirection.y);
        }
    }

    private void HandleRun()
    {
        if (!MovementFreezed())
        {
            animator.SetFloat("MoveSpeed", inputDirection.sqrMagnitude);
        }
    }

    private IEnumerator HandleAttack()
    {
        if (Time.time >= attackPhaseExpirationTime)
        {
            lastAttackPhase = 0;
        }

        if (Time.time >= nextAttackTime)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                state = State.Attack;
                int thisAttackPhase = lastAttackPhase % ATTACK_PHASE_COUNT + 1;
                lastAttackPhase = thisAttackPhase;
                attackPhaseExpirationTime = Time.time + 1.5f;
                nextAttackTime = Time.time + 1f / attackRate;

                animator.SetTrigger("Attack" + thisAttackPhase);

                rb.MovePosition(rb.position + faceDirection * 10 * Time.fixedDeltaTime);

                yield return new WaitForSeconds(0.5f);
                state = State.Idle;
            }
        }
    }

    private void HandleDodge()
    {
        if (Time.time >= nextDodgeTime)
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                nextDodgeTime = Time.time + dodgeCoolDown;

                animator.SetTrigger("Dodge");
                dodgeSpeed = 2.5f;
                state = State.Dodge;
            }
        }
    }

    private void Dodge()
    {
        AdjustDodgeSpeed();
        rb.MovePosition(rb.position + faceDirection.normalized * dodgeSpeed * Time.fixedDeltaTime);

        if (dodgeSpeed < 1)
        {
            maxDodgeSpeedReached = false;
            state = State.Idle;
        }
    }

    private void AdjustDodgeSpeed()
    {
        if (!maxDodgeSpeedReached && dodgeSpeed < maxDodgeSpeed)
        {
            dodgeSpeed += dodgeSpeed * 5f * Time.fixedDeltaTime;
        }
        else
        {
            maxDodgeSpeedReached = true;
           dodgeSpeed -= dodgeSpeed * 5f * Time.fixedDeltaTime;
        }
    }

    private void HandleParry()
    {
        if (Time.time >= nextParryTime && Input.GetKeyDown(KeyCode.C))
        {
            state = State.Parry;
            nextParryTime = Time.time + parryCoolDown;
            blockPressedTime = Time.time;
            blockHeld = false;
        }
    }

    private IEnumerator ParryOrBlock()
    {
        if (Input.GetKeyUp(KeyCode.C))
        {
            animator.SetBool("Block", false);
            if (!blockHeld)
            {
                animator.SetTrigger("Parry");
                yield return new WaitForSeconds(0.4f);
            }
            blockHeld = false;
            state = State.Idle;
        }

        if (Input.GetKey(KeyCode.C))
        {
            if (Time.time - blockPressedTime > minimumBlockHeldDuration)
            {
                state = State.Block;
                blockHeld = true;
                animator.SetBool("Block", true);
            }
        }
    }
}
