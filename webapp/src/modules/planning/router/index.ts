export const planningRoutes = [
    {
        path: '',
        redirect: 'step-out-plans',
    },
    {
        path: 'step-out-plans',
        name: 'step-out-plan-list',
        component: () => import('@/modules/planning/views/StepOutPlanListView.vue'),
        meta: { title: 'Step-Out Plans' },
    },
    {
        path: 'step-out-plans/new',
        name: 'step-out-plan-new',
        component: () => import('@/modules/planning/views/StepOutPlanFormView.vue'),
        meta: { title: 'New Step-Out Plan' },
    },
    {
        path: 'step-out-plans/:id',
        name: 'step-out-plan-detail',
        component: () => import('@/modules/planning/views/StepOutPlanFormView.vue'),
        meta: { title: 'Step-Out Plan' },
    },
    {
        path: 'work-packages',
        name: 'work-package-list',
        component: () => import('@/modules/planning/views/WorkPackageListView.vue'),
        meta: { title: 'Work Packages' },
    },
    {
        path: 'fco',
        name: 'fco-list',
        component: () => import('@/modules/planning/views/FcoListView.vue'),
        meta: { title: 'FCO / Change Orders' },
    },
];
