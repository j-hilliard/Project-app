<template>
    <div class="empty-state">
        <i :class="icon" />
        <p>{{ message }}</p>
    </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{ message: string; icon?: string }>(), {
    icon: 'pi pi-inbox',
});
</script>

<style scoped>
.empty-state {
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
.empty-state i { font-size: 2rem; color: var(--text-color-secondary); }
.empty-state p { margin: 0; color: var(--text-color-secondary); }
</style>
