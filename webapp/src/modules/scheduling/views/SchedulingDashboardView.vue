<template>
    <div class="sched-view">
        <div class="sched-view-header">
            <div>
                <h1>Scheduling Dashboard</h1>
                <p>Coverage gaps, ending-soon alerts, available resources, and suggested matches.</p>
            </div>
            <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
        </div>

        <!-- KPI strip -->
        <div class="sched-kpi-strip">
            <div class="sched-kpi-item">
                <span class="sched-kpi-value" :class="{ 'sched-kpi-warn': (kpis?.craftShortagesCount ?? 0) > 0 }">
                    {{ loading ? '—' : (kpis?.craftShortagesCount ?? 0) }}
                </span>
                <span class="sched-kpi-label">Craft Shortages</span>
            </div>
            <div class="sched-kpi-divider" />
            <div class="sched-kpi-item">
                <span class="sched-kpi-value" :class="{ 'sched-kpi-warn': (kpis?.endingSoonCount ?? 0) > 0 }">
                    {{ loading ? '—' : (kpis?.endingSoonCount ?? 0) }}
                </span>
                <span class="sched-kpi-label">Ending This Week</span>
            </div>
            <div class="sched-kpi-divider" />
            <div class="sched-kpi-item">
                <span class="sched-kpi-value">
                    {{ loading ? '—' : (kpis?.availableSoonCount ?? 0) }}
                </span>
                <span class="sched-kpi-label">Available Soon</span>
            </div>
            <div class="sched-kpi-divider" />
            <div class="sched-kpi-item">
                <span class="sched-kpi-value">
                    {{ loading ? '—' : (kpis?.activeJobCount ?? 0) }}
                </span>
                <span class="sched-kpi-label">Active Jobs</span>
            </div>
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load scheduling data. Make sure the API is running.
        </Message>

        <!-- Quick-nav cards -->
        <div class="sched-nav-grid">
            <div class="sched-nav-card" @click="router.push('/scheduling/jobs')">
                <i class="pi pi-briefcase sched-nav-icon" />
                <div>
                    <div class="sched-nav-title">Jobs Board</div>
                    <div class="sched-nav-desc">View all demand — estimates, staffing plans, work packages</div>
                </div>
                <i class="pi pi-chevron-right sched-nav-arrow" />
            </div>
            <div class="sched-nav-card" @click="router.push('/scheduling/resources')">
                <i class="pi pi-users sched-nav-icon" />
                <div>
                    <div class="sched-nav-title">Resources</div>
                    <div class="sched-nav-desc">Manage craft workers, certifications, and availability</div>
                </div>
                <i class="pi pi-chevron-right sched-nav-arrow" />
            </div>
            <div class="sched-nav-card" @click="router.push('/scheduling/assignments')">
                <i class="pi pi-calendar sched-nav-icon" />
                <div>
                    <div class="sched-nav-title">Assignments</div>
                    <div class="sched-nav-desc">Create and manage job assignments with conflict detection</div>
                </div>
                <i class="pi pi-chevron-right sched-nav-arrow" />
            </div>
            <div class="sched-nav-card" :class="{ 'sched-nav-card-warn': (kpis?.craftShortagesCount ?? 0) > 0 }" @click="router.push('/scheduling/coverage')">
                <i class="pi pi-chart-bar sched-nav-icon" />
                <div>
                    <div class="sched-nav-title">Coverage Analysis</div>
                    <div class="sched-nav-desc">Craft demand vs assigned headcount</div>
                </div>
                <i class="pi pi-chevron-right sched-nav-arrow" />
            </div>
            <div class="sched-nav-card" :class="{ 'sched-nav-card-warn': (kpis?.endingSoonCount ?? 0) > 0 }" @click="router.push('/scheduling/roll-off')">
                <i class="pi pi-sign-out sched-nav-icon" />
                <div>
                    <div class="sched-nav-title">Roll-Off</div>
                    <div class="sched-nav-desc">Resources whose assignments end within 7 days</div>
                </div>
                <i class="pi pi-chevron-right sched-nav-arrow" />
            </div>
        </div>

        <!-- Recent jobs -->
        <div class="sched-section">
            <div class="sched-section-header">
                <h2>Active Demand</h2>
                <Button label="View All" text size="small" @click="router.push('/scheduling/jobs')" />
            </div>
            <DataTable :value="recentJobs" :loading="loading" stripedRows size="small" class="ent-grid">
                <Column field="sourceType" header="Source" style="width:110px">
                    <template #body="{ data }">
                        <Tag :value="data.sourceType" :severity="sourceTagSeverity(data.sourceType)" />
                    </template>
                </Column>
                <Column field="name" header="Job Name" />
                <Column field="client" header="Client" />
                <Column field="status" header="Status" style="width:100px">
                    <template #body="{ data }">
                        <Tag :value="data.status" severity="info" />
                    </template>
                </Column>
                <Column header="Dates" style="width:180px">
                    <template #body="{ data }">
                        <span class="sched-date-range">
                            {{ fmtDate(data.startDate) }} — {{ fmtDate(data.endDate) }}
                        </span>
                    </template>
                </Column>
                <template #empty>
                    <span class="sched-empty">No active demand found. Seed demo data or create estimates.</span>
                </template>
            </DataTable>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useSchedulingService } from '../services/useSchedulingService';
