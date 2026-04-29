<template>
    <div class="sched-view">
        <div class="sched-view-header">
            <div>
                <h1>Resources</h1>
                <p>Manage craft workers, certifications, and availability.</p>
            </div>
            <div class="sched-header-actions">
                <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
                <Button label="Add Resource" icon="pi pi-plus" @click="openNew" />
            </div>
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load resources. Make sure the API is running.
        </Message>

        <!-- Filters -->
        <div class="sched-filters">
            <Dropdown v-model="craftFilter" :options="craftOptions" optionLabel="label" optionValue="value"
                placeholder="All Crafts" showClear class="w-10rem" @change="applyFilters" />
            <SelectButton v-model="activeFilter" :options="activeOptions" optionLabel="label" optionValue="value" />
            <InputText v-model="search" placeholder="Search resources..." class="flex-1 min-w-10rem" @input="applyFilters" />
            <Tag :value="`${filtered.length} resources`" severity="info" />
        </div>

        <DataTable :value="filtered" :loading="loading" stripedRows dataKey="resourceId" size="small"
            class="ent-grid" selectionMode="single" @row-click="openDetail">
            <Column field="name" header="Name" sortable>
                <template #body="{ data }">
                    <span class="ent-truncate">{{ data.name }}</span>
                </template>
            </Column>
            <Column field="craftCode" header="Craft" style="width:80px" sortable>
                <template #body="{ data }">
                    <Tag :value="data.craftCode" severity="info" />
                </template>
            </Column>
            <Column field="branch" header="Branch" style="width:110px">
                <template #body="{ data }">
                    <span class="ent-truncate">{{ data.branch }}</span>
                </template>
            </Column>
            <Column header="Active" style="width:64px">
                <template #body="{ data }">
                    <i :class="data.isActive ? 'pi pi-check-circle text-green-500' : 'pi pi-times-circle text-red-400'"
                        style="font-size:0.9rem" />
                </template>
            </Column>
            <Column header="Certifications" style="width:180px">
                <template #body="{ data }">
                    <div class="cert-pills">
                        <Tag v-for="c in (data.certifications ?? []).slice(0, 2)" :key="c.certId"
                            :value="c.type" severity="secondary" class="cert-pill" />
                        <span v-if="(data.certifications ?? []).length > 2" class="cert-overflow">
                            +{{ data.certifications.length - 2 }}
                        </span>
                    </div>
                </template>
            </Column>
            <Column header="" style="width:68px">
                <template #body="{ data }">
                    <div class="row-actions">
                        <Button icon="pi pi-pencil" text size="small" @click.stop="openEdit(data)" />
                        <Button icon="pi pi-trash" text severity="danger" size="small" @click.stop="confirmDelete(data)" />
                    </div>
                </template>
            </Column>
            <template #empty>
                <span class="sched-empty">No resources found. Add a resource or call the seed endpoint.</span>
            </template>
        </DataTable>

        <!-- Add/Edit Dialog -->
        <Dialog v-model:visible="formVisible" :header="editMode ? 'Edit Resource' : 'New Resource'" modal :style="{ width: '420px' }">
            <div class="form-grid">
                <div class="form-field">
                    <label>Name</label>
                    <InputText v-model="form.name" class="w-full" />
                </div>
                <div class="form-field">
                    <label>Craft Code</label>
                    <Dropdown v-model="form.craftCode" :options="craftOptions" optionLabel="label" optionValue="value"
                        placeholder="Select craft" class="w-full" />
                </div>
                <div class="form-field">
                    <label>Branch</label>
                    <InputText v-model="form.branch" class="w-full" placeholder="e.g. Branch A" />
                </div>
                <div class="form-field form-field-inline">
                    <label>Active</label>
                    <ToggleButton v-model="form.isActive" onLabel="Active" offLabel="Inactive" />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="formVisible = false" />
                <Button :label="editMode ? 'Save Changes' : 'Create'" :loading="saving" @click="saveResource" />
            </template>
        </Dialog>

        <!-- Detail Side Panel (Dialog) -->
        <Dialog v-model:visible="detailVisible" :header="detailResource?.name ?? 'Resource'" modal :style="{ width: '560px' }">
            <div v-if="detailResource" class="detail-panel">
                <div class="detail-meta">
                    <Tag :value="detailResource.craftCode" severity="info" />
                    <span>{{ detailResource.branch }}</span>
                    <Tag :value="detailResource.isActive ? 'Active' : 'Inactive'"
                        :severity="detailResource.isActive ? 'success' : 'secondary'" />
                </div>

                <div class="detail-section">
                    <div class="detail-section-header">
                        <h3>Certifications</h3>
                        <Button label="Add Cert" size="small" outlined @click="openAddCert" />
                    </div>
                    <div v-if="!detailCerts.length" class="sched-empty">No certifications on file.</div>
                    <div v-for="cert in detailCerts" :key="cert.certId" class="cert-row">
                        <Tag :value="cert.type" severity="secondary" />
                        <span class="cert-exp">Exp: {{ fmtDate(cert.expirationDate) }}</span>
                        <Button icon="pi pi-trash" text severity="danger" size="small" @click="deleteCert(cert)" />
                    </div>
                </div>
            </div>
            <template #footer>
                <Button label="Close" text @click="detailVisible = false" />
            </template>
        </Dialog>

        <!-- Add Cert Dialog -->
        <Dialog v-model:visible="certVisible" header="Add Certification" modal :style="{ width: '380px' }">
            <div class="form-grid">
                <div class="form-field">
                    <label>Type</label>
                    <Dropdown v-model="certForm.type"
                        :options="['OSHA-10', 'OSHA-30', 'H2S', 'CPR', 'Rigging', 'Crane-Operator']"
                        placeholder="Select type" class="w-full" />
                </div>
                <div class="form-field">
                    <label>Expiration Date</label>
                    <Calendar v-model="certForm.expirationDate" showIcon dateFormat="yy-mm-dd" class="w-full" />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="certVisible = false" />
                <Button label="Add" :loading="saving" @click="saveCert" />
            </template>
        </Dialog>

        <ConfirmDialog />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import { useApiStore } from '@/stores/apiStore';

