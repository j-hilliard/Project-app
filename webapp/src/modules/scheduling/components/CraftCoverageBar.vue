<template>
    <div class="craft-bar-row" :class="[severityClass, { selected }]"
        tabindex="0"
        @click="$emit('click')"
        @dblclick="$emit('dblclick')"
        @keydown.enter="$emit('dblclick')">

        <!-- Identity -->
        <div class="craft-identity">
            <span class="craft-code">{{ displayCode }}</span>
            <span class="craft-name">{{ displayName }}</span>
        </div>

        <!-- Big visual bar -->
        <div class="bar-section">
            <div class="bar-track" :class="{ 'track-shortage': hasDemand && pct === 0, 'track-no-demand': !hasDemand }">
                <div class="bar-fill" :style="fillStyle" />
                <div v-if="!hasDemand" class="bar-label-inside bar-label-dim">No Active Demand</div>
                <div v-else-if="pct < 100" class="bar-label-inside">{{ pct }}%</div>
            </div>
        </div>

        <!-- Numeric stats -->
        <div class="craft-stats">
            <div class="stat-item">
                <span class="stat-num">{{ demand }}</span>
                <span class="stat-lbl">DEMAND</span>
            </div>
            <div class="stat-sep" />
            <div class="stat-item">
                <span class="stat-num">{{ assigned }}</span>
                <span class="stat-lbl">ASSIGNED</span>
            </div>
            <div class="stat-sep" />
            <div class="stat-item">
                <span class="stat-num" :class="gapClass">{{ gapDisplay }}</span>
                <span class="stat-lbl">GAP</span>
            </div>
            <div class="stat-sep" />
            <div class="stat-item stat-pct">
                <span class="stat-num" :class="pctClass">{{ pct }}%</span>
                <span class="stat-lbl">COVERAGE</span>
            </div>
        </div>

        <!-- Drill hint -->
        <i class="pi pi-angle-right drill-arrow" />
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
    craft: any;
    selected: boolean;
}>();

defineEmits<{ click: []; dblclick: [] }>();

const demand = computed(() =>
    props.craft.demand ?? props.craft.demandTotal ?? props.craft.demandCount ?? 0
);
const assigned = computed(() =>
    props.craft.assigned ?? props.craft.assignedTotal ?? props.craft.assignedCount ?? 0
);
const gap = computed(() => Math.max(0, demand.value - assigned.value));
const hasDemand = computed(() => demand.value > 0);
const pct = computed(() =>
    hasDemand.value ? Math.min(100, Math.round((assigned.value / demand.value) * 100)) : 0
);

const displayName = computed(() =>
    props.craft.craftName ?? props.craft.craftTitle ?? props.craft.craftCode
);
const displayCode = computed(() => {
    if (props.craft.craftCode) return props.craft.craftCode;
    const fallback = (props.craft.craftTitle ?? props.craft.craftName ?? '').substring(0, 5).toUpperCase();
    return fallback || '???';
});

const gapDisplay = computed(() => {
    if (!hasDemand.value) return '—';
    return gap.value > 0 ? `–${gap.value}` : '✓';
});
const gapClass = computed(() => {
    if (!hasDemand.value) return '';
    return gap.value > 0 ? 'num-warn' : 'num-ok';
});
const pctClass = computed(() => {
    if (!hasDemand.value) return 'num-neutral';
    return pct.value >= 80 ? 'num-ok' : pct.value >= 40 ? 'num-mid' : 'num-warn';
});

const fillColor = computed(() => {
    if (!hasDemand.value) return 'var(--surface-border)';
    return pct.value >= 80
        ? 'var(--green-500, #22c55e)'
        : pct.value >= 40
          ? 'var(--yellow-500, #eab308)'
          : 'var(--red-500, #ef4444)';
});

const fillStyle = computed(() => ({
    width: hasDemand.value ? Math.max(pct.value, 0) + '%' : '0%',
    backgroundColor: fillColor.value,
}));

