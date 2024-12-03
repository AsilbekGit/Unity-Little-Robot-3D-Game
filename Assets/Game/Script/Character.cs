using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering.Universal;

public class Character : MonoBehaviour
{
    private CharacterController _cc;
    public float MoveSpeed = 5f;
    private Vector3 _movementVeloctity;
    private PlayerInput _playerInput; 
    public float Gravity = -9.8f;
    public float _verticalVelocity; 
    private Animator _animator;

    public int Coin;

    //Enemy
    public bool IsPlayer = true;
    private UnityEngine.AI.NavMeshAgent _navMeshAgent;
    private Transform TargetPlayer;

    //Health
    private Health _health;

    //Damage Caster
    private DamageCaster _damageCaster;

    //Player slides
    private float AttackStartTime;
    public float AttackSlideDuration = 0.4f;
    public float AttackSlideSpeed = 0.06f;


    private Vector3 impactOnCharacter;

    public bool IsInvincible;
    public float invincibleDuration = 2f;


    private float AttackAnimationDuration;

    public float SlideSpeed = 9f;


    //State Machine
    public enum CharacterState{
        Normal, Attacking, Dead, BeingHit, Slide, Spawn
    }

    public CharacterState CurrentState;

    public float SpawnDuration = 2f;
    private float currentSpawnTime;

    //Material Animation
    private MaterialPropertyBlock _materialProperBlock;
    private SkinnedMeshRenderer _skinnedMeshRenderer;


    public GameObject ItemToDrop;


    private void Awake(){
        _cc = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _health = GetComponent<Health>();
        _damageCaster = GetComponentInChildren<DamageCaster>();

        _skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        _materialProperBlock = new MaterialPropertyBlock();
        _skinnedMeshRenderer.GetPropertyBlock(_materialProperBlock);

        if(!IsPlayer){
            _navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            TargetPlayer = GameObject.FindWithTag("Player").transform;
            _navMeshAgent.speed = MoveSpeed;
            SwitchStateTo(CharacterState.Spawn);
        }else{
            _playerInput = GetComponent<PlayerInput>();
        }

    }

    private void CalculatePlayerMovement(){

        if(_playerInput.MouseButtonDown && _cc.isGrounded){
            SwitchStateTo(CharacterState.Attacking);
            return;
        }else if(_playerInput.SpaceKeyDown && _cc.isGrounded){
            SwitchStateTo(CharacterState.Slide);
            return;
        }


        _movementVeloctity.Set(_playerInput.HorizontalInput, 0f, _playerInput.VerticalInput);
        _movementVeloctity.Normalize();
        _movementVeloctity = Quaternion.Euler(0, -45f, 0) * _movementVeloctity;

        _animator.SetFloat("Speed", _movementVeloctity.magnitude);



        _movementVeloctity *= MoveSpeed * Time.deltaTime;

        


        if(_movementVeloctity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_movementVeloctity);

        _animator.SetBool("AirBorne", !_cc.isGrounded);

    }


    private void CalculateEnemyMovement(){
        if(Vector3.Distance(TargetPlayer.position, transform.position) >= _navMeshAgent.stoppingDistance){
            _navMeshAgent.SetDestination(TargetPlayer.position);
            _animator.SetFloat("Speed", 0.2f);
        }else{
            _navMeshAgent.SetDestination(transform.position);
            _animator.SetFloat("Speed", 0f);

            SwitchStateTo(CharacterState.Attacking);
        }
    }

