<template>
    <div class="plan-detail">

        <div class="plan-back-row">
            <Button text icon="pi pi-arrow-left" label="All Projects" size="small"
                @click="router.push('/planning/projects')" />
        </div>

        <div v-if="loading" class="plan-loading-state">
            <i class="pi pi-spin pi-spinner" />
            <span>Loading project...</span>
        </div>
        <Message v-if="error" severity="error" :closable="false">Could not load project.</Message>

        <template v-if="project">
            <!-- Header -->
            <div class="plan-detail-header">
                <div class="plan-detail-title-row">
                    <div class="plan-detail-title">
                        <span class="plan-detail-number">{{ project.projectNumber }}</span>
                        <h1>{{ project.name }}</h1>
                        <span class="plan-detail-client" v-if="project.client">
                            {{ project.client }}<span v-if="project.site"> · {{ project.site }}</span>
                            <span v-if="project.city"> · {{ project.city }}<span v-if="project.state">, {{ project.state }}</span></span>
                        </span>
                    </div>
                    <div class="plan-detail-actions">
                        <Tag :value="project.status" :severity="statusSeverity(project.status)" />
                        <Button label="Change Status" icon="pi pi-refresh" outlined size="small"
                            @click="openStatusDialog" />
                        <Button label="Lock Baseline" icon="pi pi-lock" outlined size="small"
                            @click="showBaseline = true" />
                    </div>
                </div>

                <div class="plan-detail-meta">
                    <div class="plan-meta-item" v-if="project.plannedStart || project.plannedEnd">
                        <span class="plan-meta-label">Planned</span>
                        <span class="plan-meta-value">{{ fmtDate(project.plannedStart) }} – {{ fmtDate(project.plannedEnd) }}</span>
                    </div>
                    <div class="plan-meta-item" v-if="project.forecastEnd">
                        <span class="plan-meta-label">Forecast End</span>
                        <span class="plan-meta-value" :class="forecastClass">{{ fmtDate(project.forecastEnd) }}</span>
                    </div>
                    <div class="plan-meta-item" v-if="project.actualStart">
                        <span class="plan-meta-label">Actual Start</span>
                        <span class="plan-meta-value">{{ fmtDate(project.actualStart) }}</span>
                    </div>
                    <div class="plan-meta-divider" />
                    <div class="plan-meta-stat">
                        <span class="plan-meta-stat-value">{{ project.phases?.length ?? 0 }}</span>
                        <span class="plan-meta-stat-label">Phases</span>
                    </div>
                    <div class="plan-meta-stat">
                        <span class="plan-meta-stat-value">{{ totalTaskCount }}</span>
                        <span class="plan-meta-stat-label">Tasks</span>
                    </div>
                    <div class="plan-meta-stat">
                        <span class="plan-meta-stat-value">{{ project.workOrders?.length ?? 0 }}</span>
                        <span class="plan-meta-stat-label">Work Orders</span>
                    </div>
                </div>

                <Message v-if="statusError" severity="error" :closable="true" @close="statusError = null">
                    {{ statusError }}
                </Message>
            </div>

            <!-- Tabs -->
            <TabView v-model:activeIndex="activeTab">

                <!-- Phases & Tasks -->
                <TabPanel header="Phases & Tasks">
                    <div v-if="!project.phases?.length" class="plan-empty">
                        No phases defined for this project.
                    </div>
                    <div v-for="phase in project.phases" :key="phase.phaseId" class="plan-phase-block">
                        <div class="plan-phase-header" @click="togglePhase(phase.phaseId)">
                            <i class="plan-phase-chevron"
                                :class="expandedPhases.has(phase.phaseId) ? 'pi pi-chevron-down' : 'pi pi-chevron-right'" />
                            <span class="plan-phase-name">{{ phase.name }}</span>
                            <Tag :value="phase.status" :severity="phaseStatusSeverity(phase.status)"
                                class="plan-phase-status-tag" />
                            <span class="plan-phase-dates">{{ fmtDate(phase.plannedStart) }} – {{ fmtDate(phase.plannedEnd) }}</span>
                            <span class="plan-phase-count">{{ phase.tasks?.length ?? 0 }} tasks</span>
                        </div>
                        <div v-if="expandedPhases.has(phase.phaseId)" class="plan-task-list">
                            <div v-if="!phase.tasks?.length" class="plan-task-empty">No tasks in this phase.</div>
                            <div v-for="task in phase.tasks" :key="task.taskId" class="plan-task-row"
                                :class="{ 'plan-task-is-milestone': task.isMilestone || task.taskType === 'Milestone' }">
                                <span class="plan-task-type-icon">
                                    <i v-if="task.isMilestone || task.taskType === 'Milestone'" class="pi pi-diamond" />
                                    <i v-else-if="task.taskType === 'Gate'" class="pi pi-flag" />
                                    <i v-else class="pi pi-check-square" />
                                </span>
                                <span class="plan-task-title">{{ task.title }}</span>
                                <Tag :value="task.status" :severity="taskStatusSeverity(task.status)"
                                    class="plan-task-status-tag" />
                                <div class="plan-task-progress">
                                    <div class="plan-task-bar">
                                        <div class="plan-task-bar-fill"
                                            :style="{ width: (task.percentComplete ?? 0) + '%' }"
                                            :class="barClass(task)" />
                                    </div>
                                    <span class="plan-task-pct">{{ task.percentComplete ?? 0 }}%</span>
                                </div>
                                <span class="plan-task-dates">{{ fmtDate(task.plannedStart) }} – {{ fmtDate(task.plannedEnd) }}</span>
                                <span class="plan-task-duration" v-if="task.durationDays">{{ task.durationDays }}d</span>
                            </div>
                        </div>
                    </div>
                </TabPanel>

                <!-- Work Orders -->
                <TabPanel header="Work Orders">
                    <div v-if="!project.workOrders?.length" class="plan-empty">
                        No work orders for this project yet. Work orders are created from within a released project.
                    </div>
                    <DataTable v-else :value="project.workOrders" size="small" class="ent-grid" stripedRows
                        dataKey="workOrderId">
                        <Column field="workOrderNumber" header="WO #" style="width:150px">
                            <template #body="{ data }">
                                <span class="plan-number plan-link"
                                    @click="router.push(`/planning/work-orders/${data.workOrderId}`)">
                                    {{ data.workOrderNumber }}
                                </span>
                            </template>
                        </Column>
                        <Column field="title" header="Title">
                            <template #body="{ data }"><span class="ent-truncate">{{ data.title }}</span></template>
                        </Column>
                        <Column field="status" header="Status" style="width:120px">
                            <template #body="{ data }">
                                <Tag :value="data.status" :severity="woStatusSeverity(data.status)" />
                            </template>
                        </Column>
                        <Column header="Auth $" style="width:130px">
                            <template #body="{ data }">
                                <span class="plan-currency">{{ fmtCurrency(data.authorizedValue) }}</span>
                            </template>
                        </Column>
                        <Column header="Planned End" style="width:120px">
                            <template #body="{ data }">{{ fmtDate(data.plannedEnd) }}</template>
                        </Column>
                        <Column header="Released" style="width:120px">
                            <template #body="{ data }">{{ fmtDate(data.releasedAt) }}</template>
                        </Column>
                    </DataTable>
                </TabPanel>

                <!-- Milestones -->
                <TabPanel header="Milestones">
                    <div v-if="!project.milestones?.length" class="plan-empty">No milestones defined.</div>
                    <DataTable v-else :value="project.milestones" size="small" class="ent-grid" stripedRows
                        dataKey="milestoneId">
                        <Column field="name" header="Milestone" />
                        <Column header="Planned Date" style="width:130px">
                            <template #body="{ data }">{{ fmtDate(data.plannedDate) }}</template>
                        </Column>
                        <Column header="Baseline Date" style="width:130px">
                            <template #body="{ data }">{{ data.baselineDate ? fmtDate(data.baselineDate) : '—' }}</template>
                        </Column>
                        <Column header="Actual Date" style="width:130px">
                            <template #body="{ data }">{{ data.actualDate ? fmtDate(data.actualDate) : '—' }}</template>
                        </Column>
                        <Column field="status" header="Status" style="width:110px">
                            <template #body="{ data }">
                                <Tag :value="data.status" :severity="msStatusSeverity(data.status)" />
                            </template>
                        </Column>
                        <Column header="Flags" style="width:80px">
                            <template #body="{ data }">
                                <span v-if="data.isCritical" class="plan-flag plan-flag-crit">CRIT</span>
                                <span v-if="data.isDeadline" class="plan-flag plan-flag-dl">DL</span>
                            </template>
                        </Column>
                    </DataTable>
                </TabPanel>

            </TabView>
        </template>

        <!-- Status Dialog -->
        <Dialog v-model:visible="showStatusDialog" header="Change Project Status" :style="{ width: '360px' }" modal>
            <div class="plan-dialog-body">
                <label class="plan-dialog-label">New Status</label>
                <Dropdown v-model="newStatus" :options="projectStatusOptions" optionLabel="label" optionValue="value"
                    class="w-full" placeholder="Select status" />
            </div>
            <template #footer>
                <Button label="Cancel" text @click="showStatusDialog = false" />
                <Button label="Update Status" :loading="statusLoading" :disabled="!newStatus" @click="changeStatus" />
            </template>
        </Dialog>

        <!-- Baseline Dialog -->
        <Dialog v-model:visible="showBaseline" header="Lock Baseline Snapshot" :style="{ width: '420px' }" modal>
            <div class="plan-dialog-body">
                <label class="plan-dialog-label">Label <span class="plan-dialog-opt">(optional)</span></label>
                <InputText v-model="baselineLabel" placeholder="e.g. Initial Baseline" class="w-full" />
                <label class="plan-dialog-label" style="margin-top:0.75rem">Reason <span class="plan-dialog-opt">(optional)</span></label>
                <Textarea v-model="baselineReason" placeholder="Reason for locking this baseline..." rows="3"
                    class="w-full" />
                <Message v-if="baselineError" severity="error" :closable="false" class="mt-2">{{ baselineError }}</Message>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="showBaseline = false" />
                <Button label="Lock Baseline" icon="pi pi-lock" :loading="baselineLoading" @click="lockBaseline" />
            </template>
        </Dialog>

    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { usePlanningService } from '../services/usePlanningService';
