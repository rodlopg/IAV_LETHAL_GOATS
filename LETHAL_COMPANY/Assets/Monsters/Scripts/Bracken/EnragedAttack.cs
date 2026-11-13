using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.Bracken
{
    public class EnragedAttack : State
    {
        private float stateTimer;
        private float attackTime;
        private bool attackInvoked;

        public EnragedAttack(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.BRACKEN_ENRAGED_ATTACK;
        }

        public override void Enter()
        {
            SetTrigger("isEnraged");
            settings.ResetBrackenStare();
            settings.ConsumeAttackedFlag();
            stateTimer = 0f;
            attackTime = -1f;
            attackInvoked = false;
            if (AgentReady())
            {
                agent.speed = settings.brackenAttackSpeed;
                agent.isStopped = false;
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            stateTimer += Time.deltaTime;

            if (player == null || stateTimer >= settings.brackenAttackDuration)
            {
                TransitionTo(new Stalking(npc, agent, animator, player));
                return;
            }

            if (AgentReady())
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }

            float distance = Vector3.Distance(npc.transform.position, player.position);
            if (!attackInvoked && distance <= settings.brackenAttackDistance)
            {
                attackInvoked = true;
                attackTime = stateTimer;
                SetAgentStopped(true);
                settings.InvokeBrackenAttack();
            }

            if (attackInvoked && stateTimer - attackTime >= settings.brackenPostAttackPause)
                TransitionTo(new Stalking(npc, agent, animator, player));
        }

        public override void Exit()
        {
            ResetTrigger("isEnraged");
            base.Exit();
        }
    }
}