    private void FixedUpdate(){

        switch(CurrentState){
            case CharacterState.Normal:
                if (IsPlayer)
                    CalculatePlayerMovement();
                else
                    CalculateEnemyMovement();
                break;
            case CharacterState.Attacking:

                if(IsPlayer){
                    

                    if(Time.time < AttackStartTime + AttackSlideDuration){
                        float timePassed = Time.time - AttackStartTime;
                        float lerpTime = timePassed/AttackSlideDuration;
                        _movementVeloctity = Vector3.Lerp(transform.forward * AttackSlideSpeed, Vector3.zero, lerpTime);
                    }
                    
                    if(_playerInput.MouseButtonDown && _cc.isGrounded){
                        string currentClipName = _animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
                        AttackAnimationDuration = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

                        if (currentClipName != "LittleAdventurerAndie_ATTACK_03" && AttackAnimationDuration > 0.5f && AttackAnimationDuration < 0.7f)
                        {
                            _playerInput.MouseButtonDown = false;
                            SwitchStateTo(CharacterState.Attacking);

                            //CalculatePlayerMovement();
                        }
                    }
                }
                break;
            case CharacterState.Dead:
                return;


            case CharacterState.BeingHit:
                
                break;

            case CharacterState.Slide:
                _movementVeloctity = transform.forward * SlideSpeed * Time.deltaTime;
                break;
            
            case CharacterState.Spawn:
                currentSpawnTime -= Time.deltaTime;
                if(currentSpawnTime <= 0){
                    SwitchStateTo(CharacterState.Normal);
                }
                break;
        }
        if(impactOnCharacter.magnitude > 0.2f){
                _movementVeloctity = impactOnCharacter * Time.deltaTime;
            }
            impactOnCharacter = Vector3.Lerp(impactOnCharacter, Vector3.zero, Time.deltaTime * 5);

        if(IsPlayer){
            if(_cc.isGrounded == false){
            _verticalVelocity = Gravity;
            }else{
            _verticalVelocity = Gravity * 0.3f;

            }

            _movementVeloctity += _verticalVelocity * Vector3.up * Time.deltaTime;
            _cc.Move(_movementVeloctity);
            _movementVeloctity = Vector3.zero;
        }else {
            if(CurrentState != CharacterState.Normal){
                _cc.Move(_movementVeloctity);
                _movementVeloctity = Vector3.zero;
            }
        }
        

        
    }

    public void SwitchStateTo(CharacterState newState){

        if(IsPlayer){
            _playerInput.ClearCache();
        }

        
        // Exiting state
        switch(CurrentState){
            case CharacterState.Normal:
                break;
            case CharacterState.Attacking:

                if(_damageCaster !=null){
                    DisableDamageCaster();
                }

                if(IsPlayer){
                    GetComponent<PlayerVFXManager>().StopBlade();
                }
                break;
            case CharacterState.Dead:
                return;
            case CharacterState.BeingHit:
                break;
            case CharacterState.Slide:
                break;

            case CharacterState.Spawn:
                IsInvincible = false;
                break;

            
        }
//Entering State
        switch (newState){
            case CharacterState.Normal:
                break;
            case CharacterState.Attacking:
                if(!IsPlayer){
                    Quaternion newRotation = Quaternion.LookRotation(TargetPlayer.position - transform.position);
                    transform.rotation = newRotation;
                }
                _animator.SetTrigger("Attack");

                if(IsPlayer)
                {
                    AttackStartTime = Time.time;
                    RotateToCursor();
                }
                break;
            case CharacterState.Dead:
                _cc.enabled = false;
                _animator.SetTrigger("Dead");
                StartCoroutine(MaterialDissolve());
                
                if(!IsPlayer){
                    SkinnedMeshRenderer mesh = GetComponentInChildren<SkinnedMeshRenderer>();
                    mesh.gameObject.layer = 0; 
                }

                break;

            case CharacterState.BeingHit:
                _animator.SetTrigger("BeingHit");

                if(IsPlayer){
                    IsInvincible = true;
                    StartCoroutine(DelayCancelInvincible());
                }

                break;
            
            case CharacterState.Slide:
                _animator.SetTrigger("Slide");
                break;

            case CharacterState.Spawn:
                IsInvincible = true;
                currentSpawnTime = SpawnDuration;
                StartCoroutine(MaterialAppear());
                break;

        }

        CurrentState = newState;

        Debug.Log("Switched to "+ CurrentState);
    }

    public void SlideAnimationEnds(){
        SwitchStateTo(CharacterState.Normal);
    }

    public void AttackAnimationEnds(){
        SwitchStateTo(CharacterState.Normal);
    }

    public void BeingHitAnimationEnds(){
        SwitchStateTo(CharacterState.Normal);
    }

    public void ApplyDamage(int damage, Vector3 attackerPos = new Vector3()){
        if(IsInvincible){
            return;
        }
        if(_health!=null){
            _health.ApplyDamage(damage);
        }

        if(!IsPlayer){
            GetComponent<EnemyVFXManager>().PlayBeingHitVFX(attackerPos);
        }

        StartCoroutine(MaterialBlink());

        if(IsPlayer){
            SwitchStateTo(CharacterState.BeingHit);
            AddImpact(attackerPos, 10f);
        }else{
            AddImpact(attackerPos, 2.5f);
        }
    }