import { useFormatters } from '@/ui';
import {
    projectStatusSeverity as statusSeverity,
    phaseStatusSeverity,
    taskStatusSeverity,
    workOrderStatusSeverity as woStatusSeverity,
    milestoneStatusSeverity as msStatusSeverity,
} from '@/ui';

const route = useRoute();
const router = useRouter();
const toast = useToast();
const { getProjectFull, setProjectStatus, lockProjectBaseline } = usePlanningService();
const { fmtDate, fmtCurrency } = useFormatters();

const projectId = Number(route.params.id);
const loading = ref(false);
const error = ref(false);
const project = ref<any>(null);
const activeTab = ref(0);
const expandedPhases = ref(new Set<number>());

const showStatusDialog = ref(false);
const newStatus = ref<string | null>(null);
const statusLoading = ref(false);
const statusError = ref<string | null>(null);

const showBaseline = ref(false);
const baselineLabel = ref('');
const baselineReason = ref('');
const baselineLoading = ref(false);
const baselineError = ref<string | null>(null);

const projectStatusOptions = [
    { label: 'Initiating', value: 'Initiating' },
    { label: 'Planning', value: 'Planning' },
    { label: 'Active', value: 'Active' },
    { label: 'Monitoring', value: 'Monitoring' },
    { label: 'On Hold', value: 'OnHold' },
    { label: 'Closing', value: 'Closing' },
    { label: 'Closed', value: 'Closed' },
    { label: 'Cancelled', value: 'Cancelled' },
];