import { useFormatters } from '@/ui';
import { sourceTagSeverity } from '@/ui';

const router = useRouter();
const { getDashboard, listJobs } = useSchedulingService();
const { fmtDate } = useFormatters();

const loading = ref(false);
const error = ref(false);
const kpis = ref<any>(null);
const recentJobs = ref<any[]>([]);

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const [kpiData, jobData] = await Promise.all([getDashboard(), listJobs()]);
        kpis.value = kpiData;
        recentJobs.value = (jobData as any[]).slice(0, 8);
    } catch {
        error.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
.sched-view {
    max-width: 1100px;
    margin: 0 auto;
    padding: 1.5rem 0;
    display: flex;
    flex-direction: column;
    gap: 2rem;
}
.sched-view-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    flex-wrap: wrap;
}
.sched-view-header h1 { margin: 0 0 0.25rem; font-size: 1.5rem; font-weight: 700; color: var(--text-color); }
.sched-view-header p { margin: 0; color: var(--text-color-secondary); font-size: 0.88rem; }

.sched-kpi-strip {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    display: flex;
    align-items: center;
    padding: 1rem 1.5rem;
    gap: 0;
    flex-wrap: wrap;
}
.sched-kpi-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 0 1.5rem;
    flex: 1;
    min-width: 90px;
}
.sched-kpi-divider {
    width: 1px;
    height: 36px;
    background: var(--surface-border);
    flex-shrink: 0;
}
.sched-kpi-value {
    font-size: 1.75rem;
    font-weight: 700;
    color: var(--text-color);
    line-height: 1;
}
.sched-kpi-warn { color: var(--orange-500, #f97316); }
.sched-kpi-label {
    font-size: 0.72rem;
    color: var(--text-color-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
    margin-top: 0.25rem;
    text-align: center;
}

.sched-nav-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 1rem;
}
.sched-nav-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    padding: 1rem 1.25rem;
    display: flex;
    align-items: center;
    gap: 1rem;
    cursor: pointer;
    transition: all 0.15s ease;
}
.sched-nav-card:hover {
    border-color: var(--primary-color);
    box-shadow: 0 2px 12px rgba(0,0,0,0.08);
}
.sched-nav-card-warn { border-left: 3px solid var(--orange-500, #f97316); }
.sched-nav-icon { font-size: 1.4rem; color: var(--primary-color); flex-shrink: 0; }
.sched-nav-title { font-weight: 600; font-size: 0.92rem; color: var(--text-color); }
.sched-nav-desc { font-size: 0.78rem; color: var(--text-color-secondary); margin-top: 0.15rem; }
.sched-nav-arrow { color: var(--text-color-secondary); margin-left: auto; flex-shrink: 0; }

.sched-section { display: flex; flex-direction: column; gap: 0.75rem; }
.sched-section-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
}
.sched-section-header h2 { margin: 0; font-size: 1rem; font-weight: 600; color: var(--text-color); }
.sched-date-range { font-size: 0.8rem; color: var(--text-color-secondary); }
.sched-empty { font-size: 0.85rem; color: var(--text-color-secondary); }
</style>
