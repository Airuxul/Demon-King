using System;
using AI.Condition;
using CleverCrow.Fluid.BTs.Trees;

namespace AI
{
    // Condition
    public static partial class BehaviorTreeBuilderExtensions
    {
        public static BehaviorTreeBuilder FindTargetCondition(this BehaviorTreeBuilder builder, string name, Func<bool> action) {
            return builder.AddNode(new FindTargetCondition());
        }
    }
}