<template>
    <div class="plan-view">
        <div class="plan-view-header">
            <div>
                <h1>Work Orders</h1>
                <p>Released execution packages. Work orders gate all field execution and actuals.</p>
            </div>
            <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load work orders. Make sure the API is running.
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
            <InputText v-model="search" placeholder="Search work orders..." class="flex-1 min-w-10rem" />
            <Tag :value="`${filtered.length} work order${filtered.length === 1 ? '' : 's'}`" severity="secondary" />
        </div>

        <DataTable
            :value="filtered"
            :loading="loading"
            stripedRows
            dataKey="workOrderId"
            size="small"
            class="ent-grid ent-grid-clickable"
            :rows="25"
            paginator
            :rowsPerPageOptions="[10, 25, 50]"
            @row-click="(e) => router.push(`/planning/work-orders/${e.data.workOrderId}`)"
        >
            <Column field="workOrderNumber" header="WO #" style="width:150px" sortable>
                <template #body="{ data }">
                    <span class="plan-number">{{ data.workOrderNumber }}</span>
                </template>
            </Column>
            <Column field="title" header="Title" sortable>
                <template #body="{ data }">
                    <span class="ent-truncate">{{ data.title }}</span>
                </template>
            </Column>
            <Column field="status" header="Status" style="width:120px" sortable>
                <template #body="{ data }">
                    <Tag :value="data.status" :severity="statusSeverity(data.status)" />
                </template>
            </Column>
            <Column header="Authorized $" style="width:130px; text-align:right" sortable sortField="authorizedValue">
                <template #body="{ data }">
                    <span class="plan-currency">{{ fmtCurrency(data.authorizedValue) }}</span>
                </template>
            </Column>
            <Column header="Planned Start" style="width:130px" sortable sortField="plannedStart">
                <template #body="{ data }">{{ fmtDate(data.plannedStart) }}</template>
            </Column>
            <Column header="Planned End" style="width:120px" sortable sortField="plannedEnd">
                <template #body="{ data }">{{ fmtDate(data.plannedEnd) }}</template>
            </Column>
            <Column header="Released" style="width:130px" sortable sortField="releasedAt">
                <template #body="{ data }">{{ fmtDate(data.releasedAt) }}</template>
            </Column>
            <template #empty>
                <span class="plan-empty">No work orders found. Work orders are created and released from within a project.</span>
            </template>
        </DataTable>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { usePlanningService } from '../services/usePlanningService';
import { useFormatters } from '@/ui';
import { workOrderStatusSeverity as statusSeverity } from '@/ui';

const router = useRouter();
const { listWorkOrders } = usePlanningService();
const { fmtDate, fmtCurrency } = useFormatters();

const loading = ref(false);
const error = ref(false);
const workOrders = ref<any[]>([]);
const statusFilter = ref<string | null>(null);
const search = ref('');

const statusOptions = [
    { label: 'Draft', value: 'Draft' },
    { label: 'Released', value: 'Released' },
    { label: 'In Progress', value: 'InProgress' },
    { label: 'Complete', value: 'Complete' },
    { label: 'Closed', value: 'Closed' },
    { label: 'Cancelled', value: 'Cancelled' },
];

const filtered = computed(() => {
    let list = workOrders.value;
    if (statusFilter.value) list = list.filter(w => w.status === statusFilter.value);
    if (search.value.trim()) {
        const q = search.value.toLowerCase();
        list = list.filter(w =>
            w.workOrderNumber?.toLowerCase().includes(q) ||
            w.title?.toLowerCase().includes(q)
        );
    }
    return list;
});

async function load() {
    loading.value = true;
    error.value = false;
    try {
        workOrders.value = await listWorkOrders();
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
.plan-currency {
    font-size: 0.82rem;
    font-weight: 600;
    color: var(--text-color);
    display: block;
    text-align: right;
}
.plan-empty {
    font-size: 0.85rem;
    color: var(--text-color-secondary);
}
</style>
