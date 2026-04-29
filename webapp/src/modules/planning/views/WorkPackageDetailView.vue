<template>
    <div class="wp-detail">

        <div class="wp-back-row">
            <Button text icon="pi pi-arrow-left" label="Work Packages" size="small"
                @click="router.push('/planning/work-packages')" />
        </div>

        <div v-if="loading" class="wp-loading">
            <i class="pi pi-spin pi-spinner" />
            <span>Loading work package...</span>
        </div>
        <Message v-if="error" severity="error" :closable="false">Could not load work package.</Message>

        <template v-if="wp">

            <!-- Header -->
            <div class="wp-header-card">
                <div class="wp-header-title-row">
                    <div class="wp-header-title">
                        <span class="wp-pkg-id">WP-{{ wp.packageId }}</span>
                        <h1>{{ wp.title }}</h1>
                    </div>
                    <div class="wp-header-actions">
                        <Tag :value="wp.status" :severity="statusSeverity(wp.status)" />
                        <Tag v-if="wp.craftCode" :value="wp.craftCode" severity="info" />
                    </div>
                </div>

                <!-- Context breadcrumb: Project → Work Order → Plan -->
                <div class="wp-context-row">
                    <span v-if="wp.projectNumber" class="wp-ctx-chip"
                        @click="wp.projectId && router.push(`/planning/projects/${wp.projectId}`)">
                        <i class="pi pi-folder-open" />
                        {{ wp.projectNumber }} {{ wp.projectName ? '— ' + wp.projectName : '' }}
                    </span>
                    <i v-if="wp.projectNumber && wp.workOrderNumber" class="pi pi-angle-right wp-ctx-sep" />
                    <span v-if="wp.workOrderNumber" class="wp-ctx-chip"
                        @click="wp.workOrderId && router.push(`/planning/work-orders/${wp.workOrderId}`)">
                        <i class="pi pi-file-edit" />
                        {{ wp.workOrderNumber }} {{ wp.workOrderTitle ? '— ' + wp.workOrderTitle : '' }}
                    </span>
                    <i v-if="wp.workOrderNumber && wp.planName" class="pi pi-angle-right wp-ctx-sep" />
                    <span v-if="wp.planName" class="wp-ctx-chip wp-ctx-chip-plain">
                        <i class="pi pi-sitemap" />
                        {{ wp.planName }}
                    </span>
                </div>

                <!-- Key meta -->
                <div class="wp-meta-row">
                    <div class="wp-meta-item">
                        <span class="wp-meta-label">Required People</span>
                        <span class="wp-meta-value">{{ wp.requiredPeople }}</span>
                    </div>
                    <div v-if="wp.plannedStart || wp.plannedEnd" class="wp-meta-item">
                        <span class="wp-meta-label">Planned</span>
                        <span class="wp-meta-value">{{ fmtDate(wp.plannedStart) }} – {{ fmtDate(wp.plannedEnd) }}</span>
                    </div>
                    <div v-if="wp.area" class="wp-meta-item">
                        <span class="wp-meta-label">Area</span>
                        <span class="wp-meta-value">{{ wp.area }}</span>
                    </div>
                    <div v-if="wp.location" class="wp-meta-item">
                        <span class="wp-meta-label">Location</span>
                        <span class="wp-meta-value">{{ wp.location }}</span>
                    </div>
                    <div class="wp-meta-item">
                        <span class="wp-meta-label">Ready for Scheduling</span>
                        <span class="wp-meta-value">
                            <i v-if="wp.readyForScheduling" class="pi pi-check-circle"
                                style="color: var(--green-500, #22c55e); font-size: 1rem" />
                            <i v-else class="pi pi-circle"
                                style="color: var(--text-color-secondary); font-size: 1rem" />
                        </span>
                    </div>
                </div>
            </div>

            <!-- Field Start Gates -->
            <div class="wp-gates-card">
                <div class="wp-gates-title">Field Start Gates</div>
                <div class="wp-gates-grid">

                    <!-- Permit -->
                    <div class="wp-gate-item" :class="permitGateClass">
                        <div class="wp-gate-icon-row">
                            <i :class="permitGateIcon" />
                            <span class="wp-gate-name">Work Permit</span>
                        </div>
                        <span class="wp-gate-status">{{ permitGateLabel }}</span>
                        <span v-if="wp.permitNumber" class="wp-gate-ref">{{ wp.permitNumber }}</span>
                    </div>

                    <!-- JSA -->
                    <div class="wp-gate-item" :class="jsaGateClass">
                        <div class="wp-gate-icon-row">
                            <i :class="jsaGateIcon" />
                            <span class="wp-gate-name">JSA / Safety Analysis</span>
                        </div>
                        <span class="wp-gate-status">{{ jsaGateLabel }}</span>
                    </div>

                </div>
            </div>

            <!-- Notes -->
            <div class="wp-notes-card" v-if="wp.notes">
                <div class="wp-notes-label">Notes</div>
                <div class="wp-notes-text">{{ wp.notes }}</div>
            </div>

        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useApiStore } from '@/stores/apiStore';

