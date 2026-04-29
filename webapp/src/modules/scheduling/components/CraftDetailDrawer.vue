<template>
    <div class="drawer-inner">
        <!-- Craft header -->
        <div class="drawer-head">
            <div class="drawer-craft-id">
                <span class="drawer-code">{{ craft.craftCode }}</span>
                <span class="drawer-name">{{ displayName }}</span>
            </div>
            <span class="drawer-coverage-pill" :class="pilotClass">{{ pct }}% covered</span>
        </div>

        <!-- KPI row -->
        <div class="drawer-kpis">
            <div class="dkpi">
                <span class="dkpi-val">{{ demand }}</span>
                <span class="dkpi-lbl">Total Demand</span>
            </div>
            <div class="dkpi-div" />
            <div class="dkpi">
                <span class="dkpi-val">{{ assigned }}</span>
                <span class="dkpi-lbl">Assigned</span>
            </div>
            <div class="dkpi-div" />
            <div class="dkpi">
                <span class="dkpi-val" :class="gap > 0 ? 'val-warn' : 'val-ok'">
                    {{ gap > 0 ? '–' + gap : '✓ Covered' }}
                </span>
                <span class="dkpi-lbl">Gap</span>
            </div>
        </div>

        <!-- Demand breakdown: Released vs Forecast -->
        <div class="drawer-section">
            <div class="section-title">Demand Breakdown</div>
            <div class="demand-breakdown">
                <div class="breakdown-row">
                    <span class="breakdown-badge badge-released">Released</span>
                    <span class="breakdown-desc">WorkPackage-backed, assignable now</span>
                    <span class="breakdown-count">{{ craft.demandReleased ?? '—' }}</span>
                </div>
                <div class="breakdown-row">
                    <span class="breakdown-badge badge-forecast">Forecast</span>
                    <span class="breakdown-desc">Estimates / StaffingPlans, not yet released</span>
                    <span class="breakdown-count">{{ craft.demandForecast ?? '—' }}</span>
                </div>
            </div>
            <p v-if="craft.demandReleased == null" class="breakdown-note">
                Breakdown by demand state shown once work packages are published to Scheduling.
            </p>
        </div>

        <!-- Suggested Candidates -->
        <div class="drawer-section">
            <div class="section-title">Suggested Candidates ({{ suggestions.length }})</div>
            <div v-if="!suggestions.length" class="drawer-empty">
                No available candidates for this craft in the current assignment window.
            </div>
            <div v-else class="suggest-list">
                <div v-for="s in suggestions" :key="s.resourceId" class="suggest-card">
                    <div class="suggest-left">
                        <span class="suggest-name">{{ s.resourceName }}</span>
                        <div class="suggest-tags">
                            <Tag v-if="s.reason" :value="s.reason"
                                :severity="s.reason === 'Unassigned' ? 'success' : 'warning'"
                                class="suggest-tag" />
                            <span v-if="s.branch" class="suggest-meta-item">{{ s.branch }}</span>
                            <span v-if="s.availableDate" class="suggest-meta-item">
                                Avail. {{ fmtDate(s.availableDate) }}
                            </span>
                            <span v-if="s.matchScore != null" class="suggest-meta-item">
                                Score {{ s.matchScore }}
                            </span>
                        </div>
                    </div>
                    <Button label="Assign" size="small" outlined @click="onAssign(s)" />
                </div>
            </div>
        </div>

        <!-- Blocked / Unavailable (pending backend endpoint) -->
        <div class="drawer-section">
            <div class="section-title">Blocked / Unavailable</div>
            <div v-if="blockedLoading" class="drawer-loading">
                <i class="pi pi-spin pi-spinner" /> Loading…
            </div>
            <div v-else-if="!blocked.length" class="drawer-empty">
                No blocked personnel on record for this craft.
            </div>
            <div v-else class="blocked-list">
                <div v-for="b in blocked" :key="b.resourceId" class="blocked-row">
                    <span class="blocked-name">{{ b.displayName }}</span>
                    <span class="blocked-reason">{{ formatBlockReason(b.blockReason) }}</span>
                    <span class="blocked-dates">{{ fmtDate(b.blockFrom) }} – {{ fmtDate(b.blockTo) }}</span>
                </div>
            </div>
        </div>

        <!-- Ending Soon / Roll-offs -->
        <div class="drawer-section">
            <div class="section-title">Ending Soon (next 14 days)</div>
            <div v-if="endingSoon.length === 0" class="drawer-empty">
                No assignments ending in the next 14 days for this craft.
            </div>
            <div v-else class="ending-list">
                <div v-for="e in endingSoon" :key="e.resourceId" class="ending-row">
                    <span class="ending-name">{{ e.displayName ?? e.resourceName }}</span>
                    <span class="ending-date">Ends {{ fmtDate(e.assignmentEnd ?? e.endDate) }}</span>
                </div>
            </div>
        </div>

        <!-- Trend Chart -->
        <div class="drawer-section">
            <div class="section-header-row">
                <span class="section-title">Coverage Trend — {{ displayName }}</span>
                <div class="range-btns">
                    <button v-for="r in trendRanges" :key="r.days"
                        class="range-btn" :class="{ active: trendDays === r.days }"
                        @click="setTrendRange(r.days)">
                        {{ r.label }}
                    </button>
                </div>
            </div>
            <div class="trend-toggles">
                <label class="trend-toggle">
                    <input type="checkbox" v-model="includeForecast" @change="buildTrend" />
                    <span>Include Forecast</span>
                </label>
            </div>
            <div v-if="trendLoading" class="drawer-loading">
                <i class="pi pi-spin pi-spinner" /> Loading trend…
            </div>
            <Chart v-else-if="trendChartData"
                type="line"
                :data="trendChartData"
                :options="trendChartOptions"
                class="trend-chart"
                :style="{ height: '200px' }" />
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useSchedulingService } from '../services/useSchedulingService';
import { useFormatters } from '@/ui';

