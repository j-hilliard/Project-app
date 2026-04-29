export const planningRoutes = [
    {
        path: '',
        redirect: 'projects',
    },
    // ── Projects ──────────────────────────────────────────────────────────
    {
        path: 'projects',
        name: 'project-list',
        component: () => import('@/modules/planning/views/ProjectListView.vue'),
        meta: { title: 'Projects' },
    },
    {
        path: 'projects/:id',
        name: 'project-detail',
        component: () => import('@/modules/planning/views/ProjectDetailView.vue'),
        meta: { title: 'Project' },
    },
    // ── Work Orders ───────────────────────────────────────────────────────
    {
        path: 'work-orders',
        name: 'work-order-list',
        component: () => import('@/modules/planning/views/WorkOrderListView.vue'),
        meta: { title: 'Work Orders' },
    },
    {
        path: 'work-orders/:id',
        name: 'work-order-detail',
        component: () => import('@/modules/planning/views/WorkOrderDetailView.vue'),
        meta: { title: 'Work Order' },
    },
    // ── Step-Out Plans (existing) ─────────────────────────────────────────
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
    // ── Work Packages (existing) ──────────────────────────────────────────
    {
        path: 'work-packages',
        name: 'work-package-list',
        component: () => import('@/modules/planning/views/WorkPackageListView.vue'),
        meta: { title: 'Work Packages' },
    },
    {
        path: 'work-packages/:id',
        name: 'work-package-detail',
        component: () => import('@/modules/planning/views/WorkPackageDetailView.vue'),
        meta: { title: 'Work Package' },
    },
    // ── FCO / Change Orders (existing) ────────────────────────────────────
    {
        path: 'fco',
        name: 'fco-list',
        component: () => import('@/modules/planning/views/FcoListView.vue'),
        meta: { title: 'FCO / Change Orders' },
    },
];
