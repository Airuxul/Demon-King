using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using UnityEngine;

namespace AI.Battle
{
    public class BattleAI : MonoBehaviour
    {
        [SerializeField] 
        private BehaviorTree tree;

        public bool findTarget;

        public int curHp = 100;
        
        private void Start()
        {
            var injectTree = new BehaviorTreeBuilder(gameObject)
                .Selector()
                    .Do("Eat Action", () =>
                    {
                        Debug.Log("Eat");
                        return TaskStatus.Success;
                    })
                .End();

            var battleTree = new BehaviorTreeBuilder(gameObject)
                .Selector()
                    .Sequence()
                        .Condition("Is Low Hp", () => curHp < 10)
                        .Do("Run Away", () =>
                        {
                            Debug.Log("Run Away");
                            return TaskStatus.Success;
                        })
                    .End()
                    .Sequence()
                        .Condition("Custom Condition", () =>
                        {
                            return findTarget;
                        })
                        .Do("Custom Action", () =>
                        {
                            Debug.Log("Attack");
                            return TaskStatus.Success;
                        })
                    .End()
                    .Splice(injectTree.Build())
                .End()
                .Build();

            tree = battleTree;
        }

        private void Update()
        {
            tree.Tick();
        }
    }
}
