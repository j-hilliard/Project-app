<template>
    <div class="planning-view">
        <div class="planning-view-header">
            <div class="planning-view-header-left">
                <h1>FCO / Change Orders</h1>
                <p>Field Change Orders for scope modifications, schedule impacts, and contract value adjustments.</p>
            </div>
            <div class="planning-header-actions">
                <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
                <Button label="New FCO" icon="pi pi-plus" @click="openNew" />
            </div>
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load FCOs. Make sure the API is running.
        </Message>

        <!-- Filters -->
        <div class="planning-filters">
            <Dropdown v-model="statusFilter" :options="statusOptions" optionLabel="label" optionValue="value"
                placeholder="All Status" showClear class="w-10rem" @change="applyFilters" />
            <InputText v-model="search" placeholder="Search FCOs..." class="flex-1 min-w-10rem" @input="applyFilters" />
            <Tag :value="`${filtered.length} FCOs`" severity="info" />
        </div>

        <DataTable :value="filtered" :loading="loading" stripedRows dataKey="fcoDocumentId" size="small"
            :rows="25" paginator :rowsPerPageOptions="[10, 25, 50]">
            <Column field="fcoNumber" header="FCO #" style="width:100px" sortable />
            <Column field="title" header="Title" sortable />
            <Column field="date" header="Date" style="width:110px">
                <template #body="{ data }">{{ fmtDate(data.date) }}</template>
            </Column>
            <Column field="requestedBy" header="Requested By" style="width:130px" />
            <Column field="scheduleImpactDays" header="Schedule Impact" style="width:130px">
                <template #body="{ data }">
                    <span v-if="data.scheduleImpactDays">{{ data.scheduleImpactDays }} days</span>
                    <span v-else class="planning-empty-cell">—</span>
                </template>
            </Column>
            <Column field="updatedContractValue" header="Contract Value" style="width:130px">
                <template #body="{ data }">
                    <span v-if="data.updatedContractValue">{{ fmtCurrency(data.updatedContractValue) }}</span>
                    <span v-else class="planning-empty-cell">—</span>
                </template>
            </Column>
            <Column field="status" header="Status" style="width:100px" sortable>
                <template #body="{ data }">
                    <Tag :value="data.status" :severity="statusSeverity(data.status)" />
                </template>
            </Column>
            <Column header="" style="width:120px">
                <template #body="{ data }">
                    <Button icon="pi pi-eye" text size="small" @click="openDetail(data)" />
                    <Button icon="pi pi-file" text size="small" title="Generate Document" @click="generateDoc(data)" />
                </template>
            </Column>
            <template #empty>
                <span class="planning-empty">No FCOs found. Create one when scope or schedule changes are needed.</span>
            </template>
        </DataTable>

        <!-- New FCO Dialog -->
        <Dialog v-model:visible="formVisible" header="New FCO / Change Order" modal :style="{ width: '540px' }">
            <div class="form-grid">
                <div class="form-field">
                    <label>FCO Number</label>
                    <InputText v-model="form.fcoNumber" class="w-full" placeholder="e.g. FCO-2026-001" />
                </div>
                <div class="form-field">
                    <label>Title</label>
                    <InputText v-model="form.title" class="w-full" />
                </div>
                <div class="form-field">
                    <label>Scope Description</label>
                    <Textarea v-model="form.scopeDescription" rows="3" class="w-full" />
                </div>
                <div class="form-field">
                    <label>Reason / Basis</label>
                    <InputText v-model="form.reason" class="w-full" />
                </div>
                <div class="form-field-row">
                    <div class="form-field">
                        <label>Requested By</label>
                        <InputText v-model="form.requestedBy" class="w-full" />
                    </div>
                    <div class="form-field">
                        <label>Prepared By</label>
                        <InputText v-model="form.preparedBy" class="w-full" />
                    </div>
                </div>
                <div class="form-field-row">
                    <div class="form-field">
                        <label>Schedule Impact (days)</label>
                        <InputNumber v-model="form.scheduleImpactDays" class="w-full" />
                    </div>
                    <div class="form-field">
                        <label>Updated Contract Value</label>
                        <InputNumber v-model="form.updatedContractValue" mode="currency" currency="USD" class="w-full" />
                    </div>
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="formVisible = false" />
                <Button label="Create FCO" :loading="saving" @click="saveFco" />
            </template>
        </Dialog>

        <!-- FCO Detail Dialog -->
        <Dialog v-model:visible="detailVisible" :header="detailFco?.fcoNumber ?? 'FCO Detail'" modal :style="{ width: '600px' }">
            <div v-if="detailFco" class="fco-detail">
                <div class="fco-detail-row">
                    <span class="fco-label">Title</span>
                    <span>{{ detailFco.title }}</span>
                </div>
                <div class="fco-detail-row">
                    <span class="fco-label">Status</span>
                    <Tag :value="detailFco.status" :severity="statusSeverity(detailFco.status)" />
                </div>
                <div class="fco-detail-row">
                    <span class="fco-label">Scope</span>
                    <span>{{ detailFco.scopeDescription }}</span>
                </div>
                <div class="fco-detail-row">
                    <span class="fco-label">Reason</span>
                    <span>{{ detailFco.reason }}</span>
                </div>
                <div class="fco-detail-row">
                    <span class="fco-label">Schedule Impact</span>
                    <span>{{ detailFco.scheduleImpactDays != null ? detailFco.scheduleImpactDays + ' days' : '—' }}</span>
                </div>
                <div class="fco-detail-row">
                    <span class="fco-label">Contract Value</span>
                    <span>{{ detailFco.updatedContractValue ? fmtCurrency(detailFco.updatedContractValue) : '—' }}</span>
                </div>
                <div class="fco-status-actions">
                    <Button v-if="detailFco.status === 'Draft'" label="Submit for Approval" @click="updateFcoStatus(detailFco, 'Submitted')" />
                    <Button v-if="detailFco.status === 'Submitted'" label="Approve" severity="success" @click="updateFcoStatus(detailFco, 'Approved')" />
                    <Button v-if="detailFco.status === 'Submitted'" label="Reject" severity="danger" @click="updateFcoStatus(detailFco, 'Rejected')" />
                </div>
            </div>
            <template #footer>
                <Button label="Generate Document" icon="pi pi-file" outlined @click="generateDoc(detailFco)" />
                <Button label="Close" text @click="detailVisible = false" />
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
const fcos = ref<any[]>([]);
const statusFilter = ref<string | null>(null);
const search = ref('');

