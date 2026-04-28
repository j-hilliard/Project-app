<template>
    <div class="sched-view">
        <div class="sched-view-header">
            <div>
                <h1>Ending Soon</h1>
                <p>Resources whose assignments end within the selected window. Plan their next deployment.</p>
            </div>
            <div class="sched-header-actions">
                <SelectButton v-model="days" :options="dayOptions" optionLabel="label" optionValue="value" @change="load" />
                <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
            </div>
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load roll-off data. Make sure the API is running.
        </Message>

        <div v-if="!loading && endingSoon.length === 0 && !error" class="sched-empty-state">
            <i class="pi pi-check-circle" />
            <p>No resources ending within {{ days }} days. Great coverage!</p>
        </div>

        <DataTable v-else :value="enriched" :loading="loading" stripedRows dataKey="assignmentId" size="small">
            <Column field="resource.name" header="Resource" sortable />
            <Column field="resource.craftCode" header="Craft" style="width:90px">
                <template #body="{ data }">
                    <Tag :value="data.resource?.craftCode" severity="info" />
                </template>
            </Column>
            <Column field="jobName" header="Current Job" sortable />
            <Column header="End Date" style="width:120px" sortable sortField="end">
                <template #body="{ data }">{{ fmtDate(data.end) }}</template>
            </Column>
            <Column header="Days Left" style="width:90px">
                <template #body="{ data }">
                    <span :class="daysLeftClass(data.end)">{{ daysLeft(data.end) }}</span>
                </template>
            </Column>
            <Column header="Next Match" style="width:160px">
                <template #body="{ data }">
                    <span v-if="nextMatch(data.resource?.craftCode)" class="next-match">
                        {{ nextMatch(data.resource?.craftCode)?.resourceName }}
                    </span>
                    <span v-else class="sched-empty">—</span>
                </template>
            </Column>
            <Column header="" style="width:130px">
                <template #body="{ data }">
                    <Button label="Re-Assign" size="small" outlined @click="openAssign(data)" />
                </template>
            </Column>
            <template #empty>
                <span class="sched-empty">No assignments ending within {{ days }} days.</span>
            </template>
        </DataTable>

        <!-- Assignment Dialog -->
        <Dialog v-model:visible="assignVisible" header="New Assignment" modal :style="{ width: '480px' }">
            <div class="form-grid">
                <div class="form-field">
                    <label>Resource</label>
                    <InputText :value="assignResource?.resource?.name" disabled />
                </div>
                <div class="form-field">
                    <label>Job (Demand Source)</label>
                    <Dropdown v-model="selectedJob" :options="jobs" optionLabel="name"
                        placeholder="Select job" class="w-full" filter />
                </div>
                <div class="form-field">
                    <label>Craft</label>
                    <InputText v-model="assignForm.craftCode" class="w-full" />
                </div>
                <div class="form-field-row">
                    <div class="form-field">
                        <label>Start</label>
                        <Calendar v-model="assignForm.start" showIcon dateFormat="yy-mm-dd" class="w-full" />
                    </div>
                    <div class="form-field">
                        <label>End</label>
                        <Calendar v-model="assignForm.end" showIcon dateFormat="yy-mm-dd" class="w-full" />
                    </div>
                </div>
                <div class="form-field">
                    <label>Shift</label>
                    <Dropdown v-model="assignForm.shift" :options="['Day', 'Night', 'Rotation']" class="w-full" />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="assignVisible = false" />
                <Button label="Create Assignment" :loading="saving" @click="saveAssign" />
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
const days = ref(7);
const endingSoon = ref<any[]>([]);
const suggestedMatches = ref<any[]>([]);
const jobs = ref<any[]>([]);

const dayOptions = [
    { label: '7 days', value: 7 },
    { label: '14 days', value: 14 },
    { label: '30 days', value: 30 },
];

const enriched = computed(() => endingSoon.value);

function nextMatch(craftCode: string | null | undefined) {
    if (!craftCode) return null;
    return suggestedMatches.value.find(m => m.craftCode === craftCode) ?? null;
}

