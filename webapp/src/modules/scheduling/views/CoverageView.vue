<template>
    <div class="coverage-view">
        <!-- Header -->
        <div class="coverage-header">
            <div>
                <h1>Craft Coverage</h1>
                <p>Demand vs assigned headcount by craft. Single-click to select — double-click to drill down.</p>
            </div>
            <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load coverage data. Make sure the API is running.
        </Message>

        <!-- KPI Strip -->
        <div class="kpi-strip">
            <div class="kpi-item">
                <span class="kpi-value" :class="{ 'kpi-warn': shortageCount > 0 }">
                    {{ loading ? '—' : shortageCount }}
                </span>
                <span class="kpi-label">Crafts Short</span>
            </div>
            <div class="kpi-div" />
            <div class="kpi-item">
                <span class="kpi-value" :class="{ 'kpi-warn': openPositions > 0 }">
                    {{ loading ? '—' : openPositions }}
                </span>
                <span class="kpi-label">Open Positions</span>
            </div>
            <div class="kpi-div" />
            <div class="kpi-item">
                <span class="kpi-value">{{ loading ? '—' : totalAssigned }}</span>
                <span class="kpi-label">Assigned Today</span>
            </div>
            <div class="kpi-div" />
            <div class="kpi-item">
                <span class="kpi-value" :class="overallCoverageClass">
                    {{ loading ? '—' : overallCoveragePct + '%' }}
                </span>
                <span class="kpi-label">Overall Coverage</span>
            </div>
        </div>

        <!-- Filter Bar -->
        <div class="filter-bar">
            <div class="demand-toggle-group">
                <span class="filter-label-sm">Demand</span>
                <div class="demand-toggle">
                    <button
                        v-for="opt in demandStateOptions"
                        :key="opt.value"
                        class="toggle-btn"
                        :class="{ active: demandState === opt.value }"
                        @click="demandState = opt.value">
                        {{ opt.label }}
                    </button>
                </div>
            </div>

            <Dropdown
                v-model="regionFilter"
                :options="regionOptions"
                optionLabel="label"
                optionValue="value"
                placeholder="All Regions"
                showClear
                class="filter-dropdown" />

            <Dropdown
                v-model="branchFilter"
                :options="branchOptions"
                optionLabel="label"
                optionValue="value"
                placeholder="All Branches"
                showClear
                class="filter-dropdown" />

            <span class="filter-hint">
                <i class="pi pi-info-circle" />
                Single click = select &nbsp;·&nbsp; Double click = drill down
            </span>
        </div>

        <!-- Coverage Bar Dashboard -->
        <div class="coverage-dashboard">
            <!-- Column labels -->
            <div v-if="displayRows.length" class="dashboard-col-labels">
                <span class="col-lbl-craft">Craft</span>
                <span class="col-lbl-bar">Coverage</span>
                <span class="col-lbl-stats">Demand / Assigned / Gap / %</span>
            </div>

            <!-- Skeleton loading -->
            <template v-if="loading && !displayRows.length">
                <div v-for="i in 7" :key="i" class="skeleton-row" :style="{ opacity: 1 - i * 0.1 }" />
            </template>

            <!-- Empty state -->
            <div v-else-if="!loading && !displayRows.length" class="empty-state">
                <i class="pi pi-chart-bar empty-icon" />
                <p>No coverage data available.</p>
                <p class="empty-sub">Ensure staffing plans are approved and resources have demand assigned.</p>
            </div>

            <!-- Bar rows -->
            <CraftCoverageBar
                v-for="row in displayRows"
                :key="row.craftCode"
                :craft="row"
                :selected="selectedCraft?.craftCode === row.craftCode"
                @click="selectCraft(row)"
                @dblclick="openDrawer(row)" />
        </div>

        <!-- Craft Detail Drawer -->
        <Sidebar
            v-model:visible="drawerVisible"
            position="right"
            :style="{ width: 'clamp(380px, 40vw, 520px)' }"
            class="craft-detail-sidebar">
            <template #header>
                <div class="sidebar-header">
                    <i class="pi pi-chart-bar" style="color: var(--primary-color)" />
                    <span>Craft Detail</span>
                </div>
            </template>
            <CraftDetailDrawer
                v-if="drawerCraft"
                :craft="drawerCraft"
                :suggestions="suggestionsForDrawer" />
        </Sidebar>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useSchedulingService } from '../services/useSchedulingService';
