<template>
    <div class="portal-dashboard">
        <!-- Header -->
        <div class="portal-hero">
            <div class="portal-hero-text">
                <h1>Stronghold Platform</h1>
                <p class="portal-tagline">Welcome back, {{ userName }}</p>
            </div>
            <div class="portal-company-badge">
                <i class="pi pi-building" />
                <span>{{ companyName }}</span>
            </div>
        </div>

        <!-- App Launcher Tiles -->
        <div class="portal-app-grid">
            <div
                v-for="tile in appTiles"
                :key="tile.slug"
                class="portal-app-tile"
                @click="router.push(tile.path)"
            >
                <div class="portal-tile-icon">
                    <i :class="tile.icon" />
                </div>
                <div class="portal-tile-body">
                    <h3>{{ tile.name }}</h3>
                    <p>{{ tile.description }}</p>
                    <div class="portal-tile-stat" v-if="tile.stat !== undefined">
                        <span class="portal-stat-value" :class="tile.statClass">{{ tile.stat }}</span>
                        <span class="portal-stat-label">{{ tile.statLabel }}</span>
                    </div>
                </div>
                <div class="portal-tile-arrow">
                    <i class="pi pi-arrow-right" />
                </div>
            </div>
        </div>

        <!-- KPI Summary Strip -->
        <div class="portal-kpi-strip" v-if="dashboardData">
            <div class="portal-kpi-item">
                <span class="portal-kpi-value">{{ dashboardData.openEstimateCount }}</span>
                <span class="portal-kpi-label">Open Estimates</span>
            </div>
            <div class="portal-kpi-divider" />
            <div class="portal-kpi-item" :class="{ 'portal-kpi-warn': dashboardData.craftShortagesCount > 0 }">
                <span class="portal-kpi-value">{{ dashboardData.craftShortagesCount }}</span>
                <span class="portal-kpi-label">Craft Shortages</span>
            </div>
            <div class="portal-kpi-divider" />
            <div class="portal-kpi-item" :class="{ 'portal-kpi-warn': dashboardData.jobsEndingSoonCount > 0 }">
                <span class="portal-kpi-value">{{ dashboardData.jobsEndingSoonCount }}</span>
                <span class="portal-kpi-label">Jobs Ending Soon</span>
            </div>
            <div class="portal-kpi-divider" />
            <div class="portal-kpi-item">
                <span class="portal-kpi-value">{{ dashboardData.peopleFreeSoonCount }}</span>
                <span class="portal-kpi-label">People Free Soon</span>
            </div>
            <div class="portal-kpi-divider" />
            <div class="portal-kpi-item" :class="{ 'portal-kpi-warn': dashboardData.pendingFcoCount > 0 }">
                <span class="portal-kpi-value">{{ dashboardData.pendingFcoCount }}</span>
                <span class="portal-kpi-label">Pending FCOs</span>
            </div>
            <div class="portal-kpi-divider" />
            <div class="portal-kpi-item" :class="{ 'portal-kpi-alert': dashboardData.alertCount > 0 }">
                <span class="portal-kpi-value">{{ dashboardData.alertCount }}</span>
                <span class="portal-kpi-label">Alerts</span>
            </div>
        </div>

        <!-- Recent Estimates -->
        <div class="portal-recent" v-if="dashboardData?.recentEstimates?.length">
            <h2 class="portal-section-title">
                <i class="pi pi-clock" />
                Recent Estimates
            </h2>
            <div class="portal-recent-list">
                <div
                    v-for="est in dashboardData.recentEstimates"
                    :key="est.estimateId"
                    class="portal-recent-item"
                    @click="router.push(`/estimating/estimates/${est.estimateId}`)"
                >
                    <div class="portal-recent-number">{{ est.estimateNumber }}</div>
                    <div class="portal-recent-info">
                        <span class="portal-recent-name">{{ est.name }}</span>
                        <span class="portal-recent-client">{{ est.client }}</span>
                    </div>
                    <div class="portal-recent-status">
                        <span :class="statusClass(est.status)">{{ est.status }}</span>
                    </div>
                    <div class="portal-recent-arrow">
                        <i class="pi pi-chevron-right" />
                    </div>
                </div>
            </div>
        </div>

        <!-- Loading / Error -->
        <div v-if="loading" class="portal-loading">
            <i class="pi pi-spin pi-spinner" />
            <span>Loading platform data...</span>
        </div>
        <div v-if="error" class="portal-error">
            <i class="pi pi-exclamation-triangle" />
            <span>Could not load dashboard data. The API may be starting up.</span>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useUserStore } from '@/stores/userStore';
