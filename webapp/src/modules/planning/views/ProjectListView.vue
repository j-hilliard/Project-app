<template>
    <div class="plan-view">
        <div class="plan-view-header">
            <div>
                <h1>Projects</h1>
                <p>Execution projects for this company. Each project traces to an awarded estimate.</p>
            </div>
            <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load projects. Make sure the API is running.
        </Message>

        <!-- Filters -->
        <div class="plan-filters">
            <Dropdown
                v-model="statusFilter"
                :options="statusOptions"
                optionLabel="label"
                optionValue="value"
                placeholder="All Statuses"
                class="w-12rem"
                showClear
                @change="() => {}"
            />
            <InputText v-model="search" placeholder="Search projects..." class="flex-1 min-w-10rem" />
            <Tag :value="`${filtered.length} project${filtered.length === 1 ? '' : 's'}`" severity="secondary" />
        </div>

        <DataTable
            :value="filtered"
            :loading="loading"
            stripedRows
            dataKey="projectId"
            size="small"
            class="ent-grid ent-grid-clickable"
            :rows="25"
            paginator
            :rowsPerPageOptions="[10, 25, 50]"
            @row-click="(e) => router.push(`/planning/projects/${e.data.projectId}`)"
        >
            <Column field="projectNumber" header="Project #" style="width:150px" sortable>
                <template #body="{ data }">
                    <span class="plan-number">{{ data.projectNumber }}</span>
                </template>
            </Column>
            <Column field="name" header="Name" sortable>
                <template #body="{ data }">
                    <span class="ent-truncate">{{ data.name }}</span>
                </template>
            </Column>
            <Column field="client" header="Client" style="width:180px" sortable>
                <template #body="{ data }">
                    <span class="ent-truncate">{{ data.client }}</span>
                </template>
            </Column>
            <Column field="status" header="Status" style="width:120px" sortable>
                <template #body="{ data }">
                    <Tag :value="data.status" :severity="statusSeverity(data.status)" />
                </template>
            </Column>
            <Column header="Planned Start" style="width:130px" sortable sortField="plannedStart">
                <template #body="{ data }">{{ fmtDate(data.plannedStart) }}</template>
            </Column>
            <Column header="Planned End" style="width:120px" sortable sortField="plannedEnd">
                <template #body="{ data }">{{ fmtDate(data.plannedEnd) }}</template>
            </Column>
            <Column header="Forecast End" style="width:120px" sortable sortField="forecastEnd">
                <template #body="{ data }">{{ fmtDate(data.forecastEnd) }}</template>
            </Column>
            <Column header="WOs" style="width:55px; text-align:center">
                <template #body="{ data }">
                    <span class="plan-count">{{ data.workOrders?.length ?? 0 }}</span>
                </template>
            </Column>
            <template #empty>
                <span class="plan-empty">No projects found. Projects are created from awarded estimates with an active commercial authorization.</span>
            </template>
        </DataTable>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useApiStore } from '@/stores/apiStore';

const router = useRouter();

const apiStore = useApiStore();

const loading = ref(false);
const error = ref(false);
const projects = ref<any[]>([]);
const statusFilter = ref<string | null>(null);
const search = ref('');

const statusOptions = [
    { label: 'Initiating', value: 'Initiating' },
    { label: 'Planning', value: 'Planning' },
    { label: 'Active', value: 'Active' },
    { label: 'Monitoring', value: 'Monitoring' },
    { label: 'Closing', value: 'Closing' },
    { label: 'Closed', value: 'Closed' },
    { label: 'On Hold', value: 'OnHold' },
    { label: 'Cancelled', value: 'Cancelled' },
];

const filtered = computed(() => {
    let list = projects.value;
    if (statusFilter.value) list = list.filter(p => p.status === statusFilter.value);
    if (search.value.trim()) {
        const q = search.value.toLowerCase();
        list = list.filter(p =>
            p.projectNumber?.toLowerCase().includes(q) ||
            p.name?.toLowerCase().includes(q) ||
            p.client?.toLowerCase().includes(q)
        );
    }
    return list;
});

function statusSeverity(status: string): string {
    switch (status) {
        case 'Active': return 'success';
        case 'Monitoring': return 'info';
        case 'Planning': return 'secondary';
        case 'Initiating': return 'secondary';
        case 'Closing': return 'warn';
        case 'OnHold': return 'warn';
        case 'Closed': return 'contrast';
        case 'Cancelled': return 'danger';
        default: return 'secondary';
    }
}

function fmtDate(d: string | null | undefined): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const { data } = await apiStore.api.get('/api/v1/projects');
        projects.value = data;
    } catch {
        error.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
.plan-view {
    max-width: 1200px;
    margin: 0 auto;
    padding: 1.5rem 0;
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}
.plan-view-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    flex-wrap: wrap;
}
.plan-view-header h1 {
    margin: 0 0 0.25rem;
    font-size: 1.5rem;
    font-weight: 700;
    color: var(--text-color);
}
.plan-view-header p {
    margin: 0;
    color: var(--text-color-secondary);
    font-size: 0.88rem;
}
.plan-filters {
    display: flex;
    gap: 0.75rem;
    align-items: center;
    flex-wrap: wrap;
}
.plan-number {
    font-family: 'Courier New', monospace;
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--primary-color);
}
.plan-count {
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--text-color-secondary);
    text-align: center;
    display: block;
}
.plan-empty {
    font-size: 0.85rem;
    color: var(--text-color-secondary);
}
</style>
