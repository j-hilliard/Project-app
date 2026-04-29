export { useProjectService } from './projectService';
export { useWorkOrderService } from './workOrderService';
export { useWorkPackageService } from './workPackageService';
export { useStepOutPlanService } from './stepOutPlanService';
export { useFcoService } from './fcoService';

// Legacy aggregate — prefer importing from feature services directly
import { useProjectService } from './projectService';
import { useWorkOrderService } from './workOrderService';
import { useWorkPackageService } from './workPackageService';
import { useStepOutPlanService } from './stepOutPlanService';
import { useFcoService } from './fcoService';

export function usePlanningService() {
    return {
        ...useProjectService(),
        ...useWorkOrderService(),
        ...useWorkPackageService(),
        ...useStepOutPlanService(),
        ...useFcoService(),
    };
}
