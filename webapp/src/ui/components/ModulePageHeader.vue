<template>
    <div class="page-header">
        <div>
            <h1>{{ title }}</h1>
            <p v-if="subtitle">{{ subtitle }}</p>
        </div>
        <div class="header-actions">
            <slot />
        </div>
    </div>
</template>

<script setup lang="ts">
defineProps<{ title: string; subtitle?: string }>();
</script>

<style scoped>
.page-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.page-header h1 { margin: 0 0 0.25rem; font-size: 1.5rem; font-weight: 700; color: var(--text-color); }
.page-header p { margin: 0; color: var(--text-color-secondary); font-size: 0.88rem; }
.header-actions { display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
</style>
