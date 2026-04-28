<template>
    <div class="sched-view">
        <div class="sched-view-header">
            <div>
                <h1>Jobs Board</h1>
                <p>All active demand — awarded/pending estimates, approved staffing plans, and work packages ready for scheduling.</p>
            </div>
            <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load demand. Make sure the API is running.
        </Message>

        <!-- Filters -->
        <div class="sched-filters">
            <Dropdown
                v-model="sourceFilter"
                :options="sourceOptions"
                optionLabel="label"
                optionValue="value"
                placeholder="All Sources"
                class="w-12rem"
                showClear
                @change="applyFilters"
            />
            <InputText v-model="search" placeholder="Search jobs..." class="flex-1 min-w-10rem" @input="applyFilters" />
            <Tag :value="`${filtered.length} jobs`" severity="info" />
        </div>

        <DataTable :value="filtered" :loading="loading" stripedRows dataKey="sourceId" size="small"
            :rows="25" paginator :rowsPerPageOptions="[10, 25, 50]">
            <Column field="sourceType" header="Source" style="width:130px" sortable>
                <template #body="{ data }">
                    <Tag :value="data.sourceType" :severity="sourceTagSeverity(data.sourceType)" />
                </template>
            </Column>
            <Column field="name" header="Job Name" sortable />
            <Column field="client" header="Client" sortable />
            <Column field="site" header="Site" />
            <Column field="status" header="Status" style="width:100px" sortable>
                <template #body="{ data }">
                    <span :class="statusClass(data.status)">{{ data.status }}</span>
                </template>
            </Column>
            <Column header="Start" style="width:110px" sortable sortField="startDate">
                <template #body="{ data }">{{ fmtDate(data.startDate) }}</template>
            </Column>
            <Column header="End" style="width:110px" sortable sortField="endDate">
                <template #body="{ data }">{{ fmtDate(data.endDate) }}</template>
            </Column>
            <Column header="" style="width:130px">
                <template #body="{ data }">
                    <Button label="Assign" size="small" outlined @click="openAssign(data)" />
                </template>
            </Column>
            <template #empty>
                <span class="sched-empty">No demand found. Ensure estimates are Awarded/Pending or call the seed endpoint.</span>
            </template>
        </DataTable>

        <!-- Assign Resource Dialog -->
        <Dialog v-model:visible="assignVisible" header="New Assignment" modal :style="{ width: '480px' }">
            <div class="assign-form">
                <div class="assign-field">
                    <label>Job</label>
                    <InputText :value="assignJob?.name" disabled />
                </div>
                <div class="assign-field">
                    <label>Resource</label>
                    <Dropdown v-model="assignForm.resourceId" :options="resources" optionLabel="name" optionValue="resourceId"
                        placeholder="Select resource" class="w-full" filter />
                </div>
                <div class="assign-field">
                    <label>Craft</label>
                    <InputText v-model="assignForm.craftCode" placeholder="e.g. PP" />
                </div>
                <div class="assign-field-row">
                    <div class="assign-field">
                        <label>Start</label>
                        <Calendar v-model="assignForm.start" showIcon dateFormat="yy-mm-dd" />
                    </div>
                    <div class="assign-field">
                        <label>End</label>
                        <Calendar v-model="assignForm.end" showIcon dateFormat="yy-mm-dd" />
                    </div>
                </div>
                <div class="assign-field">
                    <label>Shift</label>
                    <Dropdown v-model="assignForm.shift" :options="['Day', 'Night', 'Rotation']"
                        placeholder="Day" class="w-full" />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="assignVisible = false" />
                <Button label="Create Assignment" :loading="saving" @click="saveAssign" />
            </template>
        </Dialog>

        <!-- Conflict Warning Dialog -->
        <Dialog v-model:visible="conflictVisible" header="Scheduling Conflicts Detected" modal :style="{ width: '440px' }">
            <div class="conflict-list">
                <div v-for="c in conflicts" :key="c.type" class="conflict-item">
                    <i class="pi pi-exclamation-triangle" />
                    <span>{{ c.message }}</span>
                </div>
            </div>
            <p class="conflict-note">The assignment was saved. Review conflicts and adjust if needed.</p>
            <template #footer>
                <Button label="OK" @click="conflictVisible = false" />
            </template>
        </Dialog>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import { useApiStore } from '@/stores/apiStore';

