<template>
    <div class="planning-view">
        <div class="planning-view-header">
            <div class="planning-view-header-left">
                <h1>Step-Out Plans</h1>
                <p>Sequenced execution plans with work package generation and dependency tracking.</p>
            </div>
            <div class="planning-header-actions">
                <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
                <Button label="New Plan" icon="pi pi-plus" @click="openNew" />
            </div>
        </div>

        <Message v-if="error" severity="error" :closable="false">
            {{ errorMessage }}
        </Message>

        <!-- Filters -->
        <div class="planning-filters">
            <Dropdown v-model="statusFilter" :options="statusOptions" optionLabel="label" optionValue="value"
                placeholder="All Status" showClear class="w-10rem" @change="applyFilters" />
            <InputText v-model="search" placeholder="Search plans..." class="flex-1 min-w-10rem" @input="applyFilters" />
            <Tag :value="`${filtered.length} plans`" severity="info" />
        </div>

        <DataTable :value="filtered" :loading="loading" stripedRows dataKey="planId" size="small"
            class="ent-grid ent-grid-clickable"
            :rows="25" paginator :rowsPerPageOptions="[10, 25, 50]"
            @row-click="(e) => router.push(`/planning/step-out-plans/${e.data.planId}`)">
            <Column field="name" header="Plan Name" sortable />
            <Column field="sourceType" header="Source" style="width:110px">
                <template #body="{ data }">
                    <Tag v-if="data.sourceType" :value="data.sourceType" severity="secondary" />
                    <span v-else class="planning-empty-cell">—</span>
                </template>
            </Column>
            <Column field="client" header="Client" sortable />
            <Column field="site" header="Site" />
            <Column field="status" header="Status" style="width:100px" sortable>
                <template #body="{ data }">
                    <Tag :value="data.status" :severity="statusSeverity(data.status)" />
                </template>
            </Column>
            <Column header="Start" style="width:110px">
                <template #body="{ data }">{{ fmtDate(data.plannedStart) }}</template>
            </Column>
            <Column header="End" style="width:110px">
                <template #body="{ data }">{{ fmtDate(data.plannedEnd) }}</template>
            </Column>
            <Column header="Steps" style="width:70px">
                <template #body="{ data }">
                    <span class="step-count">{{ data.steps?.length ?? 0 }}</span>
                </template>
            </Column>
            <Column header="" style="width:50px">
                <template #body="{ data }">
                    <Button icon="pi pi-trash" text severity="danger" size="small" @click.stop="confirmDelete(data)" />
                </template>
            </Column>
            <template #empty>
                <span class="planning-empty">No step-out plans found. Create one to start planning execution.</span>
            </template>
        </DataTable>

        <!-- New Plan Dialog -->
        <Dialog v-model:visible="formVisible" header="New Step-Out Plan" modal :style="{ width: '500px' }">
            <div class="form-grid">
                <div class="form-field">
                    <label>Plan Name</label>
                    <InputText v-model="form.name" class="w-full" placeholder="e.g. Turnaround 2026 — Phase 1" />
                </div>
                <div class="form-field">
                    <label>Client</label>
                    <InputText v-model="form.client" class="w-full" />
                </div>
                <div class="form-field">
                    <label>Site / Location</label>
                    <InputText v-model="form.site" class="w-full" />
                </div>
                <div class="form-field-row">
                    <div class="form-field">
                        <label>Planned Start</label>
                        <Calendar v-model="form.plannedStart" showIcon dateFormat="yy-mm-dd" class="w-full" />
                    </div>
                    <div class="form-field">
                        <label>Planned End</label>
                        <Calendar v-model="form.plannedEnd" showIcon dateFormat="yy-mm-dd" class="w-full" />
                    </div>
                </div>
                <div class="form-field">
                    <label>Notes</label>
                    <Textarea v-model="form.notes" rows="2" class="w-full" />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="formVisible = false" />
                <Button label="Create Plan" :loading="saving" @click="savePlan" />
            </template>
        </Dialog>

        <ConfirmDialog />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import { usePlanningService } from '../services/usePlanningService';