import CraftCoverageBar from '../components/CraftCoverageBar.vue';
import CraftDetailDrawer from '../components/CraftDetailDrawer.vue';

const { getCoverage, getSuggestedMatches } = useSchedulingService();

const loading = ref(false);
const error = ref(false);
const coverage = ref<any[]>([]);
const allMatches = ref<any[]>([]);

// ── Normalize API response shape ─────────────────────────────────────────────
// Existing endpoint: craftCode, craftTitle, demandCount, assignedCount, gap
// Future endpoint adds: craftName, demandTotal, assignedTotal, demandReleased, demandForecast

function normalize(row: any) {
    const demand = row.demandTotal ?? row.demandCount ?? 0;
    const assigned = row.assignedTotal ?? row.assignedCount ?? 0;
    const gap = Math.max(0, demand - assigned);
    const craftName = row.craftName ?? row.craftTitle ?? row.craftCode;
    return { ...row, demand, assigned, gap, craftName };
}

const normalizedRows = computed(() => coverage.value.map(normalize));

// ── Filters ──────────────────────────────────────────────────────────────────

const demandState = ref<'all' | 'released' | 'forecast'>('all');
const regionFilter = ref<string | null>(null);
const branchFilter = ref<string | null>(null);

const demandStateOptions = [
    { label: 'All',      value: 'all' },
    { label: 'Released', value: 'released' },
    { label: 'Forecast', value: 'forecast' },
];

const regionOptions = computed(() => {
    const regions = [...new Set(allMatches.value.map((m: any) => m.region).filter(Boolean))];
    return regions.map(r => ({ label: r, value: r }));
});

const branchOptions = computed(() => {
    const branches = [...new Set(allMatches.value.map((m: any) => m.branch).filter(Boolean))];
    return branches.map(b => ({ label: b, value: b }));
});

// ── Display rows: filtered + sorted by gap descending ────────────────────────

const displayRows = computed(() => {
    let rows = [...normalizedRows.value];

    // demandState filter: only applies when rows carry the field (future endpoint)
    if (demandState.value !== 'all') {
        const stateAware = rows.filter(r => r.demandState != null);
        if (stateAware.length) {
            rows = rows.filter(r => r.demandState === demandState.value);
        }
    }

    // Region / branch: cross-reference craft codes through allMatches
    if (regionFilter.value) {
        const regionCrafts = new Set(
            allMatches.value
                .filter((m: any) => m.region === regionFilter.value)
                .map((m: any) => m.craftCode)
                .filter(Boolean)
        );
        if (regionCrafts.size) rows = rows.filter(r => regionCrafts.has(r.craftCode));
    }

    if (branchFilter.value) {
        const branchCrafts = new Set(
            allMatches.value
                .filter((m: any) => m.branch === branchFilter.value)
                .map((m: any) => m.craftCode)
                .filter(Boolean)
        );
        if (branchCrafts.size) rows = rows.filter(r => branchCrafts.has(r.craftCode));
    }

    return rows.sort((a, b) => b.gap - a.gap);
});

// ── KPIs ─────────────────────────────────────────────────────────────────────

const shortageCount = computed(() => normalizedRows.value.filter(r => r.gap > 0).length);
const openPositions = computed(() =>
    normalizedRows.value.reduce((s, r) => s + Math.max(0, r.gap), 0)
);
const totalAssigned = computed(() =>
    normalizedRows.value.reduce((s, r) => s + (r.assigned ?? 0), 0)
);
const totalDemand = computed(() =>
    normalizedRows.value.reduce((s, r) => s + (r.demand ?? 0), 0)
);
const overallCoveragePct = computed(() => {
    if (!totalDemand.value) return 0;
    return Math.round((totalAssigned.value / totalDemand.value) * 100);
});
const overallCoverageClass = computed(() => {
    const pct = overallCoveragePct.value;
    if (pct >= 80) return 'kpi-ok';
    if (pct >= 40) return 'kpi-mid';
    return 'kpi-warn';
});

// ── Selection + Drawer ───────────────────────────────────────────────────────

const selectedCraft = ref<any>(null);
const drawerVisible = ref(false);
const drawerCraft = ref<any>(null);

function selectCraft(row: any) {
    selectedCraft.value = selectedCraft.value?.craftCode === row.craftCode ? null : row;
}

function openDrawer(row: any) {
    drawerCraft.value = row;
    selectedCraft.value = row;
    drawerVisible.value = true;
}

