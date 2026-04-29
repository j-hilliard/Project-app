<template>
    <div class="plan-detail">

        <div class="plan-back-row">
            <Button text icon="pi pi-arrow-left" :label="backLabel" size="small" @click="goBack" />
        </div>

        <div v-if="loading" class="plan-loading-state">
            <i class="pi pi-spin pi-spinner" />
            <span>Loading work order...</span>
        </div>
        <Message v-if="error" severity="error" :closable="false">Could not load work order.</Message>

        <template v-if="wo">
            <!-- Header -->
            <div class="plan-detail-header">
                <div class="plan-detail-title-row">
                    <div class="plan-detail-title">
                        <span class="plan-detail-number">{{ wo.workOrderNumber }}</span>
                        <h1>{{ wo.title }}</h1>
                        <span class="plan-detail-client" v-if="wo.description">{{ wo.description }}</span>
                    </div>
                    <div class="plan-detail-actions">
                        <Tag :value="wo.status" :severity="statusSeverity(wo.status)" />
                        <Button
                            v-if="wo.status === 'Draft'"
                            label="Release"
                            icon="pi pi-send"
                            severity="success"
                            size="small"
                            :loading="releaseLoading"
                            @click="release"
                        />
                        <Button label="Change Status" icon="pi pi-refresh" outlined size="small"
                            @click="openStatusDialog" />
                    </div>
                </div>

                <!-- Release Gate Error -->
                <div v-if="releaseError" class="plan-gate-error">
                    <i class="pi pi-ban plan-gate-icon" />
                    <div class="plan-gate-body">
                        <span class="plan-gate-title">Release Blocked</span>
                        <span class="plan-gate-message">{{ releaseError }}</span>
                    </div>
                    <Button icon="pi pi-times" text rounded size="small" @click="releaseError = null" />
                </div>

                <!-- Key Info -->
                <div class="plan-detail-meta">
                    <div class="plan-meta-item">
                        <span class="plan-meta-label">Authorized Value</span>
                        <span class="plan-meta-value plan-meta-value-lg">{{ fmtCurrency(wo.authorizedValue) }}</span>
                    </div>
                    <div class="plan-meta-divider" />
                    <div class="plan-meta-item" v-if="wo.plannedStart || wo.plannedEnd">
                        <span class="plan-meta-label">Planned</span>
                        <span class="plan-meta-value">{{ fmtDate(wo.plannedStart) }} – {{ fmtDate(wo.plannedEnd) }}</span>
                    </div>
                    <div class="plan-meta-item" v-if="wo.forecastEnd">
                        <span class="plan-meta-label">Forecast End</span>
                        <span class="plan-meta-value" :class="forecastClass">{{ fmtDate(wo.forecastEnd) }}</span>
                    </div>
                    <div class="plan-meta-item" v-if="wo.releasedAt">
                        <span class="plan-meta-label">Released</span>
                        <span class="plan-meta-value">{{ fmtDate(wo.releasedAt) }}</span>
                        <span class="plan-meta-subvalue" v-if="wo.releasedBy">by {{ wo.releasedBy }}</span>
                    </div>
                    <div class="plan-meta-item" v-if="wo.actualStart">
                        <span class="plan-meta-label">Actual Start</span>
                        <span class="plan-meta-value">{{ fmtDate(wo.actualStart) }}</span>
                    </div>
                    <div class="plan-meta-item" v-if="wo.actualEnd">
                        <span class="plan-meta-label">Actual End</span>
                        <span class="plan-meta-value">{{ fmtDate(wo.actualEnd) }}</span>
                    </div>
                    <div class="plan-meta-divider" />
                    <div class="plan-meta-stat">
                        <span class="plan-meta-stat-value">{{ wo.stepOutPlans?.length ?? 0 }}</span>
                        <span class="plan-meta-stat-label">Step-Out Plans</span>
                    </div>
                    <div class="plan-meta-stat">
                        <span class="plan-meta-stat-value">{{ wo.workPackages?.length ?? 0 }}</span>
                        <span class="plan-meta-stat-label">Work Packages</span>
                    </div>
                </div>
            </div>

            <!-- Scope -->
            <div class="plan-scope-card" v-if="wo.scope">
                <div class="plan-scope-label">Scope of Work</div>
                <div class="plan-scope-text">{{ wo.scope }}</div>
            </div>

            <!-- Tabs -->
            <TabView v-model:activeIndex="activeTab">

                <!-- Step-Out Plans -->
                <TabPanel :header="`Step-Out Plans (${wo.stepOutPlans?.length ?? 0})`">
                    <div v-if="!wo.stepOutPlans?.length" class="plan-empty">
                        No step-out plans attached to this work order.
                    </div>
                    <DataTable v-else :value="wo.stepOutPlans" size="small" class="ent-grid" stripedRows
                        dataKey="stepOutPlanId">
                        <Column field="planNumber" header="Plan #" style="width:140px">
                            <template #body="{ data }">
                                <span class="plan-number plan-link"
                                    @click="router.push(`/planning/step-out-plans/${data.stepOutPlanId}`)">
                                    {{ data.planNumber }}
                                </span>
                            </template>
                        </Column>
                        <Column field="title" header="Title">
                            <template #body="{ data }"><span class="ent-truncate">{{ data.title }}</span></template>
                        </Column>
                        <Column field="status" header="Status" style="width:110px">
                            <template #body="{ data }">
                                <Tag :value="data.status" severity="secondary" />
                            </template>
                        </Column>
                        <Column header="Steps" style="width:80px">
                            <template #body="{ data }">{{ data.steps?.length ?? '—' }}</template>
                        </Column>
                    </DataTable>
                </TabPanel>

                <!-- Work Packages -->
                <TabPanel :header="`Work Packages (${wo.workPackages?.length ?? 0})`">
                    <div v-if="!wo.workPackages?.length" class="plan-empty">
                        No work packages attached to this work order.
                    </div>
                    <DataTable v-else :value="wo.workPackages" size="small" class="ent-grid" stripedRows
                        dataKey="workPackageId">
                        <Column field="packageNumber" header="WP #" style="width:140px">
                            <template #body="{ data }">
                                <span class="plan-number">{{ data.packageNumber }}</span>
                            </template>
                        </Column>
                        <Column field="title" header="Title">
                            <template #body="{ data }"><span class="ent-truncate">{{ data.title }}</span></template>
                        </Column>
                        <Column field="status" header="Status" style="width:110px">
                            <template #body="{ data }">
                                <Tag :value="data.status" severity="secondary" />
                            </template>
                        </Column>
                        <Column header="Ready" style="width:80px">
                            <template #body="{ data }">
                                <i v-if="data.readyForScheduling" class="pi pi-check-circle"
                                    style="color: var(--green-500, #22c55e)" />
                                <i v-else class="pi pi-circle" style="color: var(--text-color-secondary)" />
                            </template>
                        </Column>
                    </DataTable>
                </TabPanel>

            </TabView>
        </template>

        <!-- Status Dialog -->
        <Dialog v-model:visible="showStatusDialog" header="Change Work Order Status" :style="{ width: '360px' }" modal>
            <div class="plan-dialog-body">
                <label class="plan-dialog-label">New Status</label>
                <Dropdown v-model="newStatus" :options="woStatusOptions" optionLabel="label" optionValue="value"
                    class="w-full" placeholder="Select status" />
                <Message v-if="statusError" severity="error" :closable="false" class="mt-2">{{ statusError }}</Message>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="showStatusDialog = false" />
                <Button label="Update Status" :loading="statusLoading" :disabled="!newStatus" @click="changeStatus" />
            </template>
        </Dialog>

    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useApiStore } from '@/stores/apiStore';