const route = useRoute();
const router = useRouter();
const apiStore = useApiStore();

const pkgId = Number(route.params.id);

const loading = ref(false);
const error = ref(false);
const wp = ref<any>(null);

function statusSeverity(s: string): string {
    switch (s) {
        case 'InProgress': return 'info';
        case 'Complete': return 'contrast';
        case 'Draft': return 'secondary';
        default: return 'secondary';
    }
}

function fmtDate(d: string | null | undefined): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

const permitGateClass = computed(() => {
    if (!wp.value?.permitRequired) return 'wp-gate-na';
    switch (wp.value.permitStatus) {
        case 'Issued': return 'wp-gate-ok';
        case 'Pending': return 'wp-gate-warn';
        case 'Expired': return 'wp-gate-danger';
        default: return 'wp-gate-warn';
    }
});

const permitGateIcon = computed(() => {
    if (!wp.value?.permitRequired) return 'pi pi-minus-circle';
    switch (wp.value.permitStatus) {
        case 'Issued': return 'pi pi-check-circle';
        case 'Expired': return 'pi pi-times-circle';
        default: return 'pi pi-clock';
    }
});

const permitGateLabel = computed(() => {
    if (!wp.value?.permitRequired) return 'Not Required';
    return wp.value.permitStatus ?? 'Pending';
});

const jsaGateClass = computed(() => {
    if (!wp.value?.jsaRequired) return 'wp-gate-na';
    switch (wp.value.jsaStatus) {
        case 'Approved': return 'wp-gate-ok';
        case 'Pending': return 'wp-gate-warn';
        case 'Expired': return 'wp-gate-danger';
        default: return 'wp-gate-warn';
    }
});

const jsaGateIcon = computed(() => {
    if (!wp.value?.jsaRequired) return 'pi pi-minus-circle';
    switch (wp.value.jsaStatus) {
        case 'Approved': return 'pi pi-check-circle';
        case 'Expired': return 'pi pi-times-circle';
        default: return 'pi pi-clock';
    }
});

const jsaGateLabel = computed(() => {
    if (!wp.value?.jsaRequired) return 'Not Required';
    return wp.value.jsaStatus ?? 'Pending';
});

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const { data } = await apiStore.api.get(`/api/v1/planning/work-packages/${pkgId}`);
        wp.value = data;
    } catch {
        error.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
.wp-detail {
    max-width: 960px;
    margin: 0 auto;
    padding: 1.5rem 0;
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
}

.wp-back-row {
    display: flex;
    align-items: center;
}

.wp-loading {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    color: var(--text-color-secondary);
    font-size: 0.9rem;
}

/* Header card */
.wp-header-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 12px;
    padding: 1.25rem 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 0.85rem;
}

.wp-header-title-row {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    flex-wrap: wrap;
}