function fmtDate(d: string | null | undefined) {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: '2-digit' });
}

function daysLeft(end: string | null | undefined) {
    if (!end) return '—';
    const diff = Math.ceil((new Date(end).getTime() - Date.now()) / 86400000);
    return diff <= 0 ? 'Today' : `${diff}d`;
}

function daysLeftClass(end: string | null | undefined) {
    if (!end) return '';
    const diff = Math.ceil((new Date(end).getTime() - Date.now()) / 86400000);
    if (diff <= 2) return 'days-urgent';
    if (diff <= 5) return 'days-soon';
    return 'days-ok';
}

const assignVisible = ref(false);
const assignResource = ref<any>(null);
const selectedJob = ref<any>(null);
const assignForm = ref({ craftCode: '', start: null as Date | null, end: null as Date | null, shift: 'Day' });

function openAssign(assignment: any) {
    assignResource.value = assignment;
    selectedJob.value = null;
    assignForm.value = {
        craftCode: assignment.resource?.craftCode ?? '',
        start: assignment.end ? new Date(assignment.end) : null,
        end: null,
        shift: 'Day',
    };
    assignVisible.value = true;
}

async function saveAssign() {
    if (!selectedJob.value || !assignForm.value.start || !assignForm.value.end) {
        toast.add({ severity: 'warn', summary: 'Required', detail: 'Job, start, and end are required.', life: 3000 });
        return;
    }
    saving.value = true;
    try {
        const payload = {
            resourceId: assignResource.value.resource?.resourceId ?? assignResource.value.resourceId,
            jobSourceType: selectedJob.value.sourceType,
            jobSourceId: selectedJob.value.sourceId,
            jobName: selectedJob.value.name,
            craftCode: assignForm.value.craftCode || 'PP',
            start: assignForm.value.start,
            end: assignForm.value.end,
            shift: assignForm.value.shift,
            status: 'Planned',
        };
        await apiStore.api.value.post('/api/v1/scheduling/assignments', payload);
        assignVisible.value = false;
        toast.add({ severity: 'success', summary: 'Assignment Created', life: 2500 });
        await load();
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
        const [endResp, matchResp, jobsResp] = await Promise.all([
            apiStore.api.value.get(`/api/v1/scheduling/ending-soon?days=${days.value}`),
            apiStore.api.value.get('/api/v1/scheduling/suggested-matches'),
            apiStore.api.value.get('/api/v1/scheduling/jobs'),
        ]);
        endingSoon.value = endResp.data;
        suggestedMatches.value = matchResp.data;
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
.sched-view { max-width: 1100px; margin: 0 auto; padding: 1.5rem 0; display: flex; flex-direction: column; gap: 1.5rem; }
.sched-view-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.sched-view-header h1 { margin: 0 0 0.25rem; font-size: 1.5rem; font-weight: 700; color: var(--text-color); }
.sched-view-header p { margin: 0; color: var(--text-color-secondary); font-size: 0.88rem; }
.sched-header-actions { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; }
.sched-empty { font-size: 0.85rem; color: var(--text-color-secondary); }
.sched-empty-state {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    padding: 3rem 2rem;
    text-align: center;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
}
.sched-empty-state i { font-size: 2rem; color: var(--green-500, #22c55e); }
.sched-empty-state p { margin: 0; color: var(--text-color-secondary); }
.days-urgent { color: var(--red-500, #ef4444); font-weight: 700; }
.days-soon { color: var(--orange-500, #f97316); font-weight: 600; }
.days-ok { color: var(--text-color-secondary); }
.next-match { font-size: 0.82rem; color: var(--primary-color); font-weight: 500; }
.form-grid { display: flex; flex-direction: column; gap: 1rem; }
.form-field { display: flex; flex-direction: column; gap: 0.35rem; }
.form-field label { font-size: 0.82rem; font-weight: 600; color: var(--text-color-secondary); }
.form-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
</style>
