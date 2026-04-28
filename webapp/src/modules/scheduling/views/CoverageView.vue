<template>
    <div class="sched-view">
        <div class="sched-view-header">
            <div>
                <h1>Craft Coverage</h1>
                <p>Demand headcount vs assigned headcount by craft. Red rows indicate open positions.</p>
            </div>
            <Button label="Refresh" text icon="pi pi-refresh" :loading="loading" @click="load" />
        </div>

        <Message v-if="error" severity="error" :closable="false">
            Could not load coverage data. Make sure the API is running.
        </Message>

        <!-- Summary banner -->
        <div class="coverage-summary" v-if="coverage.length">
            <div class="coverage-summary-item">
                <span class="coverage-summary-value">{{ shortageCount }}</span>
                <span class="coverage-summary-label">Crafts Short</span>
            </div>
            <div class="coverage-summary-divider" />
            <div class="coverage-summary-item">
                <span class="coverage-summary-value">{{ totalGap }}</span>
                <span class="coverage-summary-label">Open Positions</span>
            </div>
            <div class="coverage-summary-divider" />
            <div class="coverage-summary-item">
                <span class="coverage-summary-value">{{ totalAssigned }}</span>
                <span class="coverage-summary-label">Assigned Today</span>
            </div>
        </div>

        <DataTable :value="coverage" :loading="loading" stripedRows dataKey="craftCode" size="small"
            selectionMode="single" @row-click="openSuggestions"
            :rowClass="rowClass">
            <Column field="craftTitle" header="Craft" sortable />
            <Column field="demandCount" header="Demand" style="width:90px" />
            <Column field="assignedCount" header="Assigned" style="width:90px" />
            <Column field="gap" header="Gap" style="width:80px">
                <template #body="{ data }">
                    <span :class="data.gap > 0 ? 'gap-warn' : 'gap-ok'">
                        {{ data.gap > 0 ? `–${data.gap}` : '✓' }}
                    </span>
                </template>
            </Column>
            <Column header="Coverage" style="width:200px">
                <template #body="{ data }">
                    <div class="coverage-bar-wrap">
                        <div class="coverage-bar">
                            <div class="coverage-bar-fill" :style="coverageBarStyle(data)" />
                        </div>
                        <span class="coverage-pct">{{ coveragePct(data) }}%</span>
                    </div>
                </template>
            </Column>
            <Column header="" style="width:130px">
                <template #body="{ data }">
                    <Button v-if="data.gap > 0" label="Suggestions" size="small" outlined @click.stop="openSuggestions({ data })" />
                </template>
            </Column>
            <template #empty>
                <span class="sched-empty">No coverage data. Ensure staffing plans are Approved and resources have active assignments.</span>
            </template>
        </DataTable>

        <!-- Suggested Matches Panel -->
        <Dialog v-model:visible="suggestVisible" :header="`Suggested Matches — ${selectedCraft?.craftTitle ?? ''}`" modal :style="{ width: '540px' }">
            <div v-if="suggestLoading" class="suggest-loading">
                <i class="pi pi-spin pi-spinner" /> Loading suggestions...
            </div>
            <div v-else-if="!matchesForCraft.length" class="sched-empty">
                No available resources found for this craft. All resources may be assigned.
            </div>
            <div v-else class="suggest-list">
                <div v-for="m in matchesForCraft" :key="m.resourceId" class="suggest-item">
                    <div class="suggest-info">
                        <span class="suggest-name">{{ m.resourceName }}</span>
                        <Tag :value="m.craftCode" severity="info" />
                        <Tag :value="m.reason" :severity="m.reason === 'Unassigned' ? 'success' : 'warning'" />
                    </div>
                    <div class="suggest-meta">
                        <span>Available: {{ fmtDate(m.availableDate) }}</span>
                        <span>Score: {{ m.matchScore }}</span>
                        <span v-if="m.branch">{{ m.branch }}</span>
                    </div>
                    <Button label="Assign" size="small" outlined @click="startAssignFromMatch(m)" />
                </div>
            </div>
            <template #footer>
                <Button label="Close" text @click="suggestVisible = false" />
            </template>
        </Dialog>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useApiStore } from '@/stores/apiStore';