const severityClass = computed(() => {
    if (!hasDemand.value) return 'sev-neutral';
    return pct.value >= 80 ? 'sev-ok' : pct.value >= 40 ? 'sev-mid' : 'sev-warn';
});
</script>

<style scoped>
.craft-bar-row {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 0.85rem 1.25rem;
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    cursor: pointer;
    transition: all 0.15s ease;
    outline: none;
    position: relative;
}
.craft-bar-row:hover {
    border-color: var(--primary-color);
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.12);
    transform: translateY(-1px);
}
.craft-bar-row.selected {
    border-color: var(--primary-color);
    box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.2);
}
.craft-bar-row:focus-visible {
    box-shadow: 0 0 0 2px var(--primary-color);
}

/* Severity tints */
.sev-warn    { border-left: 3px solid var(--red-500, #ef4444); }
.sev-mid     { border-left: 3px solid var(--yellow-500, #eab308); }
.sev-ok      { border-left: 3px solid var(--green-500, #22c55e); }
.sev-neutral { border-left: 3px solid var(--surface-border); }

/* Identity */
.craft-identity {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    min-width: 220px;
    flex-shrink: 0;
}
.craft-code {
    font-family: 'Courier New', monospace;
    font-size: 0.78rem;
    font-weight: 700;
    background: var(--surface-ground);
    border: 1px solid var(--surface-border);
    color: var(--text-color);
    padding: 0.2rem 0.5rem;
    border-radius: 4px;
    letter-spacing: 0.05em;
    white-space: nowrap;
}
.craft-name {
    font-size: 0.88rem;
    font-weight: 500;
    color: var(--text-color);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

/* Bar */
.bar-section {
    flex: 1;
    min-width: 0;
}
.bar-track {
    width: 100%;
    height: 22px;
    background: var(--surface-ground);
    border-radius: 5px;
    overflow: hidden;
    position: relative;
}
.track-shortage {
    background: rgba(239, 68, 68, 0.08);
}
.track-no-demand {
    background: repeating-linear-gradient(
        -45deg,
        transparent,
        transparent 6px,
        rgba(148, 163, 184, 0.06) 6px,
        rgba(148, 163, 184, 0.06) 7px
    );
}
.bar-fill {
    height: 100%;
    border-radius: 5px;
    transition: width 0.4s ease;
    opacity: 0.85;
}
.bar-label-inside {
    position: absolute;
    right: 6px;
    top: 50%;
    transform: translateY(-50%);
    font-size: 0.72rem;
    font-weight: 700;
    color: #fff;
    text-shadow: 0 1px 2px rgba(0, 0, 0, 0.5);
    white-space: nowrap;
}
.bar-label-dim {
    color: var(--text-color-secondary);
    font-weight: 500;
    text-shadow: none;
    font-size: 0.68rem;
    right: 50%;
    transform: translate(50%, -50%);
}

/* Stats */
.craft-stats {
    display: flex;
    align-items: center;
    gap: 0;
    flex-shrink: 0;
}
.stat-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    min-width: 62px;
    padding: 0 0.6rem;
}
.stat-sep {
    width: 1px;
    height: 28px;
    background: var(--surface-border);
    flex-shrink: 0;
}
.stat-num {
    font-size: 1.05rem;
    font-weight: 700;
    color: var(--text-color);
    line-height: 1;
}
.stat-pct .stat-num { font-size: 1rem; }
.stat-lbl {
    font-size: 0.62rem;
    color: var(--text-color-secondary);
    text-transform: uppercase;
    letter-spacing: 0.06em;
    margin-top: 0.2rem;
}
.num-warn    { color: var(--red-500, #ef4444); }
.num-mid     { color: var(--yellow-500, #eab308); }
.num-ok      { color: var(--green-500, #22c55e); }
.num-neutral { color: var(--text-color-secondary); }

/* Drill arrow */
.drill-arrow {
    color: var(--text-color-secondary);
    font-size: 0.85rem;
    flex-shrink: 0;
    opacity: 0;
    transition: opacity 0.15s ease;
}
.craft-bar-row:hover .drill-arrow { opacity: 1; }
</style>
