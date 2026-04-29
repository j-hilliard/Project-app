<template>
    <div class="planning-view">
        <!-- Header -->
        <div class="planning-view-header">
            <div class="planning-view-header-left">
                <Button icon="pi pi-arrow-left" text size="small" @click="router.push('/planning/step-out-plans')" />
                <div>
                    <h1>{{ plan?.name ?? 'Step-Out Plan' }}</h1>
                    <p v-if="plan">{{ plan.client }} {{ plan.site ? `— ${plan.site}` : '' }}</p>
                </div>
            </div>
            <div class="planning-header-actions">
                <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
                <Button label="Generate Work Packages" icon="pi pi-bolt" outlined :loading="generating"
                    @click="generatePackages" :disabled="!planId" />
            </div>
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load plan. Make sure the API is running.
        </Message>

        <!-- Plan Header Card -->
        <div v-if="plan" class="plan-header-card">
            <div class="plan-meta-grid">
                <div class="plan-meta-item">
                    <span class="plan-meta-label">Status</span>
                    <Dropdown v-model="plan.status" :options="planStatusOptions" @change="savePlanStatus" size="small" />
                </div>
                <div class="plan-meta-item">
                    <span class="plan-meta-label">Client</span>
                    <span>{{ plan.client || '—' }}</span>
                </div>
                <div class="plan-meta-item">
                    <span class="plan-meta-label">Site</span>
                    <span>{{ plan.site || '—' }}</span>
                </div>
                <div class="plan-meta-item">
                    <span class="plan-meta-label">Planned Start</span>
                    <span>{{ fmtDate(plan.plannedStart) }}</span>
                </div>
                <div class="plan-meta-item">
                    <span class="plan-meta-label">Planned End</span>
                    <span>{{ fmtDate(plan.plannedEnd) }}</span>
                </div>
                <div class="plan-meta-item">
                    <span class="plan-meta-label">Steps</span>
                    <span>{{ steps.length }}</span>
                </div>
            </div>
        </div>

        <!-- Steps Section -->
        <div class="steps-section">
            <div class="steps-section-header">
                <h2>Steps</h2>
                <Button label="Add Step" icon="pi pi-plus" size="small" @click="openAddStep" />
            </div>

            <DataTable :value="steps" :loading="loading" stripedRows dataKey="stepId" size="small" class="ent-grid">
                <Column field="stepCode" header="Code" style="width:80px" sortable />
                <Column field="title" header="Title" sortable />
                <Column field="craftCode" header="Craft" style="width:90px">
                    <template #body="{ data }">
                        <Tag v-if="data.craftCode" :value="data.craftCode" severity="info" />
                        <span v-else class="planning-empty-cell">—</span>
                    </template>
                </Column>
                <Column field="requiredPeople" header="People" style="width:80px" />
                <Column field="durationMinutes" header="Duration" style="width:100px">
                    <template #body="{ data }">
                        {{ data.durationMinutes ? `${data.durationMinutes} min` : '—' }}
                    </template>
                </Column>
                <Column header="Parallel" style="width:80px">
                    <template #body="{ data }">
                        <i :class="data.isParallel ? 'pi pi-check text-green-500' : 'pi pi-minus text-color-secondary'" />
                    </template>
                </Column>
                <Column field="status" header="Status" style="width:100px">
                    <template #body="{ data }">
                        <Tag :value="data.status" :severity="stepStatusSeverity(data.status)" />
                    </template>
                </Column>
                <Column header="" style="width:90px">
                    <template #body="{ data }">
                        <Button icon="pi pi-pencil" text size="small" @click.stop="openEditStep(data)" />
                        <Button icon="pi pi-trash" text severity="danger" size="small" @click.stop="deleteStep(data)" />
                    </template>
                </Column>
                <template #empty>
                    <span class="planning-empty">No steps yet. Add a step to start building the execution plan.</span>
                </template>
            </DataTable>
        </div>

        <!-- Work Packages Section -->
        <div v-if="workPackages.length > 0" class="steps-section">
            <div class="steps-section-header">
                <h2>Generated Work Packages</h2>
                <Button label="View All" text size="small" @click="router.push('/planning/work-packages')" />
            </div>
            <DataTable :value="workPackages" stripedRows dataKey="packageId" size="small"
                class="ent-grid ent-grid-clickable"
                @row-click="(e) => router.push(`/planning/work-packages/${e.data.packageId}`)">
                <Column field="title" header="Title" />
                <Column field="craftCode" header="Craft" style="width:90px">
                    <template #body="{ data }">
                        <Tag :value="data.craftCode" severity="info" />
                    </template>
                </Column>
                <Column field="requiredPeople" header="People" style="width:80px" />
                <Column field="status" header="Status" style="width:100px">
                    <template #body="{ data }">
                        <Tag :value="data.status" severity="info" />
                    </template>
                </Column>
                <Column header="Ready" style="width:80px">
                    <template #body="{ data }">
                        <i :class="data.readyForScheduling ? 'pi pi-check-circle text-green-500' : 'pi pi-clock text-color-secondary'" />
                    </template>
                </Column>
            </DataTable>
        </div>

        <!-- Add/Edit Step Dialog -->
        <Dialog v-model:visible="stepFormVisible" :header="editStepMode ? 'Edit Step' : 'Add Step'" modal :style="{ width: '480px' }">
            <div class="form-grid">
                <div class="form-field-row">
                    <div class="form-field">
                        <label>Step Code</label>
                        <InputText v-model="stepForm.stepCode" placeholder="e.g. 1, 1.2, 1.5" class="w-full" />
                    </div>
                    <div class="form-field">
                        <label>Sort Order</label>
                        <InputNumber v-model="stepForm.sortOrder" :minFractionDigits="0" :maxFractionDigits="4" class="w-full" />
                    </div>
                </div>
                <div class="form-field">
                    <label>Title</label>
                    <InputText v-model="stepForm.title" class="w-full" />
                </div>
                <div class="form-field">
                    <label>Description</label>
                    <Textarea v-model="stepForm.description" rows="2" class="w-full" />
                </div>
                <div class="form-field-row">
                    <div class="form-field">
                        <label>Craft Code</label>
                        <InputText v-model="stepForm.craftCode" placeholder="e.g. PP" class="w-full" />
                    </div>
                    <div class="form-field">
                        <label>Required People</label>
                        <InputNumber v-model="stepForm.requiredPeople" class="w-full" />
                    </div>
                </div>
                <div class="form-field-row">
                    <div class="form-field">
                        <label>Duration (minutes)</label>
                        <InputNumber v-model="stepForm.durationMinutes" class="w-full" />
                    </div>
                    <div class="form-field form-field-inline">
                        <label>Parallel Step</label>
                        <ToggleButton v-model="stepForm.isParallel" onLabel="Yes" offLabel="No" />
                    </div>
                </div>
                <div class="form-field">
                    <label>Status</label>
                    <Dropdown v-model="stepForm.status" :options="['Pending', 'InProgress', 'Complete', 'Blocked']" class="w-full" />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="stepFormVisible = false" />
                <Button :label="editStepMode ? 'Save Step' : 'Add Step'" :loading="saving" @click="saveStep" />
            </template>
        </Dialog>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useToast } from 'primevue/usetoast';
