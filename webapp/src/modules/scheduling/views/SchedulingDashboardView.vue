<template>
    <ModulePageShell>
        <ModulePageHeader title="Scheduling Dashboard" subtitle="Coverage gaps, ending-soon alerts, available resources, and suggested matches.">
            <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
        </ModulePageHeader>

        <!-- KPI strip -->
        <div class="kpi-strip">
            <div class="kpi-item">
                <span class="kpi-value" :class="{ 'kpi-warn': (kpis?.craftShortagesCount ?? 0) > 0 }">
                    {{ loading ? '—' : (kpis?.craftShortagesCount ?? 0) }}
                </span>
                <span class="kpi-label">Craft Shortages</span>
            </div>
            <div class="kpi-div" />
            <div class="kpi-item">
                <span class="kpi-value" :class="{ 'kpi-warn': (kpis?.endingSoonCount ?? 0) > 0 }">
                    {{ loading ? '—' : (kpis?.endingSoonCount ?? 0) }}
                </span>
                <span class="kpi-label">Ending This Week</span>
            </div>
            <div class="kpi-div" />
            <div class="kpi-item">
                <span class="kpi-value">{{ loading ? '—' : (kpis?.availableSoonCount ?? 0) }}</span>
                <span class="kpi-label">Available Soon</span>
            </div>
            <div class="kpi-div" />
            <div class="kpi-item">
                <span class="kpi-value">{{ loading ? '—' : (kpis?.activeJobCount ?? 0) }}</span>
                <span class="kpi-label">Active Jobs</span>
            </div>
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load scheduling data. Make sure the API is running.
        </Message>

        <!-- Quick-nav cards -->
        <div class="nav-grid">
            <div class="nav-card" @click="router.push('/scheduling/jobs')">
                <i class="pi pi-briefcase nav-icon" />
                <div>
                    <div class="nav-title">Jobs Board</div>
                    <div class="nav-desc">View all demand — estimates, staffing plans, work packages</div>
                </div>
                <i class="pi pi-chevron-right nav-arrow" />
            </div>
            <div class="nav-card" @click="router.push('/scheduling/resources')">
                <i class="pi pi-users nav-icon" />
                <div>
                    <div class="nav-title">Resources</div>
                    <div class="nav-desc">Manage craft workers, certifications, and availability</div>
                </div>
                <i class="pi pi-chevron-right nav-arrow" />
            </div>
            <div class="nav-card" @click="router.push('/scheduling/assignments')">
                <i class="pi pi-calendar nav-icon" />
                <div>
                    <div class="nav-title">Assignments</div>
                    <div class="nav-desc">Create and manage job assignments with conflict detection</div>
                </div>
                <i class="pi pi-chevron-right nav-arrow" />
            </div>
            <div class="nav-card" :class="{ 'nav-card-warn': (kpis?.craftShortagesCount ?? 0) > 0 }" @click="router.push('/scheduling/coverage')">
                <i class="pi pi-chart-bar nav-icon" />
                <div>
                    <div class="nav-title">Coverage Analysis</div>
                    <div class="nav-desc">Craft demand vs assigned headcount</div>
                </div>
                <i class="pi pi-chevron-right nav-arrow" />
            </div>
            <div class="nav-card" :class="{ 'nav-card-warn': (kpis?.endingSoonCount ?? 0) > 0 }" @click="router.push('/scheduling/roll-off')">
                <i class="pi pi-sign-out nav-icon" />
                <div>
                    <div class="nav-title">Roll-Off</div>
                    <div class="nav-desc">Resources whose assignments end within 7 days</div>
                </div>
                <i class="pi pi-chevron-right nav-arrow" />
            </div>
        </div>

        <!-- Recent jobs -->
        <div class="section">
            <div class="section-header">
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
                        <span class="date-range">
                            <AppDateValue :value="data.startDate" /> — <AppDateValue :value="data.endDate" />
                        </span>
                    </template>
                </Column>
                <template #empty>
                    <span class="ent-empty">No active demand found. Seed demo data or create estimates.</span>
                </template>
            </DataTable>
        </div>
    </ModulePageShell>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useCoverageService } from '../services/coverageService';
import { useAssignmentService } from '../services/assignmentService';
import { sourceTagSeverity } from '@/ui';
import { ModulePageShell, ModulePageHeader, AppDateValue } from '@/ui';

const router = useRouter();
const { getDashboard } = useCoverageService();
const { listJobs } = useAssignmentService();

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
.kpi-strip {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    display: flex;
    align-items: center;
    padding: 1rem 1.5rem;
    gap: 0;
    flex-wrap: wrap;
}
.kpi-item { display: flex; flex-direction: column; align-items: center; padding: 0 1.5rem; flex: 1; min-width: 90px; }
.kpi-div { width: 1px; height: 36px; background: var(--surface-border); flex-shrink: 0; }
.kpi-value { font-size: 1.75rem; font-weight: 700; color: var(--text-color); line-height: 1; }
.kpi-warn { color: var(--orange-500, #f97316); }
.kpi-label { font-size: 0.72rem; color: var(--text-color-secondary); text-transform: uppercase; letter-spacing: 0.04em; margin-top: 0.25rem; text-align: center; }

.nav-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1rem; }
.nav-card {
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
.nav-card:hover { border-color: var(--primary-color); box-shadow: 0 2px 12px rgba(0,0,0,0.08); }
.nav-card-warn { border-left: 3px solid var(--orange-500, #f97316); }
.nav-icon { font-size: 1.4rem; color: var(--primary-color); flex-shrink: 0; }
.nav-title { font-weight: 600; font-size: 0.92rem; color: var(--text-color); }
.nav-desc { font-size: 0.78rem; color: var(--text-color-secondary); margin-top: 0.15rem; }
.nav-arrow { color: var(--text-color-secondary); margin-left: auto; flex-shrink: 0; }

.section { display: flex; flex-direction: column; gap: 0.75rem; }
.section-header { display: flex; align-items: center; justify-content: space-between; }
.section-header h2 { margin: 0; font-size: 1rem; font-weight: 600; color: var(--text-color); }
.date-range { font-size: 0.8rem; color: var(--text-color-secondary); }
.ent-empty { font-size: 0.85rem; color: var(--text-color-secondary); }
</style>
