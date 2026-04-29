<template>
    <div class="sched-view">
        <div class="sched-view-header">
            <div>
                <h1>Assignments</h1>
                <p>Assign resources to jobs with double-booking and certification conflict detection.</p>
            </div>
            <div class="sched-header-actions">
                <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
                <Button label="New Assignment" icon="pi pi-plus" @click="openNew" />
            </div>
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load assignments. Make sure the API is running.
        </Message>

        <!-- Filters -->
        <div class="sched-filters">
            <InputText v-model="search" placeholder="Search resource or job..." class="flex-1 min-w-10rem" @input="applyFilters" />
            <Tag :value="`${filtered.length} assignments`" severity="info" />
        </div>

        <DataTable :value="filtered" :loading="loading" stripedRows dataKey="assignmentId" size="small"
            class="ent-grid" :rows="25" paginator :rowsPerPageOptions="[10, 25, 50]">
            <Column field="resource.name" header="Resource" style="width:160px" sortable>
                <template #body="{ data }">
                    <span class="ent-truncate">{{ data.resource?.name }}</span>
                </template>
            </Column>
            <Column field="jobName" header="Job" sortable>
                <template #body="{ data }">
                    <span class="ent-truncate">{{ data.jobName }}</span>
                </template>
            </Column>
            <Column field="craftCode" header="Craft" style="width:72px">
                <template #body="{ data }">
                    <Tag :value="data.craftCode" severity="info" />
                </template>
            </Column>
            <Column field="shift" header="Shift" style="width:80px">
                <template #body="{ data }">
                    <Tag :value="data.shift" severity="secondary" />
                </template>
            </Column>
            <Column header="Start" style="width:100px" sortable sortField="start">
                <template #body="{ data }">{{ fmtDate(data.start) }}</template>
            </Column>
            <Column header="End" style="width:100px" sortable sortField="end">
                <template #body="{ data }">{{ fmtDate(data.end) }}</template>
            </Column>
            <Column field="status" header="Status" style="width:96px">
                <template #body="{ data }">
                    <Tag :value="data.status" :severity="statusSeverity(data.status)" />
                </template>
            </Column>
            <Column header="" style="width:68px">
                <template #body="{ data }">
                    <div class="row-actions">
                        <Button icon="pi pi-pencil" text size="small" @click="openEdit(data)" />
                        <Button icon="pi pi-trash" text severity="danger" size="small" @click="confirmDelete(data)" />
                    </div>
                </template>
            </Column>
            <template #empty>
                <span class="sched-empty">No assignments found. Create one using the button above or from the Jobs Board.</span>
            </template>
        </DataTable>

        <!-- Create/Edit Dialog -->
        <Dialog v-model:visible="formVisible" :header="editMode ? 'Edit Assignment' : 'New Assignment'" modal :style="{ width: '500px' }">
            <div class="form-grid">
                <div class="form-field">
                    <label>Resource</label>
                    <Dropdown v-model="form.resourceId" :options="resources" optionLabel="name" optionValue="resourceId"
                        placeholder="Select resource" class="w-full" filter />
                </div>
                <div class="form-field">
                    <label>Job (Demand Source)</label>
                    <Dropdown v-model="selectedJob" :options="jobs" optionLabel="name"
                        placeholder="Select job" class="w-full" filter />
                </div>
                <div class="form-field">
                    <label>Craft</label>
                    <InputText v-model="form.craftCode" placeholder="e.g. PP" class="w-full" />
                </div>
                <div class="form-field-row">
                    <div class="form-field">
                        <label>Start</label>
                        <Calendar v-model="form.start" showIcon dateFormat="yy-mm-dd" class="w-full" />
                    </div>
                    <div class="form-field">
                        <label>End</label>
                        <Calendar v-model="form.end" showIcon dateFormat="yy-mm-dd" class="w-full" />
                    </div>
                </div>
                <div class="form-field">
                    <label>Shift</label>
                    <Dropdown v-model="form.shift" :options="['Day', 'Night', 'Rotation']" class="w-full" />
                </div>
                <div class="form-field">
                    <label>Status</label>
                    <Dropdown v-model="form.status" :options="['Planned', 'Confirmed', 'Cancelled']" class="w-full" />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="formVisible = false" />
                <Button :label="editMode ? 'Save Changes' : 'Create Assignment'" :loading="saving" @click="saveAssignment" />
            </template>
        </Dialog>

        <!-- Conflict Warning -->
        <Dialog v-model:visible="conflictVisible" header="Scheduling Conflicts Detected" modal :style="{ width: '440px' }">
            <div class="conflict-list">
                <div v-for="c in conflicts" :key="c.type" class="conflict-item">
                    <i class="pi pi-exclamation-triangle" />
                    <span>{{ c.message }}</span>
                </div>
            </div>
            <p class="conflict-note">The assignment was saved. Review and adjust if needed.</p>
            <template #footer>
                <Button label="OK" @click="conflictVisible = false" />
            </template>
        </Dialog>

        <ConfirmDialog />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import { useApiStore } from '@/stores/apiStore';

const apiStore = useApiStore();
const toast = useToast();
const confirm = useConfirm();

const loading = ref(false);
const error = ref(false);
const saving = ref(false);
const assignments = ref<any[]>([]);
const resources = ref<any[]>([]);
const jobs = ref<any[]>([]);
const search = ref('');