import { usePlanningService } from '../services/usePlanningService';
import { useFormatters } from '@/ui';
import { stepStatusSeverity } from '@/ui';

const router = useRouter();
const route = useRoute();
const toast = useToast();
const {
    getStepOutPlan, updateStepOutPlan,
    createStep, updateStep, deleteStep: deleteStepApi,
    generateWorkPackages,
} = usePlanningService();
const { fmtDateLong: fmtDate } = useFormatters();

const planId = computed(() => route.params.id ? Number(route.params.id) : null);
const loading = ref(false);
const error = ref(false);
const saving = ref(false);
const generating = ref(false);
const plan = ref<any>(null);
const steps = ref<any[]>([]);
const workPackages = ref<any[]>([]);

const planStatusOptions = ['Draft', 'Active', 'Complete', 'Archived'];

async function savePlanStatus() {
    if (!planId.value || !plan.value) return;
    try {
        await updateStepOutPlan(planId.value, plan.value);
        toast.add({ severity: 'success', summary: 'Status Updated', life: 2000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not update plan.', life: 3000 });
    }
}

// Steps
const stepFormVisible = ref(false);
const editStepMode = ref(false);
const editStepId = ref<number | null>(null);
const stepForm = ref({
    stepCode: '', sortOrder: 1.0, title: '', description: '',
    craftCode: '', requiredPeople: 1, durationMinutes: null as number | null,
    isParallel: false, status: 'Pending',
});

function openAddStep() {
    editStepMode.value = false;
    editStepId.value = null;
    stepForm.value = { stepCode: '', sortOrder: steps.value.length + 1, title: '', description: '', craftCode: '', requiredPeople: 1, durationMinutes: null, isParallel: false, status: 'Pending' };
    stepFormVisible.value = true;
}

function openEditStep(step: any) {
    editStepMode.value = true;
    editStepId.value = step.stepId;
    stepForm.value = {
        stepCode: step.stepCode, sortOrder: step.sortOrder, title: step.title,
        description: step.description ?? '', craftCode: step.craftCode ?? '',
        requiredPeople: step.requiredPeople ?? 1, durationMinutes: step.durationMinutes ?? null,
        isParallel: step.isParallel ?? false, status: step.status,
    };
    stepFormVisible.value = true;
}

async function saveStep() {
    if (!stepForm.value.title || !stepForm.value.stepCode) {
        toast.add({ severity: 'warn', summary: 'Required', detail: 'Step code and title are required.', life: 3000 });
        return;
    }
    if (!planId.value) return;
    saving.value = true;
    try {
        if (editStepMode.value && editStepId.value) {
            await updateStep(planId.value, editStepId.value, stepForm.value);
        } else {
            await createStep(planId.value, stepForm.value);
        }
        stepFormVisible.value = false;
        toast.add({ severity: 'success', summary: editStepMode.value ? 'Step Saved' : 'Step Added', life: 2000 });
        await load();
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not save step.', life: 3000 });
    } finally {
        saving.value = false;
    }
}

async function deleteStep(step: any) {
    if (!planId.value) return;
    try {
        await deleteStepApi(planId.value, step.stepId);
        steps.value = steps.value.filter(s => s.stepId !== step.stepId);
        toast.add({ severity: 'success', summary: 'Step Deleted', life: 2000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not delete step.', life: 3000 });
    }
}

async function generatePackages() {
    if (!planId.value) return;
    generating.value = true;
    try {
        await generateWorkPackages(planId.value);
        toast.add({ severity: 'success', summary: 'Work Packages Generated', life: 2500 });
        await load();
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not generate work packages.', life: 3000 });
    } finally {
        generating.value = false;
    }
}

async function load() {
    if (!planId.value) return;
    loading.value = true;
    error.value = false;
    try {
        const data = await getStepOutPlan(planId.value);
        plan.value = data;
        steps.value = (data.steps ?? []).sort((a: any, b: any) => a.sortOrder - b.sortOrder);
        workPackages.value = data.workPackages ?? [];
    } catch {
        error.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(() => {
    if (!planId.value) {
        router.replace('/planning/step-out-plans');
        return;
    }
    load();
});
</script>

<style scoped>
.planning-view { max-width: 1100px; margin: 0 auto; padding: 1.5rem 0; display: flex; flex-direction: column; gap: 1.5rem; }
.planning-view-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.planning-view-header-left { display: flex; align-items: center; gap: 0.75rem; }
.planning-view-header-left h1 { margin: 0 0 0.1rem; font-size: 1.4rem; font-weight: 700; color: var(--text-color); }
.planning-view-header-left p { margin: 0; color: var(--text-color-secondary); font-size: 0.85rem; }
.planning-header-actions { display: flex; gap: 0.5rem; align-items: center; }
.planning-empty { font-size: 0.85rem; color: var(--text-color-secondary); }
.planning-empty-cell { color: var(--text-color-secondary); font-size: 0.85rem; }

.plan-header-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    padding: 1rem 1.5rem;
}
.plan-meta-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: 1rem; }
.plan-meta-item { display: flex; flex-direction: column; gap: 0.25rem; }
.plan-meta-label { font-size: 0.75rem; font-weight: 600; color: var(--text-color-secondary); text-transform: uppercase; letter-spacing: 0.04em; }

.steps-section { display: flex; flex-direction: column; gap: 0.75rem; }
.steps-section-header { display: flex; align-items: center; justify-content: space-between; }
.steps-section-header h2 { margin: 0; font-size: 1rem; font-weight: 600; color: var(--text-color); }

.form-grid { display: flex; flex-direction: column; gap: 1rem; }
.form-field { display: flex; flex-direction: column; gap: 0.35rem; }
.form-field label { font-size: 0.82rem; font-weight: 600; color: var(--text-color-secondary); }
.form-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.form-field-inline { flex-direction: row; align-items: center; justify-content: space-between; }
</style>