import { apps } from '@/apps';
import axios from 'axios';

const router = useRouter();
const userStore = useUserStore();

const loading = ref(false);
const error = ref(false);
const dashboardData = ref<PortalDashboardDto | null>(null);

interface RecentEstimate {
    estimateId: number;
    estimateNumber: string;
    name: string;
    client: string;
    status: string;
}

interface PortalDashboardDto {
    openEstimateCount: number;
    pendingFcoCount: number;
    craftShortagesCount: number;
    jobsEndingSoonCount: number;
    peopleFreeSoonCount: number;
    alertCount: number;
    recentEstimates: RecentEstimate[];
}

const userName = computed(() => userStore.username || 'there');
const companyName = computed(() => userStore.companyName || userStore.companyCode || '');

const appTiles = computed(() => [
    {
        slug: 'estimating',
        name: apps.estimating.name,
        description: apps.estimating.description,
        icon: apps.estimating.icon,
        path: '/estimating/estimates',
        stat: dashboardData.value?.openEstimateCount,
        statLabel: 'open',
        statClass: '',
    },
    {
        slug: 'planning',
        name: apps.planning.name,
        description: apps.planning.description,
        icon: apps.planning.icon,
        path: '/planning/step-out-plans',
        stat: dashboardData.value?.pendingFcoCount,
        statLabel: 'pending FCOs',
        statClass: dashboardData.value?.pendingFcoCount ? 'portal-stat-warn' : '',
    },
    {
        slug: 'scheduling',
        name: apps.scheduling.name,
        description: apps.scheduling.description,
        icon: apps.scheduling.icon,
        path: '/scheduling/dashboard',
        stat: dashboardData.value?.craftShortagesCount,
        statLabel: 'craft shortages',
        statClass: (dashboardData.value?.craftShortagesCount ?? 0) > 0 ? 'portal-stat-warn' : '',
    },
]);

function statusClass(status: string) {
    const s = status?.toLowerCase();
    if (s === 'awarded') return 'portal-status portal-status-awarded';
    if (s === 'pending' || s === 'submitted') return 'portal-status portal-status-pending';
    if (s === 'lost' || s === 'canceled') return 'portal-status portal-status-lost';
    return 'portal-status portal-status-draft';
}

async function loadDashboard() {
    loading.value = true;
    error.value = false;
    try {
        const token = localStorage.getItem('auth_token');
        const { data } = await axios.get('/api/v1/portal/dashboard', {
            headers: token ? { Authorization: `Bearer ${token}` } : {},
        });
        dashboardData.value = data;
    } catch {
        error.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(loadDashboard);
</script>

<style scoped>
.portal-dashboard {
    max-width: 1100px;
    margin: 0 auto;
    padding: 1.5rem 0;
    display: flex;
    flex-direction: column;
    gap: 2rem;
}

/* Hero */
.portal-hero {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 1rem;
}
.portal-hero h1 {
    font-size: 1.75rem;
    font-weight: 700;
    margin: 0;
    color: var(--text-color);
}
.portal-tagline {
    margin: 0.25rem 0 0;
    color: var(--text-color-secondary);
    font-size: 0.95rem;
}
.portal-company-badge {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 2rem;
    padding: 0.4rem 1rem;
    font-size: 0.85rem;
    font-weight: 600;
    color: var(--text-color-secondary);
}

/* App Tiles */
.portal-app-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 1.25rem;
}
@media (max-width: 768px) {
    .portal-app-grid { grid-template-columns: 1fr; }
}
.portal-app-tile {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 12px;
    padding: 1.5rem;
    cursor: pointer;
    transition: all 0.18s ease;
    display: flex;
    align-items: flex-start;
    gap: 1rem;
    position: relative;
}
.portal-app-tile:hover {
    border-color: var(--primary-color);
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.12);
    transform: translateY(-2px);
}
.portal-tile-icon {
    width: 48px;
    height: 48px;
    border-radius: 10px;
    background: var(--primary-color);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}