const totalTaskCount = computed(() =>
    (project.value?.phases ?? []).reduce((sum: number, ph: any) => sum + (ph.tasks?.length ?? 0), 0)
);

const forecastClass = computed(() => {
    if (!project.value?.forecastEnd || !project.value?.plannedEnd) return '';
    return new Date(project.value.forecastEnd) > new Date(project.value.plannedEnd) ? 'plan-date-late' : '';
});

function togglePhase(phaseId: number) {
    if (expandedPhases.value.has(phaseId)) {
        expandedPhases.value.delete(phaseId);
    } else {
        expandedPhases.value.add(phaseId);
    }
}

function barClass(task: any): string {
    if (task.status === 'Complete') return 'plan-bar-complete';
    if (task.status === 'Blocked') return 'plan-bar-blocked';
    return 'plan-bar-active';
}

function openStatusDialog() {
    newStatus.value = project.value?.status ?? null;
    statusError.value = null;
    showStatusDialog.value = true;
}

async function changeStatus() {
    if (!newStatus.value) return;
    statusLoading.value = true;
    statusError.value = null;
    try {
        const result = await setProjectStatus(projectId, newStatus.value);
        project.value.status = result.status;
        project.value.actualStart = result.actualStart;
        project.value.actualEnd = result.actualEnd;
        showStatusDialog.value = false;
        toast.add({ severity: 'success', summary: 'Status Updated', life: 2500 });
    } catch (e: any) {
        statusError.value = e?.response?.data?.message ?? 'Status update failed.';
    } finally {
        statusLoading.value = false;
    }
}

