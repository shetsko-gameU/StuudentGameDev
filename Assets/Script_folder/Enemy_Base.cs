//using System.Numerics;
using UnityEngine;


public class Enemy_Base : MonoBehaviour
{
   [Header("Stats")]
    public StatsManager stats;
    public StatsManager Player_Stats;
    public Animator animator;
    Vector2 movevalue;


    [SerializeField] Enemy_State_Machine stateMachine {get; set;}
    [SerializeField] Enemy_Idle idleState { get; set; }
    [SerializeField] Enemy_Attack attackState { get; set; }
    [SerializeField] Enemy_Move moveState { get; set; }

    void Awake()
    {
        stateMachine = new Enemy_State_Machine();
        // construct state instances and pass references they need
        idleState = new Enemy_Idle(this, stateMachine);
        // Create constructed state instances
        attackState = new Enemy_Attack(this, stateMachine);
        moveState = new Enemy_Move(this, stateMachine);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stateMachine.Initialize(idleState);
    }

    // Update is called once per frame
    void Update()
    {
        stateMachine.CurrentEnemyState?.FrameUpdate();
    }
    void FixedUpdate()
    {
        stateMachine?.CurrentEnemyState?.PhysicsUpdate();
    }
    void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {

    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player_Attack")
        {
            stats.currentHealth -= Player_Stats.Attack;
        }
    }
    public void OnAttack()
    {
        animator.SetTrigger("Attack");


    }
    /*public void OnMove(InputAction.CallbackContext context)
    {
        movevalue = context.ReadValue<Vector2>();
        if (movevalue.magnitude != 0)
        {
        }
    }*/
}
