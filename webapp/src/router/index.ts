import { apps } from '@/apps.ts';
import { useAppStore } from '@/stores/appStore.ts';
import { createRouter, createWebHistory } from 'vue-router';
import { estimatingRoutes } from '@/modules/estimating/router';
import { planningRoutes } from '@/modules/planning/router';
import { schedulingRoutes } from '@/modules/scheduling/router';
import { isAuthenticated, hasPendingCompanySelection } from '@/services/authService';
import NProgress from 'nprogress';
import 'nprogress/nprogress.css';

const PUBLIC_PATHS = ['/login'];

const routes = [
    // Public — no auth required
    {
        path: '/login',
        name: 'login',
        component: () => import('@/views/auth/LoginView.vue'),
        meta: { public: true, title: 'Sign In' },
    },

    // Company selection — requires completed step 1 (temp token)
    {
        path: '/company-select',
        name: 'company-select',
        component: () => import('@/views/auth/CompanySelectView.vue'),
        meta: { public: false, companySelect: true, title: 'Select Company' },
    },

    // Default redirect → portal home
    {
        path: '/',
        redirect: `/${apps.portal.baseSlug}`,
    },

    // Portal (platform home / launcher)
    {
        path: `/${apps.portal.baseSlug}`,
        component: () => import('@/layout/AppLayout.vue'),
        children: [
            {
                path: '',
                name: 'portal-dashboard',
                component: () => import('@/modules/portal/views/PortalDashboardView.vue'),
                meta: { title: 'Home' },
            },
        ],
    },

    // Estimating
    {
        path: `/${apps.estimating.baseSlug}`,
        component: () => import('@/layout/AppLayout.vue'),
        children: estimatingRoutes,
    },

    // Planning / PM
    {
        path: `/${apps.planning.baseSlug}`,
        component: () => import('@/layout/AppLayout.vue'),
        children: planningRoutes,
    },

    // Scheduling
    {
        path: `/${apps.scheduling.baseSlug}`,
        component: () => import('@/layout/AppLayout.vue'),
        children: schedulingRoutes,
    },

    // Global Analytics (cross-company, Admins + Analytics role only)
    {
        path: '/global-analytics',
        component: () => import('@/layout/AppLayout.vue'),
        children: [
            {
                path: '',
                name: 'global-analytics',
                component: () => import('@/modules/global-analytics/views/GlobalAnalyticsView.vue'),
                meta: { title: 'Global Analytics' },
            },
        ],
    },

];

const router = createRouter({
    routes,
    history: createWebHistory(),
    scrollBehavior() {
        return { left: 0, top: 0 };
    },
});

router.beforeEach((to, from, next) => {
    NProgress.start();

    // Trim trailing slash
    if (to.path !== '/' && to.path.endsWith('/')) {
        return next({ path: to.path.slice(0, -1), query: to.query, hash: to.hash });
    }

    // Allow public routes through
    const isPublic = to.meta.public === true || PUBLIC_PATHS.includes(to.path);
    if (isPublic) return next();

    // Company select screen: needs a valid temp token (or already fully authed)
    if (to.meta.companySelect) {
        if (isAuthenticated() || hasPendingCompanySelection()) return next();
        return next({ path: '/login' });
    }

    // All other protected routes require full authentication (company selected)
    if (!isAuthenticated()) {
        // If they have a pending company selection, send them there
        if (hasPendingCompanySelection()) {
            return next({ path: '/company-select' });
        }
        return next({ path: '/login', query: { redirect: to.fullPath } });
    }

    next();
});

router.afterEach(to => {
    NProgress.done();
    const appStore = useAppStore();
    const routeTitle = to.meta.title ? `${to.meta.title} - ` : '';
    const appName = appStore.currentApp.name || 'Stronghold Enterprise Estimating';
    document.title = routeTitle + appName;
});

router.onError(() => {
    NProgress.done();
});

export default router;