const suggestionsForDrawer = computed(() => {
    if (!drawerCraft.value) return [];
    return allMatches.value.filter((m: any) => m.craftCode === drawerCraft.value.craftCode);
});

// ── Data load ────────────────────────────────────────────────────────────────

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const [covData, matchData] = await Promise.all([getCoverage(), getSuggestedMatches()]);
        coverage.value = covData;
        allMatches.value = matchData;
    } catch {
        error.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
.coverage-view {
    max-width: 1100px;
    margin: 0 auto;
    padding: 1.5rem 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

/* Header */
.coverage-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    flex-wrap: wrap;
}
.coverage-header h1 {
    margin: 0 0 0.25rem;
    font-size: 1.5rem;
    font-weight: 700;
    color: var(--text-color);
}
.coverage-header p {
    margin: 0;
    color: var(--text-color-secondary);
    font-size: 0.88rem;
}

/* KPI Strip */
.kpi-strip {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    display: flex;
    align-items: center;
    padding: 1rem 1.5rem;
}
.kpi-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    flex: 1;
    padding: 0 1rem;
}
.kpi-div {
    width: 1px;
    height: 36px;
    background: var(--surface-border);
    flex-shrink: 0;
}
.kpi-value {
    font-size: 1.75rem;
    font-weight: 700;
    color: var(--text-color);
    line-height: 1;
}
.kpi-label {
    font-size: 0.68rem;
    color: var(--text-color-secondary);
    text-transform: uppercase;
    letter-spacing: 0.06em;
    margin-top: 0.25rem;
    text-align: center;
}
.kpi-warn { color: var(--red-500, #ef4444); }
.kpi-mid  { color: var(--yellow-500, #eab308); }
.kpi-ok   { color: var(--green-500, #22c55e); }

/* Filter bar */
.filter-bar {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    padding: 0.75rem 1.25rem;
}
.demand-toggle-group {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-shrink: 0;
}
.filter-label-sm {
    font-size: 0.72rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.06em;
    color: var(--text-color-secondary);
}
.demand-toggle {
    display: flex;
    background: var(--surface-ground);
    border-radius: 6px;
    padding: 2px;
    gap: 1px;
}
.toggle-btn {
    background: transparent;
    border: none;
    color: var(--text-color-secondary);
    font-size: 0.75rem;
    font-weight: 600;
    padding: 0.25rem 0.7rem;
    border-radius: 5px;
    cursor: pointer;
    transition: all 0.12s ease;
}
.toggle-btn:hover { color: var(--text-color); background: var(--surface-border); }
.toggle-btn.active { background: var(--primary-color); color: #fff; }

.filter-dropdown { width: 150px; font-size: 0.82rem; }
:deep(.filter-dropdown .p-dropdown-label) { font-size: 0.82rem; padding: 0.4rem 0.6rem; }
:deep(.filter-dropdown .p-dropdown) { height: 34px; }

.filter-hint {
    margin-left: auto;
    font-size: 0.75rem;
    color: var(--text-color-secondary);
    display: flex;
    align-items: center;
    gap: 0.35rem;
}

/* Dashboard */
.coverage-dashboard {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}
.dashboard-col-labels {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0 1.25rem;
    font-size: 0.65rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--text-color-secondary);
}
.col-lbl-craft { min-width: 220px; flex-shrink: 0; }
.col-lbl-bar   { flex: 1; }
.col-lbl-stats { min-width: 300px; text-align: right; flex-shrink: 0; }

/* Skeleton */
.skeleton-row {
    height: 58px;
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    animation: pulse 1.5s ease-in-out infinite;
}
@keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.4; }
}

/* Empty */
.empty-state {
    text-align: center;
    padding: 3rem 1rem;
    color: var(--text-color-secondary);
    font-size: 0.88rem;
}
.empty-icon {
    font-size: 2.5rem;
    opacity: 0.3;
    display: block;
    margin-bottom: 1rem;
}
.empty-sub { font-size: 0.8rem; opacity: 0.7; margin-top: 0.25rem; }

/* Sidebar header */
.sidebar-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-weight: 700;
    font-size: 0.95rem;
    color: var(--text-color);
}

/* Override PrimeVue Sidebar styles */
:deep(.craft-detail-sidebar .p-sidebar-header) {
    padding: 1rem 1.25rem 0.75rem;
    border-bottom: 1px solid var(--surface-border);
}
:deep(.craft-detail-sidebar .p-sidebar-content) {
    padding: 1rem 1.25rem;
    overflow-y: auto;
}
</style>