const apiStore = useApiStore();
const router = useRouter();

const loading = ref(false);
const error = ref(false);
const coverage = ref<any[]>([]);
const allMatches = ref<any[]>([]);

const shortageCount = computed(() => coverage.value.filter(c => c.gap > 0).length);
const totalGap = computed(() => coverage.value.reduce((s, c) => s + c.gap, 0));
const totalAssigned = computed(() => coverage.value.reduce((s, c) => s + c.assignedCount, 0));

function rowClass(row: any) {
    return row.gap > 0 ? 'coverage-row-short' : '';
}

function coveragePct(row: any) {
    if (!row.demandCount) return 100;
    return Math.min(100, Math.round(row.assignedCount / row.demandCount * 100));
}

function coverageBarStyle(row: any) {
    const pct = coveragePct(row);
    const color = pct >= 100 ? 'var(--green-500, #22c55e)' : pct >= 70 ? 'var(--yellow-500, #eab308)' : 'var(--red-500, #ef4444)';
    return { width: `${pct}%`, backgroundColor: color };
}

function fmtDate(d: string | null | undefined) {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: '2-digit' });
}

// Suggestions panel
const suggestVisible = ref(false);
const suggestLoading = ref(false);
const selectedCraft = ref<any>(null);
const matchesForCraft = computed(() =>
    selectedCraft.value ? allMatches.value.filter(m => m.craftCode === selectedCraft.value.craftCode) : []
);

async function openSuggestions(event: any) {
    const row = event.data;
    if (!row.gap || row.gap <= 0) return;
    selectedCraft.value = row;
    suggestVisible.value = true;
}

function startAssignFromMatch(_m: any) {
    suggestVisible.value = false;
    router.push('/scheduling/assignments');
}

async function load() {
    loading.value = true;
    error.value = false;
    try {
        const [covResp, matchResp] = await Promise.all([
            apiStore.api.value.get('/api/v1/scheduling/coverage'),
            apiStore.api.value.get('/api/v1/scheduling/suggested-matches'),
        ]);
        coverage.value = covResp.data;
        allMatches.value = matchResp.data;
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
.sched-empty { font-size: 0.85rem; color: var(--text-color-secondary); }

.coverage-summary {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    display: flex;
    align-items: center;
    padding: 0.75rem 1.5rem;
    gap: 0;
    flex-wrap: wrap;
}
.coverage-summary-item { display: flex; flex-direction: column; align-items: center; padding: 0 1.5rem; flex: 1; min-width: 80px; }
.coverage-summary-divider { width: 1px; height: 30px; background: var(--surface-border); flex-shrink: 0; }
.coverage-summary-value { font-size: 1.5rem; font-weight: 700; color: var(--text-color); }
.coverage-summary-label { font-size: 0.72rem; color: var(--text-color-secondary); text-transform: uppercase; letter-spacing: 0.04em; }

.gap-warn { color: var(--red-500, #ef4444); font-weight: 700; }
.gap-ok { color: var(--green-600, #16a34a); font-weight: 600; }

.coverage-bar-wrap { display: flex; align-items: center; gap: 0.5rem; }
.coverage-bar { flex: 1; height: 8px; background: var(--surface-border); border-radius: 4px; overflow: hidden; }
.coverage-bar-fill { height: 100%; border-radius: 4px; transition: width 0.3s ease; }
.coverage-pct { font-size: 0.78rem; color: var(--text-color-secondary); width: 35px; text-align: right; }

:deep(.coverage-row-short) td { background: rgba(239, 68, 68, 0.05) !important; }

.suggest-loading { display: flex; gap: 0.5rem; align-items: center; color: var(--text-color-secondary); }
.suggest-list { display: flex; flex-direction: column; gap: 0.75rem; }
.suggest-item {
    background: var(--surface-ground);
    border-radius: 8px;
    padding: 0.75rem 1rem;
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
}
.suggest-info { display: flex; align-items: center; gap: 0.4rem; flex: 1; flex-wrap: wrap; }
.suggest-name { font-weight: 600; font-size: 0.9rem; }
.suggest-meta { font-size: 0.78rem; color: var(--text-color-secondary); display: flex; gap: 0.75rem; width: 100%; }
</style>
