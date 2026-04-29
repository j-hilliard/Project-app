<template>
    <div class="detail-card">
        <h3 v-if="title" class="detail-card-title">{{ title }}</h3>
        <slot />
    </div>
</template>

<script setup lang="ts">
defineProps<{ title?: string }>();
</script>

<style scoped>
.detail-card {
    background: var(--surface-card);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    padding: 1.25rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}
.detail-card-title { font-size: 0.88rem; font-weight: 700; color: var(--text-color); margin: 0 0 0.25rem; }
</style>