const filtered = computed(() => {
    if (!search.value.trim()) return assignments.value;
    const q = search.value.toLowerCase();
    return assignments.value.filter(a =>
        a.resource?.name?.toLowerCase().includes(q) ||
        a.jobName?.toLowerCase().includes(q)
    );
});

function applyFilters() { /* computed */ }

function fmtDate(d: string | null | undefined) {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function statusSeverity(s: string) {
    if (s === 'Confirmed') return 'success';
    if (s === 'Cancelled') return 'danger';
    return 'info';
}

const formVisible = ref(false);
const editMode = ref(false);
const editId = ref<number | null>(null);
const selectedJob = ref<any>(null);
const form = ref({ resourceId: null as number | null, craftCode: '', start: null as Date | null, end: null as Date | null, shift: 'Day', status: 'Planned' });
const conflictVisible = ref(false);
const conflicts = ref<any[]>([]);

watch(selectedJob, (job) => {
    if (job) form.value.craftCode = form.value.craftCode || '';
});

function openNew() {
    editMode.value = false;
    editId.value = null;
    selectedJob.value = null;
    form.value = { resourceId: null, craftCode: '', start: null, end: null, shift: 'Day', status: 'Planned' };
    formVisible.value = true;
}

function openEdit(a: any) {
    editMode.value = true;
    editId.value = a.assignmentId;
    selectedJob.value = jobs.value.find(j => j.sourceType === a.jobSourceType && j.sourceId === a.jobSourceId) ?? null;
    form.value = {
        resourceId: a.resourceId,
        craftCode: a.craftCode,
        start: a.start ? new Date(a.start) : null,
        end: a.end ? new Date(a.end) : null,
        shift: a.shift,
        status: a.status,
    };
    formVisible.value = true;
}

async function saveAssignment() {
    if (!form.value.resourceId || !form.value.start || !form.value.end) {
        toast.add({ severity: 'warn', summary: 'Required', detail: 'Resource, start, and end are required.', life: 3000 });
        return;
    }
    saving.value = true;
    try {
        const payload = {
            resourceId: form.value.resourceId,
            jobSourceType: selectedJob.value?.sourceType ?? 'Estimate',
            jobSourceId: selectedJob.value?.sourceId ?? 0,
            jobName: selectedJob.value?.name ?? '',
            craftCode: form.value.craftCode || 'PP',
            start: form.value.start,
            end: form.value.end,
            shift: form.value.shift,
            status: form.value.status,
        };
        let data: any;
        if (editMode.value && editId.value) {
            const resp = await apiStore.api.put(`/api/v1/scheduling/assignments/${editId.value}`, payload);
            data = resp.data;
        } else {
            const resp = await apiStore.api.post('/api/v1/scheduling/assignments', payload);
            data = resp.data;
        }
        formVisible.value = false;
        toast.add({ severity: 'success', summary: editMode.value ? 'Saved' : 'Assignment Created', life: 2000 });
        if (data.hasConflicts) {
            conflicts.value = data.conflicts;
            conflictVisible.value = true;
        }
        await load();
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not save assignment.', life: 3000 });
    } finally {
        saving.value = false;
    }
}

function confirmDelete(a: any) {
    confirm.require({
        message: `Delete assignment for "${a.resource?.name ?? 'this resource'}"?`,
        header: 'Confirm Delete',
        icon: 'pi pi-trash',
        acceptSeverity: 'danger',
        accept: async () => {
            try {
                await apiStore.api.delete(`/api/v1/scheduling/assignments/${a.assignmentId}`);
                toast.add({ severity: 'success', summary: 'Deleted', life: 2000 });
                await load();
            } catch {
                toast.add({ severity: 'error', summary: 'Error', detail: 'Could not delete.', life: 3000 });
            }
        },
    });
}

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const [assignResp, resResp, jobsResp] = await Promise.all([
            apiStore.api.get('/api/v1/scheduling/assignments'),
            apiStore.api.get('/api/v1/scheduling/resources'),
            apiStore.api.get('/api/v1/scheduling/jobs'),
        ]);
        assignments.value = assignResp.data;
        resources.value = resResp.data;
        jobs.value = jobsResp.data;
    } catch {
        error.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
.sched-view { max-width: 1200px; margin: 0 auto; padding: 1.5rem 0; display: flex; flex-direction: column; gap: 1.5rem; }
.sched-view-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.sched-view-header h1 { margin: 0 0 0.25rem; font-size: 1.5rem; font-weight: 700; color: var(--text-color); }
.sched-view-header p { margin: 0; color: var(--text-color-secondary); font-size: 0.88rem; }
.sched-header-actions { display: flex; gap: 0.5rem; align-items: center; }
.sched-filters { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; }
.sched-empty { font-size: 0.85rem; color: var(--text-color-secondary); }
.form-grid { display: flex; flex-direction: column; gap: 1rem; }
.form-field { display: flex; flex-direction: column; gap: 0.35rem; }
.form-field label { font-size: 0.82rem; font-weight: 600; color: var(--text-color-secondary); }
.form-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.conflict-list { display: flex; flex-direction: column; gap: 0.75rem; margin-bottom: 1rem; }
.conflict-item { display: flex; align-items: flex-start; gap: 0.5rem; }
.conflict-item i { color: var(--orange-500, #f97316); margin-top: 0.15rem; flex-shrink: 0; }
.conflict-note { font-size: 0.85rem; color: var(--text-color-secondary); margin: 0; }
</style>
