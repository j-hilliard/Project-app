import { apps } from '@/apps.ts';

export const estimateFormRoutes = [
    {
        path: 'estimates/new',
        name: `${apps.estimating.baseSlug}-estimate-new`,
        component: () => import('@/modules/estimating/features/estimate-form/views/EstimateFormView.vue'),
        meta: {
            title: 'New Estimate',
            breadcrumbItems: () => [
                { label: apps.estimating.name, to: '/portal' },
                { label: 'Estimates', to: '/estimating/estimates' },
                { label: 'New' },
            ],
        },
    },
    {
        path: 'estimates/:id',
        name: `${apps.estimating.baseSlug}-estimate-edit`,
        component: () => import('@/modules/estimating/features/estimate-form/views/EstimateFormView.vue'),
        meta: {
            title: 'Edit Estimate',
            breadcrumbItems: () => [
                { label: apps.estimating.name, to: '/portal' },
                { label: 'Estimates', to: '/estimating/estimates' },
                { label: 'Edit' },
            ],
        },
    },
];