const apiStore = useApiStore();
const toast = useToast();
const confirm = useConfirm();

const loading = ref(false);
const error = ref(false);
const saving = ref(false);
const resources = ref<any[]>([]);
const craftFilter = ref<string | null>(null);
const activeFilter = ref<boolean | null>(null);
const search = ref('');

const craftOptions = [
    { label: 'Pipefitter', value: 'PP' },
    { label: 'Electrician', value: 'EL' },
    { label: 'Crane Operator', value: 'CR' },
];

const activeOptions = [
    { label: 'All', value: null },
    { label: 'Active', value: true },
    { label: 'Inactive', value: false },
];

const filtered = computed(() => {
    let list = resources.value;
    if (craftFilter.value) list = list.filter(r => r.craftCode === craftFilter.value);
    if (activeFilter.value !== null) list = list.filter(r => r.isActive === activeFilter.value);
    if (search.value.trim()) {
        const q = search.value.toLowerCase();
        list = list.filter(r => r.name?.toLowerCase().includes(q) || r.branch?.toLowerCase().includes(q));
    }
    return list;
});

function applyFilters() { /* computed */ }

function fmtDate(d: string | null | undefined) {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

// Form
const formVisible = ref(false);
const editMode = ref(false);
const editId = ref<number | null>(null);
const form = ref({ name: '', craftCode: '', branch: '', isActive: true });

function openNew() {
    editMode.value = false;
    editId.value = null;
    form.value = { name: '', craftCode: '', branch: '', isActive: true };
    formVisible.value = true;
}

function openEdit(r: any) {
    editMode.value = true;
    editId.value = r.resourceId;
    form.value = { name: r.name, craftCode: r.craftCode, branch: r.branch ?? '', isActive: r.isActive };
    formVisible.value = true;
}

async function saveResource() {
    if (!form.value.name || !form.value.craftCode) {
        toast.add({ severity: 'warn', summary: 'Required', detail: 'Name and craft are required.', life: 3000 });
        return;
    }
    saving.value = true;
    try {
        if (editMode.value && editId.value) {
            await apiStore.api.put(`/api/v1/scheduling/resources/${editId.value}`, form.value);
            toast.add({ severity: 'success', summary: 'Saved', life: 2000 });
        } else {
            await apiStore.api.post('/api/v1/scheduling/resources', form.value);
            toast.add({ severity: 'success', summary: 'Resource Created', life: 2000 });
        }
        formVisible.value = false;
        await load();
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not save resource.', life: 3000 });
    } finally {
        saving.value = false;
    }
}

function confirmDelete(r: any) {
    confirm.require({
        message: `Delete resource "${r.name}"?`,
        header: 'Confirm Delete',
        icon: 'pi pi-trash',
        acceptSeverity: 'danger',
        accept: async () => {
            try {
                await apiStore.api.delete(`/api/v1/scheduling/resources/${r.resourceId}`);
                toast.add({ severity: 'success', summary: 'Deleted', life: 2000 });
                await load();
            } catch {
                toast.add({ severity: 'error', summary: 'Error', detail: 'Could not delete resource.', life: 3000 });
            }
        },
    });
}

// Detail panel
const detailVisible = ref(false);
const detailResource = ref<any>(null);
const detailCerts = ref<any[]>([]);

async function openDetail(event: any) {
    const r = event.data;
    detailResource.value = r;
    detailVisible.value = true;
    try {
        const { data } = await apiStore.api.get(`/api/v1/scheduling/resources/${r.resourceId}/certifications`);
        detailCerts.value = data;
    } catch {
        detailCerts.value = [];
    }
}

// Cert management
const certVisible = ref(false);
const certForm = ref({ type: '', expirationDate: null as Date | null });

function openAddCert() {
    certForm.value = { type: '', expirationDate: null };
    certVisible.value = true;
}

async function saveCert() {
    if (!certForm.value.type) return;
    saving.value = true;
    try {
        const payload = {
            resourceId: detailResource.value.resourceId,
            type: certForm.value.type,
            expirationDate: certForm.value.expirationDate,
        };
        await apiStore.api.post('/api/v1/scheduling/certifications', payload);
        certVisible.value = false;
        const { data } = await apiStore.api.get(`/api/v1/scheduling/resources/${detailResource.value.resourceId}/certifications`);
        detailCerts.value = data;
        toast.add({ severity: 'success', summary: 'Certification Added', life: 2000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not add certification.', life: 3000 });
    } finally {
        saving.value = false;
    }
}

async function deleteCert(cert: any) {
    try {
        await apiStore.api.delete(`/api/v1/scheduling/certifications/${cert.certId}`);
        detailCerts.value = detailCerts.value.filter(c => c.certId !== cert.certId);
        toast.add({ severity: 'success', summary: 'Removed', life: 2000 });
    } catch {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Could not remove certification.', life: 3000 });
    }
}

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const { data } = await apiStore.api.get('/api/v1/scheduling/resources');
        resources.value = data;
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
.sched-header-actions { display: flex; gap: 0.5rem; align-items: center; }
.sched-filters { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; }
.sched-empty { font-size: 0.85rem; color: var(--text-color-secondary); }
.cert-pills { display: flex; flex-wrap: nowrap; gap: 0.25rem; align-items: center; overflow: hidden; }
.cert-pill { font-size: 0.7rem !important; flex-shrink: 0; }
.form-grid { display: flex; flex-direction: column; gap: 1rem; }
.form-field { display: flex; flex-direction: column; gap: 0.35rem; }
.form-field label { font-size: 0.82rem; font-weight: 600; color: var(--text-color-secondary); }
.form-field-inline { flex-direction: row; align-items: center; justify-content: space-between; }
.detail-panel { display: flex; flex-direction: column; gap: 1.25rem; }
.detail-meta { display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
.detail-section { display: flex; flex-direction: column; gap: 0.5rem; }
.detail-section-header { display: flex; align-items: center; justify-content: space-between; }
.detail-section-header h3 { margin: 0; font-size: 0.95rem; font-weight: 600; color: var(--text-color); }
.cert-row { display: flex; align-items: center; gap: 0.5rem; padding: 0.35rem 0; border-bottom: 1px solid var(--surface-border); }
.cert-exp { font-size: 0.8rem; color: var(--text-color-secondary); flex: 1; }
</style>