.wp-header-title {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
}

.wp-pkg-id {
    font-family: 'Courier New', monospace;
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--primary-color);
    letter-spacing: 0.04em;
}

.wp-header-title h1 {
    margin: 0;
    font-size: 1.35rem;
    font-weight: 700;
    color: var(--text-color);
    line-height: 1.2;
}

.wp-header-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    flex-wrap: wrap;
}

/* Context breadcrumb */
.wp-context-row {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    flex-wrap: wrap;
}

.wp-ctx-chip {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    font-size: 0.78rem;
    font-weight: 500;
    color: var(--primary-color);
    cursor: pointer;
    padding: 2px 6px;
    border-radius: 4px;
    transition: background 0.15s;
}

.wp-ctx-chip:hover {
    background: var(--primary-50, rgba(var(--primary-color-rgb), 0.08));
}

.wp-ctx-chip-plain {
    color: var(--text-color-secondary);
    cursor: default;
}

.wp-ctx-chip-plain:hover {
    background: none;
}

.wp-ctx-sep {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
}

/* Meta row */
.wp-meta-row {
    display: flex;
    gap: 1.5rem 2.5rem;
    flex-wrap: wrap;
    padding-top: 0.5rem;
    border-top: 1px solid var(--surface-border);
}

.wp-meta-item {
    display: flex;
    flex-direction: column;
    gap: 0.1rem;
}

.wp-meta-label {
    font-size: 0.68rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-color-secondary);
    font-weight: 600;
}

.wp-meta-value {
    font-size: 0.88rem;
    font-weight: 500;
    color: var(--text-color);
    display: flex;
    align-items: center;
    gap: 0.3rem;
}

/* Gates */
.wp-gates-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 1rem 1.25rem;
}

.wp-gates-title {
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-color-secondary);
    margin-bottom: 0.75rem;
}

.wp-gates-grid {
    display: flex;
    gap: 1rem;
    flex-wrap: wrap;
}

.wp-gate-item {
    flex: 1;
    min-width: 180px;
    border-radius: 8px;
    padding: 0.75rem 1rem;
    border: 1px solid transparent;
    display: flex;
    flex-direction: column;
    gap: 0.2rem;
}

.wp-gate-icon-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.wp-gate-icon-row i {
    font-size: 1rem;
}

.wp-gate-name {
    font-size: 0.82rem;
    font-weight: 600;
}

.wp-gate-status {
    font-size: 0.78rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.04em;
    padding-left: 1.5rem;
}

.wp-gate-ref {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
    padding-left: 1.5rem;
}

.wp-gate-ok {
    background: #f0fdf4;
    border-color: #86efac;
}
.wp-gate-ok .wp-gate-icon-row i { color: #16a34a; }
.wp-gate-ok .wp-gate-status { color: #15803d; }

.wp-gate-warn {
    background: #fffbeb;
    border-color: #fcd34d;
}
.wp-gate-warn .wp-gate-icon-row i { color: #d97706; }
.wp-gate-warn .wp-gate-status { color: #b45309; }

.wp-gate-danger {
    background: #fef2f2;
    border-color: #fca5a5;
}
.wp-gate-danger .wp-gate-icon-row i { color: #dc2626; }
.wp-gate-danger .wp-gate-status { color: #b91c1c; }

.wp-gate-na {
    background: var(--surface-ground);
    border-color: var(--surface-border);
}
.wp-gate-na .wp-gate-icon-row i { color: var(--text-color-secondary); }
.wp-gate-na .wp-gate-status { color: var(--text-color-secondary); }

/* Notes */
.wp-notes-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 8px;
    padding: 1rem 1.25rem;
}

.wp-notes-label {
    font-size: 0.72rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--text-color-secondary);
    margin-bottom: 0.4rem;
}

.wp-notes-text {
    font-size: 0.88rem;
    color: var(--text-color);
    line-height: 1.6;
    white-space: pre-wrap;
}
</style>