const statusOptions = [
    { label: 'Draft', value: 'Draft' },
    { label: 'Submitted', value: 'Submitted' },
    { label: 'Approved', value: 'Approved' },
    { label: 'Rejected', value: 'Rejected' },
];

const filtered = computed(() => {
    let list = fcos.value;
    if (statusFilter.value) list = list.filter(f => f.status === statusFilter.value);
    if (search.value.trim()) {
        const q = search.value.toLowerCase();
        list = list.filter(f => f.title?.toLowerCase().includes(q) || f.fcoNumber?.toLowerCase().includes(q));
    }
    return list;
});

function applyFilters() { /* computed */ }

function statusSeverity(s: string) {
    if (s === 'Approved') return 'success';
    if (s === 'Submitted') return 'warning';
    if (s === 'Rejected') return 'danger';
    return 'secondary';
}

function fmtDate(d: string | null | undefined) {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: '2-digit' });
}

function fmtCurrency(v: number) {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(v);
}

const formVisible = ref(false);
const form = ref({
    fcoNumber: '', title: '', scopeDescription: '', reason: '',
    requestedBy: '', preparedBy: '', scheduleImpactDays: null as number | null,
    updatedContractValue: null as number | null,
});

function openNew() {
    form.value = { fcoNumber: '', title: '', scopeDescription: '', reason: '', requestedBy: '', preparedBy: '', scheduleImpactDays: null, updatedContractValue: null };
    formVisible.value = true;
}

async function saveFco() {
    if (!form.value.title) {
        toast.add({ severity: 'warn', summary: 'Required', detail: 'Title is required.', life: 3000 });
        return;
    }
    saving.value = true;
    try {
        await apiStore.api.value.post('/api/v1/planning/fco', { ...form.value, status: 'Draft', date: new Date() });
        formVisible.value = false;
        toast.add({ severity: 'success', summary: 'FCO Created', life: 2000 });
        await load();
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not create FCO.', life: 3000 });
    } finally {
        saving.value = false;
    }
}

const detailVisible = ref(false);
const detailFco = ref<any>(null);

function openDetail(fco: any) {
    detailFco.value = fco;
    detailVisible.value = true;
}

async function updateFcoStatus(fco: any, status: string) {
    try {
        const { data } = await apiStore.api.value.put(`/api/v1/planning/fco/${fco.fcoDocumentId}`, { ...fco, status });
        const idx = fcos.value.findIndex(f => f.fcoDocumentId === fco.fcoDocumentId);
        if (idx >= 0) fcos.value[idx] = data;
        if (detailFco.value?.fcoDocumentId === fco.fcoDocumentId) detailFco.value = data;
        toast.add({ severity: 'success', summary: `FCO ${status}`, life: 2000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not update FCO.', life: 3000 });
    }
}

async function generateDoc(fco: any) {
    if (!fco) return;
    try {
        const { data } = await apiStore.api.value.post(`/api/v1/planning/fco/${fco.fcoDocumentId}/generate-document`);
        const blob = new Blob([data], { type: 'text/html' });
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => URL.revokeObjectURL(url), 10000);
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not generate FCO document.', life: 3000 });
    }
}

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const { data } = await apiStore.api.value.get('/api/v1/planning/fco');
        fcos.value = data;
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
.planning-header-actions { display: flex; gap: 0.5rem; align-items: center; }
.planning-filters { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; }
.planning-empty { font-size: 0.85rem; color: var(--text-color-secondary); }
.planning-empty-cell { color: var(--text-color-secondary); font-size: 0.85rem; }
.form-grid { display: flex; flex-direction: column; gap: 1rem; }
.form-field { display: flex; flex-direction: column; gap: 0.35rem; }
.form-field label { font-size: 0.82rem; font-weight: 600; color: var(--text-color-secondary); }
.form-field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
.fco-detail { display: flex; flex-direction: column; gap: 0.75rem; }
.fco-detail-row { display: grid; grid-template-columns: 130px 1fr; gap: 0.5rem; align-items: start; }
.fco-label { font-size: 0.82rem; font-weight: 600; color: var(--text-color-secondary); }
.fco-status-actions { display: flex; gap: 0.5rem; margin-top: 0.5rem; }
</style>