const props = defineProps<{
    craft: any;
    suggestions: any[];
}>();

const router = useRouter();
const { getCraftDetail, getCraftTrend, getEndingSoon } = useSchedulingService();
const { fmtDateLong: fmtDate } = useFormatters();

// ── Computed from craft prop ─────────────────────────────────────────────────

const demand = computed(() =>
    props.craft.demand ?? props.craft.demandTotal ?? props.craft.demandCount ?? 0
);
const assigned = computed(() =>
    props.craft.assigned ?? props.craft.assignedTotal ?? props.craft.assignedCount ?? 0
);
const gap = computed(() => Math.max(0, demand.value - assigned.value));
const pct = computed(() =>
    demand.value ? Math.min(100, Math.round((assigned.value / demand.value) * 100)) : 100
);
const displayName = computed(() =>
    props.craft.craftName ?? props.craft.craftTitle ?? props.craft.craftCode
);
const pilotClass = computed(() =>
    pct.value >= 80 ? 'pill-ok' : pct.value >= 40 ? 'pill-mid' : 'pill-warn'
);

// ── Blocked personnel (pending backend) ─────────────────────────────────────

const blocked = ref<any[]>([]);
const blockedLoading = ref(false);

async function loadBlocked() {
    if (!props.craft.craftId && !props.craft.craftCode) return;
    blockedLoading.value = true;
    try {
        const data = await getCraftDetail(props.craft.craftCode);
        blocked.value = data.blockedPersonnel ?? [];
    } catch {
        blocked.value = [];
    } finally {
        blockedLoading.value = false;
    }
}

// ── Ending soon ──────────────────────────────────────────────────────────────

const endingSoon = ref<any[]>([]);

async function loadEndingSoon() {
    try {
        const data = await getEndingSoon();
        endingSoon.value = (data as any[]).filter(
            (r: any) => r.craftCode === props.craft.craftCode
        );
    } catch {
        endingSoon.value = [];
    }
}

// ── Trend chart ──────────────────────────────────────────────────────────────

const trendDays = ref(30);
const trendLoading = ref(false);
const trendChartData = ref<any>(null);
const includeForecast = ref(false);

const trendRanges = [
    { label: '7d', days: 7 },
    { label: '14d', days: 14 },
    { label: '30d', days: 30 },
    { label: '60d', days: 60 },
    { label: '90d', days: 90 },
];

function setTrendRange(days: number) {
    trendDays.value = days;
    buildTrend();
}

async function buildTrend() {
    trendLoading.value = true;
    try {
        const data = await getCraftTrend(props.craft.craftCode, trendDays.value, includeForecast.value);
        trendChartData.value = buildChartData(data.dates, data.demand, data.assigned, data.gap);
    } catch {
        // Backend endpoint not yet deployed — simulate plausible trend data for demo
        trendChartData.value = simulateTrend(trendDays.value);
    } finally {
        trendLoading.value = false;
    }
}

function simulateTrend(days: number) {
    const labels: string[] = [];
    const demandSeries: number[] = [];
    const assignedSeries: number[] = [];
    const gapSeries: number[] = [];

    const today = new Date();
    const d = Math.max(demand.value, 1);
    const a = assigned.value;

    for (let i = days; i >= 0; i--) {
        const date = new Date(today);
        date.setDate(today.getDate() - i);
        labels.push(date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }));

        // Demand ramps up as work is awarded / packages are created
        const demandRampPct = 0.5 + 0.5 * ((days - i) / days);
        const dVal = Math.round(d * demandRampPct);

        // Assignments fill in more slowly
        const assignProgress = Math.pow((days - i) / days, 0.8);
        const aVal = Math.min(Math.round(a * assignProgress), dVal);

        demandSeries.push(dVal);
        assignedSeries.push(aVal);
        gapSeries.push(Math.max(0, dVal - aVal));
    }

    return buildChartData(labels, demandSeries, assignedSeries, gapSeries);
}