import { useToast } from 'primevue/usetoast';

const route = useRoute();
const router = useRouter();
const apiStore = useApiStore();
const toast = useToast();

const woId = Number(route.params.id);
const fromProjectId = route.query.projectId ? Number(route.query.projectId) : null;

const loading = ref(false);
const error = ref(false);
const wo = ref<any>(null);
const activeTab = ref(0);

const releaseLoading = ref(false);
const releaseError = ref<string | null>(null);

const showStatusDialog = ref(false);
const newStatus = ref<string | null>(null);
const statusLoading = ref(false);
const statusError = ref<string | null>(null);

const backLabel = computed(() =>
    fromProjectId ? 'Back to Project' : 'All Work Orders'
);

function goBack() {
    if (fromProjectId) {
        router.push(`/planning/projects/${fromProjectId}`);
    } else {
        router.push('/planning/work-orders');
    }
}

const woStatusOptions = [
    { label: 'Draft', value: 'Draft' },
    { label: 'In Progress', value: 'InProgress' },
    { label: 'Complete', value: 'Complete' },
    { label: 'Closed', value: 'Closed' },
    { label: 'Cancelled', value: 'Cancelled' },
];

const forecastClass = computed(() => {
    if (!wo.value?.forecastEnd || !wo.value?.plannedEnd) return '';
    return new Date(wo.value.forecastEnd) > new Date(wo.value.plannedEnd) ? 'plan-date-late' : '';
});

