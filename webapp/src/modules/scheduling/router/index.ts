export const schedulingRoutes = [
    {
        path: '',
        redirect: 'dashboard',
    },
    {
        path: 'dashboard',
        name: 'scheduling-dashboard',
        component: () => import('@/modules/scheduling/views/SchedulingDashboardView.vue'),
        meta: { title: 'Scheduling Dashboard' },
    },
    {
        path: 'jobs',
        name: 'jobs-board',
        component: () => import('@/modules/scheduling/views/JobsBoardView.vue'),
        meta: { title: 'Jobs Board' },
    },
    {
        path: 'resources',
        name: 'resources-board',
        component: () => import('@/modules/scheduling/views/ResourcesBoardView.vue'),
        meta: { title: 'Resources' },
    },
    {
        path: 'assignments',
        name: 'assignments',
        component: () => import('@/modules/scheduling/views/AssignmentsView.vue'),
        meta: { title: 'Assignments' },
    },
    {
        path: 'coverage',
        name: 'craft-coverage',
        component: () => import('@/modules/scheduling/views/CoverageView.vue'),
        meta: { title: 'Craft Coverage' },
    },
    {
        path: 'roll-off',
        name: 'roll-off',
        component: () => import('@/modules/scheduling/views/RollOffView.vue'),
        meta: { title: 'Ending Soon' },
    },
];