async function lockBaseline() {
    baselineLoading.value = true;
    baselineError.value = null;
    try {
        const result = await lockProjectBaseline(projectId, baselineLabel.value || null, baselineReason.value || null);
        showBaseline.value = false;
        baselineLabel.value = '';
        baselineReason.value = '';
        toast.add({ severity: 'success', summary: 'Baseline Locked', detail: `${result.snapshotsCreated} snapshots created.`, life: 3000 });
    } catch {
        baselineError.value = 'Failed to lock baseline. Please try again.';
    } finally {
        baselineLoading.value = false;
    }
}

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const data = await getProjectFull(projectId);
        project.value = data;
        for (const ph of data.phases ?? []) {
            expandedPhases.value.add(ph.phaseId);
        }
    } catch {
        error.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
.plan-detail {
    max-width: 1200px;
    margin: 0 auto;
    padding: 1.5rem 0;
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.plan-back-row {
    display: flex;
    align-items: center;
}

.plan-loading-state {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    color: var(--text-color-secondary);
    font-size: 0.9rem;
}

/* Header */
.plan-detail-header {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 12px;
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.plan-detail-title-row {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    flex-wrap: wrap;
}

.plan-detail-title {
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
}

.plan-detail-number {
    font-family: 'Courier New', monospace;
    font-size: 0.78rem;
    font-weight: 600;
    color: var(--primary-color);
    letter-spacing: 0.03em;
}

.plan-detail-title h1 {
    margin: 0;
    font-size: 1.5rem;
    font-weight: 700;
    color: var(--text-color);
    line-height: 1.2;
}

.plan-detail-client {
    font-size: 0.85rem;
    color: var(--text-color-secondary);
}

.plan-detail-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;
}

/* Meta row */
.plan-detail-meta {
    display: flex;
    align-items: center;
    gap: 1.5rem;
    flex-wrap: wrap;
    padding-top: 0.25rem;
    border-top: 1px solid var(--surface-border);
}

.plan-meta-item {
    display: flex;
    flex-direction: column;
    gap: 0.1rem;
}

.plan-meta-label {
    font-size: 0.7rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-color-secondary);
    font-weight: 600;
}

.plan-meta-value {
    font-size: 0.88rem;
    font-weight: 500;
    color: var(--text-color);
}

.plan-meta-value.plan-date-late {
    color: var(--red-500, #ef4444);
    font-weight: 700;
}

.plan-meta-divider {
    width: 1px;
    height: 32px;
    background: var(--surface-border);
}

.plan-meta-stat {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.1rem;
}

.plan-meta-stat-value {
    font-size: 1.35rem;
    font-weight: 700;
    color: var(--text-color);
    line-height: 1;
}

.plan-meta-stat-label {
    font-size: 0.7rem;
    color: var(--text-color-secondary);
    text-transform: uppercase;
    letter-spacing: 0.04em;
}

/* Phase blocks */
.plan-phase-block {
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    overflow: hidden;
    margin-bottom: 0.5rem;
}

.plan-phase-header {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.75rem 1rem;
    background: var(--surface-ground);
    cursor: pointer;
    user-select: none;
    transition: background 0.12s;
}

.plan-phase-header:hover {
    background: var(--surface-hover);
}

.plan-phase-chevron {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
    flex-shrink: 0;
}

.plan-phase-name {
    font-size: 0.9rem;
    font-weight: 700;
    color: var(--text-color);
    flex: 1;
}

.plan-phase-status-tag {
    flex-shrink: 0;
}

.plan-phase-dates {
    font-size: 0.78rem;
    color: var(--text-color-secondary);
    flex-shrink: 0;
}

.plan-phase-count {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
    flex-shrink: 0;
}

/* Task rows */
.plan-task-list {
    background: var(--surface-card);
}

.plan-task-empty {
    padding: 0.75rem 1.25rem;
    font-size: 0.82rem;
    color: var(--text-color-secondary);
}

.plan-task-row {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.5rem 1rem 0.5rem 1.25rem;
    border-top: 1px solid var(--surface-border);
    min-height: 44px;
}

.plan-task-row:hover {
    background: var(--surface-hover);
}

.plan-task-is-milestone .plan-task-title {
    font-style: italic;
}

.plan-task-type-icon {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
    flex-shrink: 0;
    width: 16px;
}

.plan-task-title {
    flex: 1;
    font-size: 0.85rem;
    color: var(--text-color);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.plan-task-status-tag {
    flex-shrink: 0;
}

.plan-task-progress {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    width: 120px;
    flex-shrink: 0;
}

.plan-task-bar {
    flex: 1;
    height: 6px;
    background: var(--surface-border);
    border-radius: 3px;
    overflow: hidden;
}

.plan-task-bar-fill {
    height: 100%;
    border-radius: 3px;
    transition: width 0.2s;
}

.plan-bar-active { background: var(--primary-color); }
.plan-bar-complete { background: var(--green-500, #22c55e); }
.plan-bar-blocked { background: var(--red-400, #f87171); }

.plan-task-pct {
    font-size: 0.72rem;
    color: var(--text-color-secondary);
    min-width: 28px;
    text-align: right;
}

.plan-task-dates {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
    flex-shrink: 0;
    width: 180px;
}

.plan-task-duration {
    font-size: 0.72rem;
    color: var(--text-color-secondary);
    flex-shrink: 0;
    width: 28px;
    text-align: right;
}

/* Shared */
.plan-empty {
    padding: 1.5rem 0.5rem;
    font-size: 0.85rem;
    color: var(--text-color-secondary);
}

.plan-number {
    font-family: 'Courier New', monospace;
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--primary-color);
}

.plan-link {
    cursor: pointer;
    text-decoration: underline dotted;
}

.plan-link:hover {
    color: var(--primary-700, var(--primary-color));
}

.plan-currency {
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--text-color);
    display: block;
    text-align: right;
}

.plan-flag {
    font-size: 0.65rem;
    font-weight: 700;
    padding: 0.1rem 0.35rem;
    border-radius: 3px;
    letter-spacing: 0.04em;
    margin-right: 0.25rem;
}

.plan-flag-crit { background: #fee2e2; color: #991b1b; }
.plan-flag-dl { background: #fef9c3; color: #854d0e; }

/* Dialog */
.plan-dialog-body {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding: 0.5rem 0;
}

.plan-dialog-label {
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--text-color);
}

.plan-dialog-opt {
    font-weight: 400;
    color: var(--text-color-secondary);
}
</style>
