using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    private CharacterController _cc;
    public float MoveSpeed = 5f;
    private Vector3 _movementVelocity;
    private PlayerInput _playerInput;

    private float _verticalVelocity;
    public float Gravity = -9.8f;
    private Animator _animator;
   
   // Enemy
    public bool IsPlayer = true;
    private UnityEngine.AI.NavMeshAgent _navMeshAgent;
    private Transform TargetPlayer;

    //State Machine
     public enum CharacterState
    {
        Normal, Attacking
    }
    public CharacterState CurrentState;

    public float SpawnDuration = 2f;
    private float currentSpawnTime;


private void Awake() 
{
    _cc = GetComponent<CharacterController>();
    
    _animator = GetComponent<Animator>();

      if (!IsPlayer)
        {
            _navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            TargetPlayer = GameObject.FindWithTag("Player").transform;
            _navMeshAgent.speed = MoveSpeed;
            
        }
        else
        {
            _playerInput = GetComponent<PlayerInput>();
        }
}
private void CalculatePlayerMovement()
{
    if (_playerInput.MouseButtonDown && _cc.isGrounded)
        {
            SwitchStateTo(CharacterState.Attacking);
            return;
        }
    _movementVelocity.Set(_playerInput.HorizontalInput,0f,_playerInput.VerticalInput);
    _movementVelocity.Normalize();
    _movementVelocity = Quaternion. Euler(0,-45f,0) * _movementVelocity;

    _animator.SetFloat("Speed", _movementVelocity.magnitude);
    _movementVelocity *= MoveSpeed * Time.deltaTime;
    if(_movementVelocity != Vector3.zero)
        transform.rotation = Quaternion.LookRotation(_movementVelocity);
    _animator.SetBool("Airborne",!_cc.isGrounded);
}
 private void CalculateEnemyMovement()
    {
        if (Vector3.Distance(TargetPlayer.position, transform.position) >= _navMeshAgent.stoppingDistance)
        {
            _navMeshAgent.SetDestination(TargetPlayer.position);
            _animator.SetFloat("Speed", 0.2f);
        }
        else
        {
            _navMeshAgent.SetDestination(transform.position);
            _animator.SetFloat("Speed", 0f);

        }
    }

private void FixedUpdate()
{   
    switch (CurrentState)
        {
            case CharacterState.Normal:
                if (IsPlayer)
                    CalculatePlayerMovement();
                else
                    CalculateEnemyMovement();
                break;

            case CharacterState.Attacking:
            break;
        }


    
    if(IsPlayer)
    {
       if(_cc.isGrounded== false)
        _verticalVelocity = Gravity;
    else 
        _verticalVelocity = Gravity *0.3f;
    
    _movementVelocity += _verticalVelocity * Vector3.up * Time.deltaTime;
    _cc.Move(_movementVelocity);  
    }

}

public void SwitchStateTo(CharacterState newState)
    {
        _playerInput.MouseButtonDown = false;

        
        //Exiting state
        switch (CurrentState)
        {
            case CharacterState.Normal:
                break;
            case CharacterState.Attacking:
                break;
        }
        //Entiring state 
        switch (newState)
                {
                    case CharacterState.Normal:
                        break;
                    case CharacterState.Attacking:
                        break;
                }
        CurrentState = newState;

    }


}
