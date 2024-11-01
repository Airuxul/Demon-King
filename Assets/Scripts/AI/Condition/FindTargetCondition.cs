using CleverCrow.Fluid.BTs.Tasks;

namespace AI.Condition
{
    public class FindTargetCondition : ConditionBase
    {
        protected override void OnInit()
        {
            Name = "Find Target";
        }

        protected override bool OnUpdate()
        {
            return false;
        }
    }
}