.portal-tile-icon i {
    font-size: 1.4rem;
    color: #fff;
}
.portal-tile-body {
    flex: 1;
    min-width: 0;
}
.portal-tile-body h3 {
    margin: 0 0 0.25rem;
    font-size: 1rem;
    font-weight: 700;
    color: var(--text-color);
}
.portal-tile-body p {
    margin: 0 0 0.5rem;
    font-size: 0.8rem;
    color: var(--text-color-secondary);
    line-height: 1.4;
}
.portal-tile-stat {
    display: flex;
    align-items: baseline;
    gap: 0.35rem;
}
.portal-stat-value {
    font-size: 1.25rem;
    font-weight: 700;
    color: var(--primary-color);
}
.portal-stat-value.portal-stat-warn {
    color: var(--orange-500, #f97316);
}
.portal-stat-label {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
}
.portal-tile-arrow {
    color: var(--text-color-secondary);
    opacity: 0.4;
    transition: opacity 0.15s;
    padding-top: 0.1rem;
}
.portal-app-tile:hover .portal-tile-arrow {
    opacity: 1;
    color: var(--primary-color);
}

/* KPI Strip */
.portal-kpi-strip {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    display: flex;
    align-items: center;
    padding: 1rem 1.5rem;
    gap: 0;
    flex-wrap: wrap;
}
.portal-kpi-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 0 1.5rem;
    flex: 1;
    min-width: 90px;
}
.portal-kpi-divider {
    width: 1px;
    height: 36px;
    background: var(--surface-border);
    flex-shrink: 0;
}
.portal-kpi-value {
    font-size: 1.5rem;
    font-weight: 700;
    color: var(--text-color);
    line-height: 1;
}
.portal-kpi-label {
    font-size: 0.72rem;
    color: var(--text-color-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
    margin-top: 0.25rem;
    text-align: center;
}
.portal-kpi-warn .portal-kpi-value { color: var(--orange-500, #f97316); }
.portal-kpi-alert .portal-kpi-value { color: var(--red-500, #ef4444); }

/* Recent Estimates */
.portal-section-title {
    font-size: 0.9rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--text-color-secondary);
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin: 0 0 0.75rem;
}
.portal-recent-list {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}
.portal-recent-item {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 0.75rem 1rem;
    display: flex;
    align-items: center;
    gap: 1rem;
    cursor: pointer;
    transition: background 0.15s;
}
.portal-recent-item:hover {
    background: var(--surface-hover);
}
.portal-recent-number {
    font-size: 0.78rem;
    font-weight: 600;
    color: var(--primary-color);
    min-width: 110px;
    font-family: 'Courier New', monospace;
}
.portal-recent-info {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.1rem;
}
.portal-recent-name {
    font-size: 0.88rem;
    font-weight: 600;
    color: var(--text-color);
}
.portal-recent-client {
    font-size: 0.78rem;
    color: var(--text-color-secondary);
}
.portal-recent-status { min-width: 90px; text-align: right; }
.portal-recent-arrow { color: var(--text-color-secondary); opacity: 0.4; }

/* Status badges */
.portal-status {
    font-size: 0.72rem;
    font-weight: 700;
    padding: 0.2rem 0.6rem;
    border-radius: 4px;
    text-transform: uppercase;
    letter-spacing: 0.04em;
}
.portal-status-awarded { background: #dcfce7; color: #166534; }
.portal-status-pending { background: #fef9c3; color: #854d0e; }
.portal-status-lost { background: #fee2e2; color: #991b1b; }
.portal-status-draft { background: var(--surface-ground); color: var(--text-color-secondary); }

/* Loading / Error */
.portal-loading,
.portal-error {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 1rem;
    border-radius: 8px;
    font-size: 0.88rem;
    color: var(--text-color-secondary);
}
.portal-error {
    background: #fff7ed;
    color: #9a3412;
    border: 1px solid #fed7aa;
}
</style>