import { useFormatters } from '@/ui';
import { planStatusSeverity as statusSeverity } from '@/ui';

const router = useRouter();
const toast = useToast();
const confirm = useConfirm();
const { listStepOutPlans, createStepOutPlan, deleteStepOutPlan } = usePlanningService();
const { fmtDate } = useFormatters();

const loading = ref(false);
const error = ref(false);
const errorMessage = ref('Could not load plans. Make sure the API is running.');
const saving = ref(false);
const plans = ref<any[]>([]);
const statusFilter = ref<string | null>(null);
const search = ref('');

const statusOptions = [
    { label: 'Draft', value: 'Draft' },
    { label: 'Active', value: 'Active' },
    { label: 'Complete', value: 'Complete' },
    { label: 'Archived', value: 'Archived' },
];

const filtered = computed(() => {
    let list = plans.value;
    if (statusFilter.value) list = list.filter(p => p.status === statusFilter.value);
    if (search.value.trim()) {
        const q = search.value.toLowerCase();
        list = list.filter(p => p.name?.toLowerCase().includes(q) || p.client?.toLowerCase().includes(q));
    }
    return list;
});

function applyFilters() { /* computed */ }

const formVisible = ref(false);
const form = ref({ name: '', client: '', site: '', plannedStart: null as Date | null, plannedEnd: null as Date | null, notes: '' });

function openNew() {
    form.value = { name: '', client: '', site: '', plannedStart: null, plannedEnd: null, notes: '' };
    formVisible.value = true;
}

async function savePlan() {
    if (!form.value.name) {
        toast.add({ severity: 'warn', summary: 'Required', detail: 'Plan name is required.', life: 3000 });
        return;
    }
    saving.value = true;
    try {
        const newPlan = await createStepOutPlan({ ...form.value, status: 'Draft' });
        formVisible.value = false;
        toast.add({ severity: 'success', summary: 'Plan Created', life: 2000 });
        router.push(`/planning/step-out-plans/${newPlan.planId}`);
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not create plan.', life: 3000 });
    } finally {
        saving.value = false;
    }
}

function confirmDelete(plan: any) {
    confirm.require({
        message: `Delete plan "${plan.name}"? This will also delete all steps.`,
        header: 'Confirm Delete',
        icon: 'pi pi-trash',
        acceptSeverity: 'danger',
        accept: async () => {
            try {
                await deleteStepOutPlan(plan.planId);
                toast.add({ severity: 'success', summary: 'Deleted', life: 2000 });
                await load();
            } catch {
                toast.add({ severity: 'error', summary: 'Error', detail: 'Could not delete plan.', life: 3000 });
            }
        },
    });
}

async function load() {
    loading.value = true;
    error.value = false;
    try {
        plans.value = await listStepOutPlans();
    } catch (e: any) {
        if (!e.response) {
            errorMessage.value = 'Could not reach the API. It may still be starting — try refreshing in a moment.';
        } else if (e.response.status === 401) {
            errorMessage.value = 'Session expired. Redirecting to login...';
        } else {
            errorMessage.value = `API error (${e.response.status}). Could not load plans.`;
        }
        error.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
.planning-view { max-width: 1100px; margin: 0 auto; padding: 1.5rem 0; display: flex; flex-direction: column; gap: 1.5rem; }
.planning-view-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.planning-view-header-left h1 { margin: 0 0 0.25rem; font-size: 1.5rem; font-weight: 700; color: var(--text-color); }
.planning-view-header-left p { margin: 0; color: var(--text-color-secondary); font-size: 0.88rem; }
.planning-header-actions { display: flex; gap: 0.5rem; align-items: center; }
.planning-filters { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; }
.planning-empty { font-size: 0.85rem; color: var(--text-color-secondary); }
.planning-empty-cell { color: var(--text-color-secondary); font-size: 0.85rem; }
.step-count { font-size: 0.85rem; color: var(--text-color-secondary); }
.form-grid { display: flex; flex-direction: column; gap: 1rem; }
.form-field { display: flex; flex-direction: column; gap: 0.35rem; }
.form-field label { font-size: 0.82rem; font-weight: 600; color: var(--text-color-secondary); }
.form-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
</style>