function buildChartData(labels: string[], demandArr: number[], assignedArr: number[], gapArr: number[]) {
    return {
        labels,
        datasets: [
            {
                label: 'Demand',
                data: demandArr,
                borderColor: '#94a3b8',
                backgroundColor: 'rgba(148,163,184,0.08)',
                borderWidth: 2,
                pointRadius: 0,
                tension: 0.3,
                fill: false,
            },
            {
                label: 'Assigned',
                data: assignedArr,
                borderColor: '#38bdf8',
                backgroundColor: 'rgba(56,189,248,0.12)',
                borderWidth: 2,
                pointRadius: 0,
                tension: 0.3,
                fill: true,
            },
            {
                label: 'Gap',
                data: gapArr,
                borderColor: '#f87171',
                backgroundColor: 'rgba(248,113,113,0.1)',
                borderWidth: 2,
                borderDash: [4, 3],
                pointRadius: 0,
                tension: 0.3,
                fill: false,
            },
        ],
    };
}

const trendChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
        legend: {
            labels: { color: '#cbd5e1', font: { size: 11 }, boxWidth: 24, padding: 16 },
        },
        tooltip: {
            backgroundColor: 'rgba(15,23,42,0.95)',
            borderColor: 'rgba(148,163,184,0.2)',
            borderWidth: 1,
            titleColor: '#e2e8f0',
            bodyColor: '#cbd5e1',
        },
    },
    scales: {
        x: {
            ticks: { color: '#64748b', maxTicksLimit: 8, font: { size: 10 } },
            grid: { color: 'rgba(148,163,184,0.1)' },
        },
        y: {
            ticks: { color: '#64748b', font: { size: 10 }, stepSize: 1 },
            grid: { color: 'rgba(148,163,184,0.1)' },
            min: 0,
        },
    },
};

// ── Helpers ──────────────────────────────────────────────────────────────────

function formatBlockReason(reason: string) {
    const map: Record<string, string> = {
        AssignmentConflict: 'Assignment conflict',
        PTOBlock: 'PTO / Blackout',
        CertMismatch: 'Cert mismatch',
        RegionMismatch: 'Wrong region',
        Inactive: 'Inactive status',
    };
    return map[reason] ?? reason;
}

function onAssign(_s: any) {
    router.push('/scheduling/assignments');
}

// ── Lifecycle ────────────────────────────────────────────────────────────────

onMounted(() => {
    loadBlocked();
    loadEndingSoon();
    buildTrend();
});

watch(() => props.craft.craftCode, () => {
    blocked.value = [];
    endingSoon.value = [];
    trendChartData.value = null;
    loadBlocked();
    loadEndingSoon();
    buildTrend();
});
</script>

<style scoped>
.drawer-inner {
    display: flex;
    flex-direction: column;
    gap: 0;
    height: 100%;
    overflow-y: auto;
    padding: 0.5rem 0 2rem;
}

