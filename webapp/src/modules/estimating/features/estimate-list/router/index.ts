import { apps } from '@/apps.ts';

export const estimateListRoutes = [
    {
        path: 'estimates',
        name: `${apps.estimating.baseSlug}-estimates`,
        component: () => import('@/modules/estimating/features/estimate-list/views/EstimateListView.vue'),
        meta: {
            title: 'Estimates',
            breadcrumbItems: () => [{ label: apps.estimating.name, to: '/portal' }, { label: 'Estimates' }],
        },
    },
];