function fmtDate(d: string | null | undefined): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function fmtCurrency(v: number | null | undefined): string {
    if (v == null || v === 0) return '—';
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(v);
}

function statusSeverity(s: string): string {
    switch (s) {
        case 'Released': return 'success';
        case 'InProgress': return 'info';
        case 'Draft': return 'secondary';
        case 'Complete': case 'Closed': return 'contrast';
        case 'Cancelled': return 'danger';
        default: return 'secondary';
    }
}

function openStatusDialog() {
    newStatus.value = wo.value?.status ?? null;
    statusError.value = null;
    showStatusDialog.value = true;
}

async function release() {
    releaseLoading.value = true;
    releaseError.value = null;
    try {
        const { data } = await apiStore.api.patch(`/api/v1/work-orders/${woId}/release`);
        wo.value = { ...wo.value, ...data };
        toast.add({ severity: 'success', summary: 'Work Order Released', life: 2500 });
    } catch (e: any) {
        const body = e?.response?.data;
        releaseError.value = body?.message ?? 'Release failed. Please try again.';
    } finally {
        releaseLoading.value = false;
    }
}

async function changeStatus() {
    if (!newStatus.value) return;
    statusLoading.value = true;
    statusError.value = null;
    try {
        const { data } = await apiStore.api.patch(`/api/v1/work-orders/${woId}/status`, { status: newStatus.value });
        wo.value = { ...wo.value, ...data };
        showStatusDialog.value = false;
        toast.add({ severity: 'success', summary: 'Status Updated', life: 2500 });
    } catch (e: any) {
        statusError.value = e?.response?.data?.message ?? 'Status update failed.';
    } finally {
        statusLoading.value = false;
    }
}

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const { data } = await apiStore.api.get(`/api/v1/work-orders/${woId}`);
        wo.value = data;
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

/* Gate error */
.plan-gate-error {
    display: flex;
    align-items: flex-start;
    gap: 0.75rem;
    padding: 0.75rem 1rem;
    background: #fff7ed;
    border: 1px solid #fed7aa;
    border-radius: 8px;
}

.plan-gate-icon {
    font-size: 1.1rem;
    color: #ea580c;
    flex-shrink: 0;
    padding-top: 0.1rem;
}

.plan-gate-body {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
}

.plan-gate-title {
    font-size: 0.85rem;
    font-weight: 700;
    color: #9a3412;
}

.plan-gate-message {
    font-size: 0.82rem;
    color: #c2410c;
    line-height: 1.4;
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

.plan-meta-value-lg {
    font-size: 1.15rem;
    font-weight: 700;
}

.plan-meta-value.plan-date-late {
    color: var(--red-500, #ef4444);
    font-weight: 700;
}

.plan-meta-subvalue {
    font-size: 0.72rem;
    color: var(--text-color-secondary);
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

/* Scope card */
.plan-scope-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 1rem 1.25rem;
}

.plan-scope-label {
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-color-secondary);
    margin-bottom: 0.4rem;
}

.plan-scope-text {
    font-size: 0.88rem;
    color: var(--text-color);
    line-height: 1.6;
    white-space: pre-wrap;
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
</style>