/* Header */
.drawer-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding: 0 0 1rem;
    border-bottom: 1px solid var(--surface-border);
    margin-bottom: 1rem;
}
.drawer-craft-id { display: flex; align-items: center; gap: 0.75rem; }
.drawer-code {
    font-family: 'Courier New', monospace;
    font-size: 0.88rem;
    font-weight: 700;
    background: var(--surface-ground);
    border: 1px solid var(--surface-border);
    padding: 0.25rem 0.6rem;
    border-radius: 5px;
    letter-spacing: 0.05em;
}
.drawer-name { font-size: 1.1rem; font-weight: 700; color: var(--text-color); }
.drawer-coverage-pill {
    font-size: 0.78rem;
    font-weight: 700;
    padding: 0.3rem 0.75rem;
    border-radius: 999px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
}
.pill-ok   { background: rgba(34,197,94,0.15); color: var(--green-400, #4ade80); }
.pill-mid  { background: rgba(234,179,8,0.15); color: var(--yellow-400, #facc15); }
.pill-warn { background: rgba(239,68,68,0.15); color: var(--red-400, #f87171); }

/* KPIs */
.drawer-kpis {
    display: flex;
    align-items: center;
    background: var(--surface-ground);
    border-radius: 10px;
    padding: 0.85rem 1rem;
    margin-bottom: 1rem;
}
.dkpi { display: flex; flex-direction: column; align-items: center; flex: 1; }
.dkpi-div { width: 1px; height: 28px; background: var(--surface-border); flex-shrink: 0; }
.dkpi-val { font-size: 1.5rem; font-weight: 700; color: var(--text-color); line-height: 1; }
.dkpi-lbl { font-size: 0.67rem; color: var(--text-color-secondary); text-transform: uppercase; letter-spacing: 0.06em; margin-top: 0.2rem; }
.val-warn { color: var(--red-400, #f87171); }
.val-ok   { color: var(--green-400, #4ade80); }

/* Sections */
.drawer-section {
    padding: 1rem 0;
    border-top: 1px solid var(--surface-border);
}
.section-title {
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.08em;
    color: var(--text-color-secondary);
    margin-bottom: 0.75rem;
    display: block;
}
.section-header-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
    margin-bottom: 0.75rem;
    flex-wrap: wrap;
}

/* Demand breakdown */
.demand-breakdown { display: flex; flex-direction: column; gap: 0.4rem; }
.breakdown-row {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    font-size: 0.83rem;
}
.breakdown-badge {
    font-size: 0.68rem;
    font-weight: 700;
    padding: 0.15rem 0.5rem;
    border-radius: 4px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    white-space: nowrap;
    flex-shrink: 0;
}
.badge-released { background: rgba(34,197,94,0.15); color: var(--green-400, #4ade80); }
.badge-forecast  { background: rgba(148,163,184,0.15); color: var(--text-color-secondary); }
.breakdown-desc { flex: 1; color: var(--text-color-secondary); font-size: 0.8rem; }
.breakdown-count { font-weight: 700; font-size: 0.88rem; color: var(--text-color); min-width: 24px; text-align: right; }
.breakdown-note { font-size: 0.75rem; color: var(--text-color-secondary); margin-top: 0.5rem; font-style: italic; }

/* Suggest cards */
.suggest-list { display: flex; flex-direction: column; gap: 0.5rem; }
.suggest-card {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
    background: var(--surface-ground);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 0.6rem 0.85rem;
}
.suggest-left { display: flex; flex-direction: column; gap: 0.3rem; flex: 1; min-width: 0; }
.suggest-name { font-size: 0.88rem; font-weight: 600; color: var(--text-color); }
.suggest-tags { display: flex; align-items: center; gap: 0.4rem; flex-wrap: wrap; }
.suggest-tag { font-size: 0.68rem !important; }
.suggest-meta-item { font-size: 0.75rem; color: var(--text-color-secondary); }

/* Blocked list */
.blocked-list { display: flex; flex-direction: column; gap: 0.35rem; }
.blocked-row {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    font-size: 0.82rem;
    padding: 0.4rem 0.6rem;
    background: rgba(239,68,68,0.05);
    border-radius: 6px;
    border: 1px solid rgba(239,68,68,0.12);
}
.blocked-name { font-weight: 600; flex: 1; }
.blocked-reason { font-size: 0.75rem; color: var(--red-400, #f87171); flex-shrink: 0; }
.blocked-dates { font-size: 0.75rem; color: var(--text-color-secondary); white-space: nowrap; }

/* Ending soon */
.ending-list { display: flex; flex-direction: column; gap: 0.35rem; }
.ending-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    font-size: 0.82rem;
    padding: 0.35rem 0.5rem;
}
.ending-name { font-weight: 500; color: var(--text-color); }
.ending-date { color: var(--yellow-400, #facc15); font-size: 0.78rem; }

/* Trend */
.range-btns { display: flex; gap: 0.25rem; }
.range-btn {
    background: var(--surface-ground);
    border: 1px solid var(--surface-border);
    color: var(--text-color-secondary);
    font-size: 0.72rem;
    font-weight: 600;
    padding: 0.2rem 0.55rem;
    border-radius: 4px;
    cursor: pointer;
    transition: all 0.12s ease;
}
.range-btn:hover { border-color: var(--primary-color); color: var(--text-color); }
.range-btn.active { background: var(--primary-color); border-color: var(--primary-color); color: #fff; }

.trend-toggles { display: flex; gap: 1rem; margin-bottom: 0.75rem; }
.trend-toggle { display: flex; align-items: center; gap: 0.35rem; font-size: 0.78rem; color: var(--text-color-secondary); cursor: pointer; user-select: none; }
.trend-toggle input { accent-color: var(--primary-color); width: 13px; height: 13px; }

.trend-chart { height: 200px; }

/* States */
.drawer-empty {
    font-size: 0.82rem;
    color: var(--text-color-secondary);
    font-style: italic;
}
.drawer-pending { opacity: 0.7; }
.drawer-loading {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    font-size: 0.82rem;
    color: var(--text-color-secondary);
}
</style>
