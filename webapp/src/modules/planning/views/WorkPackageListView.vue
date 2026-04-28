<template>
    <div class="planning-view">
        <div class="planning-view-header">
            <div class="planning-view-header-left">
                <h1>Work Packages</h1>
                <p>Work packages generated from step-out plans, ready for scheduling assignment.</p>
            </div>
            <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load work packages. Make sure the API is running.
        </Message>

        <!-- Filters -->
        <div class="planning-filters">
            <Dropdown v-model="statusFilter" :options="statusOptions" optionLabel="label" optionValue="value"
                placeholder="All Status" showClear class="w-10rem" @change="applyFilters" />
            <ToggleButton v-model="readyOnly" onLabel="Ready Only" offLabel="All Packages"
                onIcon="pi pi-check" offIcon="pi pi-list" @change="applyFilters" />
            <InputText v-model="search" placeholder="Search packages..." class="flex-1 min-w-10rem" @input="applyFilters" />
            <Tag :value="`${filtered.length} packages`" severity="info" />
        </div>

        <DataTable :value="filtered" :loading="loading" stripedRows dataKey="packageId" size="small"
            :rows="25" paginator :rowsPerPageOptions="[10, 25, 50]">
            <Column field="title" header="Title" sortable />
            <Column field="craftCode" header="Craft" style="width:90px">
                <template #body="{ data }">
                    <Tag :value="data.craftCode" severity="info" />
                </template>
            </Column>
            <Column field="requiredPeople" header="People" style="width:80px" />
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
            <Column header="Ready for Scheduling" style="width:150px">
                <template #body="{ data }">
                    <ToggleButton
                        :modelValue="data.readyForScheduling"
                        onLabel="Ready"
                        offLabel="Not Ready"
                        onIcon="pi pi-check"
                        offIcon="pi pi-clock"
                        size="small"
                        @change="toggleReady(data)"
                    />
                </template>
            </Column>
            <template #empty>
                <span class="planning-empty">
                    No work packages found. Generate them from a step-out plan using the "Generate Work Packages" button.
                </span>
            </template>
        </DataTable>
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
const packages = ref<any[]>([]);
const statusFilter = ref<string | null>(null);
const readyOnly = ref(false);
const search = ref('');

const statusOptions = [
    { label: 'Draft', value: 'Draft' },
    { label: 'Active', value: 'Active' },
    { label: 'Complete', value: 'Complete' },
];

const filtered = computed(() => {
    let list = packages.value;
    if (statusFilter.value) list = list.filter(p => p.status === statusFilter.value);
    if (readyOnly.value) list = list.filter(p => p.readyForScheduling);
    if (search.value.trim()) {
        const q = search.value.toLowerCase();
        list = list.filter(p => p.title?.toLowerCase().includes(q) || p.craftCode?.toLowerCase().includes(q));
    }
    return list;
});

function applyFilters() { /* computed */ }

function statusSeverity(s: string) {
    if (s === 'Active') return 'success';
    if (s === 'Complete') return 'info';
    return 'warning';
}

function fmtDate(d: string | null | undefined) {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: '2-digit' });
}

async function toggleReady(pkg: any) {
    const newVal = !pkg.readyForScheduling;
    try {
        await apiStore.api.value.put(`/api/v1/planning/work-packages/${pkg.packageId}`, {
            ...pkg,
            readyForScheduling: newVal,
        });
        pkg.readyForScheduling = newVal;
        toast.add({
            severity: 'success',
            summary: newVal ? 'Sent to Scheduling' : 'Removed from Scheduling',
            life: 2000,
        });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not update package.', life: 3000 });
    }
}

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const { data } = await apiStore.api.value.get('/api/v1/planning/work-packages');
        packages.value = data;
    } catch {
        error.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
.planning-view { max-width: 1200px; margin: 0 auto; padding: 1.5rem 0; display: flex; flex-direction: column; gap: 1.5rem; }
.planning-view-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.planning-view-header-left h1 { margin: 0 0 0.25rem; font-size: 1.5rem; font-weight: 700; color: var(--text-color); }
.planning-view-header-left p { margin: 0; color: var(--text-color-secondary); font-size: 0.88rem; }
.planning-filters { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; }
.planning-empty { font-size: 0.85rem; color: var(--text-color-secondary); }
</style>
