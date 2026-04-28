import { apps } from '@/apps.ts';

export const staffingListRoutes = [
    {
        path: 'staffing-plans',
        name: `${apps.estimating.baseSlug}-staffing-plans`,
        component: () => import('@/modules/estimating/features/staffing-list/views/StaffingListView.vue'),
        meta: {
            title: 'Staffing Plans',
            breadcrumbItems: () => [{ label: apps.estimating.name, to: '/portal' }, { label: 'Staffing Plans' }],
        },
    },
];
