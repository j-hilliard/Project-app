export { useResourceService } from './resourceService';
export { useAssignmentService } from './assignmentService';
export { useCoverageService } from './coverageService';

// Legacy aggregate — prefer importing from feature services directly
import { useResourceService } from './resourceService';
import { useAssignmentService } from './assignmentService';
import { useCoverageService } from './coverageService';

export function useSchedulingService() {
    return {
        ...useResourceService(),
        ...useAssignmentService(),
        ...useCoverageService(),
    };
}