    IEnumerator DelayCancelInvincible(){
        yield return new WaitForSeconds(invincibleDuration);
        IsInvincible = false;
    }


    private void AddImpact(Vector3 attackerPos, float force){
        Vector3 impactDir = transform.position - attackerPos;
        impactDir.Normalize();
        impactDir.y = 0;
        impactOnCharacter = impactDir * force;
    }

    public void EnableDamageCaster(){
        _damageCaster.EnableDamageCaster();
    }

    public void DisableDamageCaster(){
        _damageCaster.DisableDamageCaster();
    }

    IEnumerator MaterialBlink(){
        _materialProperBlock.SetFloat("_blink", 0.4f);
        _skinnedMeshRenderer.SetPropertyBlock(_materialProperBlock);

        yield return new WaitForSeconds(0.2f);

        _materialProperBlock.SetFloat("_blink", 0f);
        _skinnedMeshRenderer.SetPropertyBlock(_materialProperBlock);
    }


    IEnumerator MaterialDissolve(){
        yield return new WaitForSeconds(2);

        float dissolveTimeDuration = 2f;
        float currentDissolveTime = 0;
        float dissolveHight_start = 20f;
        float dissolveHight_target = -10f;
        float dissolveHight;

        _materialProperBlock.SetFloat("_enableDissolve", 1f);
        _skinnedMeshRenderer.SetPropertyBlock(_materialProperBlock);

        while(currentDissolveTime < dissolveTimeDuration){

            currentDissolveTime += Time.deltaTime;
            dissolveHight = Mathf.Lerp(dissolveHight_start, dissolveHight_target, currentDissolveTime/dissolveTimeDuration);
            _materialProperBlock.SetFloat("_dissolve_height", dissolveHight);
            _skinnedMeshRenderer.SetPropertyBlock(_materialProperBlock);
            yield return null;           
        }

        DropItem();
    }

    public void DropItem(){
        if(ItemToDrop != null){
            Instantiate(ItemToDrop, transform.position, Quaternion.identity);
        }
    }

    public void PickUpItem(PickUp item){
        switch(item.Type){
            case PickUp.PickUpType.Heal:
                AddHealth(item.Value);
                break;
            case PickUp.PickUpType.Coin:
                AddCoin(item.Value);
                break;
        }
    }

    private void AddHealth(int health){
        _health.AddHealth(health);
        GetComponent<PlayerVFXManager>().PlayHealVFX();
    }

    private void AddCoin(int coin){
        Coin += coin;
    }

    public void RotateToTarget(){
        if(CurrentState != CharacterState.Dead){
            transform.LookAt(TargetPlayer, Vector3.up);
        }
    }

    IEnumerator MaterialAppear(){
        float dissolveTimeDuration = SpawnDuration;
        float currentDissolveTime = 0;
        float dissolveHight_start = -10f;
        float dissolveHight_target = 20f;
        float dissolveHight;

        _materialProperBlock.SetFloat("_enableDissolve", 1f);
        _skinnedMeshRenderer.SetPropertyBlock(_materialProperBlock);

        while(currentDissolveTime < dissolveTimeDuration){
            currentDissolveTime += Time.deltaTime;
            dissolveHight = Mathf.Lerp(dissolveHight_start, dissolveHight_target, currentDissolveTime/dissolveTimeDuration);
            _materialProperBlock.SetFloat("_dissolve_height", dissolveHight);
            _skinnedMeshRenderer.SetPropertyBlock(_materialProperBlock);
            yield return null;
        }

        _materialProperBlock.SetFloat("_enableDissolve", 0f);
        _skinnedMeshRenderer.SetPropertyBlock(_materialProperBlock);
    }
     private void OnDrawGizmos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitResult;

        if (Physics.Raycast(ray, out hitResult, 1000, 1 << LayerMask.NameToLayer("CursorTest")))
        {
            Vector3 cursorPos = hitResult.point;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cursorPos, 1);
        }
    }
     private void RotateToCursor()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitResult;

        if (Physics.Raycast(ray, out hitResult, 1000, 1 << LayerMask.NameToLayer("CursorTest")))
        {
            Vector3 cursorPos = hitResult.point;
            transform.rotation = Quaternion.LookRotation(cursorPos - transform.position, Vector3.up);
        }
    }
}