const apiStore = useApiStore();
const toast = useToast();

const loading = ref(false);
const error = ref(false);
const saving = ref(false);
const jobs = ref<any[]>([]);
const resources = ref<any[]>([]);
const sourceFilter = ref<string | null>(null);
const search = ref('');

const sourceOptions = [
    { label: 'Estimate', value: 'Estimate' },
    { label: 'Staffing Plan', value: 'StaffingPlan' },
    { label: 'Work Package', value: 'WorkPackage' },
];

const filtered = computed(() => {
    let list = jobs.value;
    if (sourceFilter.value) list = list.filter(j => j.sourceType === sourceFilter.value);
    if (search.value.trim()) {
        const q = search.value.toLowerCase();
        list = list.filter(j =>
            j.name?.toLowerCase().includes(q) ||
            j.client?.toLowerCase().includes(q) ||
            j.site?.toLowerCase().includes(q)
        );
    }
    return list;
});

function applyFilters() { /* computed handles it */ }

function sourceTagSeverity(type: string) {
    if (type === 'Estimate') return 'success';
    if (type === 'StaffingPlan') return 'warning';
    return 'info';
}

function statusClass(s: string) {
    const l = s?.toLowerCase();
    if (l === 'awarded') return 'status-awarded';
    if (l === 'pending' || l === 'approved') return 'status-pending';
    return 'status-other';
}

function fmtDate(d: string | null | undefined) {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: '2-digit' });
}

const assignVisible = ref(false);
const conflictVisible = ref(false);
const assignJob = ref<any>(null);
const conflicts = ref<any[]>([]);
const assignForm = ref({ resourceId: null as number | null, craftCode: '', start: null as Date | null, end: null as Date | null, shift: 'Day' });

function openAssign(job: any) {
    assignJob.value = job;
    assignForm.value = { resourceId: null, craftCode: '', start: null, end: null, shift: 'Day' };
    assignVisible.value = true;
}

async function saveAssign() {
    if (!assignForm.value.resourceId || !assignForm.value.start || !assignForm.value.end) {
        toast.add({ severity: 'warn', summary: 'Required', detail: 'Resource, start, and end are required.', life: 3000 });
        return;
    }
    saving.value = true;
    try {
        const payload = {
            resourceId: assignForm.value.resourceId,
            jobSourceType: assignJob.value.sourceType,
            jobSourceId: assignJob.value.sourceId,
            jobName: assignJob.value.name,
            craftCode: assignForm.value.craftCode || 'PP',
            start: assignForm.value.start,
            end: assignForm.value.end,
            shift: assignForm.value.shift,
            status: 'Planned',
        };
        const { data } = await apiStore.api.value.post('/api/v1/scheduling/assignments', payload);
        assignVisible.value = false;
        toast.add({ severity: 'success', summary: 'Assignment Created', life: 2500 });
        await load();
        if (data.hasConflicts) {
            conflicts.value = data.conflicts;
            conflictVisible.value = true;
        }
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not create assignment.', life: 3000 });
    } finally {
        saving.value = false;
    }
}

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const [jobsResp, resResp] = await Promise.all([
            apiStore.api.value.get('/api/v1/scheduling/jobs'),
            apiStore.api.value.get('/api/v1/scheduling/resources'),
        ]);
        jobs.value = jobsResp.data;
        resources.value = resResp.data;
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
.sched-filters { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; }
.sched-empty { font-size: 0.85rem; color: var(--text-color-secondary); }
.status-awarded { color: var(--green-600, #16a34a); font-weight: 600; }
.status-pending { color: var(--orange-500, #f97316); font-weight: 600; }
.status-other { color: var(--text-color-secondary); }
.assign-form { display: flex; flex-direction: column; gap: 1rem; }
.assign-field { display: flex; flex-direction: column; gap: 0.35rem; }
.assign-field label { font-size: 0.82rem; font-weight: 600; color: var(--text-color-secondary); }
.assign-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.conflict-list { display: flex; flex-direction: column; gap: 0.75rem; margin-bottom: 1rem; }
.conflict-item { display: flex; align-items: flex-start; gap: 0.5rem; }
.conflict-item i { color: var(--orange-500, #f97316); margin-top: 0.15rem; flex-shrink: 0; }
.conflict-note { font-size: 0.85rem; color: var(--text-color-secondary); margin: 0; }
</style>
